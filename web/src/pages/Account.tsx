import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
export function Account() {
  const { user } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const mutation = useMutation({
    mutationFn: () => apiFetch('/api/auth/change-password', { method: 'POST', body: JSON.stringify({ currentPassword, newPassword }) }),
    onSuccess: () => { setCurrentPassword(''); setNewPassword(''); },
  });
  return <div><h1>Account</h1><p>{user?.username} ? {user?.email} ? Plan {user?.plan}</p>
    <h2>Change password</h2><form onSubmit={e => { e.preventDefault(); mutation.mutate(); }}>
      <label>Current password<input type="password" autoComplete="current-password" required value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} /></label>
      <label>New password<input type="password" autoComplete="new-password" minLength={8} required value={newPassword} onChange={e => setNewPassword(e.target.value)} /></label>
      <p>Use at least 8 characters, including a number and special character.</p>
      {mutation.error && <p role="alert">{mutation.error.message}</p>}
      {mutation.isSuccess && <p role="status">Password changed.</p>}
      <button disabled={mutation.isPending}>Change password</button>
    </form></div>;
}
