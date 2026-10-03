# Web application

Run `npm ci`, then `npm run dev`. Vite serves port 5173 and proxies `/api` to the API at `http://localhost:5026`. Start the API using its HTTP launch profile and the configuration described in `../api/README.md`. The local HTTP profile uses non-secure development cookies; production retains secure cookies.

`npm run build` checks TypeScript and builds the app. `npm test` verifies API route coverage, form serialization, date round trips, and HTTP error/session handling.

For deployment, serve `dist` with SPA fallback to `index.html` and proxy `/api` to the backend on the same origin. Alternatively, set `VITE_API_BASE_URL` at build time and configure credentialed CORS for that exact frontend origin in the API/reverse proxy. Never put secrets in Vite variables.

## Connected paths

- `/login`, `/register`: cookie authentication and return to the requested page.
- `/finance`, `/body`, `/mind`: dashboards; `/wellbeing` remains a compatibility path for Mind.
- `/<section>/<resource>`: all 18 configured API resources, with create/edit/delete only where the API supports them. Goals, budgets, and investments require plan 1.
- `/account`: account details and password change.
- `/reports`: date-range review, with explicit consent to combine domains.
- `/admin/users`: administrator-only list, detail, edit, and delete.
- Unknown paths show a missing-page message.

Bot token issuance and health checks are infrastructure endpoints, not user-facing actions. Password reset, plan purchases, and bill payment actions have no API endpoints and are not presented as working features. Some existing update endpoints treat null as ?leave unchanged,? so clearing optional fields is subject to backend semantics.
