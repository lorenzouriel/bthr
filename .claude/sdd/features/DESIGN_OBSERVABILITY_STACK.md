# DESIGN: Observability Stack

> Technical design for retargeting the existing `monitor/` OTel stack from a nonexistent demo app to FinPulse.Api and PostgreSQL

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | OBSERVABILITY_STACK |
| **Date** | 2026-08-25 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_OBSERVABILITY_STACK.md](./DEFINE_OBSERVABILITY_STACK.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌───────────────────────────────────────────────────────────────────────────────┐
│  api/ (own compose project OR `dotnet run` on host)                          │
│                                                                               │
│   FinPulse.Api                                                              │
│     ├─ Serilog: console sink (unchanged) + OTLP sink (NEW, gRPC, 4317)      │
│     ├─ OpenTelemetry SDK: TracerProvider                                    │
│     │    AddAspNetCoreInstrumentation + AddHttpClientInstrumentation        │
│     │    + AddNpgsql (Npgsql.OpenTelemetry) + OTLP exporter (gRPC, 4317)    │
│     └─ OpenTelemetry SDK: MeterProvider                                     │
│          AddAspNetCoreInstrumentation + AddRuntimeInstrumentation           │
│          + OTLP exporter (gRPC, 4317)                                       │
│                                                                               │
│   All three (logs/traces/metrics) tagged with resource service.name=        │
│   "FinPulse.Api" — required for Grafana's pre-wired trace<->log correlation │
└───────────────────────────────────────────────────────────────────────────────┘
                    │ OTLP gRPC :4317
                    │ (host.docker.internal:4317 if api/ is containerized,
                    │  localhost:4317 if running via `dotnet run` on host)
                    ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│  monitor/ (own compose project — UNCHANGED pipeline wiring)                  │
│                                                                               │
│   otel-collector (otlp receiver :4317/:4318)                                │
│        ├─ logs    → loki exporter ────────────────┐                         │
│        ├─ traces  → otlp/trace exporter (tempo) ───┼──┐                     │
│        └─ metrics → prometheus exporter (:9464) ───┼──┼──┐                  │
│                                                     ▼  ▼  ▼                  │
│                                                  [loki][tempo][:9464]        │
│                                                                               │
│   postgres-exporter (NEW)                                                   │
│     DATA_SOURCE_NAME=postgresql://postgres:***@host.docker.internal:5432/   │
│       fin_pulse?sslmode=disable                                             │
│     exposes :9187/metrics ───────────────────────────────────┐              │
│                                                                ▼              │
│   prometheus  ── scrapes: otel-collector:9464, postgres-exporter:9187,      │
│                            loki:3100 (unchanged)                            │
│        │                                                                     │
│        ▼                                                                     │
│   grafana ── datasources: Prometheus + Loki + Tempo (already provisioned,   │
│               trace<->log<->metric correlation already wired)               │
│             ── dashboards (NEW): finpulse-api-observability.json,           │
│                                   postgresql-observability.json             │
│             ── dashboards (REMOVED): greenhouse-app-observability.json      │
└───────────────────────────────────────────────────────────────────────────────┘
                    ▲ scrapes :5432
                    │ (host.docker.internal:5432)
┌───────────────────────────────────────────────────────────────────────────────┐
│  database/ (own compose project — UNCHANGED, already shipped)                │
│   postgres:17-alpine, published on host port 5432                           │
└───────────────────────────────────────────────────────────────────────────────┘
```

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| OTel SDK in `FinPulse.Api` | Emits traces + metrics via OTLP; ASP.NET Core, HttpClient, Npgsql auto-instrumentation | `OpenTelemetry` 1.17.0, `OpenTelemetry.Extensions.Hosting` 1.16.0, `OpenTelemetry.Instrumentation.AspNetCore` 1.16.0, `OpenTelemetry.Instrumentation.Http` 1.17.0, `OpenTelemetry.Instrumentation.Runtime` 1.16.0, `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.17.0, `Npgsql.OpenTelemetry` 10.0.3 (exact match to the already-pinned `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3) |
| Serilog OTLP sink | Exports existing Serilog logs to the collector alongside console output | `Serilog.Sinks.OpenTelemetry` 4.2.0 |
| `postgres-exporter` | Scrapes PostgreSQL system views, exposes Prometheus-format metrics | `quay.io/prometheuscommunity/postgres-exporter:v0.19.1` |
| OTel Collector, Prometheus, Loki, Tempo, Grafana | Unchanged pipeline infrastructure | Existing `monitor/` versions (no bumps in this feature) |
| 2 new Grafana dashboards | FinPulse API observability; PostgreSQL health | Hand-authored JSON, provisioned via existing `dashboards.yml` provider |

---

## Key Decisions

### Decision 1: OTLP protocol standardized on gRPC (port 4317) for all three telemetry types

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** The OTel Collector's `otlp` receiver accepts both gRPC (4317) and HTTP/protobuf (4318). `Serilog.Sinks.OpenTelemetry`, the OTel .NET SDK's OTLP exporter, and Npgsql's tracing all support either protocol independently — an inconsistent mix would need per-signal endpoint/protocol configuration.

**Choice:** Configure traces, metrics, and logs to all export via OTLP gRPC to port 4317.

**Rationale:** `monitor/docs/otel-config.md` (already written, pre-dating this feature) explicitly documents gRPC as "Default, efficient, interoperable with most SDKs (preferred)." One protocol, one port, one endpoint value to configure per environment — simpler than tracking two different ports/protocols per signal type.

**Alternatives Rejected:**
1. HTTP/protobuf (4318) for logs, gRPC for traces/metrics — rejected: no technical requirement forces this split; it only adds configuration surface.

**Consequences:**
- A single `OTEL_EXPORTER_OTLP_ENDPOINT` value configures all three signal types.
- If gRPC connectivity is ever blocked (e.g. a restrictive proxy), all three signals fail together rather than degrading independently — an accepted trade-off for local-dev simplicity, consistent with this feature's local-dev-only scope.

---

### Decision 2: Environment-aware OTLP endpoint — `localhost` vs `host.docker.internal`

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** `host.docker.internal` only resolves from **inside** a Docker container back to the host — it does not resolve when `FinPulse.Api` runs directly on the host via `dotnet run` (the exact way it was run during the prior migration's live verification). The database connection string already solves this identical problem with two separate values: `appsettings.json` uses `localhost` (for `dotnet run`), while `.env`/`docker-compose.yml` use a container-reachable host.

**Choice:** Apply the same dual-value pattern to `OTEL_EXPORTER_OTLP_ENDPOINT`: `appsettings.json` gets `http://localhost:4317` (local `dotnet run` path); `api/.env.example` and `api/docker-compose.yml` get `http://host.docker.internal:4317` (containerized path).

**Rationale:** This is not a new pattern — it is the exact mechanism already proven correct for `ConnectionStrings:DefaultConnection` across the prior PostgreSQL API migration. Missing this distinction would silently break telemetry export whenever a developer runs the API directly via `dotnet run` (the most common local-dev path, and the one used for this project's own live verification), producing no errors — just an empty Grafana dashboard, which is exactly the kind of silent, hard-to-diagnose failure the DEFINE phase's Assumption A-004 flagged as a risk.

**Alternatives Rejected:**
1. Only support the containerized path (`host.docker.internal` everywhere) — rejected: the most common verified local-dev workflow (`dotnet run`) would silently produce zero telemetry.

**Consequences:**
- Two config files carry two different (correct) values for the same logical setting — matches the existing, already-understood convention for the DB connection string, so no new mental model for developers.

---

### Decision 3: `postgres-exporter` uses default collectors only — no custom queries file

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** DEFINE's success criteria require table/index size panels on the PostgreSQL dashboard. Table/index sizes are sometimes only available in `postgres_exporter` via a custom `queries.yaml` (`PG_EXPORTER_EXTEND_QUERY_PATH`) in older exporter versions.

**Choice:** Use `postgres_exporter` v0.19.1's default `stat-user-tables` collector, which exposes `pg_stat_user_tables_table_size_bytes` and `pg_stat_user_tables_index_size_bytes` out of the box — verified live against the exporter's current collector reference, not assumed from older documentation.

**Rationale:** Avoids an entirely unnecessary custom-queries YAML file and its own maintenance burden; the default collector set in this exporter version already covers every metric DEFINE's success criteria require (connections via `pg_stat_database_numbackends`, transaction rate via `pg_stat_database_xact_commit`/`xact_rollback`, cache hit ratio via `pg_stat_database_blks_hit`/`blks_read`, table/index sizes via `stat-user-tables`).

**Alternatives Rejected:**
1. Author a custom `queries.yaml` — rejected as unnecessary once the default collector set was verified to already cover the requirement.

**Consequences:**
- One fewer file in the manifest; `postgres-exporter`'s compose service needs no extra volume mount for a queries file.

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `monitor/docker-compose.yml` | Modify | Remove `greenhouse-app` service; add `postgres-exporter` service (Decision 3) | (general) | None |
| 2 | `monitor/config/prometheus.yml` | Modify | Remove `greenhouse-app` scrape job; add `postgres-exporter` scrape job (`:9187`) | (general) | 1 |
| 3 | `monitor/config/grafana/provisioning/dashboards/json/greenhouse-app-observability.json` | Delete | Remove demo-app dashboard | (general) | None |
| 4 | `monitor/config/grafana/provisioning/dashboards/json/finpulse-api-observability.json` | Create | New dashboard: request rate/latency/errors, trace exemplars, log panel, .NET runtime metrics | (general) | None |
| 5 | `monitor/config/grafana/provisioning/dashboards/json/postgresql-observability.json` | Create | New dashboard: connections, transaction rate, cache hit ratio, table/index sizes | (general) | 1, 2 |
| 6 | `monitor/docs/prometheus.md` | Modify | Replace `greenhouse-app` scrape-job docs with `postgres-exporter` docs | (general) | 2 |
| 7 | `api/FinPulse.Api/FinPulse.Api.csproj` | Modify | Add 8 OTel/Serilog packages (see Pattern 4) | (general) | None |
| 8 | `api/FinPulse.Api/Program.cs` | Modify | Wire `TracerProvider`/`MeterProvider`, add Serilog OTLP sink, shared `service.name` (Decision 1) | (general) | 7 |
| 9 | `api/FinPulse.Api/appsettings.json` | Modify | Add `Otel:ExporterEndpoint` = `http://localhost:4317` (Decision 2) | (general) | 8 |
| 10 | `api/.env.example` | Modify | Add `OTEL_EXPORTER_OTLP_ENDPOINT=http://host.docker.internal:4317` (Decision 2) | (general) | None |
| 11 | `api/docker-compose.yml` | Modify | Add `OTEL_EXPORTER_OTLP_ENDPOINT` env var to the `finapi` service | (general) | 10 |

**Total Files:** 11 (9 modified, 1 created ×2 = 2 created, 1 deleted)

---

## Agent Assignment Rationale

> Agents discovered from `.claude/agents/` — Build phase invokes matched specialists.

| Agent | Files Assigned | Why This Agent |
|-------|-----------------|-----------------|
| (general) | 1–11 | No agent in `.claude/agents/` covers .NET/OpenTelemetry instrumentation, Grafana dashboard JSON authoring, or Prometheus exporter configuration — the closest candidates (`fabric-logging-specialist`) are Microsoft Fabric/KQL-specific, an unrelated observability stack. Per the Design Confidence Matrix this is a "No KB, no agent match → 0.70 → Research first" case; that research (live package/image version verification, `postgres_exporter` default-collector confirmation) was performed directly in this Design phase (see Decisions 1–3), the same approach used successfully in the prior API migration's Design phase. |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md`
- Matched by: KB domain overlap (none), purpose keywords (none matched: no "OpenTelemetry", "Grafana", or "Prometheus exporter" specialist exists)

---

## Code Patterns

### Pattern 1: `Program.cs` — OpenTelemetry SDK wiring (traces + metrics)

```csharp
const string serviceName = "FinPulse.Api";
var otelEndpoint = builder.Configuration["Otel:ExporterEndpoint"]
    ?? throw new InvalidOperationException("Otel:ExporterEndpoint not configured");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(otelEndpoint);
            otlp.Protocol = OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(otelEndpoint);
            otlp.Protocol = OtlpExportProtocol.Grpc;
        }));
```

### Pattern 2: `Program.cs` — Serilog OTLP sink (added to the existing logger config)

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otelEndpoint;
        options.Protocol = OtlpProtocol.Grpc;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = serviceName
        };
    })
    .CreateLogger();
```

> Note: `otelEndpoint` must be read from configuration **before** `Log.Logger` is constructed (the logger is built before `builder.Configuration` exists in today's `Program.cs`) — Build must reorder the existing bootstrap so `IConfiguration` is available first, or read the endpoint directly from an environment variable at that point instead of `builder.Configuration`.

### Pattern 3: `appsettings.json` — OTel endpoint (local `dotnet run` path)

```json
{
  "Otel": {
    "ExporterEndpoint": "http://localhost:4317"
  }
}
```

### Pattern 4: `FinPulse.Api.csproj` — new package references

```xml
<PackageReference Include="OpenTelemetry" Version="1.17.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.16.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.16.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.17.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.16.0" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.17.0" />
<PackageReference Include="Npgsql.OpenTelemetry" Version="10.0.3" />
<PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
```

### Pattern 5: `api/.env.example` and `api/docker-compose.yml` — containerized OTLP endpoint

```env
# .env.example addition
OTEL_EXPORTER_OTLP_ENDPOINT=http://host.docker.internal:4317
```

```yaml
# docker-compose.yml — finapi service environment block addition
- Otel__ExporterEndpoint=${OTEL_EXPORTER_OTLP_ENDPOINT}
```

### Pattern 6: `monitor/docker-compose.yml` — `postgres-exporter` service (replaces `greenhouse-app`)

```yaml
  postgres-exporter:
    image: quay.io/prometheuscommunity/postgres-exporter:v0.19.1
    container_name: postgres-exporter
    environment:
      - DATA_SOURCE_NAME=postgresql://postgres:YourStrongPassword123!@host.docker.internal:5432/fin_pulse?sslmode=disable
    ports:
      - "9187:9187"
    networks:
      - monitoring
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--spider", "-q", "http://localhost:9187/metrics"]
      interval: 30s
      timeout: 10s
      retries: 3
```

> The `greenhouse-app` service block, its `depends_on: [otel-collector]`, and its `healthcheck` are removed entirely — no replacement app service is needed since `FinPulse.Api` lives in its own compose project (`api/`), not `monitor/`.

### Pattern 7: `monitor/config/prometheus.yml` — updated scrape configs

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:9464']

  - job_name: 'loki'
    static_configs:
      - targets: ['loki:3100']

  - job_name: 'postgres-exporter'
    static_configs:
      - targets: ['postgres-exporter:9187']
```

> The `greenhouse-app` job is removed. No new job is added for `FinPulse.Api` metrics directly — the API's metrics already flow to Prometheus indirectly via OTLP → otel-collector → the already-scraped `otel-collector:9464` target, so a second direct-scrape job would be redundant.

### Pattern 8: Dashboard JSON skeleton (both new dashboards follow this shape, matching the existing `greenhouse-app-observability.json`'s panel structure)

```json
{
  "panels": [
    {
      "datasource": { "type": "prometheus", "uid": "prometheus" },
      "title": "Active Connections",
      "type": "stat",
      "targets": [
        { "expr": "pg_stat_database_numbackends{datname=\"fin_pulse\"}", "refId": "A" }
      ]
    }
  ]
}
```

---

## Data Flow

```text
1. Client sends an HTTP request to FinPulse.Api (e.g. GET /api/users/1/expenses)
   │
   ▼
2. ASP.NET Core auto-instrumentation starts a root Activity (span); Npgsql
   auto-instrumentation starts a child Activity for the EF Core query
   │
   ▼
3. Serilog's existing request-logging middleware logs the request; the log
   event is enriched and written to both the console sink and the OTLP sink
   │
   ▼
4. On response, the completed trace + the ASP.NET Core/runtime metrics are
   flushed via the OTLP exporter (gRPC, :4317) to the OTel Collector
   │
   ▼
5. The Collector fans out: logs → Loki, traces → Tempo, metrics → its own
   Prometheus exporter (:9464)
   │
   ▼
6. Prometheus scrapes :9464 (app+collector metrics) and :9187 (postgres-exporter,
   independently, on its own 15s cycle against the live Postgres instance)
   │
   ▼
7. Grafana queries Prometheus/Loki/Tempo directly; the pre-wired
   tracesToLogs/tracesToMetrics datasource config correlates a trace to its
   logs and request-rate metrics using the shared service.name tag
```

---

## Integration Points

| External System | Integration Type | Authentication |
|-----------------|-----------------|------------------|
| OTel Collector (`monitor/`) | OTLP gRPC push, port 4317 | None (insecure/local-dev, matches existing collector config) |
| PostgreSQL (`database/`) | `postgres-exporter`'s native Postgres wire protocol scrape | Existing `postgres` superuser credentials (no new user) |
| Prometheus (`monitor/`) | Pull-based scrape, ports 9464 (collector) and 9187 (postgres-exporter) | None (local-dev) |

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-------|-----------------|
| Build verification | `FinPulse.Api` compiles with new OTel packages | `FinPulse.Api.csproj`, `Program.cs` | `dotnet build` | 0 errors |
| Compose validation | `monitor/docker-compose.yml` resolves correctly (no daemon needed) | `monitor/docker-compose.yml` | `docker compose config` | AT-001, AT-006: valid config, 0 `greenhouse-app` references |
| Live trace verification | A DB-touching endpoint produces a correlated trace | Running `FinPulse.Api` + `monitor/` stack | Manual: `curl` an endpoint, check Tempo/Grafana Explore | AT-002 |
| Live log verification | API logs reach Loki | Same running setup | Manual: Grafana Explore, Loki datasource query | AT-003 |
| Live metrics verification | Runtime + request metrics appear in Prometheus | Same running setup | Manual: Prometheus UI query for `process_runtime_dotnet_*` / `http_server_request_duration*` | AT-004 |
| Live dashboard verification | Postgres dashboard renders live data | `postgres-exporter` + `database/` running | Manual: open the new Grafana dashboard | AT-005 |
| Reference sweep | No leftover `greenhouse-app` references | `monitor/docker-compose.yml`, `monitor/config/prometheus.yml`, dashboard JSON directory | `grep -ri "greenhouse" monitor/docker-compose.yml monitor/config/prometheus.yml monitor/config/grafana/provisioning/dashboards/json/` | AT-001: 0 matches |

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|--------------------|--------|
| OTel Collector unreachable (wrong endpoint, e.g. `host.docker.internal` used outside a container) | OTLP exporter fails silently by design (telemetry is fire-and-forget by default in the .NET SDK) — the API keeps working normally, just with no telemetry. This is why Decision 2's dual-endpoint pattern matters: getting it wrong produces no error, just empty dashboards | No explicit retry configured; OTLP exporter has its own internal retry/backoff defaults |
| `postgres-exporter` can't reach Postgres (wrong `DATA_SOURCE_NAME` or DB not running) | Exporter's own healthcheck fails (`wget --spider http://localhost:9187/metrics`); Prometheus scrape shows the target as `down` | Docker Compose healthcheck retries per its configured interval |
| Serilog OTLP sink misconfigured (endpoint not yet available at `Log.Logger` construction time) | Console sink continues working independently — Serilog sinks fail independently of each other | No — fix configuration ordering per Pattern 2's note |

---

## Configuration

| Config Key | Type | Default | Description |
|------------|------|---------|--------------|
| `Otel:ExporterEndpoint` (appsettings.json) | string | `http://localhost:4317` | OTLP gRPC endpoint for local `dotnet run` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` (.env / docker-compose) | string | `http://host.docker.internal:4317` | OTLP gRPC endpoint for the containerized API path |
| `DATA_SOURCE_NAME` (postgres-exporter) | string | `postgresql://postgres:***@host.docker.internal:5432/fin_pulse?sslmode=disable` | Connection string reusing existing DB credentials |

---

## Security Considerations

- OTLP export and the Postgres-exporter scrape both use `insecure`/no-TLS connections, matching the already-existing, already-accepted local-dev-only posture of every other piece of this stack (Loki, Tempo exporters already configured with `tls.insecure: true`).
- `postgres-exporter` reuses the existing `postgres` superuser credentials rather than creating a dedicated least-privilege monitoring role — an explicit, documented trade-off (Decision in DEFINE) favoring "no new migration" over least-privilege for this local-dev-only feature. Worth revisiting if this stack is ever pointed at a non-local-dev database.
- No new secrets are introduced — the Postgres password is the same one already in `database/.env`, referenced (not duplicated) in `monitor/docker-compose.yml`.
- Grafana's existing `admin/admin` default credentials are unchanged — out of scope per DEFINE (production-hardening excluded).

---

## Observability

| Aspect | Implementation |
|--------|-----------------|
| Logging | This feature *is* the logging implementation — Serilog console (existing) + OTLP→Loki (new) |
| Metrics | This feature *is* the metrics implementation — ASP.NET Core + .NET runtime metrics via OTLP→Prometheus; Postgres metrics via `postgres-exporter`→Prometheus |
| Tracing | This feature *is* the tracing implementation — ASP.NET Core + HttpClient + Npgsql spans via OTLP→Tempo |

---

## Pipeline Architecture (if applicable)

Not applicable. DEFINE confirmed this is an observability/telemetry feature, not a data pipeline in the ETL sense (the "Data Contract" section in DEFINE describes the telemetry flow, already reflected in the Architecture Overview above).

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-25 | design-agent | Initial version, derived from `DEFINE_OBSERVABILITY_STACK.md`. All package/image versions verified live via NuGet/Docker Hub search (not assumed); `postgres_exporter`'s default-collector coverage for table/index sizes verified live, avoiding an unnecessary custom-queries file; the `dotnet run`-vs-container OTLP endpoint distinction (Decision 2) surfaced by direct analogy to the already-proven DB-connection-string pattern from the prior migration. |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_OBSERVABILITY_STACK.md`
