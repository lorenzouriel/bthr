# BRAINSTORM: Metrics & Alerting Redesign

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | METRICS_ALERTING_REDESIGN |
| **Date** | 2026-08-25 |
| **Author** | brainstorm-agent |
| **Status** | Ready for Define |

---

## Initial Idea

**Raw Input:** "Let's redefine the metrics for each: database and API. Let's focus on redo the metrics with real numbers and real alerts in the metrics. I don't want to just see numbers and random graphs, I want graphs that explain to me what's happening..."

**Context Gathered:**
- The two dashboards shipped in the prior `OBSERVABILITY_STACK` feature (`finpulse-api-observability.json`, `postgresql-observability.json`) are functionally correct — every panel queries real, live data (verified during that build) — but are a flat grid of disconnected stat/timeseries panels with decorative color thresholds and no narrative structure. This matches the complaint directly: "just numbers and random graphs."
- **Zero alerting exists anywhere in the stack today.** The only alerting-adjacent config is a dangling `ruler.alertmanager_url: http://localhost:9093` stub in `loki-config.yaml`, pointing at a service that was never deployed. No Alertmanager container, no Grafana alert rules, no notification channels.
- Grafana 10.3.3 (already running, unchanged version) ships with built-in "unified alerting" — alert rules, contact points, and notification policies can all be evaluated inside Grafana itself against the already-wired Prometheus/Loki datasources, with no new container needed.
- `postgres_exporter`'s default collector set (confirmed during the prior build) already exposes `pg_locks` (lock counts by mode) and `pg_settings_max_connections` — both currently unused by the dashboard, both directly relevant to a proper USE-method (Utilization/Saturation/Errors) redesign.
- `monitor/` has **no `.gitignore` at all** today — a gap that matters now because this feature introduces real credentials (SMTP) for the first time in that directory.
- `database/docker-compose.yml` does not override Postgres's `max_connections`, so the default (100) applies — confirmed via grep, not assumed.

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `monitor/config/grafana/provisioning/dashboards/json/*.json` (redesign), `monitor/config/grafana/provisioning/alerting/*.yaml` (new), `monitor/docker-compose.yml`, `monitor/.env.example`, `monitor/.gitignore`, `monitor/docs/alerting.md` | All changes scoped inside `monitor/`; `api/` and `database/` untouched |
| Relevant KB Domains | None in `.claude/kb/_index.yaml` directly cover Grafana alerting or SRE dashboard design (same gap noted in the prior `OBSERVABILITY_STACK` brainstorm) | Confidence 0.70 — Design phase should validate Grafana's provisioned-alerting YAML schema directly against live docs, the same live-verification discipline used in both prior builds |
| IaC Impact | Modify existing (`monitor/docker-compose.yml` gains `GF_SMTP_*` env vars); new provisioning directory (`monitor/config/grafana/provisioning/alerting/`) | No new containers — Grafana's built-in alerting engine is used as-is |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Alerting engine: Grafana-native alerting, or a separate Prometheus Alertmanager container? | Grafana-native alerting | No new container; alert rules live alongside the already-provisioned dashboards |
| 2 | Where should firing alerts go — Grafana UI only, or a real outbound notification? | Add a webhook/email contact point (real outbound) | Requires a contact-point configuration, not just alert-rule definitions |
| 3 | What contact point is actually available right now? | SMTP / email | Rules out Slack/Discord/generic webhook for this pass |
| 4 | Which SMTP provider? | Gmail / Google Workspace | `smtp.gmail.com:587`, app-specific password required — credentials go in a local `.env`, never in chat |
| 5 | Dashboard redesign: standard RED (API) / USE (DB) SRE framework with a health-at-a-glance row, or a custom FinPulse-specific narrative? | RED/USE + health-at-a-glance row | Confirms an industry-standard, well-evidenced structure over a bespoke one |
| 6 | Do the proposed 6 alert rules (3 API: error rate, latency, target-down; 3 DB: connection saturation, cache hit ratio, exporter-down) cover what matters? | Yes, as proposed | Locks in the exact alert list for Define/Design |

**Minimum Questions:** 3 ✅ (6 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Input files | `monitor/config/grafana/provisioning/dashboards/json/finpulse-api-observability.json`, `postgresql-observability.json` | 2 files | Existing, live-verified dashboards being redesigned in place, not replaced from scratch |
| Output examples | N/A | — | No existing Grafana-alerting-YAML example in this repo to mirror; standard SRE RED/USE dashboard conventions (Google's SRE book, Grafana's own guidance) used as the structural reference instead |
| Ground truth | N/A | — | Local-dev-only; thresholds derived from standard SRE practice, not a production SLO document (none exists) |
| Related code | `database/docker-compose.yml` (confirmed no `max_connections` override, so Postgres default of 100 applies), `monitor/docs/*.md` (existing per-config documentation pattern to follow for the new `alerting.md`) | 2 files | Grounds the connection-saturation alert's denominator and the new doc's format |

**How samples will be used:**

- The two existing dashboard JSON files are edited in place — their currently-verified-working panel queries are preserved and re-labeled into RED/USE sections rather than being rewritten from zero.
- `monitor/docs/otel-config.md`/`prometheus.md`/`loki-config.md`/`tempo-config.md`'s existing format (purpose, config walkthrough, table-based field reference) is the template for the new `monitor/docs/alerting.md`.

---

## Approaches Explored

### Approach A: Grafana-native alerting ⭐ Recommended

**Description:** Define alert rules, a contact point, and a notification policy entirely within Grafana's own provisioning system (`monitor/config/grafana/provisioning/alerting/*.yaml`), evaluated against the Prometheus/Loki datasources already wired up. No new container.

**Pros:**
- Zero new infrastructure — Grafana 10.3.3 already ships unified alerting, enabled by default
- Alert rules live next to the dashboards they relate to, in the same provisioning tree, using the same GitOps-style YAML pattern already established for datasources/dashboards
- One fewer moving part to keep in sync (no separate Alertmanager routing config that could drift from Grafana's own alert state)

**Cons:**
- Less standard in pure-Prometheus-only shops that don't use Grafana as the primary alerting surface

**Why Recommended:** Confirmed directly by the user. This repo already treats Grafana as the single pane of glass (dashboards, Explore, datasource correlation) — putting alerting there too keeps one mental model instead of two.

---

### Approach B: Prometheus Alertmanager

**Description:** A dedicated Alertmanager container, Prometheus recording/alerting rules (`monitor/config/prometheus-rules.yml`), and Alertmanager's own routing/receiver config.

**Pros:**
- The "textbook" Prometheus-ecosystem pattern, useful if alerting ever needs to span multiple Prometheus instances

**Cons:**
- A new container, a new config file format, and a second place (besides Grafana) where alert state and routing live — more moving parts for a single-instance local-dev stack

**Why Not Recommended:** No multi-Prometheus-instance need exists here; the added infrastructure and second alerting surface aren't justified for this stack's actual scale.

---

### Approach C: RED (API) / USE (DB) redesign with a health-at-a-glance row ⭐ Recommended

**Description:** Restructure both dashboards around the standard SRE framework — Rate/Errors/Duration for the request-driven API, Utilization/Saturation/Errors for the resource-constrained database — each opening with a top row of 3-4 large status panels colored using the *same thresholds as the alert rules*, so the dashboard and the alerts never disagree about what "healthy" means. Every panel gains a description explaining what a bad value means, not just what it measures.

**Pros:**
- Industry-standard, well-evidenced pattern — anyone who has done on-call before recognizes it immediately
- Health row directly answers "is everything OK" before the user has to interpret 8 separate graphs
- Panel thresholds and alert thresholds sourced from the same values — no risk of a green dashboard while an alert is actually firing

**Cons:**
- More design work per panel (description text, consistent threshold sourcing) than a flat metric grid

**Why Recommended:** Directly answers the "graphs that explain what's happening" complaint — RED/USE is specifically the pattern designed to answer that question, rather than a bespoke structure invented for this app alone.

---

### Approach D: Custom narrative per business concern

**Description:** Design panels around FinPulse-specific concerns ("can users log in right now," "is expense-tracking degraded") instead of the generic SRE framework.

**Pros:**
- Potentially more directly meaningful to a non-SRE reader

**Cons:**
- Significantly more design effort (defining what "expense-tracking is degraded" even means, metric-wise) for a local-dev-scale app with no real users yet
- Diverges from standard on-call practice, making it harder to reason about later if this app ever gets a real on-call rotation

**Why Not Recommended:** The user confirmed the standard framework; custom narrative work is deferred as a possible future refinement once real usage patterns exist to design around.

---

## Data Engineering Context

Not applicable — this is an observability/alerting redesign, not a data pipeline.

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A (Grafana-native alerting) + Approach C (RED/USE redesign with health row) |
| **User Confirmation** | 2026-08-25, via direct selection for both |
| **Reasoning** | Zero new infrastructure, one consistent mental model (Grafana as the single pane of glass), and a redesign that directly answers the "explain what's happening" complaint using proven SRE practice rather than inventing a new structure |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | Grafana-native alerting, no new container | Zero new infrastructure; keeps one alerting surface | Prometheus Alertmanager |
| 2 | SMTP (Gmail) contact point, real outbound notification | Confirmed available and desired by the user, over UI-only visibility | Grafana-UI-only alert visibility; a dummy local test-webhook receiver |
| 3 | RED (API) / USE (DB) redesign with a shared health-at-a-glance row, panel thresholds sourced from the same values as alert thresholds | Directly answers "explain what's happening"; industry-standard, avoids dashboard/alert disagreement | Custom per-business-concern narrative dashboards |
| 4 | 6 alert rules: API (5xx rate > 5%/5m, p95 > 1s/5m, target down/1m), DB (connection usage > 80% of `max_connections`, cache hit ratio < 90%/15m, exporter down/1m) | User-confirmed list, each tied to a metric already flowing through the stack | Advanced multi-window multi-burn-rate alerting (Google SRE's more sophisticated pattern) |
| 5 | New `monitor/.gitignore` protecting a new `monitor/.env` (SMTP credentials) | `monitor/` currently has no `.gitignore` at all, and this is the first time real credentials enter that directory | Leaving credentials uningored, or reusing `database/.env`/`api/.env` (wrong directory, wrong secret) |
| 6 | Add a `pg_locks`-based Lock Contention panel to the Postgres dashboard | `postgres_exporter`'s default collectors already expose this metric (confirmed during the prior build), and it's a genuine USE-method saturation signal the current dashboard has no panel for at all | Leaving lock contention unmonitored |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Prometheus Alertmanager container | Confirmed out of scope — Grafana-native alerting covers the same need with less infrastructure | Yes, if multi-Prometheus-instance alerting is ever needed |
| Multi-channel notifications (Slack + email simultaneously) | Confirmed out of scope — SMTP only, per the user's available contact point | Yes, as an additional contact point later |
| Custom per-business-concern dashboard narrative | Confirmed out of scope — RED/USE chosen instead | Yes, once real usage patterns exist to design around |
| Advanced multi-window burn-rate alerting (Google SRE's sophisticated SLO-burn pattern) | Overkill for local-dev traffic volume; simple single-window threshold alerts are legible and sufficient here | Yes, if this ever runs in production with real SLOs |
| A dummy local test-webhook receiver container | Not needed — the user has real Gmail SMTP credentials available | N/A — superseded by the real contact point |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| Concrete redesign description (health row, RED/USE structure, alerting mechanism) | ✅ | Confirmed correct | No |
| File-level plan (9 files across dashboards, alerting config, compose, docs, gitignore) | ✅ | Confirmed correct | No |

**Minimum Validations:** 2 ✅ (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)
The two observability dashboards shipped in the prior feature show real, live data but no narrative — every panel is a disconnected number or graph with no indication of whether it's healthy, and there is no alerting anywhere in the stack, so a real problem (high error rate, DB connection exhaustion, cache degradation) would go completely unnoticed unless someone happened to be looking at the right panel at the right time.

### Target Users (Draft)
| User | Pain Point |
|------|------------|
| Developer checking system health | Has to mentally interpret 8+ raw graphs to answer "is everything OK," with no single glance-able answer |
| Developer who isn't actively watching the dashboard | Has no way to find out about a real problem (error spike, connection exhaustion, cache collapse) except by coincidentally noticing it |

### Success Criteria (Draft)
- [ ] Both dashboards open with a health-at-a-glance row (3-4 panels) that answers "is this healthy" before any detail panel
- [ ] Every panel has a description explaining what a bad value means, not just what it measures
- [ ] 6 Grafana-native alert rules are provisioned and evaluating live: 3 API (error rate, latency, target-down), 3 DB (connection saturation, cache hit ratio, exporter-down)
- [ ] Triggering a real condition (e.g. stopping `postgres-exporter`) causes the corresponding alert to transition to `Firing` in Grafana's Alerting page within the rule's evaluation interval
- [ ] A firing alert sends a real email via the configured Gmail SMTP contact point
- [ ] `monitor/.env` (real SMTP credentials) is git-ignored; `monitor/.env.example` documents the required variables with placeholders only

### Constraints Identified
- No new containers — Grafana's built-in alerting engine only
- SMTP credentials must never appear in committed files or chat — local `.env` only, following the exact pattern already used in `database/` and `api/`
- Alert thresholds and dashboard panel thresholds must be sourced from the same values (no drift between "looks green" and "would page you")

### Out of Scope (Confirmed)
- Prometheus Alertmanager
- Slack/Discord/generic webhook contact points (SMTP only, for this pass)
- Custom per-business-concern dashboard narrative
- Advanced multi-window burn-rate alerting
- A dummy local test-webhook receiver
- Any changes to `api/` or `database/` — this is entirely a `monitor/` redesign

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 6 |
| Approaches Explored | 4 (2 decision points, A/B and C/D) |
| Features Removed (YAGNI) | 5 |
| Validations Completed | 2 |
| Duration | Single session |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_METRICS_ALERTING_REDESIGN.md`
