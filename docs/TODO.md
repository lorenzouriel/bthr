# TODO

Architecture review follow-up. Unchecked items are pending work; the validation results below describe the reviewed checkout, not production acceptance.

## High priority

- [ ] Make browser sessions revocable. Recheck account status and a session/security version so account deletion, password changes, and logout invalidate the appropriate issued tokens.
- [ ] Enforce account uniqueness in PostgreSQL for email, username, and phone. Define normalization and soft-deletion semantics, reconcile existing duplicates, and add a new migration rather than modifying applied migrations.
- [ ] Add user foreign keys to the six finance tables after checking for orphaned records.
- [ ] Add rate limiting to login and registration, with coverage for requests through the deployment proxy.
- [ ] Protect cookie-authenticated mutations against CSRF, including bodyless logout. Review whether cross-site cookies are needed and choose an explicit SameSite policy.
- [ ] Default production cookies to Secure and verify HTTPS/proxy configuration. Keep insecure cookies an explicit local-development setting.
- [ ] Persist the Telegram update inbox before acknowledging webhooks so restarts do not lose accepted messages.
- [ ] Add durable operation tracking and API idempotency keys for creates so redelivery or ambiguous responses cannot duplicate records.
- [ ] Persist the agent state needed for confirmations, recovery, and reply delivery, with expiration and retry rules.

## Data handling and API contracts

- [ ] Remove backend storage of meal images; retain only the logged meal information. Keep image processing transient for extraction, remove photo upload/read/retry endpoints and agent persistence flows, and remove the photo volume and cleanup service. Plan removal of previously saved images and update tests, configuration, and documentation. Ensure new images are not retained in logs or durable agent state.
- [ ] Add bounded pagination and filtering to browser-facing resource lists, including expenses.
- [ ] Fetch data per screen and refresh only affected collections instead of reloading all 19 resource collections after each change.
- [ ] Apply concurrency protection to browser updates and deletes, with a clear conflict/reload experience, rather than limiting row-version checks to agent requests.
- [ ] Standardize update contracts: distinguish omitted fields from explicit nulls and document partial-update versus replacement behavior.

## Release and verification

- [ ] Resolve and test the Python agent dependencies, commit a lockfile, and use a frozen dependency installation in Docker.
- [ ] Run the complete agent test suite, linting, type checks, and actual service startup with the supported Python version.
- [ ] Add test/build gates and controlled migration execution to the deployment pipeline before deployment.
- [ ] Run real PostgreSQL schema and endpoint integration tests as a release gate, including concurrent account creation and row-version conflict checks.
- [ ] Add browser end-to-end coverage for authentication, CRUD, conflict handling, reports, and Telegram linking.
- [ ] Validate Telegram linking/unlinking, webhook processing, CRUD, model adapters, and meal extraction end to end.
- [ ] Validate the live monitoring stack: configuration compatibility, ingestion, metric names, alert expressions, dashboard filters, trace/log correlation, and persistence across restarts.
- [ ] Restore dependency vulnerability metadata checks in the release environment; the review run could not reach NuGet vulnerability data.

## Operations

- [ ] Implement and test database backup and restore, with retention and recovery objectives. Account for existing photo data during the transition to logged information only; remove photo-backup requirements once image storage is retired.
- [ ] Document and exercise deployment rollback and migration recovery procedures.
- [ ] Configure external alert delivery and tune alert thresholds for the deployment.
- [ ] Move Telegram linking challenges and required agent state to shared persistent storage before supporting multiple API or agent replicas.
- [ ] Address the single agent worker's throughput limit while preserving per-user ordering and write safety.
- [ ] Use a restricted production database application role and document secret rotation.

## Account lifecycle and optional product capabilities

- [ ] Implement password recovery.
- [ ] Implement full personal-data export.
- [ ] Implement self-service account deletion with explicit retention and erasure behavior.
- [ ] Decide whether banking synchronization is in scope; implement it or remove claims that it exists.
- [ ] Decide whether automatic recurring bill generation is in scope; current recurrence fields are metadata only.
- [ ] Decide whether scheduled coaching is in scope and, if so, provide durable scheduling and delivery.
- [ ] Decide whether to add nutrition lookup; evaluate meal estimates against known portions before claiming accuracy.
- [ ] Configure reviewed exact-model pricing where cloud cost estimates are needed, preserving unknown pricing as unknown.
- [ ] Decide whether a durable spend cap and a permanent mutation audit trail are required.

## Documentation

- [ ] Add a canonical architecture/integration overview describing component boundaries, data flows, trust boundaries, deployment topology, and implemented versus planned capabilities. `INTEGRATIONS.md` was not found in the reviewed checkout.
- [ ] Correct the database README's banking integration claims and outdated setup/schema references.
- [ ] Update the API README to reflect implemented readiness checks and current Telegram authentication; remove or replace the missing `PROJECT_AUDIT.md` reference.
- [ ] Update the web README to reflect editing/deletion support for personal records, substance logs, and symptom logs.
- [ ] Document the single-instance constraint until shared durable state is implemented.
- [ ] Update all photo-related documentation to reflect transient processing and retention of logged information only after that change is implemented.

## Review validation baseline

- Frontend: 7 tests passed; production build passed.
- API: 410 tests passed; 1 real PostgreSQL integration test skipped.
- NuGet vulnerability metadata was unavailable during the review run.
- Agent tests, live infrastructure, Telegram/model interactions, and production acceptance were not validated during this review.

The current component boundaries remain appropriate: React and the Telegram agent use the ASP.NET API; the API owns business rules and persistence; Flyway owns schema migrations. Prioritize the high-priority items before expanding integrations or deployment scale.
