# BRAINSTORM: Observability Stack

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | OBSERVABILITY_STACK |
| **Date** | 2026-08-25 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "let's alreadt setup monitor/. Reuse only the docker files but let's create all from scratch, logs, traces and metrics inside API and a dashboard for PostgreSQL. Use the same architecture we are using in the monitor but let's adjust to database and api"

**Context Gathered:**
- `monitor/` already contains a full LGTM-style observability stack: `docker-compose.yml` running an OpenTelemetry Collector (OTLP receiver on 4317/4318, Prometheus exporter on 9464), Prometheus (`prometheus.yml`), Loki (`loki-config.yaml`), Tempo (`tempo-config.yaml`), and Grafana with datasources already provisioned (`datasources.yml`) — including trace↔logs↔metrics correlation (`tracesToLogs`, `tracesToMetrics`, `serviceMap`) already wired.
- This stack was originally built for a demo `greenhouse-app` — a Python service referenced in `docker-compose.yml`'s build context (`./test/greenhouse-app`) that **does not exist in this repo** (confirmed via glob — zero files). Only its wiring (compose service block, Prometheus scrape job, and a matching Grafana dashboard JSON `greenhouse-app-observability.json`) remains.
- `monitor/docs/*.md` documents each config file in detail (`otel-config.md`, `prometheus.md`, `loki-config.md`, `tempo-config.md`) with greenhouse-app-specific examples throughout.
- `FinPulse.Api` currently uses Serilog for console-only structured logging (confirmed in `Program.cs`, carried through the PostgreSQL/.NET 10 migration already shipped). No tracing or metrics instrumentation exists in the API today.
- `database/` runs PostgreSQL via its own `docker-compose.yml` (already shipped), publishing port 5432 to the host. No metrics exporter exists for it today.
- `api/`, `database/`, and `monitor/` are three **separate** Docker Compose projects with separate networks — the established pattern from the two prior migrations is host-published-port connectivity (e.g. `api` reaches `database`'s Postgres via `host.docker.internal:5432`), not shared Docker networks.

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `monitor/docker-compose.yml`, `monitor/config/*.yaml`, `monitor/config/grafana/provisioning/dashboards/json/*.json`, `monitor/docs/*.md`, `api/FinPulse.Api/FinPulse.Api.csproj`, `api/FinPulse.Api/Program.cs`, `api/FinPulse.Api/appsettings.json`, `api/.env.example`, `api/docker-compose.yml` | Three-folder scope; `database/` is unaffected |
| Relevant KB Domains | None in `.claude/kb/_index.yaml` directly cover OpenTelemetry .NET SDK, Grafana dashboard authoring, or `postgres_exporter` specifically | Confidence 0.70 — Design phase should validate exact OTel .NET package names/versions and `postgres_exporter` connection requirements directly against docs (e.g. context7 MCP), the same live-verification approach used in the prior API migration's Design phase |
| IaC Impact | Modify existing (`monitor/docker-compose.yml`) + new service (`postgres_exporter`) | No new compose projects; three-way host-published-port connectivity, consistent with the established pattern |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Should the `greenhouse-app` demo wiring (compose service, scrape job, dashboard JSON) be removed? | Yes, remove it | Confirms scope includes cleanup, not just additive work |
| 2 | How should FinPulse.Api's existing Serilog console logging be extended to export logs to Loki? | Serilog + `Serilog.Sinks.OpenTelemetry` (second sink) | Keeps existing Serilog config/enrichers intact; minimal `Program.cs` change |
| 3 | How should the API and a Postgres exporter reach `monitor/`'s OTel Collector and `database/`'s Postgres, given three separate compose projects? | Host-published ports (`host.docker.internal`) | No compose-network bridging; matches the already-established cross-stack pattern |
| 4 | Where should the Postgres exporter live, and how should the dashboard be built? | `postgres_exporter` in `monitor/`, hand-built dashboard | Keeps monitoring infra centralized in `monitor/`; no new DB user/migration; dashboard tailored to this app instead of a generic import |
| 5 | What should API instrumentation cover — auto-instrumentation only, or also custom business metrics? | Full auto-instrumentation only | Defers custom metrics (expense-created counters, login-attempt counters, etc.) to a future iteration |

**Minimum Questions:** 3 ✅ (5 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Input files | `monitor/docker-compose.yml`, `monitor/config/*.yaml`, `monitor/config/grafana/provisioning/**` | 9 files | Existing infra to adapt — OTLP receiver config, Grafana datasource correlation, Prometheus scrape config all already documented and reusable |
| Output examples | `monitor/config/grafana/provisioning/dashboards/json/greenhouse-app-observability.json` | 1 file | Reference for Grafana dashboard JSON *structure* (panel types, gridPos, datasource wiring) even though its content (greenhouse-app-specific panels) will be replaced |
| Ground truth | N/A | — | No existing FinPulse-specific telemetry data; dashboards will be validated against live metrics once instrumentation ships |
| Related code | `api/FinPulse.Api/Program.cs` (existing Serilog setup), `database/docker-compose.yml` (Postgres connection details already established) | 2 files | Provides the exact connection details (host, port, credentials) and logging conventions this feature must integrate with |

**How samples will be used:**

- `monitor/docs/*.md` (already-written documentation of each config file) grounds the Design phase's understanding of the existing pipeline — no need to re-derive OTel Collector/Prometheus/Loki/Tempo behavior from scratch.
- The existing dashboard JSON's panel structure is a structural reference for authoring the two new dashboards, even though its content is greenhouse-app-specific and gets removed.

---

## Approaches Explored

### Approach A: Serilog + OTLP sink for logs ⭐ Recommended

**Description:** Add `Serilog.Sinks.OpenTelemetry` as a second Serilog sink alongside the existing console sink in `Program.cs`, pushing structured logs to the OTel Collector (which already exports to Loki).

**Pros:**
- Keeps all current Serilog configuration, enrichers, and console formatting untouched
- Minimal `Program.cs` change — one additional `.WriteTo.OpenTelemetry(...)` call
- Serilog is mature and already proven in this codebase across two prior migrations

**Cons:**
- Two logging code paths conceptually coexist (Serilog for app logs, native `ILogger`/OTel SDK internals for anything the OTel auto-instrumentation logs itself) rather than one fully unified pipeline

**Why Recommended:** Smallest, lowest-risk change consistent with "reuse what's already proven" — confirmed directly by the user.

---

### Approach B: `postgres_exporter` in `monitor/`, hand-built dashboard ⭐ Recommended

**Description:** Add `prometheuscommunity/postgres-exporter` as a new service in `monitor/docker-compose.yml`, connecting to `database/`'s Postgres via `host.docker.internal:5432` using the existing `postgres` superuser credentials (no new DB user or migration). Build the Grafana dashboard panel-by-panel for the metrics that matter for this app (active connections, transaction rate, cache hit ratio, table/index sizes) rather than importing a generic community dashboard.

**Pros:**
- All monitoring infrastructure stays centralized in `monitor/`, consistent with "reuse only the docker files" scoping that folder as the infra home
- No new migration file or DB user needed — avoids reopening `database/migrations/`
- A tailored dashboard surfaces exactly what matters for this app instead of 50+ generic panels

**Cons:**
- More manual dashboard-authoring effort than importing a pre-built community dashboard (e.g. Grafana dashboard ID 9628 for `postgres_exporter`)

**Why Recommended:** Confirmed directly by the user — avoids scope creep into `database/` (already shipped, stable) and keeps the dashboard signal-dense rather than generic.

---

## Data Engineering Context

Not applicable in the traditional sense (no data pipeline, ETL, or warehouse involved), but this feature does involve a telemetry data flow worth noting:

### Source Systems
| Source | Type | Volume Estimate | Current Freshness |
|--------|------|-----------------|--------------------|
| FinPulse.Api (logs/traces/metrics) | OTLP push (gRPC, port 4317) | Low (single local-dev instance) | Real-time |
| PostgreSQL (`database/`) | Prometheus pull via `postgres_exporter` | Low | 15s scrape interval (matches existing `prometheus.yml` global setting) |

### Data Flow Sketch
```text
[FinPulse.Api] --OTLP (logs+traces+metrics)--> [OTel Collector] --> [Loki / Tempo / Prometheus] --> [Grafana]
[PostgreSQL]   <--scrape (pg_stat_*)--          [postgres_exporter] --scraped by--> [Prometheus] --> [Grafana]
```

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A (Serilog + OTLP sink) and Approach B (`postgres_exporter` in `monitor/`, hand-built dashboard) |
| **User Confirmation** | 2026-08-25, via direct selection for both |
| **Reasoning** | Both minimize new scope/risk while reusing the already-proven `monitor/` infrastructure and established cross-stack connectivity pattern |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | Remove `greenhouse-app` wiring entirely (compose service, scrape job, dashboard JSON) | Dead reference to a Python app that doesn't exist in this repo; not relevant to FinPulse | Keeping it alongside as a reference example |
| 2 | Logs via Serilog + `Serilog.Sinks.OpenTelemetry` | Smallest change, keeps proven Serilog setup | Migrating to native .NET OTel Logging API |
| 3 | Full auto-instrumentation only for traces/metrics (ASP.NET Core, Npgsql, HttpClient, .NET runtime) | Confirmed by user — no custom business-metric code in v1 | Auto-instrumentation + custom business metrics (counters for expenses/logins/JWT failures) |
| 4 | Host-published-port connectivity (`host.docker.internal`) across all three compose projects | Matches the established pattern from the two prior migrations; no compose restructuring | Bridging `monitor/`, `api/`, `database/` onto one shared Docker network |
| 5 | `postgres_exporter` lives in `monitor/`, reuses existing `postgres` superuser credentials | Keeps monitoring infra centralized; avoids reopening `database/migrations/` | Placing the exporter in `database/docker-compose.yml`; creating a dedicated monitoring DB user/migration |
| 6 | Grafana Postgres dashboard hand-built panel-by-panel | Tailored, signal-dense dashboard vs. generic import | Importing a known community dashboard (e.g. ID 9628) |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Native OTel Logging API migration (replacing Serilog) | Confirmed out of scope — Serilog+OTLP-sink is smaller and lower-risk | Yes, if Serilog ever becomes a limitation |
| Custom business metrics (expense-created counters, login-attempt counters, JWT-failure counters, etc.) | Confirmed out of scope for v1 — full auto-instrumentation covers the baseline need | Yes — natural follow-up once auto-instrumentation is proven |
| Bridging `monitor/`, `api/`, `database/` onto one shared Docker network | Confirmed out of scope — host-published ports already work and match the established pattern | Yes, if local-dev ergonomics ever demand it |
| Importing a generic community Postgres dashboard | Confirmed out of scope — hand-built dashboard preferred for signal density | Yes, as a supplementary reference dashboard |
| Alertmanager / alerting rules | Not requested; `loki-config.yaml`'s `ruler.alertmanager_url` stub already exists but no Alertmanager service is running | Yes — separate, deliberately-scoped feature |
| Replication-lag or multi-instance Postgres metrics | Single local Postgres instance — metric class doesn't apply | Yes, if the topology ever changes |
| Bringing back or fixing the `greenhouse-app` demo | Confirmed removed entirely, not repaired | Yes, as an unrelated future demo/reference app if ever needed |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| File-level plan across `monitor/`, `api/`, `database/` (untouched) | ✅ | Confirmed correct | No |
| Full decision summary (scope, logging, instrumentation, networking, Postgres monitoring, YAGNI) | ✅ | Confirmed correct | No |

**Minimum Validations:** 2 ✅ (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)
`FinPulse.Api` and its PostgreSQL database have no observability today — no traces, no exported metrics, no centralized log aggregation — even though a fully-built OTel Collector→Prometheus/Loki/Tempo→Grafana stack already exists in `monitor/`, currently wired for a demo app that doesn't exist in this repo.

### Target Users (Draft)
| User | Pain Point |
|------|------------|
| Developer debugging a production/local issue | No way to see request traces, correlate logs to a specific request, or see DB query performance without attaching a debugger |
| Developer monitoring DB health | No visibility into Postgres connection counts, cache hit ratio, or table growth without manually running `psql` queries |

### Success Criteria (Draft)
- [ ] `monitor/docker-compose.yml` contains zero references to `greenhouse-app`
- [ ] FinPulse.Api requests produce traces visible in Tempo/Grafana, correlated to their logs (via the existing `tracesToLogs` datasource config) and to their DB queries (via Npgsql instrumentation spans)
- [ ] FinPulse.Api logs are queryable in Loki via Grafana, in addition to the existing console output
- [ ] `.NET` runtime metrics (GC, thread pool, exceptions) and ASP.NET Core request metrics are visible in Prometheus/Grafana
- [ ] A new Grafana dashboard shows live PostgreSQL metrics (connections, transaction rate, cache hit ratio, table/index sizes) sourced from `postgres_exporter`
- [ ] `docker compose up` in `monitor/` (after `api/` and `database/` are already running) requires no manual network configuration beyond what's documented

### Constraints Identified
- No changes to `database/migrations/` or `database/docker-compose.yml` — `postgres_exporter` reuses existing credentials
- Three separate Docker Compose projects remain separate — connectivity via host-published ports only
- No custom business metrics in this pass — auto-instrumentation only

### Out of Scope (Confirmed)
- Custom business metrics (expense/login/JWT-failure counters)
- Native OTel Logging API migration
- Shared Docker network across `monitor/`/`api/`/`database/`
- Community-dashboard import for Postgres
- Alertmanager / alerting rules
- Replication-lag or multi-instance Postgres metrics
- Repairing or reintroducing the `greenhouse-app` demo

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 5 |
| Approaches Explored | 2 |
| Features Removed (YAGNI) | 7 |
| Validations Completed | 2 |
| Duration | Single session |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_OBSERVABILITY_STACK.md`
