import { useResourceList } from '../hooks/useResourceList';
import { StatCard, BarChart, DotGrid } from '../components/DashboardBlocks';
import { consecutiveStreak, dateKey, isThisMonth, lastNDates } from '../utils/dashboardMath';

interface JournalEntry { id: number; entryDate: string; mood: number | null; }
interface MeditationSession { id: number; sessionDate: string; }

export function WellbeingDashboard() {
  const journalEntries = useResourceList<JournalEntry>('journal-entries');
  const meditationSessions = useResourceList<MeditationSession>('meditation-sessions');

  if (journalEntries.isLoading || meditationSessions.isLoading) {
    return <div style={{ color: 'var(--m)' }}>Loading dashboard…</div>;
  }
  if (journalEntries.error || meditationSessions.error) {
    return <div style={{ color: 'crimson' }}>Failed to load dashboard.</div>;
  }

  const journalRows = journalEntries.data ?? [];
  const meditationRows = meditationSessions.data ?? [];

  const meditationStreak = consecutiveStreak(meditationRows.map((s) => s.sessionDate));
  const journalEntriesThisMonth = journalRows.filter((e) => isThisMonth(e.entryDate)).length;

  const last7 = lastNDates(7);
  const moodsInWindow = journalRows.filter((e) => last7.includes(dateKey(e.entryDate)) && e.mood != null).map((e) => e.mood as number);
  const avgMood = moodsInWindow.length > 0 ? moodsInWindow.reduce((a, b) => a + b, 0) / moodsInWindow.length : 0;

  const chartColumns = last7.map((day) => {
    const moodsForDay = journalRows.filter((e) => dateKey(e.entryDate) === day && e.mood != null).map((e) => e.mood as number);
    const avg = moodsForDay.length > 0 ? moodsForDay.reduce((a, b) => a + b, 0) / moodsForDay.length : 0;
    return { label: new Date(day).toLocaleDateString(undefined, { weekday: 'short' }), value: avg, display: avg ? avg.toFixed(1) : '—' };
  });

  const consistencyDots = lastNDates(28).map((day) => meditationRows.some((s) => dateKey(s.sessionDate) === day));

  return (
    <div>
      <h1 style={{ margin: 0, fontFamily: "'Newsreader',serif", fontWeight: 400, fontSize: 34 }}>Wellbeing</h1>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 14, marginTop: 30 }}>
        <StatCard label="Meditation streak" value={`${meditationStreak} days`} />
        <StatCard label="Journal entries" value={String(journalEntriesThisMonth)} note="this month" />
        <StatCard label="Avg mood" value={avgMood ? avgMood.toFixed(1) : '—'} note="7-day" />
      </div>

      <div style={{ marginTop: 30 }}>
        <BarChart title="Mood — last 7 days" columns={chartColumns} />
      </div>

      <div style={{ marginTop: 30 }}>
        <DotGrid title="Meditation consistency — last 28 days" dots={consistencyDots} />
      </div>
    </div>
  );
}
