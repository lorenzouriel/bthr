import { useResourceList } from '../hooks/useResourceList';
import { StatCard, BarChart, ProgressBars } from '../components/DashboardBlocks';
import { dateKey, isThisMonth, lastNDates, sumBy } from '../utils/dashboardMath';

interface Expense { id: number; amount: number; category: string; expenseDate: string; }
interface Earning { id: number; amount: number; category: string; earningDate: string; }
interface Goal { id: number; name: string; currentAmount: number; targetAmount: number; currencyCode: string; }

export function FinanceDashboard() {
  const expenses = useResourceList<Expense>('expenses');
  const earnings = useResourceList<Earning>('earnings');
  const goals = useResourceList<Goal>('goals');

  if (expenses.isLoading || earnings.isLoading || goals.isLoading) {
    return <div style={{ color: 'var(--m)' }}>Loading dashboard…</div>;
  }
  if (expenses.error || earnings.error || goals.error) {
    return <div style={{ color: 'crimson' }}>Failed to load dashboard.</div>;
  }

  const expenseRows = expenses.data ?? [];
  const earningRows = earnings.data ?? [];
  const goalRows = goals.data ?? [];

  const spentThisMonth = sumBy(expenseRows.filter((e) => isThisMonth(e.expenseDate)), (e) => e.amount);
  const earnedThisMonth = sumBy(earningRows.filter((e) => isThisMonth(e.earningDate)), (e) => e.amount);
  const net = earnedThisMonth - spentThisMonth;

  const chartColumns = lastNDates(7).map((day) => {
    const total = sumBy(expenseRows.filter((e) => dateKey(e.expenseDate) === day), (e) => e.amount);
    return { label: new Date(day).toLocaleDateString(undefined, { weekday: 'short' }), value: total, display: total ? total.toFixed(0) : '—' };
  });

  const progressRows = goalRows.map((g) => ({
    label: g.name,
    value: `${g.currentAmount.toFixed(0)} / ${g.targetAmount.toFixed(0)} ${g.currencyCode}`,
    percent: g.targetAmount > 0 ? (g.currentAmount / g.targetAmount) * 100 : 0,
  }));

  const transactions = [
    ...expenseRows.map((e) => ({ t: e.category, s: e.expenseDate.slice(0, 10), r: `-${e.amount.toFixed(2)}`, date: e.expenseDate })),
    ...earningRows.map((e) => ({ t: e.category, s: e.earningDate.slice(0, 10), r: `+${e.amount.toFixed(2)}`, date: e.earningDate })),
  ].sort((a, b) => b.date.localeCompare(a.date)).slice(0, 8);

  return (
    <div>
      <h1 style={{ margin: 0, fontFamily: "'Newsreader',serif", fontWeight: 400, fontSize: 34 }}>Finance</h1>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 14, marginTop: 30 }}>
        <StatCard label="Spent this month" value={spentThisMonth.toFixed(2)} />
        <StatCard label="Earned this month" value={earnedThisMonth.toFixed(2)} />
        <StatCard label="Net this month" value={net.toFixed(2)} />
      </div>

      <div style={{ marginTop: 30 }}>
        <BarChart title="Expenses — last 7 days" columns={chartColumns} />
      </div>

      {progressRows.length > 0 && (
        <div style={{ marginTop: 30 }}>
          <ProgressBars title="Goals" rows={progressRows} />
        </div>
      )}

      <div style={{ marginTop: 30 }}>
        <h2 style={{ margin: '0 0 14px', fontSize: 12, fontWeight: 600, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--m)' }}>Recent transactions</h2>
        {transactions.length === 0 ? (
          <div style={{ color: 'var(--m)' }}>No transactions yet.</div>
        ) : (
          transactions.map((tx, i) => (
            <div key={i} style={{ display: 'flex', alignItems: 'baseline', gap: 12, padding: '11px 2px', borderBottom: '1px solid var(--br)' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 1, flex: 1, minWidth: 0 }}>
                <span style={{ fontSize: 14, fontWeight: 500 }}>{tx.t}</span>
                <span style={{ fontSize: 11.5, color: 'var(--m)' }}>{tx.s}</span>
              </div>
              <span style={{ fontSize: 12, color: 'var(--m)', flex: 'none' }}>{tx.r}</span>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
