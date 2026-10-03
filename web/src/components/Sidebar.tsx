import { useEffect, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { RESOURCES, SECTIONS } from '../config/resources';
import { useAuth } from '../auth/AuthContext';
const symbols: Record<string, string> = { finance: '?', body: '?', mind: '?' };
export function Sidebar() {
  const { user, logout } = useAuth();
  const { pathname } = useLocation();
  const activeSection = pathname.split('/')[1] === 'wellbeing' ? 'mind' : pathname.split('/')[1];
  const [expanded, setExpanded] = useState<Record<string, boolean>>({ [activeSection]: true });
  const [error, setError] = useState('');
  const [loggingOut, setLoggingOut] = useState(false);
  useEffect(() => { setExpanded(previous => ({ ...previous, [activeSection]: true })); }, [activeSection]);
  return <nav className="sidebar" aria-label="Main navigation">
    <NavLink to="/finance" className="brand">Meridian<span>YOUR PERSONAL SPACE</span></NavLink>
    <p className="nav-caption">OVERVIEW</p>
    {SECTIONS.map(section => <div className={`nav-group domain-${section.key}`} key={section.key}>
      <div className={`nav-heading ${activeSection === section.key ? 'selected' : ''}`}>
        <NavLink to={`/${section.key}`}><span className="domain-icon">{symbols[section.key]}</span>{section.label}</NavLink>
        <button className="expand-button" aria-label={`${expanded[section.key] ? 'Collapse' : 'Expand'} ${section.label}`} aria-expanded={!!expanded[section.key]} aria-controls={`nav-${section.key}`} onClick={() => setExpanded(previous => ({ ...previous, [section.key]: !previous[section.key] }))}>{expanded[section.key] ? '?' : '+'}</button>
      </div>
      {expanded[section.key] && <div className="subnav" id={`nav-${section.key}`}>
        <NavLink end to={`/${section.key}`}>Overview</NavLink>
        {RESOURCES.filter(r => r.section === section.key).map(r => <NavLink key={r.key} to={`/${section.key}/${r.key}`}>{r.label}</NavLink>)}
      </div>}
    </div>)}
    <div className="sidebar-footer">
      <NavLink to="/reports">Reports</NavLink><NavLink to="/account">Account</NavLink>
      {user?.isAdmin && <NavLink to="/admin/users">Users</NavLink>}
      <div className="profile"><span className="avatar">{user?.username.slice(0, 1)}</span><span>{user?.username}<small>Personal workspace</small></span></div>
      {error && <p role="alert">{error}</p>}
      <button disabled={loggingOut} onClick={async () => { setLoggingOut(true); setError(''); try { await logout(); } catch (e) { setError((e as Error).message); } finally { setLoggingOut(false); } }}>Log out</button>
    </div>
  </nav>;
}
