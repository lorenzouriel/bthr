import { NavLink } from 'react-router-dom';
import { RESOURCES, SECTIONS } from '../config/resources';
import { useAuth } from '../auth/AuthContext';

export function Sidebar() {
  const { user, logout } = useAuth();

  return (
    <nav
      style={{
        width: 236,
        flex: 'none',
        display: 'flex',
        flexDirection: 'column',
        padding: '20px 12px 14px',
        borderRight: '1px solid var(--br)',
        overflowY: 'auto',
        height: '100vh',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 8, padding: '0 10px 18px' }}>
        <span style={{ fontFamily: "'Newsreader',serif", fontStyle: 'italic', fontSize: 21 }}>Meridian</span>
        <span style={{ fontSize: 10, fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--m)' }}>
          personal OS
        </span>
      </div>

      {SECTIONS.map((section) => (
        <div key={section.key} style={{ display: 'flex', flexDirection: 'column', gap: 2, marginTop: 24 }}>
          <NavLink
            to={`/${section.key}`}
            style={{ fontSize: 11.5, fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--m)', padding: '4px 10px', textDecoration: 'none', cursor: 'pointer' }}
          >
            {section.label}
          </NavLink>
          {RESOURCES.filter((r) => r.section === section.key).map((r) => (
            <NavLink
              key={r.key}
              to={`/${section.key}/${r.key}`}
              style={({ isActive }) => ({
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                padding: '7px 10px',
                borderRadius: 8,
                fontSize: 13.5,
                fontWeight: isActive ? 600 : 500,
                color: isActive ? 'var(--t)' : 'var(--m)',
                background: isActive ? 'var(--hl)' : 'transparent',
                textDecoration: 'none',
              })}
            >
              {r.label}
            </NavLink>
          ))}
        </div>
      ))}

      <div style={{ marginTop: 'auto', paddingTop: 18, display: 'flex', flexDirection: 'column', gap: 8, padding: '18px 10px 0' }}>
        <span style={{ fontSize: 12.5, fontWeight: 600 }}>{user?.username}</span>
        <button onClick={() => logout()}>Log out</button>
      </div>
    </nav>
  );
}
