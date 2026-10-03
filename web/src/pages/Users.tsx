import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
interface Profile { id: number; username: string; email: string; phoneNumber: string | null; plan: number; }
export function Users() {
  const { user } = useAuth();
  const cache = useQueryClient();
  const [editing, setEditing] = useState<Profile | null>(null);
  const query = useQuery({ queryKey: ['users', user!.id], enabled: !!user?.isAdmin, queryFn: () => apiFetch<Profile[]>('/api/users') });
  const mutation = useMutation({ mutationFn: ({ id, method, body }: { id: number; method: string; body?: unknown }) => apiFetch(`/api/users/${id}`, { method, body: body ? JSON.stringify(body) : undefined }),
    onSuccess: () => { setEditing(null); cache.invalidateQueries({ queryKey: ['users'] }); } });
  const detail = useMutation({ mutationFn: (id: number) => apiFetch<Profile>(`/api/users/${id}`), onSuccess: setEditing });
  if (!user?.isAdmin) return <p>This page requires administrator access.</p>;
  return <div><h1>Users</h1>
    {query.isLoading && <p>Loading users...</p>}
    {[query.error, mutation.error, detail.error].map((e, i) => e && <p role="alert" key={i}>{e.message}</p>)}
    {editing && <form onSubmit={e => { e.preventDefault(); mutation.mutate({ id: editing.id, method: 'PUT', body: { username: editing.username, email: editing.email, phoneNumber: editing.phoneNumber } }); }}>
      <label>Username<input required maxLength={100} value={editing.username} onChange={e => setEditing({ ...editing, username: e.target.value })} /></label>
      <label>Email<input type="email" required maxLength={100} value={editing.email} onChange={e => setEditing({ ...editing, email: e.target.value })} /></label>
      <label>Phone<input maxLength={15} value={editing.phoneNumber ?? ''} onChange={e => setEditing({ ...editing, phoneNumber: e.target.value })} /></label>
      <button disabled={mutation.isPending}>Save user</button><button type="button" onClick={() => setEditing(null)}>Cancel</button>
    </form>}
    {query.data?.map(profile => <div key={profile.id} className="resource-row"><span>{profile.username} ? {profile.email} ? Plan {profile.plan}</span>
      <button disabled={detail.isPending} onClick={() => detail.mutate(profile.id)}>Edit</button>
      <button disabled={mutation.isPending} onClick={() => { if (window.confirm(`Delete ${profile.username}?`)) mutation.mutate({ id: profile.id, method: 'DELETE' }); }}>Delete</button>
    </div>)}
  </div>;
}
