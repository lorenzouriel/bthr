import { context, propagation, SpanStatusCode, trace } from "@opentelemetry/api"
import { resourceFromAttributes } from "@opentelemetry/resources"
import { WebTracerProvider, BatchSpanProcessor } from "@opentelemetry/sdk-trace-web"
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-http"
import { MeterProvider, PeriodicExportingMetricReader } from "@opentelemetry/sdk-metrics"
import { OTLPMetricExporter } from "@opentelemetry/exporter-metrics-otlp-http"
import { LoggerProvider, BatchLogRecordProcessor } from "@opentelemetry/sdk-logs"
import { OTLPLogExporter } from "@opentelemetry/exporter-logs-otlp-http"
import { isTelemetryTarget, telemetryRoute } from "./telemetry-policy"

export function initializeTelemetry() {
  if (import.meta.env.VITE_TELEMETRY_ENABLED !== "true") return
  const resource = resourceFromAttributes({
    "service.name": "bthr.Web",
    "service.namespace": "bthr",
    "service.instance.id": import.meta.env.VITE_TELEMETRY_INSTANCE ?? "web-local",
    "service.version": import.meta.env.VITE_SERVICE_VERSION ?? "local",
    "deployment.environment.name": import.meta.env.VITE_TELEMETRY_ENVIRONMENT ?? "Development",
    environment: import.meta.env.VITE_TELEMETRY_ENVIRONMENT ?? "Development",
  })
  const provider = new WebTracerProvider({ resource, spanProcessors: [
    new BatchSpanProcessor(new OTLPTraceExporter({ url: "/telemetry/v1/traces" })),
  ] })
  provider.register()
  const tracer = provider.getTracer("bthr.browser")
  const meters = new MeterProvider({ resource, readers: [new PeriodicExportingMetricReader({
    exporter: new OTLPMetricExporter({ url: "/telemetry/v1/metrics" }), exportIntervalMillis: 15000,
  })] })
  const meter = meters.getMeter("bthr.browser")
  const fetchDuration = meter.createHistogram("browser.fetch.duration", { unit: "s" })
  const requests = meter.createCounter("browser.fetch.requests")
  const errors = meter.createCounter("browser.errors")
  const pageDuration = meter.createHistogram("browser.page.load.duration", { unit: "s" })
  const logProvider = new LoggerProvider({ resource, processors: [
    new BatchLogRecordProcessor({ exporter: new OTLPLogExporter({ url: "/telemetry/v1/logs" }) }),
  ] })
  const logger = logProvider.getLogger("bthr.browser")
  const nativeFetch = window.fetch.bind(window)
  window.fetch = async (input, init) => {
    const rawUrl = input instanceof Request ? input.url : String(input)
    const url = new URL(rawUrl, location.href)
    if (!isTelemetryTarget(url, location.origin)) return nativeFetch(input, init)
    const candidate = (init?.method ?? (input instanceof Request ? input.method : "GET")).toUpperCase()
    const method = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"].includes(candidate) ? candidate : "OTHER"
    const route = telemetryRoute(url.pathname)
    const attributes = { "http.request.method": method, "http.route": route }
    const span = tracer.startSpan(`${method} ${route}`, { attributes })
    const spanContext = trace.setSpan(context.active(), span)
    const headers = new Headers(init?.headers ?? (input instanceof Request ? input.headers : undefined))
    const carrier: Record<string, string> = {}
    propagation.inject(spanContext, carrier)
    Object.entries(carrier).forEach(([key, value]) => headers.set(key, value))
    const start = performance.now()
    let status = 0
    try {
      const response = await nativeFetch(input, { ...init, headers })
      status = response.status
      span.setAttribute("http.response.status_code", status)
      if (status >= 400) {
        span.setStatus({ code: SpanStatusCode.ERROR })
        logger.emit({ severityNumber: 17, severityText: "ERROR", body: "API request failed", context: spanContext,
          attributes: { ...attributes, "http.response.status_code": status } })
      }
      return response
    } catch (error) {
      span.setStatus({ code: SpanStatusCode.ERROR })
      logger.emit({ severityNumber: 17, severityText: "ERROR", body: "API network request failed", context: spanContext, attributes })
      throw error
    } finally {
      const labels = { ...attributes, "http.response.status_code": status }
      requests.add(1, labels)
      fetchDuration.record((performance.now() - start) / 1000, labels)
      span.end()
    }
  }
  const reportError = () => {
    errors.add(1)
    // Error messages/stacks can contain journal content, URLs, or credentials.
    logger.emit({ severityNumber: 17, severityText: "ERROR", body: "Unhandled browser error" })
  }
  window.addEventListener("error", reportError)
  window.addEventListener("unhandledrejection", reportError)
  const recordLoad = () => setTimeout(() => {
    const navigation = performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming | undefined
    if (!navigation || navigation.loadEventEnd <= 0) return
    pageDuration.record(navigation.loadEventEnd / 1000)
    const span = tracer.startSpan("page.load", { startTime: performance.timeOrigin })
    span.end(performance.timeOrigin + navigation.loadEventEnd)
  }, 0)
  if (document.readyState === "complete") recordLoad()
  else window.addEventListener("load", recordLoad, { once: true })
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "hidden") {
      void Promise.allSettled([provider.forceFlush(), meters.forceFlush(), logProvider.forceFlush()])
    }
  })
}

