# Monitoring implementation plan

Status: implemented; live acceptance blocked by Docker engine access, 2026-10-14.

## Implementation validation

- Four dashboard definitions provisioned with environment/instance selectors, metrics, logs and trace searches (explicit infrastructure tracing limitation).
- Isolated Compose application/monitor stack, bounded same-origin browser ingestion, shared resource identity, API DB readiness, Docker log collection and retention configuration added.
- API: 402 tests passed; 1 real-PostgreSQL test skipped. Web: 7 tests passed; production build passed. Real Edge browser telemetry test passed (intercepted exports, propagation and privacy assertions).
- Compose structure and dashboard JSON validation passed. npm audit: zero vulnerabilities after updating new telemetry dependencies to patched pinned versions. NuGet vulnerability metadata unavailable in this environment.
- Database migrations unchanged. Existing user changes/deletions preserved.
- Docker returns permission denied on both local engine pipes, including escalated execution. Services have NOT been started. Backend binary configuration validation, image compatibility, live metric names, full ingestion/correlation, dashboard navigation and persistence remain pending. Use the raw Docker Compose commands and live browser test described in README.md when Docker access is restored.

The original plan below is retained as the acceptance checklist; completed implementation is not a claim that live acceptance has passed.

## Scope and assumptions

Start locally with Docker Compose. Use one provisioned Grafana dashboard per application component: API, PostgreSQL, web, and monitoring infrastructure. Each dashboard supports environment and instance selection, with links that preserve those selections. This interprets “instance” as a selectable runtime within a component; if separate dashboard URLs for every replica are required, generate those from the same definitions rather than maintaining copies manually.

Keep all existing application tables and migrations unchanged. Preserve the user's current working-tree changes and deletions.

## Revalidation completed

- API: 401 tests passed; the PostgreSQL integration test was explicitly skipped because no test database was started during planning.
- Web: all five tests and the TypeScript/Vite production build passed.
- Monitoring Compose passes structural validation. That does not validate each backend's configuration or establish that telemetry flows.
- Both provisioned dashboard JSON files parse. API has logs/metrics and a text-only trace placeholder; PostgreSQL has metrics only. Neither has dashboard variables.
- No browser telemetry instrumentation or web hosting container is currently defined.
- API already emits Serilog logs and OpenTelemetry HTTP, Npgsql, and runtime telemetry.
- NuGet vulnerability metadata remains unavailable from this environment; the passing build is not a dependency security audit.

## Gaps to resolve

1. Collector health check calls port 13133, but its health-check extension is not enabled. Verify probe executables exist in the selected container images as well.
2. API, database, and monitor Compose projects use separate networks. Define intentional connectivity rather than assuming service-name resolution between them.
3. PostgreSQL exporter connection settings and Grafana credentials are hard-coded. Move these into documented environment configuration; use a monitoring database role with the necessary read privileges.
4. API logs/traces/metrics lack a common, explicit environment/instance/version identity. Existing dashboards aggregate by scrape job and cannot reliably separate replicas.
5. Datasource correlation assumes a particular log trace-ID format and refers to `app_requests_total`, which is not explicitly emitted in the API. Build queries from observed telemetry.
6. Tempo datasource enables a service map, but there is no configured span-metrics/service-graph generation path. Add one only if the first dashboards need it; ordinary trace search and links do not require a service graph.
7. PostgreSQL and infrastructure logs are not collected. The unused Prometheus receiver in the collector config should be removed or deliberately wired; it currently is not in any pipeline.
8. Existing component images/configuration are pinned to older releases. Select a tested compatible set, pin exact versions, and review persistent-volume compatibility before upgrades. The Loki comment claims TSDB while its configuration actually uses boltdb-shipper/v11.
9. API deployment configuration still needs the correct working directory and telemetry endpoint environment variable. Fix the deployment path used for this stack when integrating it.

## Dashboard contents

Every dashboard has the same sections: status, metrics, logs, traces. “No traffic,” “signal not configured,” and “backend unavailable” must be distinguishable.

| Dashboard | Metrics | Logs | Traces |
| --- | --- | --- | --- |
| API | Availability/readiness, request rate, status/error rates, p50/p95/p99 latency, runtime memory/GC/thread pool | Requests, exceptions, lifecycle events; filter by severity/route | Searchable recent, failed, and slow requests; HTTP and database spans |
| PostgreSQL | Database reachability, exporter health, connections, commits/rollbacks, cache hits, locks/deadlocks, database size | Startup, connection, error and slow-query events where configured | Npgsql client spans targeting the selected PostgreSQL instance; explicitly labeled as application-observed database calls |
| Web | Page/fetch timings, request failures, JavaScript error counts; hosting availability if a web host is added | Sanitized frontend errors and host access/error logs | Page loads and fetch calls, with trace context propagated into the API |
| Monitoring | Ingestion/export success, dropped telemetry, queue pressure, scrape health, backend storage/availability | Collector, Grafana, Loki, Tempo, Prometheus and exporter events | Backend request traces where supported and enabled; otherwise state the instrumentation limit clearly |

PostgreSQL exporter supplies metrics, not distributed traces. Database trace selection must use database target attributes, not the API's `service.instance.id`. Infrastructure components do not necessarily emit all three signals; avoid fake trace panels or assuming exporting a span automatically instruments its exporter.

## Implementation sequence

### 1. Make the stack reproducible

- Add `monitor/.env.example` and a practical `monitor/README.md` with prerequisites, ports, startup, shutdown, retention, troubleshooting, and upgrade steps.
- Pin compatible component versions, configure health/readiness correctly, and set names that avoid collisions with unrelated local containers.
- Define shared network connectivity for API/database/collector while keeping backend ports internal where possible. Bind local development entry points to localhost.
- Use named volumes and bounded retention. Proposed local defaults: metrics 15 days, logs 7 days, traces 24 hours; make these configurable.
- Add a documented log-collection source for Docker services. Prefer one collector path; do not ingest the API's same log through both OTLP and stdout. Limit access to the required logs and account for Docker Desktop versus Linux host differences.
- If adopting current Loki native OTLP ingestion, migrate configuration to the required schema and structured metadata settings; do not apply incompatible settings to an old volume or erase it.

### 2. Establish telemetry identity and correlation

- Configure matching `service.name`, `service.namespace`, `service.instance.id`, `service.version`, and `deployment.environment.name` on API logs, traces, and metrics.
- Normalize labels consistently for Prometheus/Loki; keep high-cardinality attributes such as trace IDs out of indexed metric labels. Keep instance identity queryable without unnecessarily multiplying Loki streams.
- Configure collector batching, memory limits, retry/queue behavior, and its own telemetry.
- Verify trace/span IDs survive ingestion; provision log-to-trace and trace-to-log links and relevant metric links using the actual emitted attributes.
- Avoid request/response bodies, credentials, journal content, and SQL parameter values in collected telemetry. Use route templates rather than user-specific URL paths as metric labels.
- Ensure request logging includes rejected authentication/authorization requests as well as controller requests. Add dependency-aware readiness for the API.

### 3. Complete the API and PostgreSQL dashboards first

- Replace the API text trace placeholder with supported Tempo trace search/table panels and trace drill-down.
- Parameterize both existing dashboards by environment and instance; parameterize database name too.
- Add database logs and Npgsql trace panels with clear source attribution.
- Confirm every metric exists in a live scrape. Do not silently keep nonexistent metric names or label sets.
- Configure database log/exporter settings without changing application tables or exposing sensitive query values.

### 4. Add web and monitoring dashboards

- Instrument browser page/fetch timings, sanitized errors, and trace propagation to the API. Define “web instance” as the deployed frontend/release, not each user's browser session.
- Route browser telemetry through a same-origin, bounded ingestion endpoint; do not expose backend administration or collector internals directly to browsers.
- Choose and document a production web host/proxy if host logs and availability are in scope; Vite development hosting alone is not a deployment architecture.
- Collect backend self-metrics/logs and enable backend tracing only where supported by the pinned versions.
- Keep resource collection overhead and log duplication under review before adding further agents.

### 5. Start and validate

Only after configuration and instrumentation are implemented:

1. Validate Compose, backend configurations, provisioning, and dashboard JSON; run relevant API/web tests.
2. Start monitoring backends and collector; verify readiness and Grafana datasource health.
3. Connect/start the intended database and API using explicit environment settings; start the web integration when ready.
4. Generate identifiable test traffic: one successful request, a controlled error, a database-backed request, and a frontend fetch/error.
5. Verify each dashboard queries real data for the selected environment/instance; check that changing selectors excludes other instances.
6. Follow a frontend request through API/database spans, then from a trace to its associated logs and back.
7. Check telemetry drops, duplicate logs, resource use, retention, and persistence after a non-destructive restart.
8. Document actual dashboard URLs and any remaining signal limitations.

## Acceptance criteria

- Four provisioned component dashboards with consistent metrics/logs/traces sections and working instance/environment filters.
- API and web traces correlate across requests; database spans can be filtered by the target database instance.
- Database and infrastructure logs reach Loki; their metrics are visible in Prometheus.
- Trace panels show real instrumented traces or an explicit, justified unsupported-signal state.
- Provisioned links work; there are no generic job-only filters accidentally mixing instances, fictional metric names, or empty placeholder panels presented as implemented monitoring.
- A documented local startup works, persistence survives restart, and application migrations remain unchanged.

## References

- [Loki native OpenTelemetry ingestion](https://grafana.com/docs/loki/latest/send-data/otel/): OTLP HTTP, structured metadata, resource mapping, and label cardinality.
- [Grafana trace-to-logs correlation](https://grafana.com/docs/grafana/latest/datasources/tempo/configure-tempo-data-source/configure-trace-to-logs/): provisioned cross-signal navigation.
- [Tempo metric generation options](https://grafana.com/docs/tempo/latest/metrics-from-traces/where-to-generate-metrics/): choose one generation path if service graphs/span metrics are added.


## Planned extension: Python agent and AI usage (2026-10-14)

Status: agent dashboard, Compose overlay and alert definitions added; live validation pending. This extends the previously implemented four-dashboard scope. See [agent plan](../agent/PLAN.md) for the detailed model adapter, telemetry and cost-accounting contract.

Add a fifth component dashboard at `config/grafana/provisioning/dashboards/json/bthr-agent-observability.json`, selectable by environment, instance, provider and model. Include metrics, correlated sanitized logs, workflow/model/tool traces, input/output/cache tokens, estimated cost per provider/model, trends, error/latency comparisons and usage/pricing coverage.

Receive Python agent OTLP through the existing collector and reuse Prometheus, Loki, Tempo and Grafana. Propagate trace context into the API. Exclude duplicate Docker log collection when the agent emits OTLP logs. Keep application schema unchanged for monitoring.

Support Claude/Anthropic, GPT/OpenAI and configured local models with normalized provider usage. Use versioned exact-model pricing configuration, non-overlapping billable categories and one accounting event per provider attempt. Count retries/fallbacks; show missing usage or pricing as unknown. Local API charges and estimated infrastructure costs are separate. Generate metrics independently of trace sampling; estimates are not invoices and can undercount during telemetry loss.

Acceptance: actual exported signals and dashboard links work; provider/model/instance filters isolate results; fixture costs reconcile without cache/streaming double counting; unknown rates do not display as zero; completed telemetry persists within configured retention; local-only mode has no cloud fallback; exported telemetry contains no prompts, images, credentials or personal content. Credentialed provider smoke tests are reported individually. Preserve the existing four dashboards and their acceptance scope.
