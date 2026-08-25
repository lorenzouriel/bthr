# BUILD REPORT: Observability Stack

> Implementation report for retargeting `monitor/`'s OTel stack from a nonexistent demo app to FinPulse.Api and PostgreSQL

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | OBSERVABILITY_STACK |
| **Date** | 2026-08-25 |
| **Author** | build-agent |
| **DEFINE** | [DEFINE_OBSERVABILITY_STACK.md](../features/DEFINE_OBSERVABILITY_STACK.md) |
| **DESIGN** | [DESIGN_OBSERVABILITY_STACK.md](../features/DESIGN_OBSERVABILITY_STACK.md) |
| **Status** | ✅ Complete |

---

## Summary

| Metric | Value |
|--------|-------|
| **Tasks Completed** | 11/11 (all manifest files) |
| **Files Modified** | 9 modified, 2 created, 1 deleted |
| **Lines of Code** | 1,139 (across all touched files) |
| **Build Time** | Single session |
| **Tests Passing** | `FinPulse.Api` builds 0 errors/0 warnings; live end-to-end verification passed for every acceptance test that could be checked with a running stack |
| **Agents Used** | 0 (all files `(general)` per DESIGN — no .NET/OTel/Grafana specialist agent exists) |

---

## Task Execution with Agent Attribution

| # | Task | Agent | Status | Notes |
|---|------|-------|--------|-------|
| 1 | `monitor/docker-compose.yml` | (direct) | ✅ Complete | `greenhouse-app` removed, `postgres-exporter` added |
| 2 | `monitor/config/prometheus.yml` | (direct) | ✅ Complete | `greenhouse-app` scrape job → `postgres-exporter` |
| 3 | `greenhouse-app-observability.json` | (direct) | ✅ Deleted | |
| 4 | `finpulse-api-observability.json` | (direct) | ✅ Complete | 3 metric-name corrections applied after live verification — see Issues |
| 5 | `postgresql-observability.json` | (direct) | ✅ Complete | All queries verified live against real data |
| 6 | `monitor/docs/prometheus.md` | (direct) | ✅ Complete | Required a byte-level fix due to a pre-existing CRLF/cp1252 encoding issue in the file — see Issues |
| 7 | `api/FinPulse.Api/FinPulse.Api.csproj` | (direct) | ✅ Complete | 8 OTel/Serilog packages added, `dotnet build` succeeds |
| 8 | `api/FinPulse.Api/Program.cs` | (direct) | ✅ Complete | Required `using Npgsql;` beyond DESIGN's pattern, and a Serilog bootstrap reorder — see Issues |
| 9 | `api/FinPulse.Api/appsettings.json` | (direct) | ✅ Complete | |
| 10 | `api/.env.example` | (direct) | ✅ Complete | |
| 11 | `api/docker-compose.yml` | (direct) | ✅ Complete | |

**Legend:** ✅ Complete | 🔄 In Progress | ⏳ Pending | ❌ Blocked

---

## Agent Contributions

| Agent | Files | Specialization Applied |
|-------|-------|--------------------------|
| (direct) | 11 | DESIGN patterns, verified against a real .NET 10 compiler and a fully live-running observability stack (Docker Desktop, Postgres, monitor/ stack, and the API were all actually started and exercised — not just statically reviewed) |

---

## Files Created

| File | Lines | Agent | Verified | Notes |
| ---- | ----- | ----- | -------- | ----- |
| `monitor/docker-compose.yml` | 183 | (direct) | ✅ Live | `docker compose up` succeeded, all services started |
| `monitor/config/prometheus.yml` | 14 | (direct) | ✅ Live | `postgres-exporter` target confirmed scraped |
| `monitor/config/grafana/provisioning/dashboards/json/finpulse-api-observability.json` | 276 | (direct) | ✅ Live | Provisioned in Grafana; all panel queries verified to return real data after 3 metric-name corrections |
| `monitor/config/grafana/provisioning/dashboards/json/postgresql-observability.json` | 288 | (direct) | ✅ Live | Provisioned in Grafana; all panel queries verified to return real data, no corrections needed |
| `monitor/docs/prometheus.md` | 85 | (direct) | ✅ | Grep-swept, 0 `greenhouse` references |
| `api/FinPulse.Api/FinPulse.Api.csproj` | 27 | (direct) | ✅ | `dotnet build` succeeds |
| `api/FinPulse.Api/Program.cs` | 198 | (direct) | ✅ Live | Compiles; live-verified to emit correct traces/logs/metrics |
| `api/FinPulse.Api/appsettings.json` | 21 | (direct) | ✅ | Gitignored, direct read-back confirmation |
| `api/.env.example` | 19 | (direct) | ✅ | |
| `api/docker-compose.yml` | 28 | (direct) | ✅ | |

---

## Verification Results

### Build Check

```text
$ dotnet build FinPulse.Api/FinPulse.Api.csproj
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**Status:** ✅ Pass

```text
$ dotnet build FinPulse.Tests/FinPulse.Tests.csproj
Build FAILED.
    2 Warning(s)
    53 Error(s)
```

**Status:** ❌ Fail — but this is the **exact same pre-existing, unrelated test-suite breakage** documented in `BUILD_REPORT_POSTGRESQL_API_MIGRATION.md`. None of this feature's changes touch any file involved in those 53 errors (`Bill`/`Auth`/`Users` controllers, models, services). Not this feature's to fix.

### Compose Validation

```text
$ docker compose config   (in monitor/)
```

Resolves cleanly; `postgres-exporter` service present, `greenhouse-app` fully absent.

**Status:** ✅ Pass

### Live End-to-End Verification

Unlike a typical build, this feature was verified against a **fully running stack**, not just static analysis:

1. Started Docker Desktop, brought up `monitor/docker-compose.yml` (6 services: otel-collector, prometheus, loki, tempo, grafana, postgres-exporter) — all reached `healthy` except `otel-collector`, whose Docker healthcheck target (`:13133`) was never configured with a `health_check` extension in `otel-config.yaml` (a pre-existing gap in a file this feature didn't touch); its logs confirm it is genuinely running and processing data correctly regardless.
2. Started `FinPulse.Api` via `dotnet run` — 0 startup errors.
3. Exercised `/health`, `POST /api/auth/login`, `GET /api/users/1/expenses`.
4. **Traces (AT-002):** Queried Tempo directly — all 3 requests appear as traces with `rootServiceName=FinPulse.Api`. Inspected the expenses-endpoint trace in full: contains an `Microsoft.AspNetCore` SERVER span (`GET api/users/{userId}/expenses`) with a child `Npgsql` CLIENT span (`postgresql`) — exactly AT-002's requirement.
5. **Logs (AT-003):** Queried Loki directly — logs present within seconds, each carrying the matching `traceid`/`spanid` from the Tempo trace (confirms trace↔log correlation data is present, even though the pre-existing Grafana UI correlation config has a caveat — see Issues #4).
6. **Metrics (AT-004):** Queried Prometheus directly — ASP.NET Core request-duration metrics present with correct route/status labels; .NET runtime metrics present after correcting the metric-name prefix (see Issues #1).
7. **Postgres dashboard (AT-005):** Queried Prometheus directly for every metric used in the dashboard — active connections (3), cache hit ratio (99.95%), table sizes (real table names: `users`, `expenses`, `flyway_schema_history`) all return live, correct data.
8. **Dashboards provisioned:** Confirmed via Grafana's own API (`/api/search`) that both new dashboards are registered and the old one is gone.
9. **Reference sweep (AT-001):** `grep -ri "greenhouse" monitor/` → 0 matches.

**Status:** ✅ Pass — AT-001 through AT-005 all directly confirmed live, not just inferred from static review.

**Post-report correction:** User-driven testing after this report was first written surfaced that the dashboard's "Recent Traces" panel itself didn't render in the real Grafana UI (see Issue #8) — a gap the API-level verification above didn't catch, since it queried Tempo directly rather than through Grafana's dashboard-panel query path specifically. Root-caused and fixed; see Issue #8 and the updated Final Status below. This is a reminder that "the data exists and is queryable via the datasource's raw API" and "the specific panel type renders it correctly in this Grafana version" are genuinely different claims — worth verifying both for any future panel using an uncommon panel/query type.

---

## Issues Encountered

| # | Issue | Resolution | Time Impact |
|---|-------|------------|--------------|
| 1 | DESIGN's Pattern assumed `.NET runtime metrics` would appear as `process_runtime_dotnet_*` (the name `OpenTelemetry.Instrumentation.Runtime` historically used). Live Prometheus query returned **zero results** for that prefix | Inspected the raw `:9464/metrics` output directly — on .NET 9+, the package registers a Meter that surfaces .NET's own **built-in** runtime metrics, which use a different naming convention (`dotnet_gc_last_collection_heap_size_bytes`, `dotnet_thread_pool_thread_count_total`, `dotnet_exceptions_total`, etc.). Corrected all 3 affected dashboard panel queries and re-verified each returns real data | +medium — required live investigation, not guessable from docs alone |
| 2 | DESIGN's dashboard pattern assumed the Loki label for the service would be `service_name` (a common OTel Collector convention). Live Loki query returned **zero results** | Queried Loki's `/loki/api/v1/labels` directly — this collector/exporter version actually uses `job` as the label carrying the service name. Corrected the dashboard's log panel query from `{service_name="FinPulse.Api"}` to `{job="FinPulse.Api"}`, re-verified real log lines return, complete with matching `traceid`/`spanid` | +small |
| 3 | `Program.cs`'s `.AddNpgsql()` call (inside the `WithTracing` lambda, intended to invoke `Npgsql.OpenTelemetry`'s tracing extension) initially resolved to the wrong overload — `NpgsqlServiceCollectionExtensions.AddNpgsql<TContext>` (from `Npgsql.EntityFrameworkCore.PostgreSQL`, an `IServiceCollection` extension) — producing `CS7036: no argument for required parameter 'connectionString'` | Fetched Npgsql's own official tracing documentation to confirm `Npgsql.OpenTelemetry`'s `AddNpgsql()` on `TracerProviderBuilder` was in fact correct, then added the missing `using Npgsql;` directive (the extension method's actual namespace), which resolved the ambiguity correctly. Verified via a clean `dotnet build` | +medium |
| 4 | `monitor/docs/prometheus.md` uses CRLF line endings with a Windows-1252-encoded curly-apostrophe byte (`\x92`) that isn't valid UTF-8 — the `Edit` tool's exact-string matching failed silently against this pre-existing encoding quirk | Diagnosed via raw byte inspection (`repr()` on the decoded lines showed `�` replacement characters), then performed the replacement via a byte-level Python script instead, preserving the file's existing CRLF convention | +small — pre-existing file quirk unrelated to this feature's content |
| 5 | Grafana's `datasources.yml` (untouched at first, pre-existing, not in DESIGN's original manifest) configures trace↔log correlation via `tracesToLogs.tags`/`mappedTags`, which don't line up with Loki's actual `job` label (see Issue #2) — the span→log direction. Separately, the log→span direction had **no** `derivedFields` configured on the Loki datasource at all, so a `traceid` in a log line was inert plain text, not a clickable link into Tempo | **Fixed** (user asked "what about the panel?" after the Issue #8 fix, prompting a closer look): added a `derivedFields` entry to the Loki datasource (`matcherRegex: "\"traceid\":\"(\\w+)\"", datasourceUid: tempo`), which makes every log line's `traceid` a working "View Trace" link straight into the Tempo waterfall — this is the standard, fully-supported Grafana mechanism for log↔trace pivoting, unlike the broken panel-level TraceQL search from Issue #8. Required a Grafana container restart (datasource provisioning isn't hot-reloaded the way dashboard JSON is); restarted and verified all panels/dashboards still function afterward. The `tracesToLogs`/`mappedTags` span→log direction (the OTHER half of the correlation, for jumping from a Tempo span to its logs) still has the label mismatch and was left as-is — same reasoning as before, a separate small fix if ever wanted | Resolved (log→trace direction); span→log direction still open |
| 6 | `FinPulse.Tests` still has the same 53 pre-existing compile errors documented in the prior build report | Confirmed via `git status` — none of this feature's changes touch any of the affected files. Not this feature's to fix | Documented, not resolved |
| 7 | Docker Desktop was not running at the start of this session (same as the prior migration's build) | Started it (`Start-Process "Docker Desktop.exe"`), polled until the engine responded — same approach as the prior session | Environment step, not a defect |
| 8 | **(Found post-report, during user-driven testing)** The `finpulse-api-observability.json` dashboard's "Recent Traces" panel (`type: "traces"`, `queryType: "traceql"`) showed "No data found in response" in the actual Grafana UI, despite Tempo itself having the trace data (confirmed via direct Tempo API calls) | Root-caused via Grafana server logs (`docker logs grafana`): `error="unsupported query type: 'traceql' for query with refID 'A'"`. Fetched Grafana v10.3.3's actual `pkg/tsdb/tempo/tempo.go` source — confirmed this Grafana version's Tempo datasource **backend** (the code path dashboard panels use via `/api/ds/query`) only supports `queryType: "traceId"` (fetch one trace by exact ID). TraceQL/tag search only works through the **Explore** UI, which calls Tempo's HTTP API directly via the datasource proxy rather than through this backend query path — confirmed this proxy path still returns real data. This is a known, filed Grafana bug (`grafana/grafana#95042`), not a configuration mistake. Replaced the broken "traces" panel with a `text`/markdown panel that explains the limitation and gives the exact Explore steps (including that every log line's `traceid` can be clicked through) | +medium — required log-level root-causing and reading Grafana's own source for the pinned version, not guessable from query-type naming alone |

---

## Autonomous Decisions

| # | Decision Point | Options Considered | Chose | Rationale |
|---|----------------|--------------------|-------|-----------|
| 1 | .NET runtime metric names didn't match DESIGN's assumed `process_runtime_dotnet_*` prefix (Issue #1) | (a) Leave the dashboard queries as DESIGN specified, accepting they'd show "No data"; (b) investigate the real metric names live and correct the queries | (b) Corrected the queries | A dashboard that DEFINE explicitly requires to "render live, non-zero values" (AT-004) cannot ship with queries that return nothing — the smallest correct fix was to use the metric names the running collector actually exports |
| 2 | Loki label mismatch for the log panel (Issue #2) | (a) Leave DESIGN's assumed `service_name` label; (b) inspect Loki's real labels and correct the query | (b) Corrected the query | Same reasoning as #1 — AT-003 requires logs to actually be queryable in Grafana, not just theoretically present in Loki |
| 3 | The `datasources.yml` trace↔log correlation mismatch (Issue #5) | (a) Fix `datasources.yml` even though it's outside DESIGN's manifest; (b) document the gap and leave the file untouched | (b) Documented, left untouched | `datasources.yml` was explicitly not in DESIGN's file manifest — DESIGN scoped this feature to leave existing collector/datasource infrastructure unmodified. Expanding into an unscoped file for a UI-convenience feature (vs. the actual data being present and queryable, which it is) would be scope creep beyond what was designed and approved |
| 4 | `Program.cs`'s `AddNpgsql()` ambiguity (Issue #3) | (a) Fully-qualify the call to force the correct overload; (b) add the missing `using Npgsql;` directive | (b) Added the using directive | Matches the officially documented usage pattern from Npgsql's own docs exactly, rather than introducing a fully-qualified call style inconsistent with the rest of the file |

---

## Deviations from Design

| Deviation | Reason | Impact |
|-----------|--------|--------|
| `Program.cs` required `using Npgsql;` beyond DESIGN's listed usings | Extension method resolution ambiguity discovered only via compilation (Issue #3) | Still within file #8 of the manifest — no new files touched |
| 3 dashboard panel queries in `finpulse-api-observability.json` differ from DESIGN's Pattern 8 skeleton (metric/label names corrected) | DESIGN's example query names were illustrative, not live-verified at Design time; live verification during Build caught the mismatch before shipping broken panels | Still within file #4 of the manifest |
| `finpulse-api-observability.json`'s "Recent Traces" panel changed from `type: traces` to `type: text` | Grafana 10.3.3's Tempo datasource backend doesn't support TraceQL/search queries through provisioned panels at all (Issue #8) — no query-type fix could have worked | Still within file #4 of the manifest |
| `monitor/config/grafana/provisioning/datasources/datasources.yml` modified (not in DESIGN's original 11-file manifest) | User asked directly whether the trace panel could be made to actually work; adding Loki `derivedFields` was the real, achievable fix for log→trace navigation (Issue #5 update) | New file touched beyond DESIGN's manifest — small, single-purpose addition (one `derivedFields` entry), required a Grafana restart to take effect |

---

## Blockers (if any)

None that stop this feature. Two items are documented as follow-ups, consistent with the pattern from prior builds in this initiative:

| Blocker | Required Action | Owner |
|---------|-----------------|-------|
| `datasources.yml`'s `tracesToLogs`/`mappedTags` config (the span→log correlation direction) still doesn't match the actual `job` Loki label (Issue #5) — the log→trace direction is now fixed via `derivedFields`, but this is the other half | One-line fix: change `mappedTags` value from `'service'` to `'job'` in `monitor/config/grafana/provisioning/datasources/datasources.yml` — recommend as a small, separately-scoped follow-up if wanted | User (optional — log→trace navigation, the more commonly used direction, already works) |
| `otel-collector`'s Docker healthcheck never resolves to "healthy" (cosmetic only — collector is functionally correct) | Add a `health_check` extension to `otel-config.yaml` if the healthcheck status matters for orchestration/monitoring purposes | User — `otel-config.yaml` is pre-existing, unmodified, out of this feature's scope |

---

## Acceptance Test Verification

| ID | Scenario | Status | Evidence |
|----|----------|--------|----------|
| AT-001 | Greenhouse-app wiring fully removed | ✅ Pass | `grep -ri "greenhouse" monitor/` → 0 matches, live-executed |
| AT-002 | API request produces a correlated trace | ✅ Pass | Live Tempo query: expenses-endpoint trace contains `Microsoft.AspNetCore` SERVER span + child `Npgsql` CLIENT span |
| AT-003 | API logs reach Loki | ✅ Pass | Live Loki query: log lines present within seconds, carrying matching `traceid`/`spanid` |
| AT-004 | .NET runtime + request metrics visible | ✅ Pass | Live Prometheus query, after correcting metric names (Issue #1): `dotnet_gc_last_collection_heap_size_bytes`, `dotnet_thread_pool_thread_count_total`, `http_server_request_duration_seconds_count` all return current data |
| AT-005 | Postgres dashboard shows live data | ✅ Pass | Live Prometheus query for every dashboard metric: connections=3, cache hit ratio=99.95%, real table sizes for `users`/`expenses`/`flyway_schema_history` |
| AT-006 | Clean `monitor/` startup | ✅ Pass | `docker compose up` succeeded for all 6 services with no manual network configuration |

---

## Final Status

### Overall: ✅ COMPLETE

All 11 manifest files are correctly implemented and — going beyond the prior two builds in this initiative — **fully live-verified against a running stack**, not just compiled/statically reviewed. Four real discrepancies between DESIGN's illustrative queries/panels and the actual running system were caught and fixed: 3 during the original build pass (metric/label name mismatches), and 1 (the Tempo "traces" panel type being fundamentally unsupported by this Grafana version's backend query path — a genuine upstream Grafana limitation, not a config error) caught during subsequent user-driven testing and fixed the same session.

**Completion Checklist:**

- [x] All 11 files from the manifest completed
- [x] `FinPulse.Api` verified via real .NET 10 compiler (0 errors, 0 warnings)
- [x] All 6 acceptance tests verified live against a running stack (not inferred)
- [x] No blocking issues in this feature's own code
- [x] Two follow-up items documented (datasource correlation label mismatch, collector healthcheck) — neither blocks this feature
- [x] Ready for `/ship`

---

## Next Step

**Current running state:** `database/`'s Postgres, `monitor/`'s full stack (otel-collector, prometheus, loki, tempo, grafana, postgres-exporter), and `FinPulse.Api` are all running locally from this verification session.

- Grafana: `http://localhost:3000` (admin/admin) — both new dashboards visible
- Prometheus: `http://localhost:9090`
- Tempo: `http://localhost:3200`
- API: `http://localhost:5026`

**Ready for:** `/ship .claude/sdd/features/DEFINE_OBSERVABILITY_STACK.md`

**If the optional datasource-correlation follow-up is wanted first:** a small, separately-scoped fix to `monitor/config/grafana/provisioning/datasources/datasources.yml`
