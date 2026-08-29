import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { RESOURCES } from '../config/resources';
import { ResourceList } from '../components/ResourceList';
import { ResourceForm } from '../components/ResourceForm';

export function ResourceSectionPage() {
  const { resourceKey } = useParams();
  const config = RESOURCES.find((r) => r.key === resourceKey);
  const [editing, setEditing] = useState<Record<string, unknown> | null>(null);
  const [showForm, setShowForm] = useState(false);

  if (!config) return <div>Unknown resource.</div>;

  return (
    <div key={config.key}>
      <h1>{config.label}</h1>
      {!showForm && (
        <button
          onClick={() => {
            setEditing(null);
            setShowForm(true);
          }}
          style={{ marginBottom: 16, background: 'var(--t)', color: 'var(--b)', border: 'none' }}
        >
          + New
        </button>
      )}
      {showForm && (
        <ResourceForm
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
