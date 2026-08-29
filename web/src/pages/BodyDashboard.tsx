import { useResourceList } from '../hooks/useResourceList';
import { StatCard, BarChart, DotGrid } from '../components/DashboardBlocks';
import { dateKey, lastNDates, mostRecentBy, sumBy } from '../utils/dashboardMath';

interface BodyMetric { id: number; measuredDate: string; weightKg: number | null; }
interface Meal { id: number; mealDate: string; calories: number; }
interface WaterIntake { id: number; intakeDate: string; amountMl: number; }
interface SleepLog { id: number; bedTime: string; wakeTime: string; totalHours: number | null; }
interface Workout { id: number; workoutDate: string; durationMinutes: number | null; }

export function BodyDashboard() {
  const bodyMetrics = useResourceList<BodyMetric>('body-metrics');
  const meals = useResourceList<Meal>('meals');
  const waterIntake = useResourceList<WaterIntake>('water-intake');
  const sleepLogs = useResourceList<SleepLog>('sleep-logs');
  const workouts = useResourceList<Workout>('workouts');

  const loading = bodyMetrics.isLoading || meals.isLoading || waterIntake.isLoading || sleepLogs.isLoading || workouts.isLoading;
  const failed = bodyMetrics.error || meals.error || waterIntake.error || sleepLogs.error || workouts.error;

  if (loading) return <div style={{ color: 'var(--m)' }}>Loading dashboard…</div>;
  if (failed) return <div style={{ color: 'crimson' }}>Failed to load dashboard.</div>;

  const bodyMetricRows = bodyMetrics.data ?? [];
  const mealRows = meals.data ?? [];
  const waterRows = waterIntake.data ?? [];
  const sleepRows = sleepLogs.data ?? [];
  const workoutRows = workouts.data ?? [];

  const today = dateKey(new Date().toISOString());

  const latestWeight = mostRecentBy(bodyMetricRows, (m) => m.measuredDate);
  const caloriesToday = sumBy(mealRows.filter((m) => dateKey(m.mealDate) === today), (m) => m.calories);
  const waterToday = sumBy(waterRows.filter((w) => dateKey(w.intakeDate) === today), (w) => w.amountMl);
  const latestSleep = mostRecentBy(sleepRows, (s) => s.wakeTime);

  const chartColumns = lastNDates(7).map((day) => {
    const total = sumBy(workoutRows.filter((w) => dateKey(w.workoutDate) === day), (w) => w.durationMinutes);
    return { label: new Date(day).toLocaleDateString(undefined, { weekday: 'short' }), value: total, display: total ? total.toFixed(0) : '—' };
  });

  const consistencyDots = lastNDates(28).map((day) => workoutRows.some((w) => dateKey(w.workoutDate) === day));

  return (
    <div>
      <h1 style={{ margin: 0, fontFamily: "'Newsreader',serif", fontWeight: 400, fontSize: 34 }}>Body</h1>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 14, marginTop: 30 }}>
        <StatCard label="Weight" value={latestWeight?.weightKg != null ? `${latestWeight.weightKg} kg` : '—'} />
        <StatCard label="Calories today" value={caloriesToday.toFixed(0)} />
        <StatCard label="Water today" value={`${waterToday} ml`} />
        <StatCard label="Sleep last night" value={latestSleep?.totalHours != null ? `${latestSleep.totalHours} h` : '—'} />
      </div>

      <div style={{ marginTop: 30 }}>
        <BarChart title="Minutes trained — last 7 days" columns={chartColumns} />
      </div>

      <div style={{ marginTop: 30 }}>
        <DotGrid title="Workout consistency — last 28 days" dots={consistencyDots} />
      </div>
    </div>
  );
}
