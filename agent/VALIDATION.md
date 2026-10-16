# Implementation and validation status

2026-10-14. Source implementation is present; release/live acceptance is incomplete.

## Implemented

- V23 adds only nullable, uniquely indexed `users.telegram_user_id`. Existing V2/V12 SQL is unchanged; no tables or meal columns were added.
- Authenticated QR/phone-assisted linking and web approval, temporary challenges, unlink, and per-request binding checks. The arbitrary-user bot token endpoint was removed. The service uses a restricted authentication scheme rather than delegated JWTs, eliminating stale-token access after unlink.
- Python FastAPI/aiogram ingress and polling, bounded in-memory workflows, LangGraph routing, LangChain model adapters, typed API catalog, pagination, CRUD, owner checks, deletion confirmations, and API xmin checks for agent writes.
- Completed update/delete coverage for personal records, substance logs and symptom logs, including web controls and API tests.
- Deterministic per-domain reviews and per-resource numeric summaries with bounded periods and unit/currency grouping. No arbitrary SQL tool.
- Photo validation/normalization, vision extraction into existing meal fields, private API photo storage, same-meal upload retries and deleted-photo reconciliation.
- Claude/GPT/Ollama/compatible profile configuration, explicit local-only policy, no implicit retries/fallbacks, normalized token accounting, unknown pricing handling and sanitized OTLP signals.
- Agent Grafana dashboard, optional Compose overlay, persistent completed-photo volume, Prometheus alert rules and raw Docker runbook.

## Checks completed

- API: **410 passed, 1 skipped**. New tests cover linking approval/replay, contact matching, service-key validation, binding revocation, blocked administrative routes, catalog coverage, and ownership/deletion for the three newly editable resources.
- Web: **7 tests passed**; TypeScript and production Vite build passed after the final UI/resource changes.
- Python 3.12 syntax compilation passed for source/tests.
- Standalone Python cost arithmetic tests: **6 passed** (cache subsets, reasoning subsets, unknown/invalid data and zero usage).
- Base/local/agent Compose configuration passed structural validation using placeholder agent secrets; this does not validate containers or telemetry ingestion.
- `git diff --check` passed; no PowerShell files added.

## Blocked or not yet verified

1. `uv sync --python 3.12` was rejected by automatic approval review because the account usage limit was reached. The action was not retried through a workaround. A retry-approval question is pending. Consequently **uv.lock is not generated**, dependency-backed pytest/Ruff/mypy and actual Python service import/startup have not run. The Dockerfile currently installs pyproject version ranges and must be converted to a frozen lockfile install after resolution succeeds.
2. Docker engine access returns OS permission denied. No new container startup, binary backend validation, migration application, real PostgreSQL concurrency test, live Grafana query/correlation, or volume-restart acceptance has run. The optional PostgreSQL integration test is skipped, not passed.
3. Telegram credentials and configured model endpoints have not been exercised. No real messages were sent, no bot webhook was changed, and no cloud/local model calls were made. Adapter fixtures are written but await the dependency-backed test run.
4. Model pricing starts empty by design; cloud cost estimates are unknown until reviewed exact-model rates are configured. No guessed prices or benchmark nutrition-accuracy claims are included.
5. Meal extraction currently uses the selected vision model's estimate; a separate nutrition lookup integration and weighed-meal evaluation are not implemented. Pending uploads are held in memory, not staged durably. Completed API uploads are persistent; interrupted pending work can require resubmission.
6. Dashboard queries follow expected collector-exported metric names; actual series names, alert expressions, log labels and Tempo filtering still need live verification. External alert notification delivery is not configured.

## Next steps to finish acceptance

After retry approval/access is restored: resolve dependencies, fix any runtime/type/test failures, commit the generated lockfile and use it in Docker. Run the full agent suite and provider/local-only fixture checks. Restore Docker access, run the optional PostgreSQL suite, configure bot/model credentials and rates, start the overlay, then verify linking, all CRUD actions, meal/image persistence and telemetry end to end. Evaluate meal estimates against consented reference photos with known portions before claiming nutrition quality.
