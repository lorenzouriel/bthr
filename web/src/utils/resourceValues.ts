import type { ResourceConfig } from '../config/resources';
export function initialValues(config: ResourceConfig, editing: Record<string, unknown> | null) {
  return Object.fromEntries(config.fields.filter(f => !f.readOnly).map(f => {
    let value = editing?.[f.name] ?? (f.type === 'checkbox' ? false : '');
    if (value && f.type === 'date') value = String(value).slice(0, 10);
    if (value && f.type === 'datetime') {
      const date = new Date(String(value));
      value = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
    }
    return [f.name, value];
  }));
}
export function requestValues(config: ResourceConfig, values: Record<string, unknown>) {
  return Object.fromEntries(config.fields.filter(f => !f.readOnly).map(f => {
    const value = values[f.name];
    if (value === '' || value == null) return [f.name, null];
    if (f.type === 'number') return [f.name, Number(value)];
    if (f.type === 'datetime') return [f.name, new Date(String(value)).toISOString()];
    return [f.name, value];
  }));
}
