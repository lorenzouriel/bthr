import { useState, type CSSProperties } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { ResourceConfig } from '../config/resources';

interface ResourceListProps {
  config: ResourceConfig;
  onEdit: (item: Record<string, unknown>) => void;
}

type SortKey = 'primary' | 'date' | 'value';
type Row = Record<string, unknown>;

const MONTH_NAMES = ['January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'];

function monthLabel(yearMonth: string): string {
  const [year, month] = yearMonth.split('-');
  return `${MONTH_NAMES[Number(month) - 1]} ${year}`;
}

function applyFilterSortGroup(
  data: Row[],
  config: ResourceConfig,
  search: string,
  sortKey: SortKey | null,
  sortDir: 'asc' | 'desc',
  groupByMonth: boolean,
) {
  const q = search.trim().toLowerCase();
  const filtered = !q ? data : data.filter((item) => {
    const primary = String(item[config.listPrimary] ?? '').toLowerCase();
    const secondary = config.listSecondary.map((f) => String(item[f] ?? '')).join(' ').toLowerCase();
    return primary.includes(q) || secondary.includes(q);
  });

  const sorted = !sortKey ? filtered : [...filtered].sort((a, b) => {
    let cmp = 0;
    if (sortKey === 'primary') cmp = String(a[config.listPrimary] ?? '').localeCompare(String(b[config.listPrimary] ?? ''));
    if (sortKey === 'date' && config.dateField) cmp = String(a[config.dateField] ?? '').localeCompare(String(b[config.dateField] ?? ''));
    if (sortKey === 'value' && config.listValue) cmp = Number(a[config.listValue] ?? 0) - Number(b[config.listValue] ?? 0);
    return sortDir === 'asc' ? cmp : -cmp;
  });

  if (!groupByMonth || !config.dateField) {
    return { flat: sorted, groups: null as null | { key: string; label: string; rows: Row[] }[] };
  }

  const byKey = new Map<string, Row[]>();
  for (const item of sorted) {
    const raw = String(item[config.dateField] ?? '');
    const key = raw.slice(0, 7) || 'unknown';
    if (!byKey.has(key)) byKey.set(key, []);
    byKey.get(key)!.push(item);
  }
  const keys = [...byKey.keys()].sort((a, b) => b.localeCompare(a));
  const groups = keys.map((key) => ({
    key, label: key === 'unknown' ? 'Unknown date' : monthLabel(key), rows: byKey.get(key)!,
  }));
  return { flat: sorted, groups };
}

function pillStyle(active: boolean): CSSProperties {
  return {
    fontSize: 12, fontWeight: 600, padding: '5px 11px', borderRadius: 99,
    border: '1px solid var(--br)', background: active ? 'var(--t)' : 'var(--s)',
    color: active ? 'var(--b)' : 'var(--m)', cursor: 'pointer',
  };
}

function rowTitle(config: ResourceConfig, item: Row): string {
  const primary = item[config.listPrimary];
  if (primary) return String(primary);
  const content = item['content'];
  if (typeof content === 'string' && content.length > 0) {
    return content.length > 40 ? `${content.slice(0, 40)}…` : content;
  }
  return 'Untitled';
}

export function ResourceList({ config, onEdit }: ResourceListProps) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const path = config.basePath.replace('{userId}', String(user!.id));

  const [search, setSearch] = useState('');
  const [sortKey, setSortKey] = useState<SortKey | null>(null);
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [groupByMonth, setGroupByMonth] = useState(false);

  const toggleSort = (key: SortKey) => {
    if (sortKey !== key) { setSortKey(key); setSortDir('asc'); }
    else if (sortDir === 'asc') { setSortDir('desc'); }
    else { setSortKey(null); }
  };

  const { data, isLoading, error } = useQuery({
    queryKey: [config.key],
    queryFn: () => apiFetch<Row[]>(path),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => apiFetch<void>(`${path}/${id}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [config.key] }),
  });

  if (isLoading) return <div style={{ color: 'var(--m)' }}>Loading {config.label}…</div>;
  if (error) return <div style={{ color: 'crimson' }}>Failed to load {config.label}: {(error as Error).message}</div>;
  if (!data || data.length === 0) return <div style={{ color: 'var(--m)' }}>No {config.label.toLowerCase()} yet.</div>;

  const { flat, groups } = applyFilterSortGroup(data, config, search, sortKey, sortDir, groupByMonth);
  const filteredCount = groups ? groups.reduce((n, g) => n + g.rows.length, 0) : flat.length;

  const renderRow = (item: Row) => (
    <div
      key={item.id as number}
      className="resource-row"
      style={{ display: 'flex', alignItems: 'baseline', gap: 12, padding: '11px 2px', borderBottom: '1px solid var(--br)' }}
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: 1, flex: 1, minWidth: 0 }}>
        <span style={{ fontSize: 14, fontWeight: 500 }}>{rowTitle(config, item)}</span>
        {config.listSecondary.length > 0 && (
          <span style={{ fontSize: 11.5, color: 'var(--m)' }}>
            {config.listSecondary.map((f) => String(item[f] ?? '—')).join(' · ')}
          </span>
        )}
      </div>
      {config.listValue && (
        <span style={{ fontSize: 12, color: 'var(--m)', fontVariantNumeric: 'tabular-nums', flex: 'none' }}>
          {String(item[config.listValue] ?? '—')}
        </span>
      )}
      {(config.hasEdit || config.hasDelete) && (
        <span className="row-actions" style={{ display: 'flex', gap: 6, flex: 'none' }}>
          {config.hasEdit && (
            <button onClick={() => onEdit(item)} aria-label="Edit"
              style={{ border: 'none', background: 'transparent', padding: 4, cursor: 'pointer', fontSize: 13 }}>
              ✎
            </button>
          )}
          {config.hasDelete && (
            <button onClick={() => deleteMutation.mutate(item.id as number)} disabled={deleteMutation.isPending} aria-label="Delete"
              style={{ border: 'none', background: 'transparent', padding: 4, cursor: 'pointer', fontSize: 13, color: 'var(--m)' }}>
              ✕
            </button>
          )}
        </span>
      )}
    </div>
  );

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16, flexWrap: 'wrap' }}>
        <input
          type="text"
          placeholder={`Search ${config.label.toLowerCase()}…`}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ width: 220 }}
        />
        <button type="button" onClick={() => toggleSort('primary')} style={pillStyle(sortKey === 'primary')}>
          Name{sortKey === 'primary' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
        </button>
        {config.dateField && (
          <button type="button" onClick={() => toggleSort('date')} style={pillStyle(sortKey === 'date')}>
            Date{sortKey === 'date' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
          </button>
        )}
        {config.listValue && (
          <button type="button" onClick={() => toggleSort('value')} style={pillStyle(sortKey === 'value')}>
            Value{sortKey === 'value' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
          </button>
        )}
        {config.dateField && (
          <button type="button" onClick={() => setGroupByMonth((g) => !g)} style={pillStyle(groupByMonth)}>
            Group by month
          </button>
        )}
        <span style={{ marginLeft: 'auto', fontSize: 11.5, color: 'var(--m)' }}>
          {filteredCount === data.length ? `${data.length} ${config.label.toLowerCase()}` : `${filteredCount} of ${data.length}`}
        </span>
      </div>

      {filteredCount === 0 ? (
        <div style={{ color: 'var(--m)' }}>No results for "{search}".</div>
      ) : groups ? (
        groups.map((group) => (
          <div key={group.key}>
            <div style={{ fontSize: 12, fontWeight: 600, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--m)', padding: '18px 2px 8px' }}>
              {group.label}
            </div>
            {group.rows.map(renderRow)}
          </div>
        ))
      ) : (
        flat.map(renderRow)
      )}
    </div>
  );
}
