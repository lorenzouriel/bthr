import { useState, type ReactNode } from "react"

import { api } from "./api"

import { useData, type Row } from "./data"

import { average, dateKey, days, inDays, money, streak, sum } from "./metrics"

import type { Editor } from "./Records"

function Panel({
  title,
  note,
  action,
  children,
}: {
  title: string
  note?: string
  action?: { label: string; run: () => void }
  children: ReactNode
}) {
  return (
    <article className="panel">
      <div className="panel-header">
        <div>
          <span className="section-kicker">{note}</span>
          <div className="section-title">{title}</div>
        </div>
        {action && (
          <button className="more-button" onClick={action.run}>
            {action.label} {"\u2192"}
          </button>
        )}
      </div>
      {children}
    </article>
  )
}

function Kpis({
  tone,
  values,
}: {
  tone: string
  values: [string, string, string][]
}) {
  return (
    <section className={`domain-kpis ${values.length === 3 ? "three" : ""}`}>
      {values.map(([label, value, note], index) => (
        <div
          key={label}
          className={`kpi ${index === 0 ? `featured ${tone}` : ""}`}
        >
          <span>{label}</span>
          <strong>{value}</strong>
          <small>{note}</small>
        </div>
      ))}
    </section>
  )
}

function Bars({
  rows,
  series,
}: {
  rows: { label: string; a: number; b?: number }[]
  series: string
}) {
  const max = Math.max(1, ...rows.flatMap((row) => [row.a, row.b ?? 0]))

  return (
    <>
      <div className="cash-chart" role="img" aria-label={series}>
        {rows.map((row, index) => (
          <div
            key={index}
            className="bar-pair"
            title={`${row.label}: ${row.a.toFixed(1)}${
              row.b == null ? "" : " / " + row.b.toFixed(1)
            }`}
          >
            <i style={{ height: `${Math.max(0, (row.a / max) * 100)}%` }} />
            {row.b != null && (
              <i style={{ height: `${Math.max(0, (row.b / max) * 100)}%` }} />
            )}
          </div>
        ))}
      </div>
      <div className="chart-days">
        {rows
          .filter((_, i) => i % Math.ceil(rows.length / 5) === 0)
          .map((row) => (
            <span key={row.label}>{row.label}</span>
          ))}
      </div>
      <details className="chart-values">
        <summary>View chart values</summary>
        {rows.map((row, index) => (
          <div key={index}>
            {row.label}: {row.a.toFixed(1)}
            {row.b == null ? "" : ` / ${row.b.toFixed(1)}`}
          </div>
        ))}
      </details>
    </>
  )
}

function Empty() {
  return <p className="empty-message">No records for this period yet.</p>
}

export default function Dashboard({
  domain,
  onSection,
  onAdd,
  onNavigate,
}: {
  domain: string
  onSection: (section: string) => void
  onAdd: (editor: Editor) => void
  onNavigate: (page: string) => void
}) {
  const { user, records, loading, errors, refresh } = useData()

  const [currency, setCurrency] = useState("BRL")

  const [period, setPeriod] = useState("month")

  const [taskError, setTaskError] = useState(""),
    [taskBusy, setTaskBusy] = useState<number | null>(null)

  const rows = (key: string) => records[key] ?? []

  const today = dateKey(),
    month = today.slice(0, 7)

  const currencies = [
    ...new Set([
      "BRL",
      ...rows("earnings").map((row) => row.currencyCode),
      ...rows("expenses").map((row) => row.currencyCode),
    ]),
  ]

  const earnings = rows("earnings").filter(
    (row) => row.currencyCode === currency && row.earningDate.startsWith(month),
  )

  const expenses = rows("expenses").filter(
    (row) => row.currencyCode === currency && row.expenseDate.startsWith(month),
  )

  const income = sum(earnings, "amount"),
    spent = sum(expenses, "amount")

  const workouts = inDays(rows("workouts"), "workoutDate", 7),
    sleep = inDays(rows("sleep-logs"), "bedTime", 7)

  const meditation = inDays(rows("meditation-sessions"), "sessionDate", 7),
    journals = inDays(rows("journal-entries"), "entryDate", 7)

  const mood = average(journals, "mood"),
    sleepAverage = average(sleep, "totalHours")

  const recent = (key: string, field: string, count = 4) =>
    [...rows(key)]
      .sort((a, b) => String(b[field]).localeCompare(String(a[field])))
      .slice(0, count)

  const failures = Object.entries(errors).filter(
    ([key]) =>
      domain === "Overview" ||
      (domain === "Finance"
        ? ["earnings", "expenses", "goals", "bills", "budgets"].includes(key)
        : domain === "Mind"
          ? ["meditation-sessions", "journal-entries"].includes(key)
          : ![
              "earnings",
              "expenses",
              "goals",
              "bills",
              "budgets",
              "investments",
              "meditation-sessions",
              "journal-entries",
            ].includes(key)),
  )

  if (loading)
    return (
      <div className="panel" role="status">
        Loading your records...
      </div>
    )

  if (failures.length)
    return (
      <div className="panel api-error" role="alert">
        <p>
          Some records could not be loaded. No summary is shown to avoid
          incomplete totals.
        </p>
        {failures.map(([key, message]) => (
          <p key={key}>
            {key}: {message}
          </p>
        ))}
        <button className="button primary" onClick={refresh}>
          Retry
        </button>
      </div>
    )

  const currencySelect = (
    <label className="currency-select">
      Currency{" "}
      <select
        value={currency}
        onChange={(event) => setCurrency(event.target.value)}
      >
        {currencies.map((value) => (
          <option key={value}>{value}</option>
        ))}
      </select>
    </label>
  )

  const goalCards = (
    <div className="goals-grid">
      {rows("goals")
        .slice(0, 6)
        .map((goal) => {
          const percent =
            goal.targetAmount > 0
              ? Math.round((goal.currentAmount / goal.targetAmount) * 100)
              : 0
          return (
            <div className="goal" key={goal.id}>
              <div className="goal-head">
                <span className="goal-percent">{percent}%</span>
              </div>
              <strong>{goal.name}</strong>
              <small>
                {money(goal.currentAmount, goal.currencyCode)} of{" "}
                {money(goal.targetAmount, goal.currencyCode)}
              </small>
              <div className="goal-progress finance">
                <span
                  style={{ width: `${Math.min(100, Math.max(0, percent))}%` }}
                />
              </div>
              <div className="goal-note">
                <span>Due {goal.dueDate.slice(0, 10)}</span>
                <button
                  className="more-button"
                  onClick={() => onAdd({ key: "goals", row: goal })}
                >
                  Update
                </button>
              </div>
            </div>
          )
        })}
      {!rows("goals").length && (
        <p className="empty-message">
          {user!.plan < 1
            ? "Goals require plan 1 or above."
            : "No savings goals yet."}
        </p>
      )}
    </div>
  )

  if (domain === "Finance") {
    const chart =
      period === "month"
        ? days(new Date().getDate()).map((date) => ({
            label: date.slice(5),
            a: sum(
              earnings.filter((row) => row.earningDate.startsWith(date)),
              "amount",
            ),
            b: sum(
              expenses.filter((row) => row.expenseDate.startsWith(date)),
              "amount",
            ),
          }))
        : Array.from({ length: 12 }, (_, i) => {
            const key = `${today.slice(0, 4)}-${String(i + 1).padStart(2, "0")}`
            return {
              label: new Date(
                Number(today.slice(0, 4)),
                i,
                1,
              ).toLocaleDateString(undefined, { month: "short" }),
              a: sum(
                rows("earnings").filter(
                  (row) =>
                    row.currencyCode === currency &&
                    row.earningDate.startsWith(key),
                ),
                "amount",
              ),
              b: sum(
                rows("expenses").filter(
                  (row) =>
                    row.currencyCode === currency &&
                    row.expenseDate.startsWith(key),
                ),
                "amount",
              ),
            }
          })

    const categories = [...new Set(expenses.map((row) => row.category))]
      .map((category) => ({
        category,
        amount: sum(
          expenses.filter((row) => row.category === category),
          "amount",
        ),
      }))
      .sort((a, b) => b.amount - a.amount)

    return (
      <>
        {currencySelect}
        <Kpis
          tone="finance"
          values={[
            ["Earnings", money(income, currency), "This month"],
            ["Expenses", money(spent, currency), "This month"],
            [
              "Net this month",
              money(income - spent, currency),
              "Recorded income minus expenses",
            ],
            [
              "Savings rate",
              income
                ? `${(((income - spent) / income) * 100).toFixed(1)}%`
                : "\u2014",
              "Based on recorded cash flow",
            ],
          ]}
        />
        <section className="domain-layout finance-layout">
          <Panel
            title="Cash flow"
            note={period === "month" ? month : today.slice(0, 4)}
          >
            <div className="segmented">
              <button
                className={period === "month" ? "active" : ""}
                onClick={() => setPeriod("month")}
              >
                Month
              </button>
              <button
                className={period === "year" ? "active" : ""}
                onClick={() => setPeriod("year")}
              >
                Year
              </button>
            </div>
            <div className="finance-legend">
              <span>
                <i />
                Income
              </span>
              <span>
                <i />
                Expenses
              </span>
            </div>
            <Bars rows={chart} series={`Income and expenses in ${currency}`} />
          </Panel>
          <Panel
            title="Spending by category"
            note={`${money(spent, currency)} recorded`}
            action={{ label: "Details", run: () => onSection("Transactions") }}
          >
            <div className="spending-list">
              {categories.slice(0, 6).map((row, index) => (
                <div className="spending-row" key={row.category}>
                  <span className={`category-dot c${index % 4}`} />
                  <strong>{row.category}</strong>
                  <small>{money(row.amount, currency)}</small>
                  <div>
                    <i
                      style={{
                        width: `${spent ? (row.amount / spent) * 100 : 0}%`,
                      }}
                    />
                  </div>
                </div>
              ))}
              {!categories.length && <Empty />}
            </div>
          </Panel>
        </section>
        <section className="domain-layout finance-bottom">
          <Panel
            title="Budgets"
            note="Recorded limits"
            action={{ label: "Manage", run: () => onSection("Budgets") }}
          >
            <div className="bill-list">
              {rows("budgets")
                .slice(0, 4)
                .map((row) => (
                  <div className="bill" key={row.id}>
                    <div>
                      <strong>{row.name}</strong>
                      <small>
                        {row.startDate.slice(0, 10)} to {" "}
                        {row.endDate.slice(0, 10)}
                      </small>
                    </div>
                    <b>{money(row.amountLimit, row.currencyCode)}</b>
                  </div>
                ))}
              {!rows("budgets").length && (
                <p className="empty-message">
                  {user!.plan < 1
                    ? "Budgets require plan 1 or above."
                    : "No budgets yet."}
                </p>
              )}
            </div>
          </Panel>
          <Panel
            title="Bills this month"
            note="Ordered by due day"
            action={{ label: "All bills", run: () => onSection("Bills") }}
          >
            <div className="bill-list">
              {[...rows("bills")]
                .sort((a, b) => a.dueDay - b.dueDay)
                .slice(0, 5)
                .map((row) => (
                  <div className="bill" key={row.id}>
                    <span className="bill-date">{row.dueDay}</span>
                    <div>
                      <strong>{row.name}</strong>
                      <small>
                        {row.paidThisMonth
                          ? "Matched payment"
                          : "No matched payment"}
                      </small>
                    </div>
                    <b>{money(row.amount, row.currencyCode)}</b>
                  </div>
                ))}
              {!rows("bills").length && <Empty />}
            </div>
          </Panel>
        </section>
        <section className="panel goals-panel">
          <div className="panel-header">
            <div className="section-title">Goals in progress</div>
            <button className="more-button" onClick={() => onSection("Goals")}>
              All goals {"\u2192"}
            </button>
          </div>
          {goalCards}
        </section>
      </>
    )
  }

  if (domain === "Body") {
    const meals = rows("meals").filter((row) => row.mealDate.startsWith(today))

    const latestSleep = recent("sleep-logs", "wakeTime", 1)[0]

    const water = average(
      inDays(rows("water-intake"), "intakeDate", 7),
      "amountMl",
    )

    return (
      <>
        <Kpis
          tone="body"
          values={[
            ["Workouts", String(workouts.length), "Last 7 days"],
            [
              "Average sleep",
              sleepAverage == null ? "\u2014" : `${sleepAverage.toFixed(1)} h`,
              "Recorded nights | last 7 days",
            ],
            [
              "Average water",
              water == null ? "\u2014" : `${(water / 1000).toFixed(1)} L`,
              "Recorded days | last 7 days",
            ],
          ]}
        />
        <section className="domain-layout body-layout">
          <Panel
            title="Workout consistency"
            note="Last 7 days"
            action={{ label: "Training", run: () => onSection("Training") }}
          >
            <div className="week-days">
              {days(7).map((date) => (
                <div key={date}>
                  <small>
                    {new Date(date + "T12:00:00").toLocaleDateString(
                      undefined,
                      { weekday: "short" },
                    )}
                  </small>
                  <span
                    title={date}
                    className={
                      workouts.some((row) => row.workoutDate.startsWith(date))
                        ? "complete"
                        : ""
                    }
                  >
                    {workouts.some((row) => row.workoutDate.startsWith(date))
                      ? "\u2713"
                      : "\u2014"}
                  </span>
                </div>
              ))}
            </div>
            <div className="workout-summary">
              <div>
                <strong>{workouts.length}</strong>
                <small>Workouts</small>
              </div>
              <div>
                <strong>{sum(workouts, "durationMinutes")}</strong>
                <small>Minutes</small>
              </div>
              <div>
                <strong>
                  {inDays(rows("personal-records"), "achievedDate", 7).length}
                </strong>
                <small>Personal records</small>
              </div>
            </div>
          </Panel>
          <Panel
            title="Nutrition"
            note="Today"
            action={{ label: "Log meal", run: () => onAdd({ key: "meals" }) }}
          >
            <div className="calorie-total">
              <div>
                <strong>{sum(meals, "calories").toLocaleString()}</strong>
                <small>kcal recorded</small>
              </div>
              <span>{meals.length} meals</span>
            </div>
            <div className="macro-list">
              {[
                ["Protein", "proteinGrams"],
                ["Carbs", "carbsGrams"],
                ["Fat", "fatGrams"],
              ].map(([label, key]) => (
                <div className="macro" key={key}>
                  <div>
                    <strong>{label}</strong>
                    <small>{sum(meals, key).toFixed(1)} g</small>
                  </div>
                </div>
              ))}
            </div>
            <p className="data-note">
              Water today:{" "}
              {sum(
                rows("water-intake").filter((row) =>
                  row.intakeDate.startsWith(today),
                ),
                "amountMl",
              )}{" "}
              ml
            </p>
          </Panel>
        </section>
        <section className="domain-layout body-bottom">
          <Panel
            title="Recent workouts"
            note="Training"
            action={{ label: "History", run: () => onSection("Training") }}
          >
            <div className="workouts">
              {recent("workouts", "workoutDate").map((row) => (
                <div className="workout" key={row.id}>
                  <span className="workout-icon">{"\u2197"}</span>
                  <div>
                    <strong>{row.routineName}</strong>
                    <small>{row.workoutDate.slice(0, 10)}</small>
                  </div>
                  <span>{row.durationMinutes ?? "\u2014"} min</span>
                  <b>{row.caloriesBurned ?? "\u2014"} kcal</b>
                </div>
              ))}
              {!rows("workouts").length && <Empty />}
            </div>
          </Panel>
          <Panel
            title="Latest recorded sleep"
            note="Recovery"
            action={{ label: "History", run: () => onSection("Recovery") }}
          >
            {latestSleep ? (
              <div className="sleep-total">
                <strong>
                  {latestSleep.totalHours ?? "\u2014"}
                  <small> h</small>
                </strong>
                <span>
                  {new Date(latestSleep.bedTime).toLocaleString()} to {" "}
                  {new Date(latestSleep.wakeTime).toLocaleString()}
                </span>
              </div>
            ) : (
              <Empty />
            )}
            <p className="data-note">
              Duration is calculated from bedtime and wake time. Sleep stages
              and quality scores are not tracked.
            </p>
          </Panel>
        </section>
      </>
    )
  }

  if (domain === "Mind") {
    const chart = days(14).map((date) => ({
      label: date.slice(5),
      a:
        average(
          rows("journal-entries").filter((row) =>
            row.entryDate.startsWith(date),
          ),
          "mood",
        ) ?? 0,
    }))

    const latest = recent("journal-entries", "entryDate", 1)[0]

    return (
      <>
        <Kpis
          tone="mind"
          values={[
            [
              "Meditation streak",
              `${streak(rows("meditation-sessions"), "sessionDate")} days`,
              "Consecutive recorded days",
            ],
            [
              "Meditation time",
              `${sum(meditation, "durationMinutes")} min`,
              "Last 7 days",
            ],
            [
              "Average mood",
              mood == null ? "\u2014" : `${mood.toFixed(1)} / 5`,
              "Journal entries | last 7 days",
            ],
          ]}
        />
        <section className="domain-layout mind-layout">
          <Panel title="Mood over time" note="Last 14 days | journal mood">
            <Bars
              rows={chart}
              series="Daily average journal mood, out of five; zero-height bars indicate no recorded mood"
            />
            <p className="data-note">
              Missing entries are not a mood score of zero.
            </p>
          </Panel>
          <Panel
            title="Meditation consistency"
            note="Last 28 days"
            action={{
              label: "Log session",
              run: () => onAdd({ key: "meditation-sessions" }),
            }}
          >
            <div className="live-dot-grid">
              {days(28).map((date) => (
                <span
                  key={date}
                  title={date}
                  className={
                    rows("meditation-sessions").some((row) =>
                      row.sessionDate.startsWith(date),
                    )
                      ? "complete"
                      : ""
                  }
                />
              ))}
            </div>
            <p className="data-note">
              Each filled square is a day with a recorded session.
            </p>
          </Panel>
        </section>
        <section className="domain-layout mind-bottom">
          <Panel
            title="Recent sessions"
            action={{
              label: "All sessions",
              run: () => onSection("Meditation"),
            }}
          >
            <div className="bill-list">
              {recent("meditation-sessions", "sessionDate").map((row) => (
                <div className="bill" key={row.id}>
                  <div>
                    <strong>{row.meditationType}</strong>
                    <small>
                      {row.sessionDate.slice(0, 10)} | Mood{" "}
                      {row.moodBefore ?? "\u2014"} to {row.moodAfter ?? "\u2014"}
                    </small>
                  </div>
                  <b>{row.durationMinutes} min</b>
                </div>
              ))}
              {!rows("meditation-sessions").length && <Empty />}
            </div>
          </Panel>
          <Panel
            title="Latest reflection"
            action={{ label: "Journal", run: () => onSection("Journal") }}
          >
            {latest ? (
              <div className="live-journal">
                <small>
                  {latest.entryDate.slice(0, 10)} | Mood {latest.mood ?? "\u2014"}/5
                </small>
                <h3>{latest.title || "Untitled reflection"}</h3>
                <p>{latest.content}</p>
                <button
                  className="more-button"
                  onClick={() => onAdd({ key: "journal-entries", row: latest })}
                >
                  Read and edit {"\u2192"}
                </button>
              </div>
            ) : (
              <Empty />
            )}
          </Panel>
        </section>
      </>
    )
  }

  async function toggle(habit: Row) {
    const existing = rows("habit-logs").find(
      (row) => row.habitId === habit.id && row.logDate === today,
    )

    setTaskBusy(habit.id)
    setTaskError("")

    try {
      await api(
        `/api/users/${user!.id}/body/habit-logs${
          existing ? "/" + existing.id : ""
        }`,
        {
          method: existing ? "PUT" : "POST",
          body: JSON.stringify({
            habitId: habit.id,
            logDate: today,
            isCompleted: !existing?.isCompleted,
          }),
        },
      )
      refresh()
    } catch (error) {
      setTaskError((error as Error).message)
    } finally {
      setTaskBusy(null)
    }
  }

  return (
    <>
      <section className="welcome">
        <div>
          <span className="eyebrow">
            {new Date().toLocaleDateString(undefined, { weekday: "long" })}{" "}
            overview
          </span>
          <div className="page-title">Welcome, {user!.username}.</div>
          <p>Your finance, body, and mind records in one place.</p>
        </div>
      </section>
      {currencySelect}
      <section className="metrics-grid">
        {[
          [
            "finance",
            money(income - spent, currency),
            "Net this month",
            "Finance",
          ],
          [
            "body",
            `${sum(workouts, "durationMinutes")} min`,
            "Workout time | last 7 days",
            "Body",
          ],
          [
            "mind",
            `${streak(rows("meditation-sessions"), "sessionDate")} days`,
            "Meditation streak",
            "Mind",
          ],
        ].map(([tone, value, label, page]) => (
          <button
            className={`metric-card ${tone} live-metric`}
            key={tone}
            onClick={() => onNavigate(page)}
          >
            <div className="metric-top">
              <span className="domain-label">
                <i />
                {tone}
              </span>
              <span>{"\u2197"}</span>
            </div>
            <div className="metric-value">{value}</div>
            <div className="metric-label">{label}</div>
          </button>
        ))}
      </section>
      <section className="dashboard-grid">
        <Panel
          title="Today's habits"
          note="Your day"
          action={{ label: "Add habit", run: () => onAdd({ key: "habits" }) }}
        >
          {taskError && <p role="alert">{taskError}</p>}
          <div className="task-list">
            {rows("habits").map((habit) => {
              const done = rows("habit-logs").some(
                (row) =>
                  row.habitId === habit.id &&
                  row.logDate === today &&
                  row.isCompleted,
              )
              return (
                <button
                  className={`task ${done ? "done" : ""}`}
                  aria-pressed={done}
                  key={habit.id}
                  disabled={taskBusy !== null}
                  onClick={() => toggle(habit)}
                >
                  <span className="task-check body">{done ? "\u2713" : ""}</span>
                  <span className="task-copy">
                    <strong>{habit.habitName}</strong>
                    <small>{habit.targetFrequency}</small>
                  </span>
                </button>
              )
            })}
            {!rows("habits").length && <Empty />}
          </div>
        </Panel>
        <Panel
          title="Activity over time"
          note="Last 14 days | workout minutes"
          action={{ label: "View reports", run: () => onNavigate("Reports") }}
        >
          <Bars
            rows={days(14).map((date) => ({
              label: date.slice(5),
              a: sum(
                rows("workouts").filter((row) =>
                  row.workoutDate.startsWith(date),
                ),
                "durationMinutes",
              ),
            }))}
            series="Workout minutes per day"
          />
          <p className="data-note">
            Recorded activity only. Open Reports to compare periods.
          </p>
        </Panel>
      </section>
      <section className="panel goals-panel">
        <div className="panel-header">
          <div className="section-title">Goals in progress</div>
          <button className="more-button" onClick={() => onNavigate("Finance")}>
            Finance {"\u2192"}
          </button>
        </div>
        {goalCards}
      </section>
    </>
  )
}
