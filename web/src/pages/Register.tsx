import { useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Register() {
  const { register, user, isLoading } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const requested = location.state?.from;
  const destination = typeof requested === 'string' && requested.startsWith('/') && !requested.startsWith('//') ? requested : '/finance';
  const [username, setUsername] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await register({ username, phoneNumber, email, password });
      navigate(destination, { replace: true });
    } catch (err) {
      setError((err as Error).message || 'Registration failed');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isLoading && user) return <Navigate to={destination} replace />;

  return (
    <div style={{ maxWidth: 360, margin: '80px auto', padding: '0 20px' }}>
      <h1>bthr</h1>
      <p style={{ color: 'var(--m)' }}>Create your account</p>
      <form onSubmit={onSubmit}>
        <label>
          Username
          <input required maxLength={100} autoComplete="username" value={username} onChange={(e) => setUsername(e.target.value)} />
        </label>
        <label>
          Phone number
          <input type="tel" required maxLength={15} autoComplete="tel" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} />
        </label>
        <label>
          Email
          <input type="email" autoComplete="email" maxLength={100} required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Password (8+ characters, including a number and special character)
          <input type="password" autoComplete="new-password" minLength={8} maxLength={100} required value={password} onChange={(e) => setPassword(e.target.value)} />
        </label>
        {error && <div style={{ color: 'crimson', marginBottom: 12 }}>{error}</div>}
        <button type="submit" disabled={isSubmitting}>{isSubmitting ? 'Creating…' : 'Register'}</button>
      </form>
      <p>Already have an account? <Link to="/login" state={{ from: destination }}>Log in</Link></p>
    </div>
  );
}
