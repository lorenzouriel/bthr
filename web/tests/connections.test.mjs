import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import ts from 'typescript';
async function load(path) {
  const source = readFileSync(new URL(path, import.meta.url), 'utf8').replace('import.meta.env.VITE_API_BASE_URL', "''");
  const output = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2020 } }).outputText;
  return import('data:text/javascript;base64,' + Buffer.from(output).toString('base64'));
}
const { RESOURCES } = await load('../src/config/resources.ts');
const { requestValues, initialValues } = await load('../src/utils/resourceValues.ts');
const { apiFetch, setUnauthorizedHandler } = await load('../src/api/client.ts');
test('all resource routes and supported mutations match API controllers', () => {
  const dir = new URL('../../api/FinPulse.Api/Controllers/', import.meta.url);
  const controllers = readdirSync(dir).map(file => readFileSync(new URL(file, dir), 'utf8'));
  for (const resource of RESOURCES) {
    const controller = controllers.find(text => text.includes(`[Route("${resource.basePath.slice(1)}")]`));
    assert.ok(controller, resource.key);
    assert.ok(controller.includes('[HttpGet]') && controller.includes('[HttpPost]'), resource.key);
    assert.equal(controller.includes('[HttpPut('), resource.hasEdit, resource.key);
    assert.equal(controller.includes('[HttpDelete('), resource.hasDelete, resource.key);
  }
});
test('optional numeric/date values become null and decimals remain numbers', () => {
  const meal = RESOURCES.find(r => r.key === 'meals');
  const body = requestValues(meal, { mealDate: '2026-10-03', mealType: 'Lunch', calories: '320', proteinGrams: '12.5', carbsGrams: '', fatGrams: '' });
  assert.equal(body.calories, 320); assert.equal(body.proteinGrams, 12.5); assert.equal(body.fatGrams, null);
  assert.equal(body.mealDate, '2026-10-03');
  const bill = requestValues(RESOURCES.find(r => r.key === 'bills'), { dueDay: '12', endDate: '', paidThisMonth: true });
  assert.equal(bill.endDate, null); assert.equal(bill.dueDay, 12); assert.equal('paidThisMonth' in bill, false);
});
test('editing dates preserves local time through a save', () => {
  const config = RESOURCES.find(r => r.key === 'sleep-logs');
  const original = '2026-10-03T02:30:00.000Z';
  const values = initialValues(config, { bedTime: original, wakeTime: '2026-10-03T10:30:00.000Z' });
  assert.equal(requestValues(config, values).bedTime, original);
  const meal = initialValues(RESOURCES.find(r => r.key === 'meals'), { mealDate: '2026-10-03T00:00:00' });
  assert.equal(meal.mealDate, '2026-10-03');
});
test('API requests send cookies and surface validation and session failures', async () => {
  const originalFetch = globalThis.fetch;
  let expired = 0;
  setUnauthorizedHandler(() => expired++);
  try {
    globalThis.fetch = async (url, options) => {
      assert.equal(url, '/api/auth/login'); assert.equal(options.credentials, 'include');
      return new Response(JSON.stringify({ message: 'Invalid credentials' }), { status: 401 });
    };
    await assert.rejects(apiFetch('/api/auth/login'), /Invalid credentials/); assert.equal(expired, 0);
    globalThis.fetch = async () => new Response('', { status: 401 });
    await assert.rejects(apiFetch('/api/auth/me')); assert.equal(expired, 1);
    globalThis.fetch = async () => new Response(JSON.stringify({ errors: { Amount: ['Amount is required.'] } }), { status: 400 });
    await assert.rejects(apiFetch('/api/records'), /Amount is required/);
    globalThis.fetch = async () => new Response(null, { status: 204 });
    assert.equal(await apiFetch('/api/records', { method: 'DELETE' }), undefined);
  } finally { globalThis.fetch = originalFetch; setUnauthorizedHandler(null); }
});
