import { useEffect, useRef, useState } from "react"

import { api } from "./api"

import { dateKey, days } from "./metrics"

import { useData } from "./data"

type Review = {
  startDate: string
  endDate: string
  previousStartDate: string
  previousEndDate: string
  domain: string
  limitation: string
  metrics: {
    key: string
    label: string
    unit: string
    value: number | null
    previousValue: number | null
    count: number
    previousCount: number
    calculation: string
  }[]
}

export default function Reports() {
  const { version } = useData()

  const [start, setStart] = useState(days(30)[0]),
    [end, setEnd] = useState(dateKey()),
    [domain, setDomain] = useState("body"),
    [combine, setCombine] = useState(false)

  const [report, setReport] = useState<Review | null>(null),
    [error, setError] = useState(""),
    [busy, setBusy] = useState(false)

  const request = useRef<AbortController | null>(null)

  useEffect(() => () => request.current?.abort(), [])

  useEffect(() => {
    setReport(null)
  }, [version])

  function changed() {
    request.current?.abort()
    setBusy(false)
    setReport(null)
    setError("")
  }

  async function generate(event: React.FormEvent) {
    event.preventDefault()
    request.current?.abort()
    const controller = new AbortController()
    request.current = controller
    setBusy(true)
    setError("")
    setReport(null)
    try {
      const result = await api<Review>(
        `/api/reports/review?${new URLSearchParams({ start_date: start, end_date: end, domain, combine: String(combine) })}`,
        { signal: controller.signal },
      )
      if (!controller.signal.aborted) setReport(result)
    } catch (error) {
      if (!controller.signal.aborted) setError((error as Error).message)
    } finally {
      if (!controller.signal.aborted) setBusy(false)
    }
  }

  function download() {
    if (!report) return
    const url = URL.createObjectURL(
      new Blob([JSON.stringify(report, null, 2)], { type: "application/json" }),
    )
    const link = document.createElement("a")
    link.href = url
    link.download = `bthr-review-${report.startDate}-${report.endDate}.json`
    link.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div className="domain-page reports-page">
      <section className="domain-heading">
        <div>
          <span className="domain-overline">Personal review</span>
          <div className="page-title">Reports</div>
          <p>Compare recorded activity with the preceding period.</p>
        </div>
        <button
          className="button add-button"
          disabled={!report}
          onClick={download}
        >
          Export review
        </button>
      </section>
      <form className="panel live-report-controls" onSubmit={generate}>
        <label>
          Start date
          <input
            type="date"
            required
            value={start}
            max={end}
            onChange={(e) => {
              changed()
              setStart(e.target.value)
            }}
          />
        </label>
        <label>
          End date
          <input
            type="date"
            required
            value={end}
            min={start}
            onChange={(e) => {
              changed()
              setEnd(e.target.value)
            }}
          />
        </label>
        <label>
          Include
          <select
            value={domain}
            onChange={(e) => {
              changed()
              setDomain(e.target.value)
              setCombine(false)
            }}
          >
            <option value="finance">Finance</option>
            <option value="body">Body</option>
            <option value="mind">Mind</option>
            <option value="all">All domains</option>
          </select>
        </label>
        {domain === "all" && (
          <label>
            <input
              type="checkbox"
              required
              checked={combine}
              onChange={(e) => {
                changed()
                setCombine(e.target.checked)
              }}
            />
            Combine my finance, body, and mind records in this report
          </label>
        )}
        <button className="button primary" disabled={busy}>
          {busy ? "Generating..." : "Generate report"}
        </button>
      </form>
      {error && (
        <p role="alert" className="api-error">
          {error}
        </p>
      )}
      {report && (
        <section className="report-sheet">
          <div className="report-cover">
            <div>
              <span>
                {report.startDate} to {report.endDate}
              </span>
              <strong>Your recorded activity</strong>
              <p>
                Compared with {report.previousStartDate} to {" "}
                {report.previousEndDate}
              </p>
            </div>
          </div>
          <div className="live-table-wrap">
            <table className="live-table">
              <thead>
                <tr>
                  <th>Metric</th>
                  <th>Current</th>
                  <th>Previous</th>
                  <th>Change</th>
                  <th>Records</th>
                </tr>
              </thead>
              <tbody>
                {report.metrics.map((metric) => (
                  <tr key={metric.key}>
                    <td title={metric.calculation}>{metric.label}</td>
                    <td>
                      {metric.value ?? "No records"} {metric.unit}
                    </td>
                    <td>
                      {metric.previousValue ?? "No records"} {metric.unit}
                    </td>
                    <td>
                      {metric.value != null && metric.previousValue != null
                        ? `${(metric.value - metric.previousValue).toFixed(2)} ${metric.unit}`
                        : "\u2014"}
                    </td>
                    <td>
                      {metric.count} / {metric.previousCount}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {!report.metrics.length && (
            <p className="empty-message">No records in this period.</p>
          )}
          <p className="data-note report-limitation">{report.limitation}</p>
        </section>
      )}
    </div>
  )
}
