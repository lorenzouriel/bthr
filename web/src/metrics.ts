import type { Row } from "./data"

export function dateKey(date = new Date()): string {
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 10)
}

export function days(count: number) {
  return Array.from({ length: count }, (_, index) => {
    const date = new Date()
    date.setDate(date.getDate() - count + index + 1)
    return dateKey(date)
  })
}

export function sum(rows: Row[], key: string) {
  return rows.reduce((value, row) => value + (Number(row[key]) || 0), 0)
}

export function average(rows: Row[], key: string): number | null {
  const valid = rows.filter((row) => row[key] != null)
  return valid.length ? sum(valid, key) / valid.length : null
}

export function inDays(rows: Row[], field: string, count: number) {
  const start = days(count)[0],
    end = dateKey()
  return rows.filter(
    (row) =>
      String(row[field]).slice(0, 10) >= start &&
      String(row[field]).slice(0, 10) <= end,
  )
}

export function streak(rows: Row[], field: string) {
  const dates = new Set(rows.map((row) => String(row[field]).slice(0, 10)))

  const cursor = new Date()
  if (!dates.has(dateKey(cursor))) cursor.setDate(cursor.getDate() - 1)

  let count = 0
  while (dates.has(dateKey(cursor))) {
    count++
    cursor.setDate(cursor.getDate() - 1)
  }
  return count
}

export function money(value: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency,
    }).format(value)
  } catch {
    return `${value.toFixed(2)} ${currency}`
  }
}

export function initialValues(
  fields: { name: string; type: string; readOnly?: boolean }[],
  row?: Row,
) {
  return Object.fromEntries(
    fields
      .filter((field) => !field.readOnly)
      .map((field) => {
        let value =
          row?.[field.name] ??
          (field.type === "checkbox"
            ? false
            : field.type === "date"
              ? dateKey()
              : field.name === "currencyCode"
                ? "BRL"
                : "")

        if (field.type === "date" && value) value = String(value).slice(0, 10)

        if (field.type === "datetime" && value) {
          const date = new Date(value)
          value = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
            .toISOString()
            .slice(0, 16)
        }

        return [field.name, value]
      }),
  )
}

export function serialize(
  fields: { name: string; type: string; readOnly?: boolean }[],
  values: Row,
) {
  return Object.fromEntries(
    fields
      .filter((field) => !field.readOnly)
      .map((field) => {
        const value = values[field.name]

        return [
          field.name,
          value === "" || value == null
            ? null
            : field.type === "number"
              ? Number(value)
              : field.type === "datetime"
                ? new Date(value).toISOString()
                : value,
        ]
      }),
  )
}
