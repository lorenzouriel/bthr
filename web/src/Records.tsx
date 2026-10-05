import { useEffect, useRef, useState } from "react"

import { api } from "./api"

import { canAccess, useData, type Row } from "./data"

import { RESOURCES } from "./resources"

import { initialValues, serialize } from "./metrics"

export type Editor = { key: string; row?: Row }

export const groups: Record<string, Record<string, string[]>> = {
  Finance: {
    Transactions: ["expenses", "earnings"],
    Bills: ["bills"],
    Budgets: ["budgets"],
    Goals: ["goals"],
    Investments: ["investments"],
  },

  Body: {
    Training: ["workouts", "weekly-routines", "personal-records"],
    Nutrition: ["meals", "water-intake"],
    Recovery: ["sleep-logs"],
    Habits: ["habits", "habit-logs"],
    Measurements: ["body-metrics"],
    "Health Log": ["symptom-logs", "substance-logs"],
  },

  Mind: { Meditation: ["meditation-sessions"], Journal: ["journal-entries"] },
}

function show(value: unknown) {
  return value == null || value === ""
    ? "\u2014"
    : typeof value === "boolean"
      ? value
        ? "Yes"
        : "No"
      : String(value)
}

export default function Records({
  resourceKey,
  onEdit,
}: {
  resourceKey: string
  onEdit: (editor: Editor) => void
}) {
  const { user, records, errors, loading, refresh } = useData()

  const config = RESOURCES.find((item) => item.key === resourceKey)!

  const [search, setSearch] = useState("")

  const [page, setPage] = useState(0)

  const [sort, setSort] = useState(config.dateField ?? config.listPrimary)

  const [asc, setAsc] = useState(false)

  const [from, setFrom] = useState(""),
    [to, setTo] = useState("")

  const [deleting, setDeleting] = useState<Row | null>(null)

  const [busy, setBusy] = useState(false),
    [error, setError] = useState("")

  const confirm = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    if (deleting) confirm.current?.showModal()
  }, [deleting])

  if (!canAccess(resourceKey, user!))
    return (
      <div className="panel">
        <h2>{config.label}</h2>
        <p>This section requires plan 1 or above.</p>
      </div>
    )

  const columns = config.fields

  const rows = (records[resourceKey] ?? [])
    .filter(
      (row) =>
        columns.some((field) =>
          show(row[field.name]).toLowerCase().includes(search.toLowerCase()),
        ) &&
        (!config.dateField ||
          ((!from || String(row[config.dateField]).slice(0, 10) >= from) &&
            (!to || String(row[config.dateField]).slice(0, 10) <= to))),
    )
    .sort((a, b) => {
      const delta =
        typeof a[sort] === "number" && typeof b[sort] === "number"
          ? a[sort] - b[sort]
          : show(a[sort]).localeCompare(show(b[sort]))
      return asc ? delta : -delta
    })

  const pages = Math.max(1, Math.ceil(rows.length / 15)),
    current = Math.min(page, pages - 1)

  async function remove() {
    setBusy(true)
    setError("")

    try {
      await api(
        `${config.basePath.replace("{userId}", String(user!.id))}/${deleting!.id}`,
        { method: "DELETE" },
      )
      setDeleting(null)
      refresh()
    } catch (error) {
      setError((error as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="panel records-panel">
      <div className="panel-header">
        <div>
          <span className="section-kicker">{rows.length} records</span>
          <div className="section-title">{config.label}</div>
        </div>
        <button
          className={`button compact ${config.section}`}
          onClick={() => onEdit({ key: resourceKey })}
        >
          + Add record
        </button>
      </div>
      <div className="live-filters">
        <label>
          Search
          <input
            type="search"
            placeholder="Search records"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPage(0)
            }}
          />
        </label>
        {config.dateField && (
          <>
            <label>
              From
              <input
                type="date"
                value={from}
                max={to || undefined}
                onChange={(e) => {
                  setFrom(e.target.value)
                  setPage(0)
                }}
              />
            </label>
            <label>
              To
              <input
                type="date"
                value={to}
                min={from || undefined}
                onChange={(e) => {
                  setTo(e.target.value)
                  setPage(0)
                }}
              />
            </label>
          </>
        )}
        <button className="filter-button" disabled={loading} onClick={refresh}>
          Refresh
        </button>
      </div>
      {loading && <p role="status">Refreshing records...</p>}
      {errors[resourceKey] ? (
        <p className="api-error" role="alert">
          {errors[resourceKey]}
        </p>
      ) : (
        <>
          <div
            className="live-table-wrap"
            tabIndex={0}
            role="region"
            aria-label={`${config.label} table`}
          >
            <table className="live-table">
              <thead>
                <tr>
                  {columns.map((field) => (
                    <th
                      key={field.name}
                      aria-sort={
                        sort === field.name
                          ? asc
                            ? "ascending"
                            : "descending"
                          : "none"
                      }
                    >
                      <button
                        onClick={() => {
                          setSort(field.name)
                          setAsc(sort === field.name ? !asc : true)
                        }}
                      >
                        {field.label}
                        {sort === field.name ? (asc ? " \u2191" : " \u2193") : ""}
                      </button>
                    </th>
                  ))}
                  {(config.hasEdit || config.hasDelete) && <th>Actions</th>}
                </tr>
              </thead>
              <tbody>
                {rows.slice(current * 15, current * 15 + 15).map((row) => (
                  <tr key={row.id}>
                    {columns.map((field) => {
                      const value =
                        field.name === "habitId"
                          ? (records.habits?.find(
                              (habit) => habit.id === row.habitId,
                            )?.habitName ?? `Habit #${row.habitId}`)
                          : field.type === "date" && row[field.name]
                            ? row[field.name].slice(0, 10)
                            : field.type === "datetime" && row[field.name]
                              ? new Date(row[field.name]).toLocaleString()
                              : row[field.name]

                      return (
                        <td key={field.name} title={show(value)}>
                          {show(value)}
                        </td>
                      )
                    })}
                    {(config.hasEdit || config.hasDelete) && (
                      <td className="live-actions">
                        {config.hasEdit && (
                          <button
                            className="filter-button"
                            onClick={() => onEdit({ key: resourceKey, row })}
                          >
                            Edit
                          </button>
                        )}
                        {config.hasDelete && (
                          <button
                            className="filter-button danger"
                            onClick={() => {
                              setError("")
                              setDeleting(row)
                            }}
                          >
                            Delete
                          </button>
                        )}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {!rows.length && !loading && (
            <p className="empty-message">
              {search || from || to
                ? "No matching records. Adjust the filters."
                : "No records yet. Add your first entry."}
            </p>
          )}
          <div className="live-pagination">
            <span>
              {rows.length
                ? `${current * 15 + 1}-${Math.min(rows.length, current * 15 + 15)} of ${rows.length}`
                : "0 records"}
            </span>
            <button
              className="filter-button"
              disabled={!current}
              onClick={() => setPage(current - 1)}
            >
              Previous
            </button>
            <button
              className="filter-button"
              disabled={current + 1 >= pages}
              onClick={() => setPage(current + 1)}
            >
              Next
            </button>
          </div>
        </>
      )}
      {!config.hasEdit && (
        <p className="data-note">
          This record type supports adding and viewing entries. Editing and
          deleting are not available in the API.
        </p>
      )}
      {deleting && (
        <dialog
          className="live-dialog"
          ref={confirm}
          onCancel={(e) => {
            if (busy) e.preventDefault()
            else setDeleting(null)
          }}
        >
          <h2>Delete this record?</h2>
          <p>{show(deleting[config.listPrimary])}</p>
          <p>It will be removed from your active records.</p>
          {error && <p role="alert">{error}</p>}
          <div className="form-actions">
            <button
              autoFocus
              className="button ghost"
              disabled={busy}
              onClick={() => setDeleting(null)}
            >
              Cancel
            </button>
            <button className="button primary" disabled={busy} onClick={remove}>
              {busy ? "Deleting..." : "Delete"}
            </button>
          </div>
        </dialog>
      )}
    </section>
  )
}

export function RecordEditor({
  editor,
  onClose,
}: {
  editor: Editor
  onClose: () => void
}) {
  const { user, records, errors, refresh } = useData()

  const config = RESOURCES.find((item) => item.key === editor.key)!

  const [values, setValues] = useState<Row>(() =>
    initialValues(config.fields, editor.row),
  )

  const [busy, setBusy] = useState(false),
    [error, setError] = useState("")

  const dialog = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    dialog.current?.showModal()
  }, [])

  const writable = config.fields.filter((field) => !field.readOnly)

  async function save(event: React.FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError("")

    try {
      if (!canAccess(config.key, user!) || (editor.row && !config.hasEdit))
        throw new Error(
          "This action is not available for this account or record type.",
        )

      const path = config.basePath.replace("{userId}", String(user!.id))

      await api(editor.row ? `${path}/${editor.row.id}` : path, {
        method: editor.row ? "PUT" : "POST",
        body: JSON.stringify(serialize(config.fields, values)),
      })

      refresh()
      onClose()
    } catch (error) {
      setError((error as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <dialog
      ref={dialog}
      className="quick-panel live-editor"
      aria-label={`${editor.row ? "Edit" : "Add"} ${config.label}`}
      onCancel={(e) => {
        if (busy) e.preventDefault()
        else onClose()
      }}
    >
      <div className="quick-header">
        <div>
          <span className="eyebrow">{config.section}</span>
          <div className="quick-title">
            {editor.row ? "Edit" : "Add"} {config.label}
          </div>
          <p>Save the details to your personal records.</p>
        </div>
        <button
          className="icon-button"
          disabled={busy}
          aria-label="Close"
          onClick={onClose}
        >
          ?
        </button>
      </div>
      <form className="entry-form" onSubmit={save}>
        <fieldset disabled={busy}>
          {writable.map((field, index) => (
            <label key={field.name}>
              {field.label}
              {field.name === "habitId" ? (
                <>
                  <select
                    required
                    value={values[field.name]}
                    onChange={(e) =>
                      setValues({ ...values, [field.name]: e.target.value })
                    }
                  >
                    <option value="">Select a habit</option>
                    {editor.row &&
                      !(records.habits ?? []).some(
                        (habit) => habit.id === editor.row!.habitId,
                      ) && (
                        <option value={editor.row.habitId}>
                          Archived habit #{editor.row.habitId}
                        </option>
                      )}
                    {records.habits?.map((habit) => (
                      <option key={habit.id} value={habit.id}>
                        {habit.habitName}
                      </option>
                    ))}
                  </select>
                  {errors.habits && <span role="alert">{errors.habits}</span>}
                  {!records.habits?.length && !editor.row && (
                    <small>Add a habit before recording its history.</small>
                  )}
                </>
              ) : field.type === "textarea" ? (
                <textarea
                  required={field.required}
                  maxLength={field.maxLength}
                  value={values[field.name]}
                  onChange={(e) =>
                    setValues({ ...values, [field.name]: e.target.value })
                  }
                />
              ) : field.type === "checkbox" ? (
                <input
                  type="checkbox"
                  checked={!!values[field.name]}
                  onChange={(e) =>
                    setValues({ ...values, [field.name]: e.target.checked })
                  }
                />
              ) : (
                <input
                  autoFocus={index === 0}
                  type={
                    field.type === "datetime" ? "datetime-local" : field.type
                  }
                  required={field.required}
                  maxLength={field.maxLength}
                  min={field.min}
                  max={field.max}
                  step={
                    field.type === "number" ? (field.step ?? "any") : undefined
                  }
                  value={values[field.name]}
                  onChange={(e) =>
                    setValues({ ...values, [field.name]: e.target.value })
                  }
                />
              )}
            </label>
          ))}
        </fieldset>
        {error && (
          <p className="api-error" role="alert">
            {error}
          </p>
        )}
        <div className="form-actions">
          <button
            className="button ghost"
            type="button"
            disabled={busy}
            onClick={onClose}
          >
            Cancel
          </button>
          <button className="button primary" disabled={busy}>
            {busy ? "Saving..." : "Save record"}
          </button>
        </div>
      </form>
    </dialog>
  )
}

export function RecordPicker({
  onSelect,
  onClose,
}: {
  onSelect: (editor: Editor) => void
  onClose: () => void
}) {
  const { user } = useData()

  const dialog = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    dialog.current?.showModal()
  }, [])

  return (
    <dialog
      ref={dialog}
      className="quick-panel live-editor"
      aria-label="Choose a record type"
      onCancel={onClose}
    >
      <div className="quick-header">
        <div>
          <span className="eyebrow">Quick entry</span>
          <div className="quick-title">What do you want to record?</div>
        </div>
        <button className="icon-button" onClick={onClose} aria-label="Close">
          ?
        </button>
      </div>
      <div className="quick-groups">
        {["finance", "body", "mind"].map((section) => (
          <div className="quick-group" key={section}>
            <div className={`quick-group-title ${section}`}>{section}</div>
            <div className="quick-options">
              {RESOURCES.filter((config) => config.section === section).map(
                (config) => (
                  <button
                    key={config.key}
                    disabled={!canAccess(config.key, user!)}
                    onClick={() => onSelect({ key: config.key })}
                  >
                    {config.label}
                    {!canAccess(config.key, user!) ? " | Plan 1" : " \u2192"}
                  </button>
                ),
              )}
            </div>
          </div>
        ))}
      </div>
    </dialog>
  )
}
