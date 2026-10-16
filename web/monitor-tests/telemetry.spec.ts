import { test, expect } from "@playwright/test"

test("browser emits three sanitized signals and propagates its span to API", async ({ page }) => {
  const exports: { signal: string; body: string }[] = []
  let traceparent = ""
  await page.route("**/telemetry/v1/*", async route => {
    exports.push({ signal: new URL(route.request().url()).pathname, body: route.request().postData() ?? "" })
    if (process.env.MONITOR_WEB_URL) await route.continue()
    else await route.fulfill({ status: 200, contentType: "application/json", body: "{}" })
  })
  if (!process.env.MONITOR_WEB_URL) {
    await page.route("**/api/**", async route => {
      if (route.request().url().includes("/auth/login")) traceparent = route.request().headers().traceparent ?? ""
      await route.fulfill({ status: 401, contentType: "application/json", body: '{"message":"Invalid credentials"}' })
    })
  } else {
    page.on("request", request => {
      if (request.url().includes("/api/auth/login")) traceparent = request.headers().traceparent ?? ""
    })
  }
  await page.goto("/")
  await page.evaluate(async () => {
    await fetch("/api/auth/login?private=canary-secret", {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email: "telemetry-smoke@example.invalid", password: "canary-secret" }),
    })
    window.dispatchEvent(new ErrorEvent("error", { message: "private-journal-canary" }))
  })
  await expect.poll(() => exports.some(item => item.signal.endsWith("/metrics")), { timeout: 25000 }).toBe(true)
  await expect.poll(() => exports.some(item => item.signal.endsWith("/logs"))).toBe(true)
  await expect.poll(() => exports.some(item => item.signal.endsWith("/traces"))).toBe(true)
  expect(traceparent).toMatch(/^00-[a-f0-9]{32}-[a-f0-9]{16}-01$/)
  const body = exports.map(item => item.body).join("\n")
  expect(body).not.toContain("canary-secret")
  expect(body).not.toContain("private-journal-canary")
  expect(body).not.toContain("telemetry-smoke@example.invalid")
  expect(body).toContain(traceparent.split("-")[1])
  expect(body).toContain(traceparent.split("-")[2])
  expect(body).toContain("browser.fetch.duration")
  expect(body).toContain("browser.page.load.duration")
  expect(body).toContain("bthr.Web")
})
