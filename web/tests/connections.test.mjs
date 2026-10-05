import { test } from "node:test"

import assert from "node:assert/strict"

import { readFileSync, readdirSync } from "node:fs"

import ts from "typescript"

async function load(path) {
  const source = readFileSync(new URL(path, import.meta.url), "utf8").replace(
    "import.meta.env.VITE_API_BASE_URL",
    "''",
  )

  const code = ts.transpileModule(source, {
    compilerOptions: {
      module: ts.ModuleKind.ESNext,
      target: ts.ScriptTarget.ES2020,
    },
  }).outputText

  return import(
    "data:text/javascript;base64," + Buffer.from(code).toString("base64")
  )
}

const { RESOURCES } = await load("../src/resources.ts")

const { serialize, initialValues, average, streak, dateKey } =
  await load("../src/metrics.ts")

const { api } = await load("../src/api.ts")

test("all 19 resource paths and available actions match API controllers", () => {
  const folder = new URL("../../api/FinPulse.Api/Controllers/", import.meta.url)

  const controllers = readdirSync(folder).map((file) =>
    readFileSync(new URL(file, folder), "utf8"),
  )

  assert.equal(RESOURCES.length, 19)

  for (const config of RESOURCES) {
    const controller = controllers.find((text) =>
      text.includes(`[Route("${config.basePath.slice(1)}")]`),
    )
    assert.ok(controller, config.key)
    assert.ok(controller.includes("[HttpGet]"))
    assert.ok(controller.includes("[HttpPost]"))
    assert.equal(config.hasEdit, controller.includes("[HttpPut("))
    assert.equal(config.hasDelete, controller.includes("[HttpDelete("))
    assert.equal(
      new Set(config.fields.map((field) => field.name)).size,
      config.fields.length,
    )
  }
})

test("forms send numeric values and nullable optional fields; computed fields are omitted", () => {
  const config = RESOURCES.find((row) => row.key === "bills")

  const body = serialize(config.fields, {
    amount: "123.45",
    dueDay: "7",
    isRecurrent: false,
    endDate: "",
    paidThisMonth: true,
  })

  assert.equal(body.amount, 123.45)
  assert.equal(body.dueDay, 7)
  assert.equal(body.endDate, null)
  assert.equal(body.isRecurrent, false)
  assert.ok(!("paidThisMonth" in body))
})

test("date fields and timestamp editing round-trip without timezone drift", () => {
  const config = RESOURCES.find((row) => row.key === "sleep-logs")

  const row = {
    bedTime: "2026-10-03T02:30:00.000Z",
    wakeTime: "2026-10-03T10:30:00.000Z",
  }

  assert.equal(
    serialize(config.fields, initialValues(config.fields, row)).bedTime,
    row.bedTime,
  )

  const meal = RESOURCES.find((row) => row.key === "meals")
  assert.equal(
    initialValues(meal.fields, { mealDate: "2026-10-03T00:00:00" }).mealDate,
    "2026-10-03",
  )
})

test("missing mood is not averaged as zero; streak permits no entry yet today", () => {
  assert.equal(average([{ mood: null }, { mood: 4 }], "mood"), 4)
  assert.equal(average([], "mood"), null)

  const yesterday = new Date()
  yesterday.setDate(yesterday.getDate() - 1)
  assert.equal(streak([{ date: dateKey(yesterday) }], "date"), 1)
})

test("client sends cookies, surfaces validation, and expires only protected sessions", async () => {
  const original = globalThis.fetch
  const originalWindow = globalThis.window
  let expired = 0

  globalThis.window = { dispatchEvent: () => expired++ }

  try {
    globalThis.fetch = async (path, options) => {
      assert.equal(options.credentials, "include")
      return new Response(JSON.stringify({ message: "Invalid credentials" }), {
        status: 401,
      })
    }

    await assert.rejects(api("/api/auth/login"), /Invalid credentials/)
    assert.equal(expired, 0)

    await assert.rejects(api("/api/auth/me"))
    assert.equal(expired, 1)

    globalThis.fetch = async () =>
      new Response(
        JSON.stringify({ errors: { Amount: ["Amount is required"] } }),
        { status: 400 },
      )
    await assert.rejects(api("/api/records"), /Amount is required/)

    globalThis.fetch = async () => new Response(null, { status: 204 })
    assert.equal(await api("/api/records", { method: "DELETE" }), undefined)
  } finally {
    globalThis.fetch = original
    globalThis.window = originalWindow
  }
})
