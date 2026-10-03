import { Link, useParams } from 'react-router-dom';
import { FinanceDashboard } from './FinanceDashboard';
import { BodyDashboard } from './BodyDashboard';
import { WellbeingDashboard } from './WellbeingDashboard';
import { RESOURCES } from '../config/resources';
const descriptions: Record<string, string> = {
  finance: 'A clearer picture of your money. Track spending, build savings, and plan what comes next.',
  body: 'Make room for movement, nourishment, and rest. Your daily activity, at a glance.',
  mind: 'A little space to pause. Notice your mood, reflect on your day, and build mindful routines.',
};
export function SectionDashboardPage() {
  const { section: raw } = useParams();
  const section = raw === 'wellbeing' ? 'mind' : raw;
  if (!section || !descriptions[section]) return <div><h1>Page not found</h1><Link to="/finance">Go to dashboard</Link></div>;
  const resources = RESOURCES.filter(r => r.section === section);
  return <div className={`domain-${section} overview-page`}>
    <div className="page-eyebrow">YOUR LIFE, IN VIEW <span>{new Date().toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: 'numeric' })}</span></div>
    <p className="overview-description">{descriptions[section]}</p>
    <div className="dashboard-content">{section === 'finance' ? <FinanceDashboard /> : section === 'body' ? <BodyDashboard /> : <WellbeingDashboard />}</div>
    <section className="explore-section" aria-label="Explore subsections">
      <div className="section-heading"><div><span className="page-eyebrow">EXPLORE & MANAGE</span><h2>Your {section} records</h2></div><span className="muted">{resources.length} sections</span></div>
      <div className="resource-cards">{resources.map((resource, index) => <Link className="resource-card" to={`/${section}/${resource.key}`} key={resource.key}>
        <span className="card-number">{String(index + 1).padStart(2, '0')}</span><span><strong>{resource.label}</strong><small>{resource.hasEdit ? 'View and manage records' : 'View and add records'}</small></span><span className="card-arrow">?</span>
      </Link>)}</div>
    </section>
  </div>;
}
