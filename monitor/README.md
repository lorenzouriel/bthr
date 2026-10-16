# Local monitoring

Four provisioned dashboards: API, PostgreSQL, Web, and Monitoring. Each has environment/instance filters, metrics, logs, and traces. Web instance means a deployment, not a browser session. PostgreSQL traces are Npgsql client spans selected by database target identity. Infrastructure request tracing is explicitly unavailable; its operational metrics and logs are collected.

## Start

Requires Docker Desktop with an accessible Linux engine, Docker Compose v2.23.1+ (inline configs), and free localhost ports 3000, 8080, 4317.

Create `monitor/.env` from `.env.example` in your editor and replace the four credential placeholders. Use distinct passwords containing letters and numbers (the exporter password is embedded in a connection URL), and a JWT secret of at least 32 characters. Keep an existing `.env` if already configured. To generate a random value with Docker, repeat this command for each credential:

```sh
docker run --rm alpine:3.22.2 sh -c "head -c 32 /dev/urandom | od -An -tx1 | tr -d ' \n'"
```

Run from `monitor/`:

```sh
docker compose -f docker-compose.yml -f compose.local.yml config --quiet
docker compose -f docker-compose.yml -f compose.local.yml up -d --build
docker compose -f docker-compose.yml -f compose.local.yml ps
```

Compose interpolates Prometheus target identities directly from `.env`; no generated config or wrapper scripts are required. This creates a NEW isolated PostgreSQL database and volumes under project `bthr-monitor`; it does not connect to or upgrade your existing database or old monitoring volumes. Existing application migrations are mounted read-only and applied by Flyway. The local API uses its isolated database owner for migration/development simplicity; the exporter uses a separate `pg_monitor` role. Production deployments should use a restricted application role.

After startup, sign in to Grafana as `admin` with `GRAFANA_ADMIN_PASSWORD` from `.env`. Application accounts are separate; register through the web UI.

| Entry point | Local address |
| --- | --- |
| Application | http://localhost:8080 |
| API dashboard | http://localhost:3000/d/bthr-api-observability |
| PostgreSQL dashboard | http://localhost:3000/d/postgresql-observability |
| Web dashboard | http://localhost:3000/d/bthr-web-observability |
| Monitoring dashboard | http://localhost:3000/d/bthr-monitoring-observability |
| Local API OTLP gRPC ingest | localhost:4317 |

These are configured addresses, not evidence services are running. Backend ports stay on the Compose network. The API is reached through `/api/` on the web host. Its `/health/live` endpoint is process liveness; `/health/ready` checks the database. Blackbox probes inspect readiness without assuming shell utilities exist in backend images.

## Configuration and data flow

Edit `.env` using `.env.example` as reference, then rerun the Compose `up -d --build` command. Identity values accept letters, numbers, dots, underscores and hyphens. API logs/traces/metrics share resource attributes: service name, namespace, version, instance, environment. Prometheus/Loki normalize the shared query labels to `service_name`, `service_instance_id`, and `environment`. Loki keeps application instance identity and trace IDs in structured metadata. Dashboard navigation retains environment/time and resets instance selection because component instance IDs differ.

API -> OTLP collector -> Tempo / Loki / Prometheus scrape endpoint. Browser -> same-origin Nginx `/telemetry/v1/{traces,metrics,logs}` -> dedicated collector receiver, which forces frontend resource identity. Nginx allows POST only, caps bodies at 256 KiB, limits requests per source address, strips cookies/authorization, and accepts the documented local origins. Change that origin allowlist when deploying elsewhere; use HTTPS and secure cookies outside this local setup. The ingestion endpoint is for untrusted browser telemetry, not authenticated business data.

Alloy tails this Compose project's Docker logs, excluding API (already sent over OTLP) and migration output. Docker socket access is privileged even with a read-only bind mount; this local setup assumes a trusted machine. Do not expose the socket or this unauthenticated backend network publicly. Use a constrained host log agent/socket proxy for a shared deployment.

PostgreSQL logs startup, connections and errors. Statement/parameter logging is disabled, and Alloy redacts database error/detail payloads; slow-query statements are deliberately not logged. Npgsql duration traces provide application-observed slow database calls. Browser errors have generic messages, no original error text, stack, bodies, query strings, email, or user IDs. HTTP metric labels and browser span names use route templates. Npgsql SQL text and raw URL attributes are removed before export. No application tables or migrations are added for monitoring.

## Validate

Run configuration checks from `monitor/` before startup:

```sh
docker compose -f docker-compose.yml -f compose.local.yml config --quiet
docker compose -f docker-compose.yml -f compose.local.yml run --rm --no-deps otel-collector validate --config=/etc/otelcol/config.yaml
docker compose -f docker-compose.yml -f compose.local.yml run --rm --no-deps --entrypoint promtool prometheus check config /etc/prometheus/prometheus.yml
docker compose -f docker-compose.yml -f compose.local.yml run --rm --no-deps loki -config.file=/etc/loki/config.yaml -config.expand-env=true -verify-config=true
docker compose -f docker-compose.yml -f compose.local.yml run --rm --no-deps alloy validate /etc/alloy/config.alloy
```

After startup, inspect readiness and actual metrics:

```sh
docker compose -f docker-compose.yml -f compose.local.yml exec api curl -fsS http://localhost:8080/health/ready
docker compose -f docker-compose.yml -f compose.local.yml exec web wget -qO- http://localhost/health
docker compose -f docker-compose.yml -f compose.local.yml exec prometheus promtool query instant http://localhost:9090 'probe_success'
docker compose -f docker-compose.yml -f compose.local.yml exec prometheus promtool query instant http://localhost:9090 'pg_up'
docker compose -f docker-compose.yml -f compose.local.yml exec prometheus promtool query instant http://localhost:9090 'http_server_request_duration_seconds_count'
docker compose -f docker-compose.yml -f compose.local.yml logs --tail=100 otel-collector tempo loki alloy postgres-exporter
```

From `web/`, run `npm test`, `npm run build`, and `npm run test:telemetry`. To test the running stack instead of intercepted exports, set `MONITOR_WEB_URL=http://localhost:8080` in your shell environment before running `npm run test:telemetry`.

The browser test uses installed Microsoft Edge, starts a telemetry-enabled Vite server by default, intercepts ingestion, and verifies all three exported signals, W3C trace propagation, and absence of sensitive canary strings. With `MONITOR_WEB_URL`, it sends telemetry to the real stack instead. Mock-mode success does not prove backend ingestion.

In Grafana, check all three datasource connections and the four provisioned dashboards. Generate traffic through the application, including a rejected login and a database-backed request, and allow at least 30 seconds for exports and scrapes. Open a browser fetch trace, inspect API and database child spans, follow trace-to-logs, then the log TraceID link back. Confirm PostgreSQL and infrastructure logs appear, and that selecting a different environment/instance excludes unrelated data.

Inspect each panel with fresh traffic: empty counters before the first error/GC are not evidence of a failed pipeline. A zero readiness value means unavailable; missing metric series mean absent telemetry/scrape failure. The Monitoring dashboard shows failed exports, refused telemetry, and queue pressure.

For a non-destructive persistence check, note a trace ID and a log timestamp, then run:

```sh
docker compose -f docker-compose.yml -f compose.local.yml restart prometheus loki tempo grafana
```

Wait for readiness, then confirm the same trace, logs and historical metrics remain queryable. These manual checks replace the removed automated verification wrapper.

The real PostgreSQL integration test remains opt-in with `BTHR_TEST_POSTGRES`; it creates and drops its own temporary database. Do not use a server without CREATE DATABASE permission for that test.

## Operations

```sh
cd monitor
docker compose -f docker-compose.yml -f compose.local.yml ps
docker compose -f docker-compose.yml -f compose.local.yml logs --tail=100 otel-collector tempo loki alloy
docker compose -f docker-compose.yml -f compose.local.yml down
```

`down` keeps named volumes. Do not add `--volumes` unless intentionally deleting this local stack's data. Default retention: Prometheus 15 days, Loki 7 days, Tempo 24 hours; `.env` exposes each setting. Collector queues are bounded in-memory queues with retry, not durable storage; process failure can lose pending exports. Alloy positions, database files and backend stores use named volumes.

If Docker reports permission denied, restore access to Docker Desktop's Linux engine and retry startup. If login/exporter authentication fails after changing `.env`, remember existing database roles and Grafana admin passwords are persisted: changing environment values does not rotate stored passwords. Rotate explicitly; do not erase volumes to resolve it. If the web host cannot resolve API/collector during startup, inspect dependency failures and restart web after they are healthy.

Versions are pinned in Compose, with a new `*-v2` volume set so the TSDB/v13 Loki configuration cannot open an old boltdb/v11 store. Before upgrades, back up named volumes, read each backend's release notes and migration guidance, validate configuration with the new binaries, then repeat ingestion/correlation/persistence checks. Never attach incompatible Loki schema settings to an existing volume.

## Validation status

Implementation is present. Structural/build/unit/browser test results are recorded in `PLAN.md`. Live backend configuration validation, image compatibility, ingestion, dashboard links and persistence require Docker access and must not be treated as passed until run. No backend request tracing or service graph is configured.

References: [Loki OTLP ingestion](https://grafana.com/docs/loki/latest/send-data/otel/), [Tempo trace-to-logs](https://grafana.com/docs/grafana/latest/datasources/tempo/configure-tempo-data-source/configure-trace-to-logs/), [.NET runtime instruments](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-runtime).

## Telegram agent overlay

The optional [agent](../agent/README.md) adds a fifth dashboard at `/d/bthr-agent-observability` with logs, traces, model tokens, estimated API costs, and unknown-usage/pricing coverage. It reuses the collector and existing storage backends. Local hardware costs are excluded. Pricing is operator-configured; empty prices remain unknown.

After configuring the agent variables in `.env`, from this directory:

```sh
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml config --quiet
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml up -d --build
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml logs --tail=100 agent
```

The agent binds loopback port 8090; configure HTTPS ingress and register the webhook explicitly as documented in the agent README. This overlay has not been live-validated while Docker engine access is denied and Python dependency installation is blocked. Alert rules are provisioned in Prometheus, without automatic external notifications.
