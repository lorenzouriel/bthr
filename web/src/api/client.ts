const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');
let onUnauthorized: (() => void) | null = null;
export function setUnauthorizedHandler(handler: (() => void) | null) { onUnauthorized = handler; }
export class ApiError extends Error {
  constructor(message: string, public status: number) { super(message); }
}
export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  if (options.body) headers.set('Content-Type', 'application/json');
  const res = await fetch(`${API_BASE}${path}`, { ...options, credentials: 'include', headers });
  const text = await res.text();
  let body;
  try { body = text ? JSON.parse(text) : undefined; } catch { body = undefined; }
  if (!res.ok) {
    if (res.status === 401 && !['/api/auth/login', '/api/auth/register', '/api/auth/change-password'].includes(path)) onUnauthorized?.();
    const validation = body?.errors ? Object.values(body.errors).flat().join(' ') : '';
    throw new ApiError(validation || body?.message || body?.title || (res.status === 403 ? 'Your account does not have access to this feature.' : `Request failed (${res.status}).`), res.status);
  }
  return body as T;
}
