import { test } from "node:test"
import assert from "node:assert/strict"
import { readFileSync } from "node:fs"
import ts from "typescript"
const source = readFileSync(new URL("../src/telemetry-policy.ts", import.meta.url), "utf8")
const code = ts.transpileModule(source, {compilerOptions: {module: ts.ModuleKind.ESNext}}).outputText
const {telemetryRoute, isTelemetryTarget} = await import("data:text/javascript;base64," + Buffer.from(code).toString("base64"))
test("telemetry strips user IDs, record IDs, queries, and unknown content", () => {
  assert.equal(telemetryRoute("/api/users/123/mind/journal-entries/789?token=secret"), "/api/users/{userId}/mind/journal-entries/{id}")
  assert.equal(telemetryRoute("/api/users/123/body/water-intake"), "/api/users/{userId}/body/water-intake")
  assert.equal(telemetryRoute("/api/users/private@example.com"), "/api/other")
  assert.equal(telemetryRoute("/api/auth/login?password=secret"), "/api/auth/login")
  assert.equal(telemetryRoute("/api/users/1/body/habits/private-note"), "/api/other")
})
test("trace context only goes to same-origin API, never ingest or third parties", () => {
  const origin = "https://bthr.example"
  assert.equal(isTelemetryTarget(new URL("/api/auth/me", origin), origin), true)
  for (const url of ["https://other.example/api/auth/me", "https://bthr.example/telemetry/v1/traces", "https://bthr.example/api-other"])
    assert.equal(isTelemetryTarget(new URL(url), origin), false)
})
