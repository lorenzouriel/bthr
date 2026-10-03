import { useEffect, useRef, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useResourceList } from '../hooks/useResourceList';
import type { ResourceConfig } from '../config/resources';
type Row = Record<string, unknown>;
function display(value: unknown, type?: string) {
  if (value == null || value === '') return '?';
  if (typeof value === 'boolean') return value ? 'Yes' : 'No';
  if (typeof value === 'number') return value.toLocaleString(undefined, { maximumFractionDigits: 2 });
  if (type === 'date') return String(value).slice(0, 10);
  if (type === 'datetime') return new Date(String(value)).toLocaleString();
  return String(value);
}
export function ResourceList({ config, onEdit }: { config: ResourceConfig; onEdit: (item: Row) => void }) {
  const { user } = useAuth();
  const cache = useQueryClient();
  const query = useResourceList(config.key);
  const [search, setSearch] = useState('');
  const [sort, setSort] = useState(config.dateField ?? config.listPrimary);
  const [ascending, setAscending] = useState(false);
  const [page, setPage] = useState(0);
  const [pendingDelete, setPendingDelete] = useState<Row | null>(null);
  const dialog = useRef<HTMLDialogElement>(null);
  useEffect(() => { if (pendingDelete) dialog.current?.showModal(); }, [pendingDelete]);
  const columns = [...new Set([config.listPrimary, ...config.listSecondary, ...(config.listValue ? [config.listValue] : []), ...config.fields.map(f => f.name)])];
  const deletion = useMutation({
    mutationFn: (id: unknown) => apiFetch(`${config.basePath.replace('{userId}', String(user!.id))}/${id}`, { method: 'DELETE' }),
    onSuccess: () => { setPendingDelete(null); cache.invalidateQueries({ queryKey: ['records', user!.id] }); cache.invalidateQueries({ queryKey: ['reports', user!.id] }); },
  });
  const rows = (query.data ?? []).filter(row => columns.some(column => display(row[column]).toLowerCase().includes(search.toLowerCase()))).sort((a, b) => {
    const left = a[sort], right = b[sort];
    const order = typeof left === 'number' && typeof right === 'number' ? left - right : String(left ?? '').localeCompare(String(right ?? ''));
    return ascending ? order : -order;
  });
  const pageCount = Math.max(1, Math.ceil(rows.length / 15));
  const currentPage = Math.min(page, pageCount - 1);
  return <section className="records-panel" aria-label={`${config.label} records`}>
    <div className="table-toolbar"><label className="search-label">Search records<input type="search" placeholder={`Search ${config.label.toLowerCase()}?`} value={search} onChange={e => { setSearch(e.target.value); setPage(0); }} /></label><span className="record-count">{rows.length} records</span><button onClick={() => query.refetch()} disabled={query.isFetching}>Refresh</button></div>
    {query.isLoading ? <p className="table-message" role="status">Loading records?</p> : query.error ? <p className="table-message" role="alert">{query.error.message}</p> : rows.length === 0 ? <div className="empty-state"><span>?</span><h2>{search ? 'No matching records' : 'A fresh start'}</h2><p>{search ? 'Try a different search.' : `Add your first entry to start tracking ${config.label.toLowerCase()}.`}</p></div> : <>
      <div className="table-scroll" tabIndex={0} role="region" aria-label="Scrollable records table"><table className="records-table"><thead><tr>{columns.map(column => <th key={column} aria-sort={sort === column ? ascending ? 'ascending' : 'descending' : 'none'}><button onClick={() => { setSort(column); setAscending(sort === column ? !ascending : true); setPage(0); }}>{config.fields.find(f => f.name === column)?.label ?? column}{sort === column ? ascending ? ' ?' : ' ?' : ''}</button></th>)}{(config.hasEdit || config.hasDelete) && <th className="actions-cell">Actions</th>}</tr></thead>
      <tbody>{rows.slice(currentPage * 15, currentPage * 15 + 15).map(row => <tr key={String(row.id)}>{columns.map(column => <td key={column} title={display(row[column], config.fields.find(f => f.name === column)?.type)}><span className={typeof row[column] === 'boolean' ? `status-pill ${row[column] ? 'complete' : ''}` : ''}>{display(row[column], config.fields.find(f => f.name === column)?.type)}</span></td>)}{(config.hasEdit || config.hasDelete) && <td className="actions-cell"><div className="table-actions">{config.hasEdit && <button onClick={() => onEdit(row)} aria-label={`Edit ${display(row[config.listPrimary])}`}>Edit</button>}{config.hasDelete && <button className="delete-button" onClick={() => { deletion.reset(); setPendingDelete(row); }} aria-label={`Delete ${display(row[config.listPrimary])}`}>Delete</button>}</div></td>}</tr>)}</tbody></table></div>
      <div className="table-pagination"><span>Showing {currentPage * 15 + 1}?{Math.min((currentPage + 1) * 15, rows.length)} of {rows.length}</span><div><button disabled={currentPage === 0} onClick={() => setPage(currentPage - 1)}>Previous</button><span> {currentPage + 1} / {pageCount} </span><button disabled={currentPage + 1 >= pageCount} onClick={() => setPage(currentPage + 1)}>Next</button></div></div>
    </>}
    {pendingDelete && <dialog ref={dialog} className="confirm-dialog" aria-labelledby="delete-title" onCancel={() => setPendingDelete(null)}><h2 id="delete-title">Delete this entry?</h2><p>{display(pendingDelete[config.listPrimary])}</p><p>The entry will be removed from your active records.</p>{deletion.error && <p role="alert">{deletion.error.message}</p>}<div className="dialog-actions"><button autoFocus disabled={deletion.isPending} onClick={() => setPendingDelete(null)}>Cancel</button><button className="danger-button" disabled={deletion.isPending} onClick={() => deletion.mutate(pendingDelete.id)}>{deletion.isPending ? 'Deleting?' : 'Delete entry'}</button></div></dialog>}
  </section>;
}
