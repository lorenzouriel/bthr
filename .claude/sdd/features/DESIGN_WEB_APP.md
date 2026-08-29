# DESIGN: Web App (Meridian SPA wired to FinPulse.Api)

> Technical design for a new React + Vite SPA — visually modeled on `app/Meridian.dc.html` — that authenticates against and performs full CRUD through the live `FinPulse.Api` for all 15 built resources.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | WEB_APP |
| **Date** | 2026-08-26 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_WEB_APP.md](./DEFINE_WEB_APP.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌───────────────────────────────────────────────────────────────────────────┐
│                         BROWSER — web/ (Vite :5173)                       │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  [Login/Register] ──POST /api/auth/*──▶ cookie set ──▶ [AuthContext]      │
│                                                              │              │
│                                            GET /api/auth/me on mount       │
│                                                              ▼              │
│  [App Router] ──▶ [ProtectedRoute] ──▶ [AppLayout: Sidebar + <Outlet/>]   │
│                                                              │              │
│                            /:section/:resourceKey            │             │
│                                                              ▼              │
│                              [ResourceSectionPage]                        │
│                          (looks up resources.ts config)                   │
│                              /              \                             │
│                    [ResourceList]      [ResourceForm]                     │
│                    GET  {basePath}     POST/PUT {basePath}                │
│                                          DELETE {basePath}/{id}            │
│                                                              │              │
└──────────────────────────────────────────────────────────────┼───────────┘
                                                                 │
                                        credentials:'include' (access_token cookie)
                                                                 ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                    FINPULSE.API — http://localhost:5026                   │
│         (unchanged except one dev-only config fix — Decision 1)          │
│  finance/* (6) · body/* (7) · mind/* (2) — all already live-verified     │
└───────────────────────────────────────────────────────────────────────────┘
```

15 resources are driven by **one** config-driven pair of components (`ResourceList` + `ResourceForm`), not 15 hand-written screens — see Decision 2.

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| `appsettings.Development.json` (new, backend) | Fixes the `Cookie:Secure` dev-blocking bug from DEFINE's Constraint | ASP.NET Core config |
| `theme/theme.css` + `ThemeContext` | Porcelain/Ink/Dusk CSS variables ported from Meridian's `themeVars()` | CSS custom properties, React Context |
| `api/client.ts` | Thin `fetch` wrapper: base URL, `credentials: 'include'`, JSON parse, centralized 401 handling | Fetch API |
| `api/auth.ts` | `register`/`login`/`logout`/`me` calls | Fetch API |
| `types/dto.ts` | TypeScript interfaces mirroring all 15 resources' C# DTOs + auth DTOs | TypeScript |
| `config/resources.ts` | Data-driven definition of all 15 resources: route, fields, list columns, edit/delete capability | TypeScript |
| `auth/AuthContext.tsx` | Current-user state, session restore via `GET /api/auth/me` on mount, login/logout actions | React Context |
| `auth/ProtectedRoute.tsx` | Redirects to `/login` when unauthenticated | React Router |
| `pages/Login.tsx` / `Register.tsx` | Real auth forms | React |
| `components/AppLayout.tsx` + `Sidebar.tsx` | Ported Meridian sidebar nav (sections → resources) + outlet | React |
| `components/ResourceList.tsx` | Generic list table/cards for any resource, driven by config | React + TanStack Query |
| `components/ResourceForm.tsx` | Generic create/edit form for any resource, driven by config field defs | React + TanStack Query |
| `pages/ResourceSectionPage.tsx` | Reads `:section/:resourceKey` route params, renders `ResourceList` + `ResourceForm` for that resource | React Router |
| `App.tsx` | Route tree | React Router |

---

## Key Decisions

### Decision 1: Fix `Cookie:Secure` for local dev via a new `appsettings.Development.json`

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-26 |

**Context:** DEFINE flagged a blocking constraint: `AuthController.BuildCookieOptions()` reads `Cookie:Secure` from config with a default of `true`, and `Program.cs` reads the JWT **exclusively** from the `access_token` cookie (`OnMessageReceived` → `context.Request.Cookies["access_token"]`, no header fallback). No `appsettings.Development.json` exists, and `launchSettings.json` only defines an `http://` profile (`http://localhost:5026`). A `Secure` cookie is never persisted by a browser over plain HTTP — every login would appear to succeed (200) while the browser silently discards the cookie, breaking every subsequent authenticated call.

**Choice:** Add `api/FinPulse.Api/appsettings.Development.json`:

```json
{
  "Cookie": {
    "Secure": false
  }
}
```

ASP.NET Core automatically layers `appsettings.{Environment}.json` over `appsettings.json`, and `ASPNETCORE_ENVIRONMENT=Development` is already set in `launchSettings.json`'s `http` profile — no other change needed. With `Secure=false`, `BuildCookieOptions()` sets `SameSite=Lax` (per its existing ternary), which is valid over plain HTTP and sent correctly for same-site cross-port requests (`localhost:5173` → `localhost:5026` are different ports but the same site for `SameSite` purposes).

**Rationale:** This is a one-line, config-only, environment-scoped fix — it changes zero routes, zero logic, and has zero effect outside `Development`. It falls squarely within DEFINE's carve-out ("any bug found is logged, not silently worked around... unless a genuine bug is discovered") — DEFINE explicitly named this exact fix as the required, blocking first step. **Build must live-verify this**: register/login in a real browser, inspect DevTools → Application → Cookies, and confirm `access_token` is actually present after login (not just that the HTTP response was 200).

**Alternatives Rejected:**
1. Run the API over HTTPS locally via the ASP.NET dev cert (`dotnet dev-certs https --trust` + an `https` launch profile) — rejected as a heavier, machine-specific setup change (trusting a local cert) for a problem a one-line config file solves.
2. Have the SPA send the token via an `Authorization: Bearer` header instead of relying on the cookie — rejected: the JWT middleware has no header-reading code path at all (confirmed by reading `Program.cs`), so this would require an actual backend logic change, not just config — larger and riskier than Decision 1's fix.

**Consequences:**
- This is the only backend file this feature touches, and it does not affect Production (`appsettings.json`'s `Cookie:Secure` default of `true` is untouched).

---

### Decision 2: One config-driven `ResourceList`/`ResourceForm` pair for all 15 resources, not 15 hand-written screens

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-26 |

**Context:** The backend explicitly rejected a shared generic base for its 13 (now 15) resource Controllers/Services during `BODY_MODULE_API`'s design, in favor of per-resource files matching 6 pre-existing resources' convention. A naive reading might expect the same choice here.

**Choice:** The frontend does the opposite: **one** generic, config-driven `ResourceList` + `ResourceForm` component pair, parameterized by a `ResourceConfig` object (route, field definitions, list columns, `hasEdit`/`hasDelete` flags) defined once per resource in `config/resources.ts`.

**Rationale:** The backend precedent and this decision are not in tension — they optimize for different things because the situations differ:
- The backend's "no shared base" choice matched **6 pre-existing, already-hand-written files' convention** — consistency with existing code was the driver, not a generic principle against abstraction.
- The frontend has **zero pre-existing files** (confirmed in DEFINE: KB confidence 0.75, "genuinely novel for this repo") — there is no existing convention to preserve. Building 15 near-identical hand-written screens (list + create form + edit form + delete action, ×15) would mean ~45 structurally-duplicated pieces of UI for zero benefit: any bug in the delete-confirmation flow, the loading-state pattern, or the error-toast pattern would need fixing in up to 15 places instead of one.
- React's whole value proposition for a dashboard like this is component reuse; a config-driven list/form pair is standard, idiomatic React engineering for a "many similar CRUD resources" problem — not scope creep, but the responsible default absent a reason to hand-roll.

**Alternatives Rejected:**
1. 15 hand-written page components (mirroring the backend's per-resource philosophy literally) — rejected: no existing frontend convention forces this, and the duplication cost is real and large (~45 near-identical pieces).
2. A heavier "admin panel generator" library (e.g., React Admin) — rejected: pulls in an entire framework's opinions (its own routing, its own data-provider abstraction) for a 15-resource internal app; DEFINE's SHOULD/COULD goals (DTO-mirrored validation, loading states) are simple enough to hand-roll in the thin config-driven components described here.

**Consequences:**
- Resource-specific behavior (Personal Records has no edit/delete; Sleep Logs has a read-only computed `totalHours`; Wellbeing mood fields are optional) is expressed as **data** in `resources.ts` (`hasEdit: false`, a field's `readOnly: true`, a field's `required: false`), not as one-off component code — see Pattern 2.
- Every resource still gets its own real route (`/:section/:resourceKey`) and its own entry in `resources.ts` — nothing is hidden behind hidden magic; the config file is the single source of truth Build/Design/anyone reads to understand what exists.

---

### Decision 3: Session handled via React Context + `GET /api/auth/me` on mount, not localStorage-stored tokens

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-26 |

**Context:** The JWT lives in an `httpOnly` cookie (`AuthController.BuildCookieOptions()` sets `HttpOnly = true`) — JavaScript cannot read it, by design (XSS protection). DEFINE requires session restore on page refresh (AT-004).

**Choice:** `AuthContext` holds `{ user, isLoading, login, register, logout }` in React state only (no token storage anywhere in JS). On mount, it calls `GET /api/auth/me`; a 200 populates `user`, a 401 leaves `user = null`. `ProtectedRoute` reads `isLoading`/`user` from this context and redirects to `/login` only after the initial `me` call resolves (never redirects while `isLoading`).

**Rationale:** The cookie already handles persistence and transport securely; re-implementing that in `localStorage` would be both redundant and worse (XSS-readable). `GET /api/auth/me` is the API's own designed mechanism for "who am I right now" — using it directly is the simplest correct approach, confirmed live-verifiable per AT-004.

**Alternatives Rejected:**
1. Store the JWT in `localStorage`/`sessionStorage` after login and attach it as a Bearer header — impossible here: the middleware never reads a header (Decision 1's alternative-rejected #2), and the cookie is `httpOnly` so JS can't even read it to store it.
2. Skip session restore, always require fresh login on every page load — rejected: directly contradicts DEFINE's AT-004 (session survives refresh).

**Consequences:**
- Every route transition after initial load doesn't need to re-check auth — `AuthContext`'s `user` is the single source of truth for the whole app's session lifetime.

---

### Decision 4: `client.ts` centralizes fetch config and 401 handling; no separate per-resource fetch logic

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-26 |

**Context:** DEFINE's SHOULD goal: a shared API client for base URL, `credentials:'include'`, JSON parsing, and 401-redirect handling, instead of each screen reimplementing it (AT-005 — session loss must redirect to login from anywhere in the app).

**Choice:** One `apiFetch<T>(path, options)` function in `api/client.ts`: prefixes `http://localhost:5026`, always sets `credentials: 'include'`, parses JSON, and on a `401` response triggers a single shared "session expired" callback (wired to `AuthContext`'s logout + redirect) before rejecting. `ResourceList`/`ResourceForm`'s TanStack Query hooks call `apiFetch` exclusively — no component calls `fetch` directly.

**Rationale:** Matches DEFINE's SHOULD goal directly; centralizing the 401 → redirect behavior in one place is the only way to satisfy AT-005 ("session loss redirects to login from anywhere") without repeating that check in every one of the ~15 resources' query/mutation hooks.

**Alternatives Rejected:**
1. Let TanStack Query's global `QueryCache`/`MutationCache` `onError` handle 401s instead of `client.ts` — considered, but `client.ts` already needs to inspect the response status to parse errors correctly either way; doing it once at the fetch layer is simpler than duplicating status-code awareness in both layers.

**Consequences:**
- Every resource's data-fetching hook is a thin wrapper: `useQuery({ queryKey: [...], queryFn: () => apiFetch(...) })` — no resource-specific networking code exists anywhere except the URL and shape, both of which live in `resources.ts`/`dto.ts`.

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `api/FinPulse.Api/appsettings.Development.json` | Create | Fix `Cookie:Secure` for dev (Decision 1) | (general) | None |
| 2 | `web/package.json` | Create | Vite + React + TS + TanStack Query + React Router deps | (general) | None |
| 3 | `web/vite.config.ts` | Create | Vite config, port 5173 | (general) | 2 |
| 4 | `web/tsconfig.json` | Create | TypeScript config | (general) | 2 |
| 5 | `web/index.html` | Create | Vite entry HTML | (general) | 2 |
| 6 | `web/src/main.tsx` | Create | React root, QueryClientProvider, BrowserRouter | (general) | 5 |
| 7 | `web/src/theme/theme.css` | Create | Porcelain/Ink/Dusk CSS variables, ported from Meridian's `themeVars()` | (general) | None |
| 8 | `web/src/theme/ThemeContext.tsx` | Create | Theme selection state | (general) | 7 |
| 9 | `web/src/types/dto.ts` | Create | TS interfaces for all 15 resources + auth DTOs | (general) | None |
| 10 | `web/src/api/client.ts` | Create | Thin fetch wrapper, 401 handling (Decision 4) | (general) | None |
| 11 | `web/src/api/auth.ts` | Create | register/login/logout/me calls | (general) | 9, 10 |
| 12 | `web/src/config/resources.ts` | Create | Data-driven config for all 15 resources (Decision 2) | (general) | 9 |
| 13 | `web/src/auth/AuthContext.tsx` | Create | Session state + restore (Decision 3) | (general) | 11 |
| 14 | `web/src/auth/ProtectedRoute.tsx` | Create | Auth-gated route wrapper | (general) | 13 |
| 15 | `web/src/pages/Login.tsx` | Create | Login form (AT-002, AT-003) | (general) | 13 |
| 16 | `web/src/pages/Register.tsx` | Create | Register form (AT-001) | (general) | 13 |
| 17 | `web/src/components/Sidebar.tsx` | Create | Ported Meridian nav (sections → resources) | (general) | 12 |
| 18 | `web/src/components/AppLayout.tsx` | Create | Sidebar + `<Outlet/>` shell | (general) | 17 |
| 19 | `web/src/components/ResourceList.tsx` | Create | Generic list, TanStack Query (Decision 2) | (general) | 10, 12 |
| 20 | `web/src/components/ResourceForm.tsx` | Create | Generic create/edit form, TanStack Query (Decision 2) | (general) | 10, 12 |
| 21 | `web/src/pages/ResourceSectionPage.tsx` | Create | Reads route params, renders List+Form for the matched resource | (general) | 19, 20 |
| 22 | `web/src/App.tsx` | Create | Route tree | (general) | 14, 15, 16, 18, 21 |

**Total Files:** 22 (22 create, 0 modify — this is a new app, nothing existing to modify except the one new backend config file)

---

## Agent Assignment Rationale

> Agents discovered from `.claude/agents/` — Build phase invokes matched specialists.

| Agent | Files Assigned | Why This Agent |
|-------|----------------|-----------------|
| (general) | All 22 | No specialist agent in `.claude/agents/` matches React/Vite/TypeScript SPA code (the roster is data-engineering-focused: `schema-designer`, `dbt-specialist`, `airflow-specialist`, etc. — none cover frontend application code, matching the same conclusion reached during `BODY_MODULE_API` and `MIND_MODULE`). Build handles all 22 files directly, following the code patterns below. |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md`
- Matched by: File type, purpose keywords, path patterns, KB domains — no match found for `.tsx`/`.ts` frontend files

---

## Code Patterns

### Pattern 1: DTO types and resources config (the data-driven core)

**`types/dto.ts`** — every field name/type mirrors its C# DTO exactly (verified by reading all 15 `*DTOs.cs` files during this Design session):

```typescript
export interface BaseFields { id: number; userId: number; status: number; createdAt: string; }

// Finance
export interface Goal extends BaseFields { name: string; description?: string; targetAmount: number; currentAmount: number; currencyCode: string; dueDate: string; }
export interface Bill extends BaseFields { name: string; description?: string; category: string; amount: number; dueDay: number; paymentMethod?: string; currencyCode: string; isRecurrent: boolean; endDate?: string; recurrenceType?: string; dueDate: string; paidThisMonth: boolean; paidDate?: string; }
export interface Budget extends BaseFields { name: string; description?: string; amountLimit: number; currencyCode: string; startDate: string; endDate: string; }
export interface Earning extends BaseFields { category: string; paymentMethod: string; currencyCode: string; amount: number; description?: string; earningDate: string; }
export interface Expense extends BaseFields { category: string; paymentMethod: string; currencyCode: string; amount: number; description?: string; expenseDate: string; }
export interface Investment extends BaseFields { investmentType: string; category: string; assetName: string; broker?: string; currencyCode: string; investedAmount: number; currentValue?: number; purchaseDate: string; maturityDate?: string; annualYieldPercent?: number; profitLoss?: number; }

// Body
export interface WeeklyRoutine extends BaseFields { dayOfWeek: number; routineName: string; description?: string; }
export interface Workout extends BaseFields { workoutDate: string; routineName: string; durationMinutes?: number; caloriesBurned?: number; notes?: string; }
export interface PersonalRecord extends BaseFields { exerciseName: string; metricType: string; value: number; unit: string; achievedDate: string; notes?: string; }
export interface Meal extends BaseFields { mealDate: string; mealType: string; description?: string; calories: number; proteinGrams?: number; carbsGrams?: number; fatGrams?: number; }
export interface WaterIntake extends BaseFields { intakeDate: string; amountMl: number; }
export interface BodyMetric extends BaseFields { measuredDate: string; weightKg?: number; heightCm?: number; bodyFatPercent?: number; notes?: string; }
export interface SleepLog extends BaseFields { bedTime: string; wakeTime: string; totalHours: number; notes?: string; }

// Wellbeing (mind schema)
export interface MeditationSession extends BaseFields { sessionDate: string; durationMinutes: number; meditationType: string; moodBefore?: number; moodAfter?: number; notes?: string; }
export interface JournalEntry extends BaseFields { entryDate: string; title?: string; content: string; mood?: number; category?: string; }

// Auth
export interface AuthUser { id: number; email: string; username: string; plan: number; }
export interface RegisterRequest { username: string; phoneNumber: string; email: string; password: string; }
export interface LoginRequest { email: string; password: string; }
```

**`config/resources.ts`** — one entry per resource; `hasEdit`/`hasDelete` and per-field `required`/`readOnly` express every resource-specific rule from DEFINE (Personal Records is create+read only; Sleep Logs' `totalHours` is read-only; mood fields are optional):

```typescript
export type FieldType = 'text' | 'textarea' | 'number' | 'date' | 'datetime' | 'checkbox';

export interface FieldConfig {
  name: string;
  label: string;
  type: FieldType;
  required: boolean;
  readOnly?: boolean;      // shown in list/detail, never submitted (e.g. totalHours, dueDate, paidThisMonth)
  maxLength?: number;
}

export interface ResourceConfig {
  key: string;              // route segment, e.g. 'goals'
  section: 'finance' | 'body' | 'wellbeing';
  label: string;            // nav + heading label
  basePath: string;         // e.g. '/api/users/{userId}/goals'
  hasEdit: boolean;
  hasDelete: boolean;
  listColumns: string[];    // field names shown in the list view, in order
  fields: FieldConfig[];    // drives the create/edit form
}

export const RESOURCES: ResourceConfig[] = [
  { key: 'goals', section: 'finance', label: 'Goals', basePath: '/api/users/{userId}/goals', hasEdit: true, hasDelete: true,
    listColumns: ['name', 'targetAmount', 'currentAmount', 'dueDate'],
    fields: [
      { name: 'name', label: 'Name', type: 'text', required: true, maxLength: 100 },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 255 },
      { name: 'targetAmount', label: 'Target amount', type: 'number', required: true },
      { name: 'currentAmount', label: 'Current amount', type: 'number', required: true },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'dueDate', label: 'Due date', type: 'date', required: true } ] },

  { key: 'bills', section: 'finance', label: 'Bills', basePath: '/api/users/{userId}/bills', hasEdit: true, hasDelete: true,
    listColumns: ['name', 'category', 'amount', 'dueDay', 'paidThisMonth'],
    fields: [
      { name: 'name', label: 'Name', type: 'text', required: true, maxLength: 255 },
      { name: 'category', label: 'Category', type: 'text', required: true, maxLength: 100 },
      { name: 'paymentMethod', label: 'Payment method', type: 'text', required: false, maxLength: 100 },
      { name: 'amount', label: 'Amount', type: 'number', required: true },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'dueDay', label: 'Due day (1-31)', type: 'number', required: true },
      { name: 'isRecurrent', label: 'Recurrent', type: 'checkbox', required: false },
      { name: 'endDate', label: 'End date', type: 'date', required: false },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 500 },
      { name: 'dueDate', label: 'Computed due date', type: 'text', required: false, readOnly: true },
      { name: 'paidThisMonth', label: 'Paid this month', type: 'checkbox', required: false, readOnly: true } ] },

  { key: 'budgets', section: 'finance', label: 'Budgets', basePath: '/api/users/{userId}/budgets', hasEdit: true, hasDelete: true,
    listColumns: ['name', 'amountLimit', 'startDate', 'endDate'],
    fields: [
      { name: 'name', label: 'Name', type: 'text', required: true, maxLength: 100 },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 255 },
      { name: 'amountLimit', label: 'Amount limit', type: 'number', required: true },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'startDate', label: 'Start date', type: 'date', required: true },
      { name: 'endDate', label: 'End date', type: 'date', required: true } ] },

  { key: 'earnings', section: 'finance', label: 'Earnings', basePath: '/api/users/{userId}/earnings', hasEdit: true, hasDelete: true,
    listColumns: ['category', 'amount', 'earningDate'],
    fields: [
      { name: 'category', label: 'Category', type: 'text', required: true, maxLength: 255 },
      { name: 'paymentMethod', label: 'Payment method', type: 'text', required: true, maxLength: 255 },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'amount', label: 'Amount', type: 'number', required: true },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 255 },
      { name: 'earningDate', label: 'Earning date', type: 'date', required: true } ] },

  { key: 'expenses', section: 'finance', label: 'Expenses', basePath: '/api/users/{userId}/expenses', hasEdit: true, hasDelete: true,
    listColumns: ['category', 'amount', 'expenseDate'],
    fields: [
      { name: 'category', label: 'Category', type: 'text', required: true, maxLength: 255 },
      { name: 'paymentMethod', label: 'Payment method', type: 'text', required: true, maxLength: 255 },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'amount', label: 'Amount', type: 'number', required: true },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 255 },
      { name: 'expenseDate', label: 'Expense date', type: 'date', required: true } ] },

  { key: 'investments', section: 'finance', label: 'Investments', basePath: '/api/users/{userId}/investments', hasEdit: true, hasDelete: true,
    listColumns: ['assetName', 'investmentType', 'investedAmount', 'currentValue'],
    fields: [
      { name: 'investmentType', label: 'Type', type: 'text', required: true, maxLength: 50 },
      { name: 'category', label: 'Category', type: 'text', required: true, maxLength: 100 },
      { name: 'assetName', label: 'Asset name', type: 'text', required: true, maxLength: 100 },
      { name: 'broker', label: 'Broker', type: 'text', required: false, maxLength: 100 },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'investedAmount', label: 'Invested amount', type: 'number', required: true },
      { name: 'currentValue', label: 'Current value', type: 'number', required: false },
      { name: 'purchaseDate', label: 'Purchase date', type: 'date', required: true },
      { name: 'maturityDate', label: 'Maturity date', type: 'date', required: false },
      { name: 'annualYieldPercent', label: 'Annual yield %', type: 'number', required: false },
      { name: 'profitLoss', label: 'Profit/loss', type: 'number', required: false } ] },

  { key: 'weekly-routines', section: 'body', label: 'Weekly Routines', basePath: '/api/users/{userId}/body/weekly-routines', hasEdit: true, hasDelete: true,
    listColumns: ['dayOfWeek', 'routineName'],
    fields: [
      { name: 'dayOfWeek', label: 'Day of week (0-6)', type: 'number', required: true },
      { name: 'routineName', label: 'Routine name', type: 'text', required: true, maxLength: 100 },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'workouts', section: 'body', label: 'Workouts', basePath: '/api/users/{userId}/body/workouts', hasEdit: true, hasDelete: true,
    listColumns: ['workoutDate', 'routineName', 'durationMinutes'],
    fields: [
      { name: 'workoutDate', label: 'Workout date', type: 'date', required: true },
      { name: 'routineName', label: 'Routine name', type: 'text', required: true, maxLength: 100 },
      { name: 'durationMinutes', label: 'Duration (min)', type: 'number', required: false },
      { name: 'caloriesBurned', label: 'Calories burned', type: 'number', required: false },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'personal-records', section: 'body', label: 'Personal Records', basePath: '/api/users/{userId}/body/personal-records', hasEdit: false, hasDelete: false,
    listColumns: ['exerciseName', 'metricType', 'value', 'unit', 'achievedDate'],
    fields: [
      { name: 'exerciseName', label: 'Exercise', type: 'text', required: true, maxLength: 100 },
      { name: 'metricType', label: 'Metric type', type: 'text', required: true, maxLength: 50 },
      { name: 'value', label: 'Value', type: 'number', required: true },
      { name: 'unit', label: 'Unit', type: 'text', required: true, maxLength: 20 },
      { name: 'achievedDate', label: 'Achieved date', type: 'date', required: true },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'meals', section: 'body', label: 'Meals', basePath: '/api/users/{userId}/body/meals', hasEdit: true, hasDelete: true,
    listColumns: ['mealDate', 'mealType', 'calories'],
    fields: [
      { name: 'mealDate', label: 'Meal date', type: 'date', required: true },
      { name: 'mealType', label: 'Meal type', type: 'text', required: true, maxLength: 50 },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 500 },
      { name: 'calories', label: 'Calories', type: 'number', required: true },
      { name: 'proteinGrams', label: 'Protein (g)', type: 'number', required: false },
      { name: 'carbsGrams', label: 'Carbs (g)', type: 'number', required: false },
      { name: 'fatGrams', label: 'Fat (g)', type: 'number', required: false } ] },

  { key: 'water-intake', section: 'body', label: 'Water Intake', basePath: '/api/users/{userId}/body/water-intake', hasEdit: true, hasDelete: true,
    listColumns: ['intakeDate', 'amountMl'],
    fields: [
      { name: 'intakeDate', label: 'Date', type: 'date', required: true },
      { name: 'amountMl', label: 'Amount (ml)', type: 'number', required: true } ] },

  { key: 'body-metrics', section: 'body', label: 'Body Metrics', basePath: '/api/users/{userId}/body/body-metrics', hasEdit: true, hasDelete: true,
    listColumns: ['measuredDate', 'weightKg', 'bodyFatPercent'],
    fields: [
      { name: 'measuredDate', label: 'Measured date', type: 'date', required: true },
      { name: 'weightKg', label: 'Weight (kg)', type: 'number', required: false },
      { name: 'heightCm', label: 'Height (cm)', type: 'number', required: false },
      { name: 'bodyFatPercent', label: 'Body fat %', type: 'number', required: false },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'sleep-logs', section: 'body', label: 'Sleep Logs', basePath: '/api/users/{userId}/body/sleep-logs', hasEdit: true, hasDelete: true,
    listColumns: ['bedTime', 'wakeTime', 'totalHours'],
    fields: [
      { name: 'bedTime', label: 'Bed time', type: 'datetime', required: true },
      { name: 'wakeTime', label: 'Wake time', type: 'datetime', required: true },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 },
      { name: 'totalHours', label: 'Total hours', type: 'number', required: false, readOnly: true } ] },

  { key: 'meditation-sessions', section: 'wellbeing', label: 'Meditation', basePath: '/api/users/{userId}/mind/meditation-sessions', hasEdit: true, hasDelete: true,
    listColumns: ['sessionDate', 'meditationType', 'durationMinutes'],
    fields: [
      { name: 'sessionDate', label: 'Session date', type: 'date', required: true },
      { name: 'durationMinutes', label: 'Duration (min)', type: 'number', required: true },
      { name: 'meditationType', label: 'Type', type: 'text', required: true, maxLength: 50 },
      { name: 'moodBefore', label: 'Mood before (1-5)', type: 'number', required: false },
      { name: 'moodAfter', label: 'Mood after (1-5)', type: 'number', required: false },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'journal-entries', section: 'wellbeing', label: 'Journal', basePath: '/api/users/{userId}/mind/journal-entries', hasEdit: true, hasDelete: true,
    listColumns: ['entryDate', 'title', 'mood', 'category'],
    fields: [
      { name: 'entryDate', label: 'Entry date', type: 'date', required: true },
      { name: 'title', label: 'Title', type: 'text', required: false, maxLength: 200 },
      { name: 'content', label: 'Content', type: 'textarea', required: true },
      { name: 'mood', label: 'Mood (1-5)', type: 'number', required: false },
      { name: 'category', label: 'Category', type: 'text', required: false, maxLength: 50 } ] },
];

export const SECTIONS = [
  { key: 'finance', label: 'Finance' },
  { key: 'body', label: 'Body' },
  { key: 'wellbeing', label: 'Wellbeing' },
] as const;
```

---

### Pattern 2: API client and auth

**`api/client.ts`**:

```typescript
const API_BASE = 'http://localhost:5026';

let onUnauthorized: (() => void) | null = null;
export function setUnauthorizedHandler(handler: () => void) { onUnauthorized = handler; }

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...options.headers },
  });

  if (res.status === 401) {
    onUnauthorized?.();
    throw new Error('Unauthorized');
  }

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.message || `Request failed with status ${res.status}`);
  }

  if (res.status === 204) return undefined as T;
  return res.json();
}
```

**`api/auth.ts`**:

```typescript
import { apiFetch } from './client';
import type { AuthUser, LoginRequest, RegisterRequest } from '../types/dto';

export const authApi = {
  register: (req: RegisterRequest) => apiFetch<{ userId: number; token: string }>('/api/auth/register', { method: 'POST', body: JSON.stringify(req) }),
  login: (req: LoginRequest) => apiFetch<{ userId: number; token: string }>('/api/auth/login', { method: 'POST', body: JSON.stringify(req) }),
  logout: () => apiFetch<void>('/api/auth/logout', { method: 'POST' }),
  me: () => apiFetch<AuthUser>('/api/auth/me'),
};
```

**`auth/AuthContext.tsx`**:

```tsx
import { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { authApi } from '../api/auth';
import { setUnauthorizedHandler } from '../api/client';
import type { AuthUser, LoginRequest, RegisterRequest } from '../types/dto';

interface AuthState {
  user: AuthUser | null;
  isLoading: boolean;
  login: (req: LoginRequest) => Promise<void>;
  register: (req: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const restoreSession = useCallback(async () => {
    try {
      const me = await authApi.me();
      setUser(me);
    } catch {
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(() => setUser(null));
    restoreSession();
  }, [restoreSession]);

  const login = async (req: LoginRequest) => { await authApi.login(req); await restoreSession(); };
  const register = async (req: RegisterRequest) => { await authApi.register(req); await restoreSession(); };
  const logout = async () => { await authApi.logout(); setUser(null); };

  return <AuthContext.Provider value={{ user, isLoading, login, register, logout }}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
```

**`auth/ProtectedRoute.tsx`**:

```tsx
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';

export function ProtectedRoute() {
  const { user, isLoading } = useAuth();
  if (isLoading) return <div>Loading…</div>;
  if (!user) return <Navigate to="/login" replace />;
  return <Outlet />;
}
```

---

### Pattern 3: Generic `ResourceList` and `ResourceForm`

**`components/ResourceList.tsx`**:

```tsx
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { ResourceConfig } from '../config/resources';

export function ResourceList({ config, onEdit }: { config: ResourceConfig; onEdit: (item: any) => void }) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const path = config.basePath.replace('{userId}', String(user!.id));

  const { data, isLoading, error } = useQuery({
    queryKey: [config.key],
    queryFn: () => apiFetch<any[]>(path),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => apiFetch<void>(`${path}/${id}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [config.key] }),
  });

  if (isLoading) return <div>Loading {config.label}…</div>;
  if (error) return <div>Failed to load {config.label}: {(error as Error).message}</div>;
  if (!data || data.length === 0) return <div>No {config.label.toLowerCase()} yet.</div>;

  return (
    <table>
      <thead><tr>{config.listColumns.map(c => <th key={c}>{c}</th>)}{(config.hasEdit || config.hasDelete) && <th />}</tr></thead>
      <tbody>
        {data.map((item) => (
          <tr key={item.id}>
            {config.listColumns.map(c => <td key={c}>{String(item[c] ?? '—')}</td>)}
            <td>
              {config.hasEdit && <button onClick={() => onEdit(item)}>Edit</button>}
              {config.hasDelete && <button onClick={() => deleteMutation.mutate(item.id)}>Delete</button>}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
```

**`components/ResourceForm.tsx`**:

```tsx
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { ResourceConfig } from '../config/resources';

export function ResourceForm({ config, editing, onDone }: { config: ResourceConfig; editing: any | null; onDone: () => void }) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const path = config.basePath.replace('{userId}', String(user!.id));
  const writableFields = config.fields.filter(f => !f.readOnly);
  const [values, setValues] = useState<Record<string, any>>(() =>
    Object.fromEntries(writableFields.map(f => [f.name, editing?.[f.name] ?? (f.type === 'checkbox' ? false : '')])));

  const mutation = useMutation({
    mutationFn: () => editing
      ? apiFetch(`${path}/${editing.id}`, { method: 'PUT', body: JSON.stringify(values) })
      : apiFetch(path, { method: 'POST', body: JSON.stringify(values) }),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: [config.key] }); onDone(); },
  });

  return (
    <form onSubmit={(e) => { e.preventDefault(); mutation.mutate(); }}>
      {writableFields.map(f => (
        <label key={f.name}>
          {f.label}
          {f.type === 'textarea'
            ? <textarea maxLength={f.maxLength} required={f.required} value={values[f.name]} onChange={e => setValues(v => ({ ...v, [f.name]: e.target.value }))} />
            : <input
                type={f.type === 'datetime' ? 'datetime-local' : f.type}
                maxLength={f.maxLength}
                required={f.required}
                checked={f.type === 'checkbox' ? values[f.name] : undefined}
                value={f.type === 'checkbox' ? undefined : values[f.name]}
                onChange={e => setValues(v => ({ ...v, [f.name]: f.type === 'checkbox' ? e.target.checked : e.target.value }))} />}
        </label>
      ))}
      {mutation.isError && <div>{(mutation.error as Error).message}</div>}
      <button type="submit" disabled={mutation.isPending}>{editing ? 'Save' : 'Create'}</button>
    </form>
  );
}
```

**`pages/ResourceSectionPage.tsx`**:

```tsx
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { RESOURCES } from '../config/resources';
import { ResourceList } from '../components/ResourceList';
import { ResourceForm } from '../components/ResourceForm';

export function ResourceSectionPage() {
  const { resourceKey } = useParams();
  const config = RESOURCES.find(r => r.key === resourceKey);
  const [editing, setEditing] = useState<any | null>(null);
  const [showForm, setShowForm] = useState(false);

  if (!config) return <div>Unknown resource.</div>;

  return (
    <div>
      <h1>{config.label}</h1>
      <button onClick={() => { setEditing(null); setShowForm(true); }}>+ New</button>
      {showForm && <ResourceForm config={config} editing={editing} onDone={() => setShowForm(false)} />}
      <ResourceList config={config} onEdit={(item) => { setEditing(item); setShowForm(true); }} />
    </div>
  );
}
```

---

### Pattern 4: Theme port and routing shell

**`theme/theme.css`** — CSS variables ported 1:1 from Meridian's `themeVars()` (`app/Meridian.dc.html` lines 400-415):

```css
:root[data-theme="Porcelain"] { --b:#F3F4F6; --s:#FFFFFF; --t:#1B1F24; --m:#6B7280; --br:rgba(27,31,36,0.08); --hl:rgba(27,31,36,0.045); --sh:0 1px 2px rgba(27,31,36,0.05); }
:root[data-theme="Ink"]       { --b:#FBFBFC; --s:#FBFBFC; --t:#17191C; --m:#5F656D; --br:rgba(23,25,28,0.16); --hl:rgba(23,25,28,0.055); --sh:none; }
:root[data-theme="Dusk"]      { --b:#131519; --s:#1B1E24; --t:#E7EAEE; --m:#8D95A1; --br:rgba(231,234,238,0.09); --hl:rgba(231,234,238,0.055); --sh:0 1px 3px rgba(0,0,0,0.35); }
body { margin:0; background: var(--b); color: var(--t); font-family: 'Instrument Sans', system-ui, sans-serif; }
```

`ThemeContext` sets `document.documentElement.dataset.theme` on selection; default `Porcelain`, matching Meridian's `props.theme ?? 'Porcelain'`.

**`App.tsx`**:

```tsx
import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { AppLayout } from './components/AppLayout';
import { ResourceSectionPage } from './pages/ResourceSectionPage';

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/finance/goals" replace />} />
            <Route path="/:section/:resourceKey" element={<ResourceSectionPage />} />
          </Route>
        </Route>
      </Routes>
    </AuthProvider>
  );
}
```

`Sidebar.tsx` maps `SECTIONS` × `RESOURCES.filter(r => r.section === section.key)` into nav links to `/{section}/{resourceKey}`, ported visually from Meridian's `navPillars`/`modules` structure (`app/Meridian.dc.html` lines 46-60).

---

## Data Flow

```text
1. Browser loads web/ (Vite :5173) → AuthProvider mounts → GET /api/auth/me
   │
   ▼
2a. 401 → user=null → ProtectedRoute redirects to /login
2b. 200 → user populated → ProtectedRoute renders AppLayout → default redirect to /finance/goals
   │
   ▼
3. ResourceSectionPage reads :section/:resourceKey → looks up ResourceConfig in resources.ts
   │
   ▼
4. ResourceList: useQuery → apiFetch(GET {basePath}) → credentials:'include' sends the
   access_token cookie → FinPulse.Api validates JWT from cookie → returns JSON list
   │
   ▼
5. User clicks "+ New" or "Edit" → ResourceForm renders fields from config.fields
   (readOnly fields excluded from the form; required mirrors DTO's [Required])
   │
   ▼
6. Submit → useMutation → apiFetch(POST/PUT {basePath}[/{id}]) → on success,
   queryClient.invalidateQueries → ResourceList automatically refetches
   │
   ▼
7. Delete → useMutation → apiFetch(DELETE {basePath}/{id}) → same invalidation →
   row disappears from the list (soft-deleted server-side, Status=0)
   │
   ▼
8. Any 401 at any point (session expired) → client.ts's onUnauthorized fires →
   AuthContext sets user=null → ProtectedRoute redirects to /login (AT-005)
```

---

## Integration Points

| External System | Integration Type | Authentication |
|-----------------|-------------------|------------------|
| `FinPulse.Api` (`http://localhost:5026`, dev) | REST over `fetch`, JSON | `access_token` httpOnly cookie, sent via `credentials:'include'`; CORS already allows `localhost:5173` with `AllowCredentials` |

No other external systems. `app/Meridian.dc.html` is not loaded or referenced by the running SPA — it remains a static design reference only.

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-----------------|-----------------|
| Live (manual, primary) | Every acceptance test in DEFINE, in a real browser against the real running API + Postgres | Full app, via `npm run dev` | Manual browser testing + DevTools (cookie inspection) | All 11 acceptance tests (AT-001–AT-011) |
| Type check | Compile-time correctness | Whole `web/` project | `tsc` via `npm run build` | 0 TypeScript errors |

Per DEFINE's explicit out-of-scope, no automated frontend test suite (Jest/Vitest/Playwright) is built this pass — live manual verification is the primary gate, matching every backend feature's own discipline in this initiative. **Decision 1's fix must be the first thing verified live** (cookie actually persists in browser DevTools after login) since every other acceptance test depends on it.

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|---------------------|--------|
| `401` from any `apiFetch` call | `client.ts`'s `onUnauthorized` callback fires → `AuthContext` clears `user` → `ProtectedRoute` redirects to `/login` | No |
| Login with invalid credentials | API returns `401` with `{message}`; `Login.tsx` catches the thrown error and displays it inline, does not navigate | No |
| Register with duplicate email/username/phone | API returns `400` with `{message}`; `Register.tsx` displays it inline | No |
| Any create/update/delete failure (validation, DB constraint) | `ResourceForm`/`ResourceList`'s mutation `isError` state displays `(error as Error).message` inline | No — user corrects and resubmits |
| Network failure (API unreachable) | `fetch` rejects; TanStack Query surfaces it via `error`; components show the generic failure message | TanStack Query's default retry (3x) applies automatically |

---

## Configuration

| Config Key | Type | Default | Description |
|------------|------|---------|-------------|
| `API_BASE` (hardcoded in `client.ts`) | string | `http://localhost:5026` | Backend base URL for local dev; not environment-driven this pass since only one environment (local dev) is in scope |
| `Cookie:Secure` (new, backend, `appsettings.Development.json`) | bool | `false` | Decision 1 — allows the auth cookie to persist over plain HTTP in Development only |

---

## Security Considerations

- The JWT is never touched by JavaScript — it lives only in the `httpOnly` cookie the API already sets/reads, matching Decision 3.
- `Cookie:Secure=false` is scoped to `appsettings.Development.json` only — `appsettings.json`'s (Production/default) value of `true` is untouched, so this fix has no production security impact.
- `client.ts` always sends `credentials: 'include'` only to `API_BASE` (`http://localhost:5026`), never to a wildcard/dynamic origin — no risk of leaking the cookie to a third party.
- Client-side field validation (`required`/`maxLength` mirroring DTOs) is a UX convenience only; the API's own `DataAnnotations` and DB constraints remain the actual authority — nothing new trusts the client.

---

## Observability

Not applicable — this feature has no backend logic changes beyond one static config file; existing Serilog/OpenTelemetry instrumentation on `FinPulse.Api` already covers every request the SPA makes. No frontend-side logging/telemetry is in scope this pass.

---

## Pipeline Architecture (if applicable)

Not applicable — this is a frontend application feature, not a data pipeline.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-26 | design-agent | Initial version. Confirmed all 15 resources' exact DTO field names/types by reading every `*DTOs.cs` file directly during this session (not from memory), and confirmed the `Goals` route pattern (`/api/users/{userId}/goals`, no `finance/` prefix) via `GoalsController.cs`. Resolved the Decision 2 tension against the backend's "no shared base" precedent explicitly, since it's the design choice most likely to be second-guessed without that context. |

---

## Next Step

**Ready for:** `/build .claude/sdd/features/DESIGN_WEB_APP.md`
