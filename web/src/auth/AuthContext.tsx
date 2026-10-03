import { useQueryClient } from '@tanstack/react-query';
import { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { authApi } from '../api/auth';
import { setUnauthorizedHandler } from '../api/client';
import type { AuthUser, LoginRequest, RegisterRequest } from '../types/dto';

interface AuthState {
  user: AuthUser | null;
  isLoading: boolean;
  login: (req: LoginRequest) => Promise<void>;
  register: (req: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const restoreSession = useCallback(async () => {
    try {
      const me = await authApi.me();
      setUser(me);
    } catch {
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(() => { queryClient.clear(); setUser(null); });
    restoreSession();
    return () => setUnauthorizedHandler(null);
  }, [restoreSession, queryClient]);

  const login = async (req: LoginRequest) => {
    await authApi.login(req);
    const me = await authApi.me();
    queryClient.clear();
    setUser(me);
  };

  const register = async (req: RegisterRequest) => {
    await authApi.register(req);
    const me = await authApi.me();
    queryClient.clear();
    setUser(me);
  };

  const logout = async () => {
    await authApi.logout();
    queryClient.clear();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
