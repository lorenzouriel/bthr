export function dateKey(value: string): string {
  return value.slice(0, 10);
}

export function isThisMonth(value: string): boolean {
  const d = new Date(value);
  const now = new Date();
  return d.getUTCFullYear() === now.getUTCFullYear() && d.getUTCMonth() === now.getUTCMonth();
}

export function lastNDates(n: number): string[] {
  const out: string[] = [];
  const now = new Date();
  const todayUtc = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
  for (let i = n - 1; i >= 0; i--) {
    out.push(new Date(todayUtc - i * 86400000).toISOString().slice(0, 10));
  }
  return out;
}

export function sumBy<T>(items: T[], pick: (item: T) => number | null | undefined): number {
  return items.reduce((acc, item) => acc + (pick(item) ?? 0), 0);
}

export function mostRecentBy<T>(items: T[], pick: (item: T) => string): T | undefined {
  return [...items].sort((a, b) => pick(b).localeCompare(pick(a)))[0];
}

export function consecutiveStreak(dateStrings: string[]): number {
  const set = new Set(dateStrings.map(dateKey));
  const now = new Date();
  let cursor = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
  let streak = 0;
  while (set.has(new Date(cursor).toISOString().slice(0, 10))) {
    streak += 1;
    cursor -= 86400000;
  }
  return streak;
}
