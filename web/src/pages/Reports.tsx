import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { RESOURCES } from '../config/resources';
interface Review { previousStartDate: string; previousEndDate: string; limitation: string; metrics: { key: string; label: string; resource: string; unit: string; value: number | null; previousValue: number | null; count: number; calculation: string }[]; }
export function Reports() {
  const { user } = useAuth();
  const now = new Date();
  const today = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 10);
  const [start, setStart] = useState(today.slice(0, 7) + '-01');
  const [end, setEnd] = useState(today);
  const [domain, setDomain] = useState('body');
  const [combine, setCombine] = useState(false);
  const [params, setParams] = useState<string | null>(null);
  const query = useQuery({ queryKey: ['reports', user!.id, params], enabled: params !== null,
    queryFn: () => apiFetch<Review>(`/api/reports/review?${params}`) });
  return <div><h1>Reports</h1><form onSubmit={e => { e.preventDefault(); setParams(new URLSearchParams({ start_date: start, end_date: end, domain, combine: String(combine) }).toString()); }}>
    <label>Start date<input type="date" required value={start} max={end} onChange={e => setStart(e.target.value)} /></label>
    <label>End date<input type="date" required value={end} min={start} onChange={e => setEnd(e.target.value)} /></label>
    <label>Domain<select value={domain} onChange={e => { setDomain(e.target.value); setCombine(false); }}><option value="body">Body</option><option value="finance">Finance</option><option value="mind">Mind</option><option value="all">All domains</option></select></label>
    {domain === 'all' && <label><input type="checkbox" required checked={combine} onChange={e => setCombine(e.target.checked)} />Combine my finance, body, and mind records</label>}
    <button disabled={query.isFetching}>Generate report</button>
  </form>
  {query.isFetching && <p role="status">Loading report...</p>}
  {query.error && <p role="alert">{query.error.message}</p>}
  {query.data && <><p>Compared with {query.data.previousStartDate} through {query.data.previousEndDate}.</p>
    <table><thead><tr><th>Metric</th><th>Current</th><th>Previous</th><th>Records</th></tr></thead><tbody>{query.data.metrics.map(m => {
      const resource = RESOURCES.find(r => r.key === m.resource)!;
      return <tr key={m.key}><td><Link to={`/${resource.section}/${resource.key}`}>{m.label}</Link><small style={{ display: 'block' }}>{m.calculation}</small></td><td>{m.value ?? 'No records'} {m.unit}</td><td>{m.previousValue ?? 'No records'} {m.unit}</td><td>{m.count}</td></tr>;
    })}</tbody></table>{query.data.metrics.length === 0 && <p>No records for this period.</p>}<p>{query.data.limitation}</p></>}
  </div>;
}
