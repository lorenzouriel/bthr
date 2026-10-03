import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { RESOURCES } from '../config/resources';
import { ResourceList } from '../components/ResourceList';
import { ResourceForm } from '../components/ResourceForm';

export function ResourceSectionPage() {
  const { section, resourceKey } = useParams();
  const config = RESOURCES.find((r) => r.key === resourceKey && r.section === (section === 'wellbeing' ? 'mind' : section));
  const { user } = useAuth();
  const [editing, setEditing] = useState<Record<string, unknown> | null>(null);
  const [showForm, setShowForm] = useState(false);

  if (!config) return <div><h1>Page not found</h1><Link to="/finance">Go to dashboard</Link></div>;

  if (['goals', 'budgets', 'investments'].includes(config.key) && user!.plan < 1) return <div><h1>{config.label}</h1><p>This feature requires plan 1 or above.</p></div>;

  return (
    <div key={config.key} className={`domain-${config.section}`}><Link className="back-link" to={`/${config.section}`}>? {config.section[0].toUpperCase() + config.section.slice(1)} overview</Link>
      <div className="resource-page-heading"><div><span className="page-eyebrow">YOUR RECORDS</span>
      <h1>{config.label}</h1><p className="muted">{config.hasEdit ? "Review, add, and keep your records up to date." : "Review your history and add new entries."}</p></div>
      {!showForm && (
        <button
          onClick={() => {
            setEditing(null);
            setShowForm(true);
          }}
          className="primary-button"
        >
          + Add entry
        </button>
      )}
      </div>
      {showForm && (
        <ResourceForm
          key={String(editing?.id ?? "new")}
          config={config}
          editing={editing}
          onDone={() => {
            setShowForm(false);
            setEditing(null);
          }}
        />
      )}
      <ResourceList
        config={config}
        onEdit={(item) => {
          setEditing(item);
          setShowForm(true);
        }}
      />
    </div>
  );
}
