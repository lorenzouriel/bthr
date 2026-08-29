import { apiFetch } from './client';
import type { AuthUser, LoginRequest, RegisterRequest } from '../types/dto';

export const authApi = {
  register: (req: RegisterRequest) =>
    apiFetch<{ userId: number; token: string }>('/api/auth/register', { method: 'POST', body: JSON.stringify(req) }),
  login: (req: LoginRequest) =>
    apiFetch<{ userId: number; token: string }>('/api/auth/login', { method: 'POST', body: JSON.stringify(req) }),
  logout: () => apiFetch<void>('/api/auth/logout', { method: 'POST' }),
  me: () => apiFetch<AuthUser>('/api/auth/me'),
};
