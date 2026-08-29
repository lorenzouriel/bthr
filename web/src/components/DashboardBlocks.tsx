import type { CSSProperties } from 'react';

const sectionTitleStyle: CSSProperties = {
  margin: '0 0 14px', fontSize: 12, fontWeight: 600, letterSpacing: '0.12em',
  textTransform: 'uppercase', color: 'var(--m)',
};

export function StatCard({ label, value, note }: { label: string; value: string; note?: string }) {
  return (
    <div style={{ padding: '16px 18px', borderRadius: 12, background: 'var(--s)', border: '1px solid var(--br)' }}>
      <div style={{ fontSize: 11.5, fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase', color: 'var(--m)' }}>{label}</div>
      <div style={{ fontFamily: "'Newsreader',serif", fontWeight: 300, fontSize: 26, marginTop: 6 }}>{value}</div>
      {note && <div style={{ fontSize: 11.5, color: 'var(--m)', marginTop: 2 }}>{note}</div>}
    </div>
  );
}

export function BarChart({ title, columns }: { title: string; columns: { label: string; value: number; display: string }[] }) {
  const max = Math.max(1, ...columns.map((c) => c.value));
  return (
    <div>
      <h2 style={sectionTitleStyle}>{title}</h2>
      <div style={{ display: 'flex', gap: 12, padding: '20px 20px 14px', borderRadius: 12, background: 'var(--s)', border: '1px solid var(--br)' }}>
        {columns.map((c, i) => (
          <div key={i} style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 7, height: 150 }}>
            <div style={{ flex: 1, display: 'flex', alignItems: 'flex-end' }}>
              <div style={{ height: `${Math.max(4, Math.round((c.value / max) * 100))}%`, width: '100%', maxWidth: 30, margin: '0 auto', borderRadius: 4, background: c.value ? 'var(--t)' : 'var(--hl)' }} />
            </div>
            <div style={{ textAlign: 'center', fontSize: 11 }}>{c.display}</div>
            <div style={{ textAlign: 'center', fontSize: 10.5, color: 'var(--m)' }}>{c.label}</div>
          </div>
        ))}
      </div>
    </div>
  );
}

export function ProgressBars({ title, rows }: { title: string; rows: { label: string; value: string; percent: number }[] }) {
  return (
    <div>
      <h2 style={sectionTitleStyle}>{title}</h2>
      <div>
        {rows.map((r, i) => (
          <div key={i} style={{ display: 'flex', flexDirection: 'column', gap: 7, padding: '11px 2px', borderBottom: '1px solid var(--br)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 12 }}>
              <span style={{ fontSize: 13.5, fontWeight: 500 }}>{r.label}</span>
              <span style={{ fontSize: 12, color: 'var(--m)' }}>{r.value}</span>
            </div>
            <div style={{ height: 4, borderRadius: 2, background: 'var(--hl)', overflow: 'hidden' }}>
              <div style={{ width: `${Math.min(100, Math.max(0, r.percent))}%`, height: '100%', borderRadius: 2, background: 'var(--t)' }} />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export function DotGrid({ title, dots }: { title: string; dots: boolean[] }) {
  return (
    <div>
      <h2 style={sectionTitleStyle}>{title}</h2>
      <div style={{ display: 'inline-flex', flexWrap: 'wrap', gap: 6, padding: 18, borderRadius: 12, background: 'var(--s)', border: '1px solid var(--br)', maxWidth: 322 }}>
        {dots.map((on, i) => (
          <span key={i} style={{ width: 15, height: 15, borderRadius: 4, background: on ? 'var(--t)' : 'var(--hl)', opacity: on ? 0.85 : 1, flex: 'none' }} />
        ))}
      </div>
    </div>
  );
}
