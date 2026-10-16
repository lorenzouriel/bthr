// Route allowlist mirrors API route templates without exporting user identifiers.
const finance = new Set(["expenses", "earnings", "bills", "budgets", "goals", "investments"])
const body = new Set(["habits", "habit-logs", "substance-logs", "symptom-logs", "weekly-routines", "workouts", "personal-records", "meals", "water-intake", "body-metrics", "sleep-logs"])
const mind = new Set(["meditation-sessions", "journal-entries"])
export function telemetryRoute(path: string): string {
  const segments = path.split("?")[0].toLowerCase().split("/").filter(Boolean)
  if (segments[0] !== "api") return "unmatched"
  if (segments[1] === "auth" && segments.length === 3 &&
      ["login", "logout", "register", "me", "change-password"].includes(segments[2]))
    return `/api/auth/${segments[2]}`
  if (segments[1] === "users" && /^\d+$/.test(segments[2] ?? "")) {
    let route = ""
    let count = 0
    if (finance.has(segments[3])) { route = `/api/users/{userId}/${segments[3]}`; count = 4 }
    if ((segments[3] === "body" && body.has(segments[4])) || (segments[3] === "mind" && mind.has(segments[4]))) {
      route = `/api/users/{userId}/${segments[3]}/${segments[4]}`; count = 5
    }
    if (route && segments.length === count) return route
    if (route && segments.length === count + 1 && /^\d+$/.test(segments[count])) return route + "/{id}"
  }
  return "/api/other"
}
export function isTelemetryTarget(url: URL, origin: string): boolean {
  return url.origin === origin && url.pathname.startsWith("/api/")
}
