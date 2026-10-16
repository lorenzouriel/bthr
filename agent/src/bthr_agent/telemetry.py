import logging
from decimal import Decimal
from .costs import categories, estimate_cost
from opentelemetry import metrics, trace
from opentelemetry.metrics import Observation
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.sdk.metrics import MeterProvider
from opentelemetry.sdk.metrics.export import PeriodicExportingMetricReader
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.exporter.otlp.proto.grpc.metric_exporter import OTLPMetricExporter
from opentelemetry.exporter.otlp.proto.grpc._log_exporter import OTLPLogExporter

tracer = trace.get_tracer("bthr.agent")
log = logging.getLogger("bthr.agent")


def configure(settings):
    resource = Resource.create({"service.name": "bthr.Agent", "service.instance.id": settings.agent_instance,
                                "service.version": settings.service_version, "environment": settings.environment,
                                "deployment.environment.name": settings.environment})
    endpoint = settings.otel_exporter_otlp_endpoint
    tp = TracerProvider(resource=resource)
    tp.add_span_processor(BatchSpanProcessor(OTLPSpanExporter(endpoint=endpoint)))
    trace.set_tracer_provider(tp)
    mp = MeterProvider(resource=resource, metric_readers=[PeriodicExportingMetricReader(
        OTLPMetricExporter(endpoint=endpoint), export_interval_millis=10000)])
    metrics.set_meter_provider(mp)
    lp = LoggerProvider(resource=resource)
    lp.add_log_record_processor(BatchLogRecordProcessor(OTLPLogExporter(endpoint=endpoint)))
    log.addHandler(LoggingHandler(logger_provider=lp))
    log.setLevel(logging.INFO)
    # Libraries must never log request URLs, prompts or provider exceptions.
    for name in ("httpx", "httpcore", "aiogram", "openai", "anthropic"):
        logging.getLogger(name).setLevel(logging.CRITICAL)
    return tp, mp, lp


class Accounting:
    def __init__(self):
        meter = metrics.get_meter("bthr.agent")
        self.queue_size = lambda: 0
        meter.create_observable_gauge("bthr.agent.alive", callbacks=[lambda _: [Observation(1)]])
        meter.create_observable_gauge("bthr.agent.queue.depth", callbacks=[lambda _: [Observation(self.queue_size())]])
        self.calls = meter.create_counter("bthr.ai.calls")
        self.tokens = meter.create_counter("bthr.ai.tokens")
        self.cost = meter.create_counter("bthr.ai.cost.usd")
        self.unknown = meter.create_counter("bthr.ai.unknown")
        self.duration = meter.create_histogram("bthr.ai.duration", unit="s")
        self.operations = meter.create_counter("bthr.agent.operations")

    def record(self, profile, task, usage, rate, elapsed, outcome):
        labels = {"provider": profile.provider, "model": profile.model, "task": task,
                  "deployment": profile.deployment, "outcome": outcome}
        values = categories(usage)
        cost = estimate_cost(values, rate)
        if profile.deployment == "local" and values is not None:
            cost = Decimal(0)  # Direct provider charge only, never infrastructure cost.
        self.calls.add(1, labels)
        self.duration.record(elapsed, labels)
        if values is not None:
            for kind, amount in values.items():
                self.tokens.add(amount, {**labels, "token_type": kind})
        else:
            self.unknown.add(1, {**labels, "reason": "usage"})
        if cost is None:
            self.unknown.add(1, {**labels, "reason": "pricing"})
        else:
            self.cost.add(float(cost), labels)
            trace.get_current_span().set_attribute("bthr.ai.cost.usd", float(cost))
        for kind, amount in (values or {}).items():
            trace.get_current_span().set_attribute("bthr.ai.tokens." + kind, amount)
        trace.get_current_span().set_attribute("bthr.ai.cost.known", cost is not None)
        fields = {**labels, "usage_known": values is not None, "cost_known": cost is not None,
                  "duration_seconds": elapsed}
        fields.update({"tokens_" + key: amount for key, amount in (values or {}).items()})
        if cost is not None:
            fields["estimated_cost_usd"] = float(cost)
        if rate:
            fields["pricing_version"] = str(rate.get("version", "unknown"))
        log.info("Model attempt completed", extra=fields)
