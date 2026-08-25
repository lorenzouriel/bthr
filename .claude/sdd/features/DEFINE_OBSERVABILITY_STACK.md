# DEFINE: Observability Stack

> Retarget the existing `monitor/` OTel Collector→Prometheus/Loki/Tempo→Grafana stack from a nonexistent demo app to FinPulse.Api and PostgreSQL: add logs/traces/metrics instrumentation inside the API and a hand-built Grafana dashboard for PostgreSQL.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | OBSERVABILITY_STACK |
| **Date** | 2026-08-25 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Built) |
| **Clarity Score** | 15/15 |

---

## Problem Statement

`FinPulse.Api` and its PostgreSQL database have zero observability today — no distributed traces, no exported metrics, no centralized log aggregation — even though a fully-built OTel Collector→Prometheus/Loki/Tempo→Grafana stack already exists in `monitor/`, currently wired end-to-end for a demo Python app (`greenhouse-app`) whose source code doesn't exist anywhere in this repo.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| Developer debugging a production/local issue | Backend engineer | No way to see request traces, correlate a log line to the request that produced it, or see DB query performance without attaching a debugger |
| Developer monitoring database health | Backend/DB engineer | No visibility into Postgres connection counts, cache hit ratio, or table growth without manually running `psql` queries against the live instance |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Remove all `greenhouse-app` wiring from `monitor/` (compose service, Prometheus scrape job, Grafana dashboard JSON) |
| **MUST** | Instrument `FinPulse.Api` with OpenTelemetry traces (ASP.NET Core, Npgsql, HttpClient auto-instrumentation) exported via OTLP to the existing collector |
| **MUST** | Instrument `FinPulse.Api` with OpenTelemetry metrics (.NET runtime + ASP.NET Core request metrics) exported via OTLP to the existing collector |
| **MUST** | Export `FinPulse.Api`'s existing Serilog logs to Loki via a `Serilog.Sinks.OpenTelemetry` sink, alongside the existing console sink |
| **MUST** | Add a `postgres_exporter` service to `monitor/docker-compose.yml`, scraped by Prometheus, connected to `database/`'s Postgres via `host.docker.internal:5432` using existing credentials |
| **MUST** | Build a new Grafana dashboard showing live PostgreSQL metrics (connections, transaction rate, cache hit ratio, table/index sizes) |
| **SHOULD** | Build a new Grafana dashboard showing FinPulse.Api observability (request rate/latency/errors, trace exemplars, log panel) |
| **SHOULD** | Update `monitor/docs/*.md` examples away from `greenhouse-app` references |
| **COULD** | Verify trace↔log↔metric correlation actually works end-to-end in the Grafana UI (the datasource wiring already supports it; this is a live-verification nice-to-have, not a build requirement) |

**Priority Guide:**
- **MUST** = MVP fails without this
- **SHOULD** = Important, but workaround exists
- **COULD** = Nice-to-have, cut first if needed

---

## Success Criteria

- [ ] `grep -ri "greenhouse" monitor/` returns 0 matches (after `docs/` examples are updated per the SHOULD goal — matches in `monitor/docs/` are acceptable to remain until that goal completes, but the compose file, scrape config, and dashboard JSON must be clean immediately)
- [ ] A request to any `FinPulse.Api` endpoint produces a trace visible in Tempo (via Grafana Explore), containing at least an ASP.NET Core request span and, for any endpoint that touches the DB, a Npgsql query span as a child
- [ ] `FinPulse.Api` log lines are queryable in Loki via Grafana within 15 seconds of being written
- [ ] Prometheus shows `FinPulse.Api`'s .NET runtime metrics (e.g. `process_runtime_dotnet_gc_*`) and ASP.NET Core request metrics (e.g. `http_server_request_duration*`) with fresh data points
- [ ] The new PostgreSQL Grafana dashboard renders live, non-zero values for at least: active connections, transactions/sec, and cache hit ratio, sourced from `postgres_exporter`
- [ ] `docker compose up` in `monitor/` succeeds without any manual network configuration, given `api/` (running via `dotnet run` or its own compose) and `database/`'s compose are already up

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | Greenhouse-app wiring fully removed | `monitor/docker-compose.yml`, `monitor/config/prometheus.yml`, `monitor/config/grafana/provisioning/dashboards/json/` | Files are inspected/grepped | 0 references to `greenhouse-app` in the compose file, Prometheus config, or dashboard JSON directory |
| AT-002 | API request produces a correlated trace | `FinPulse.Api` running with OTel instrumentation, `monitor/` stack running | A client calls an endpoint that queries the DB (e.g. `GET /api/users/{id}/expenses`) | A trace appears in Tempo containing an ASP.NET Core span and a child Npgsql span for the DB query |
| AT-003 | API logs reach Loki | Same setup as AT-002 | The API logs a request (via Serilog's existing request-logging middleware) | The log line is queryable in Grafana's Loki datasource within 15 seconds |
| AT-004 | .NET runtime + request metrics visible | Same setup as AT-002 | Prometheus scrapes the OTel Collector's `:9464` endpoint | `process_runtime_dotnet_*` and ASP.NET Core request-duration metrics appear with recent timestamps |
| AT-005 | Postgres dashboard shows live data | `postgres_exporter` running and scraped by Prometheus, `database/`'s Postgres running | The new Grafana Postgres dashboard is opened | Active connections, transaction rate, and cache hit ratio panels render non-zero, current values |
| AT-006 | Clean `monitor/` startup | `api/` and `database/` compose stacks already running | `docker compose up` is run in `monitor/` | All services (otel-collector, prometheus, loki, tempo, grafana, postgres-exporter) start healthy with no manual network steps |

---

## Out of Scope

Explicitly NOT included in this feature:

- **Custom business metrics** (expense-created counters, login-attempt counters, JWT-failure counters, etc.) — full auto-instrumentation only for this pass.
- **Native OTel Logging API migration** — Serilog stays, gains an additional OTLP sink.
- **Bridging `monitor/`, `api/`, `database/` onto a shared Docker network** — host-published-port connectivity only, matching the established pattern.
- **Importing a generic community Postgres dashboard** (e.g. Grafana ID 9628) — the new dashboard is hand-built for this app's needs.
- **Alertmanager / alerting rules** — not requested; `loki-config.yaml`'s existing `ruler.alertmanager_url` stub is left as-is, unconnected.
- **Replication-lag or multi-instance Postgres metrics** — single local Postgres instance, metric class doesn't apply.
- **Repairing or reintroducing the `greenhouse-app` demo** — removed entirely, not fixed.
- **Any changes to `database/migrations/` or `database/docker-compose.yml`** — `postgres_exporter` reuses existing credentials, no new DB user or migration.
- **Production-hardening the observability stack** (Grafana auth beyond the existing `admin/admin` default, TLS on OTLP endpoints, Loki/Tempo retention policy changes) — this is a local-dev observability setup, consistent with the local-dev-only scope of both prior migrations.

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | Three separate Docker Compose projects (`monitor/`, `api/`, `database/`) stay separate | Design must use host-published-port connectivity (`host.docker.internal`), not shared networks |
| Technical | No new DB user or migration for `postgres_exporter` | Design must use existing `postgres` superuser credentials from `database/.env` |
| Technical | Existing Serilog configuration/enrichers in `Program.cs` must remain functional | The OTLP sink is additive, not a replacement |
| Scope | Auto-instrumentation only, no custom metrics | Design must not introduce new `Meter`/custom counter code in this pass |

---

## Technical Context

> Essential context for Design phase - prevents misplaced files and missed infrastructure needs.

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `monitor/docker-compose.yml`, `monitor/config/otel-config.yaml`, `monitor/config/prometheus.yml`, `monitor/config/grafana/provisioning/dashboards/json/*.json`, `monitor/docs/*.md`, `api/FinPulse.Api/FinPulse.Api.csproj`, `api/FinPulse.Api/Program.cs`, `api/FinPulse.Api/appsettings.json`, `api/.env.example`, `api/docker-compose.yml` | All changes scoped inside `monitor/` and `api/`; `database/` is untouched |
| **KB Domains** | None in `.claude/kb/_index.yaml` directly cover OpenTelemetry .NET SDK, Grafana dashboard authoring, or `postgres_exporter` | Confidence 0.70 — Design phase should validate exact OTel .NET package names/versions and `postgres_exporter` configuration directly against live docs/NuGet, the same live-verification approach used in the prior API migration's Design phase |
| **IaC Impact** | Modify existing (`monitor/docker-compose.yml` gains a `postgres_exporter` service; `monitor/config/prometheus.yml` gains a scrape job, loses the `greenhouse-app` one) | No new compose projects |

**Why This Matters:**

- **Location** → Design phase uses correct project structure, prevents misplaced files
- **KB Domains** → Design phase pulls correct patterns from `.claude/kb/`
- **IaC Impact** → Triggers infrastructure planning, avoids "works locally" failures

---

## Data Contract (if applicable)

Not applicable in the traditional data-pipeline sense, but this feature does involve a telemetry data flow:

### Source Inventory
| Source | Type | Volume | Freshness | Owner |
|--------|------|--------|-----------|-------|
| FinPulse.Api (logs/traces/metrics) | OTLP push (gRPC, port 4317) | Low (single local-dev instance) | Real-time |
| PostgreSQL (`database/`) | Prometheus pull via `postgres_exporter` | Low | 15s scrape interval (matches `prometheus.yml`'s existing global setting) |

### Freshness SLAs
| Layer | Target | Measurement |
|-------|--------|-------------|
| Traces/Logs (OTLP push) | Visible in Grafana within 15s of emission | Manual check against wall-clock time |
| Postgres metrics (scrape) | Refreshed every 15s | Prometheus scrape interval, already configured |

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | `prometheuscommunity/postgres-exporter` can connect and scrape using the existing `postgres` superuser credentials without additional `GRANT` statements | Would need a dedicated monitoring role and a new migration, expanding scope into `database/` | [ ] |
| A-002 | OpenTelemetry .NET SDK packages (`OpenTelemetry.Extensions.Hosting`, ASP.NET Core/HttpClient/Npgsql instrumentation) have stable releases compatible with .NET 10 | Design would need to find alternative packages or a compatible version combination | [ ] |
| A-003 | `Serilog.Sinks.OpenTelemetry` can export to the existing OTLP gRPC endpoint (`otel-collector:4317` / `host.docker.internal:4317`) that the collector already accepts | Would need a different log-export mechanism (e.g. a custom Serilog HTTP sink) | [ ] |
| A-004 | `host.docker.internal` resolves correctly from containers in `monitor/`'s own compose network to reach the API and Postgres on the host | Would need an explicit host IP or `extra_hosts` mapping, as already used in `api/docker-compose.yml` | [ ] (though this exact pattern is already proven working for `api`↔`database` connectivity) |

**Note:** Validate critical assumptions before DESIGN phase. Unvalidated assumptions become risks.

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific, verifiable (zero observability today despite existing infra), directly motivated by real gaps found in the codebase |
| Users | 3 | Two personas with concrete, present-tense pain points |
| Goals | 3 | MoSCoW-prioritized, each traceable to a validated brainstorm decision (2 formal approach comparisons + 4 additional confirmed decisions) |
| Success | 3 | Every criterion is testable pass/fail (grep counts, trace presence, log queryability, metric freshness, dashboard rendering) |
| Scope | 3 | Nine explicit out-of-scope items, each with a clear rationale traced back to a brainstorm YAGNI decision |
| **Total** | **15/15** | |

**Scoring Guide:**
- 0 = Missing entirely
- 1 = Vague or incomplete
- 2 = Clear but missing details
- 3 = Crystal clear, actionable

**Minimum to proceed: 12/15**

---

## Open Questions

None - ready for Design.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-25 | define-agent | Initial version, derived from `BRAINSTORM_OBSERVABILITY_STACK.md` |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_OBSERVABILITY_STACK.md`
