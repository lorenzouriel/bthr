import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { ResourceConfig } from '../config/resources';

interface ResourceFormProps {
  config: ResourceConfig;
  editing: Record<string, unknown> | null;
  onDone: () => void;
}

export function ResourceForm({ config, editing, onDone }: ResourceFormProps) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const path = config.basePath.replace('{userId}', String(user!.id));
  const writableFields = config.fields.filter((f) => !f.readOnly);
  const readOnlyFields = config.fields.filter((f) => f.readOnly);

  const [values, setValues] = useState<Record<string, unknown>>(() =>
    Object.fromEntries(
      writableFields.map((f) => [f.name, editing?.[f.name] ?? (f.type === 'checkbox' ? false : '')])
    )
  );

  const mutation = useMutation({
    mutationFn: () =>
      editing
        ? apiFetch(`${path}/${editing.id}`, { method: 'PUT', body: JSON.stringify(values) })
        : apiFetch(path, { method: 'POST', body: JSON.stringify(values) }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [config.key] });
      onDone();
    },
  });

  return (
    <aside
      style={{
        position: 'fixed', top: 0, right: 0, height: '100vh', width: 312,
        display: 'flex', flexDirection: 'column',
        borderLeft: '1px solid var(--br)', background: 'var(--s)',
        boxShadow: '-4px 0 16px rgba(0,0,0,0.08)',
        animation: 'panelSlide .22s ease',
        zIndex: 10,
      }}
    >
      <div style={{ padding: '20px 20px 14px', borderBottom: '1px solid var(--br)', display: 'flex', alignItems: 'baseline', gap: 8 }}>
        <span style={{ fontFamily: "'Newsreader',serif", fontStyle: 'italic', fontSize: 17, flex: 1 }}>
          {editing ? `Edit ${config.label}` : `New ${config.label}`}
        </span>
        <button
          onClick={onDone}
          aria-label="Close"
          style={{ border: 'none', background: 'transparent', cursor: 'pointer', fontSize: 14, color: 'var(--m)' }}
        >
          ✕
        </button>
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          mutation.mutate();
        }}
        style={{ flex: 1, overflowY: 'auto', padding: '18px 20px', margin: 0, border: 'none', maxWidth: 'none' }}
      >
        {writableFields.map((f) => (
          <label key={f.name}>
            {f.label}
            {f.type === 'textarea' ? (
              <textarea
                maxLength={f.maxLength}
                required={f.required}
                value={String(values[f.name] ?? '')}
                onChange={(e) => setValues((v) => ({ ...v, [f.name]: e.target.value }))}
              />
            ) : f.type === 'checkbox' ? (
              <input
                type="checkbox"
                checked={Boolean(values[f.name])}
                onChange={(e) => setValues((v) => ({ ...v, [f.name]: e.target.checked }))}
              />
            ) : (
              <input
                type={f.type === 'datetime' ? 'datetime-local' : f.type}
                maxLength={f.maxLength}
                required={f.required}
                value={String(values[f.name] ?? '')}
                onChange={(e) => setValues((v) => ({ ...v, [f.name]: e.target.value }))}
              />
            )}
          </label>
        ))}

        {readOnlyFields.length > 0 && editing && (
          <div style={{ marginTop: 4, marginBottom: 16 }}>
            {readOnlyFields.map((f) => (
              <div key={f.name} style={{ fontSize: 12.5, color: 'var(--m)' }}>
                {f.label}: {String((editing as Record<string, unknown>)[f.name] ?? '—')}
              </div>
            ))}
          </div>
        )}

        {mutation.isError && <div style={{ color: 'crimson', marginBottom: 12 }}>{(mutation.error as Error).message}</div>}
        <button type="submit" disabled={mutation.isPending}>{editing ? 'Save' : 'Create'}</button>
      </form>
    </aside>
  );
}
