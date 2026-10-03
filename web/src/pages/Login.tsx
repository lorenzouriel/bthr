import { useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Login() {
  const { login, user, isLoading } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const requested = location.state?.from;
  const destination = typeof requested === 'string' && requested.startsWith('/') && !requested.startsWith('//') ? requested : '/finance';
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login({ email, password });
      navigate(destination, { replace: true });
    } catch (err) {
      setError((err as Error).message || 'Invalid credentials');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isLoading && user) return <Navigate to={destination} replace />;

  return (
    <div style={{ maxWidth: 360, margin: '80px auto', padding: '0 20px' }}>
      <h1>bthr</h1>
      <p style={{ color: 'var(--m)' }}>Log in to your account</p>
      <form onSubmit={onSubmit}>
        <label>
          Email
          <input type="email" autoComplete="email" maxLength={100} required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Password
          <input type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        </label>
        {error && <div style={{ color: 'crimson', marginBottom: 12 }}>{error}</div>}
        <button type="submit" disabled={isSubmitting}>{isSubmitting ? 'Logging in…' : 'Log in'}</button>
      </form>
      <p>No account? <Link to="/register" state={{ from: destination }}>Register</Link></p>
    </div>
  );
}
