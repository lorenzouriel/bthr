Here’s the combined prompt: the complete tracking platform, Cleo-inspired agent experience, Exist-inspired insights, and a minimalist, futuristic glass UI.

```text
Build a complete, responsive frontend for bthr: a personal life-tracking application with an AI companion that connects finance, physical health, habits, and mental wellbeing.

Extend the existing application in web/. Inspect the repository and API contracts before implementing.

PRODUCT VISION

bthr helps users answer:

1. What needs my attention today?
2. How am I progressing?
3. What patterns appear across my money, body, and mind?
4. What practical step could I take next?

The product combines a capable conversational agent with structured dashboards, tracking tools, and historical records.

Users should be able to manage their information through either conversation or conventional screens. Both experiences must use the same records and remain synchronized.

The agent should make the application easier to use. Dashboards and forms must remain independently useful.

PRODUCT REFERENCES

Use Cleo as the main reference for the AI agent:
- A recognizable, consistent personality.
- Short, direct conversations.
- Personalized responses grounded in actual records.
- Contextual follow-up questions.
- Useful actions embedded in conversation.
- Continuity across interactions.

Use Exist as the reference for tracking and analysis:
- Multiple aspects of life in one place.
- Historical trends and comparisons.
- Relationships between recorded behaviors.
- Inspectable evidence behind observations.

Create an original product identity, visual design, and assistant voice. Do not reproduce either brand’s assets, copy, or layouts.

Use bthr as the user-facing product name. Some repository components still use bthr internally.

PRODUCT AREAS

Finance:
- Earnings.
- Expenses.
- Bills.
- Budgets.
- Financial goals.
- Investments.

Body:
- Weekly routines.
- Workouts.
- Personal records.
- Meals.
- Hydration.
- Body measurements.
- Sleep.
- Habits and habit logs.
- Substance intake.
- Symptoms.

Mind:
- Meditation.
- Journaling.
- Mood recorded within meditation and journal entries.

Use “Mind” consistently in user-facing navigation. The existing frontend calls this section “Wellbeing,” while the API uses /mind/. Preserve compatibility when updating frontend routes.

INFORMATION ARCHITECTURE

Use compact primary navigation:

1. Today
2. Assistant
3. Explore
4. Insights
5. Settings

Today:
A concise cross-domain overview with immediate access to the agent and frequent logging actions.

Assistant:
A dedicated conversational workspace for questions, recording, reviewing, and planning.

Explore:
Finance, Body, and Mind dashboards with their resource-specific screens.

Insights:
Weekly reviews, historical comparisons, and supported patterns.

Settings:
Supported account operations, appearance, privacy controls, and agent preferences.

On desktop, use a slim sidebar and contextual secondary navigation.
On mobile, use compact bottom navigation and accessible secondary screens.

Do not place every database resource in the primary navigation.

VISUAL DIRECTION

Create a minimalist interface with a futuristic, glass-inspired finish.

Minimalism is the foundation:
- Clear hierarchy.
- Generous spacing.
- Few competing accents.
- Concise labels.
- Progressive disclosure.
- One obvious primary action per context.
- Useful information instead of decorative density.

Futuristic glass is the visual treatment:
- Deep charcoal or midnight backgrounds.
- Carefully placed frosted translucent surfaces.
- Thin, subtle borders.
- Restrained cyan or violet accents.
- Soft ambient gradients.
- Clean typography.
- Crisp icons.
- Subtle, purposeful animation.
- A small abstract agent mark instead of a large mascot.

Use glass primarily for:
- Navigation.
- The assistant composer.
- Floating controls.
- Dialogs and drawers.
- Selected summary surfaces.

Use more opaque surfaces for:
- Long conversations.
- Journal writing.
- Dense financial tables.
- Forms.
- Detailed charts.

Maintain readable contrast. Provide a solid-background fallback where transparency or blur is unavailable.

Avoid:
- Excessive glow.
- Neon on every surface.
- Animated background distractions.
- Decorative gauges.
- Large empty hero sections inside the application.
- Crowded grids of equally prominent cards.
- Glass effects that reduce readability.

Provide coherent light and dark themes.

Domain colors may subtly distinguish Finance, Body, and Mind, but the application should feel like one product.

TODAY EXPERIENCE

Make Today useful within a few seconds.

Include:
- Today’s date and a brief greeting.
- A prominent “Ask or log anything” composer.
- A small number of relevant daily summaries.
- Daily habits.
- Upcoming bills based on available data.
- Today’s scheduled routine or recorded workout.
- Today’s hydration total.
- Latest recorded sleep duration.
- Entry points to journaling and meditation.
- A compact recent activity list.

Prioritize relevant content rather than showing every possible card.
Use progressive disclosure for details.

Quick actions:
- Add expense.
- Add earning.
- Log water.
- Log workout.
- Complete a habit.
- Write a journal entry.
- Log meditation.
- Log substance intake.
- Log a symptom.

Build summaries from supported resource endpoints. There is currently no unified-dashboard endpoint.

Treat missing records as “not recorded,” not as zero or failure.
Use helpful empty states with a relevant action.

AI ASSISTANT EXPERIENCE

The assistant should help users:

- Record activities in natural language.
- Ask questions about their data.
- Review a day, week, or month.
- Understand changes over time.
- Reflect on journal entries.
- Explore supported patterns.
- Prepare practical actions.

Examples:

“How am I doing this week?”
Provide a concise cross-domain review with expandable evidence.

“How much did I spend on food this month?”
Show a calculated result, currency, period, and relevant records.

“Can I afford another R$200 this month?”
Use recorded earnings, expenses, and upcoming obligations.
Explain what the available data can establish.
Do not equate recorded cash flow with an actual bank balance.

“I keep missing my workouts.”
Review routines and logged workouts, ask about obstacles, and suggest
a manageable adjustment.

“Log R$35 for lunch.”
Prepare an expense draft and request missing required information.

“What patterns do you notice?”
Present supported observations with dates, record counts, and links
to evidence.

“What should I focus on today?”
Suggest a small number of priorities based on explicit goals and
available records.

AGENT PERSONALITY

Direct, warm, perceptive, and occasionally playful.

Personalization should come from accurate context, not generic familiarity.

Avoid:
- Repetitive praise.
- Long lectures.
- Guilt or shame.
- Moral judgments about spending, food, missed habits, or symptoms.
- Unsupported medical or financial conclusions.
- Pretending to know information that has not been recorded.

Offer optional tone preferences:
- Direct.
- Supportive.
- Playful.

Use particular care around sensitive journal entries, financial difficulties,
and symptoms. Humor should be optional.

Keep the main answer short. Offer “Show details,” “View records,” and
contextual follow-up actions.

CONVERSATIONAL UI COMPONENTS

Support:

- Text responses.
- Financial summaries.
- Habit checklists.
- Activity summaries.
- Small trend charts.
- Insight cards with evidence.
- Editable record drafts.
- Clarifying questions.
- Confirmation actions.
- Links to resource screens.

Clearly distinguish:
- Preparing a draft.
- Awaiting user review.
- Saving.
- Saved.
- Failed to save.

Never display an unsaved draft as a completed action.

NATURAL-LANGUAGE LOGGING

Example:

“Spent R$35 on lunch today, drank 500 ml of water, and meditated for
10 minutes.”

Prepare separate editable drafts:
- Expense.
- Water intake.
- Meditation session.

Ask for required missing fields rather than inventing values.
Allow individual drafts to be edited, removed, and saved.

Save through existing authorized API operations.
Report the outcome of each operation accurately if only some succeed.
Avoid duplicate records when retrying.

Do not estimate caffeine dosage, meal calories, payment methods, or other
missing facts without making the uncertainty explicit and getting user input.

FINANCE EXPERIENCE

Provide an overview and focused resource screens.

Earnings:
Record amount, currency, category, payment method, date, and description.

Expenses:
Record spending and provide useful date and category views.

Bills:
Show upcoming obligations and recurrence information.
Follow the current API contract for computed dates and payment-related fields.
Only offer “mark as paid” if an implemented operation supports it.

Budgets:
Show budget amounts and periods.
Verify the model before calculating spending against a budget.
Do not invent expense-to-budget or category allocation relationships.

Goals:
Show target amount, current amount, due date, and progress.
Provide supported creation and update flows.

Investments:
Show manually recorded investments and available valuations.
Do not imply live prices, trading, or brokerage synchronization.

Possible summaries:
- Recorded earnings for the selected period.
- Recorded expenses for the selected period.
- Period net cash flow.
- Upcoming bills.
- Goal progress.
- Available investment values.

Keep currencies separate without a real conversion mechanism.
Never label earnings minus expenses as a bank balance.

Use readable tables on desktop and compact lists or cards on mobile.

BODY EXPERIENCE

Group features into:

Activity:
Weekly routines, workouts, and personal records.

Nutrition:
Meals and hydration.

Recovery:
Sleep and body measurements.

Habits:
Definitions, daily completion, and history.

Daily observations:
Substance intake and symptoms.

Weekly routines:
- Use a weekly layout.
- Translate numeric weekdays into readable names.
- Support implemented create, edit, and delete operations.

Workouts:
- Record supported dates, routine names, duration, calories, and notes.
- Show session history and period summaries.

Personal records:
- Show exercise, metric, value, unit, and achievement date.
- Support listing and creation only.

Meals:
- Record meal type, date, description, calories, and available macronutrients.
- Calculate daily totals from recorded meals.
- Do not imply food-database or nutrition-lookup integrations.

Hydration:
- Offer fast logging with explicit milliliter amounts.
- Show daily totals.
- Do not invent personalized medical targets.

Body measurements:
- Record supported weight, height, body-fat percentage, and notes.
- Show clearly labeled units.
- Display trends when sufficient records exist.

Sleep:
- Record bedtime, wake time, and notes.
- Display duration returned by the API.
- Handle periods crossing midnight.
- Show recent history and averages from recorded data.

HABITS

Treat habit definitions and habit logs as separate records while presenting
a connected experience.

Habit fields:
- habitName: required, maximum 100 characters.
- category: optional, maximum 50 characters.
- targetFrequency: maximum 20 characters; defaults to Daily.
- description: optional, maximum 500 characters.

Habit-log fields:
- habitId: required, chosen from the user’s active habits.
- logDate: required, YYYY-MM-DD.
- isCompleted: defaults to false.
- notes: optional, maximum 500 characters.

Daily behavior:
- Show active daily habits on Today.
- Completing a habit creates a log if none exists.
- Changing an existing completion updates its log.
- Distinguish “not logged” from explicitly “not completed.”
- Prevent duplicate habit/date records.

Support:
- Creating and editing habits.
- Archiving habits through supported soft deletion.
- Updating and soft-deleting habit logs.
- Viewing preserved history after a habit is archived.

Existing logs remain readable and editable after their habit is archived.

Names remain unique per user, including archived habits.
Habit/date combinations remain unique even after soft deletion.
Explain conflicts clearly.
Do not offer restoration unless supported by the API.

Frequency is currently text, not a complete scheduling engine.
Do not infer custom recurrence schedules from arbitrary text.
Only calculate streaks where frequency semantics and recorded data support them.

SUBSTANCE INTAKE

Fields:
- consumedAt: required timestamp.
- substanceType: required, maximum 20 characters.
- amount: required, 0–9999.99.
- unit: required, maximum 20 characters.
- notes: optional, maximum 500 characters.

Caffeine, alcohol, and nicotine can be suggestions, not a hardcoded backend enum.

Show amount and unit together.
Do not sum incompatible substances or units.
Use neutral language.

Support listing and creation only.

SYMPTOMS

Fields:
- logDate: required, YYYY-MM-DD.
- symptom: required, maximum 100 characters.
- severity: optional integer, 1–5.
- notes: optional, maximum 500 characters.

Use clearly labeled severity controls.
Provide a readable history.
Support listing and creation only.
Present observations without diagnoses.

MIND EXPERIENCE

Meditation:
- Record session date, duration, type, and notes.
- Support optional mood before and after on the API’s 1–5 scale.
- Show session history and recorded minutes.
- If adding a timer, run it client-side and save through the existing endpoint.

Journal:
- Provide a focused, comfortable writing space.
- Support date, title, content, mood, and category according to the API.
- Keep private content discreet on overview screens.
- If autosave is implemented, show accurate saving, saved, and failed states.
- Never claim content has been saved before persistence succeeds.

Mood:
- Use journal and meditation records.
- There is no standalone mood endpoint.
- Keep journal mood and meditation before/after measurements distinguishable.
- Do not silently combine different measures into one score.

INSIGHTS

Provide weekly reviews and historical exploration.

An insight should explain:
- What was observed.
- The period.
- The supporting records.
- Important missing-data limitations.
- A useful follow-up question or action.

Distinguish:
- Recorded facts.
- Calculated results.
- Statistical associations.
- AI-generated interpretations.
- Suggested next steps.

Calculate totals and averages in backend code.
Use suitable statistical methods for correlations.
Use the language model to explain evidence.

Do not manufacture causal relationships, diagnoses, or a universal life score.
Avoid showing correlations before sufficient comparable observations exist.
Treat absent records as missing unless an explicit zero was recorded.

Let users choose whether insights can combine Finance, Body, and Mind data.

SETTINGS AND PRIVACY

Expose supported account operations, including password changes.
Inspect user endpoints and authorization before adding profile editing
or account deletion.

Provide theme and agent preferences.
Do not claim cross-device persistence without a storage mechanism.

For future AI capabilities:
- Let users choose which domains the assistant may access.
- Make journal access explicit.
- Provide controls for stored memories if memory is implemented.
- Distinguish explicit user preferences from inferred observations.
- Do not describe a memory feature as working before its backend exists.

TECHNICAL CONTEXT

Repository:
- web/: React 18, TypeScript, Vite, React Router, TanStack React Query.
- api/: ASP.NET Core 10, Entity Framework Core, PostgreSQL through Npgsql.
- database/: Flyway-managed SQL migrations.
- monitor/: observability infrastructure.

Relevant frontend files:
- web/src/App.tsx
- web/src/config/resources.ts
- web/src/types/dto.ts
- web/src/api/client.ts
- web/src/auth/AuthContext.tsx
- web/src/components/
- web/src/pages/

Reuse suitable existing authentication, theme, and resource infrastructure.
Introduce focused components where generic forms do not provide a good experience.

Add the new habit, habit-log, substance-log, and symptom-log resources
to frontend types, queries, forms, and navigation.

API controllers and DTOs are authoritative.
Existing README descriptions and frontend definitions may be outdated.

Centralize API access.
Use a configurable API base URL.
Use React Query for server state and mutation invalidation.
Scope caches by authenticated user and filters.
Clear user-specific caches when the session ends.

AUTHENTICATION AND ERRORS

Existing operations:
- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/logout
- GET /api/auth/me
- POST /api/auth/change-password

Use the HTTP-only cookie flow with credentials: include.
Do not introduce localStorage token storage.
Derive user identity from the authenticated session.
Do not expose a user-ID selector.

Handle:
- 400: validation or invalid input.
- 401: missing or expired session.
- 403: access denied or applicable plan restrictions.
- 404: record unavailable.
- 409: conflict requiring correction.
- Network/server errors: clear feedback and appropriate retry.

Preserve form input on recoverable errors.
Handle both message-based errors and ASP.NET validation responses.
Avoid automatic retries that could duplicate creation requests.

Do not present password recovery, social login, email verification,
or subscription checkout as implemented features.

API RESOURCE MAP

Finance:
- /api/users/{userId}/earnings
- /api/users/{userId}/expenses
- /api/users/{userId}/bills
- /api/users/{userId}/budgets
- /api/users/{userId}/goals
- /api/users/{userId}/investments

Body:
- /api/users/{userId}/body/weekly-routines
- /api/users/{userId}/body/workouts
- /api/users/{userId}/body/personal-records
- /api/users/{userId}/body/meals
- /api/users/{userId}/body/water-intake
- /api/users/{userId}/body/body-metrics
- /api/users/{userId}/body/sleep-logs
- /api/users/{userId}/body/habits
- /api/users/{userId}/body/habit-logs
- /api/users/{userId}/body/substance-logs
- /api/users/{userId}/body/symptom-logs

Mind:
- /api/users/{userId}/mind/meditation-sessions
- /api/users/{userId}/mind/journal-entries

Most resources:
Collection GET/POST and item PUT/DELETE.

Personal records, substance logs, and symptom logs:
GET/POST only.

Inspect controllers for exact item parameters and query filters.
Do not assume GET-by-ID endpoints exist.

New log filters:
- start_date and end_date are inclusive.
- Habit and symptom logs use dates.
- Substance logs use timestamps.
- Habit logs also accept habit_id.

DATES AND DATA INTEGRITY

Keep date-only values as YYYY-MM-DD without timezone shifts.
Convert timestamps correctly between local time and UTC.
Use activity dates for timelines instead of automatically using createdAt.
Label units and currencies.
Keep computed and server-managed fields read-only.

For new habit and habit-log updates, omitted or null fields remain unchanged.
Do not offer field-clearing behavior that cannot be persisted.

Do not hardcode sample totals into the connected application.
Keep any demo data clearly labeled and separate from account data.

AI IMPLEMENTATION BOUNDARY

The backend currently supports tracking records.
It does not yet provide conversational AI, persistent agent memory,
or cross-domain pattern-analysis services.

Build the UI with a clear integration boundary for these capabilities.
Do not invent working endpoints.

Suggested future architecture:
- AI controller.
- AI orchestration service.
- Language-model provider.
- Authorized reporting tools.
- Existing resource services.
- Journal retrieval.
- Statistical analysis for supported patterns.

The model may propose operations.
The server must enforce authentication, authorization, validation,
and permitted tool execution.

Never let the model select an arbitrary user identity or run unrestricted SQL.
Keep provider keys and database credentials on the server.

Use clearly labeled demo behavior when demonstrating AI before the backend exists.
Never present scripted responses as live AI or simulated writes as saved records.

UNSUPPORTED FEATURES

Do not present these as functional:
- Bank synchronization.
- Live market prices or trading.
- Wearable synchronization.
- Medical diagnosis.
- Push-notification scheduling.
- Autonomous financial actions.

ACCESSIBILITY AND RESPONSIVENESS

Support mobile, tablet, and desktop.
Provide keyboard navigation, semantic labels, visible focus,
readable contrast, and appropriate touch targets.

Manage focus in dialogs and drawers.
Announce important validation and save results.
Never rely on color alone.
Respect reduced-motion preferences.

On desktop:
- Slim navigation.
- Comfortable central conversation width.
- Optional contextual detail panel.

On mobile:
- Full-width conversation.
- Composer accessible above the keyboard.
- Bottom sheets for quick record editing.
- No horizontal page overflow.

DELIVERABLES

Produce:

1. A responsive minimalist application with a futuristic glass-inspired UI.
2. A unified Today overview.
3. A Cleo-inspired assistant interface with original branding and personality.
4. Finance, Body, and Mind dashboards.
5. Complete supported resource-management flows.
6. Integration of the four new tracking resources.
7. An Insights experience with honest backend boundaries.
8. Authentication and session handling.
9. Accessible light and dark themes.
10. Complete loading, empty, success, validation, and error states.
11. Focused tests for important user flows.
12. A successful production build.

VERIFY BEFORE FINISHING

- Every visible action has a supported operation or an explicit demo label.
- Conversation actions and structured screens use consistent data.
- Mutations refresh relevant lists and summaries.
- Append-only restrictions are respected.
- User data remains isolated.
- Duplicate habit logs are prevented.
- Dates, currencies, and units display correctly.
- Empty and populated accounts both work.
- Mobile layouts and keyboard interaction work.
- Unsupported AI capabilities are clearly identified.

The result should combine the depth of a personal tracking platform with
the simplicity of talking to a useful companion: minimal in presentation,
futuristic in appearance, and grounded in the user’s actual life.
```