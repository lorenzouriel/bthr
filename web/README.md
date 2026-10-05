# Bthr web

The new React UI is connected to the existing ASP.NET API using HTTP-only session cookies. The Figma layout, colors, domain navigation, panels, and quick-entry drawer are retained; sample metrics and placeholder rows are replaced with API records.

## Local development

Run `npm ci`, then `npm run dev`. The default port is 5173. Set `PORT=5174` if 5173 is occupied. `/api` requests are proxied to `http://localhost:5026`; override that target with `API_PROXY_TARGET`.

Start the API with its HTTP launch profile. This workstation's populated local demo database is `bthr_api_dev` (schema v27). Override `ConnectionStrings__DefaultConnection` when launching the API to select it. The main database's v28-v30 migrations remove columns still required by the current API; this UI does not resolve that separate schema mismatch.

Run `npm run build` for TypeScript validation and production output, and `npm test` for API contract, request serialization, session, and metric checks.

## Connected screens

- `/overview`: recorded finance/activity totals, savings goals, and persistent daily habit completion.
- `/finance`: cash flow by selected currency and month/year, category spending, budgets, bills, and goals.
- `/body`: workouts, nutrition, water, and recorded sleep.
- `/mind`: meditation, mood, and journal history.
- Domain subsections use shareable paths, e.g. `/finance/transactions/earnings`, `/body/training/personal-records`, `/body/habits/habit-logs`, and `/mind/journal`.
- `/reports`: date-range reports, preceding-period comparisons, explicit opt-in to combine domains, and JSON export.
- `/settings`: account information, password change, and logout.
- Login and registration are shown when a session is missing; the requested path is preserved. `/login` and `/register` also work.

All 19 resource types support creation and listing. Edit/delete buttons follow the actual controller capabilities: personal records, substance logs, and symptoms are add/view-only. Goals, budgets, and investments require plan 1. Some existing PUT endpoints treat null as unchanged, so clearing optional values is subject to backend behavior.

No invented sleep stages, wellness scores, nutritional targets, bank accounts, or causal insights are displayed. Charts describe recorded activity; missing observations are not treated as measured zeros. Currency totals are kept separate.

## Deployment

Serve `dist` with a fallback to `index.html` for application paths and proxy `/api` to the backend on the same origin. Alternatively set `VITE_API_BASE_URL` at build time and configure credentialed CORS for the frontend's exact origin. Keep production cookies secure. Never put secrets in Vite variables.

## Verified locally

- Production build and five frontend tests passed.
- Cookie login/session/logout and all 19 list endpoints verified through the Vite proxy against the demo database.
- A temporary expense was created, updated, read back, and deleted.
- Finance, Body, Mind, and combined reports returned metrics.
- Browser automation was unavailable, so visual/browser-interaction verification was not performed.
