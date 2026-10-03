import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { RESOURCES } from '../config/resources';

export function useResourceList<T = Record<string, unknown>>(resourceKey: string, enabled = true) {
  const { user } = useAuth();
  const config = RESOURCES.find((r) => r.key === resourceKey)!;
  const path = config.basePath.replace('{userId}', String(user!.id));

  return useQuery({
    queryKey: ['records', user!.id, resourceKey],
    enabled: enabled && !!user && (!['goals', 'budgets', 'investments'].includes(resourceKey) || user.plan >= 1),
    queryFn: () => apiFetch<T[]>(path),
  });
}
