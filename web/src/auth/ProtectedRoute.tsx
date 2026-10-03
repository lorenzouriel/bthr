import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';

export function ProtectedRoute() {
  const location = useLocation();
  const { user, isLoading } = useAuth();

  if (isLoading) return <div style={{ padding: 40 }}>Loading…</div>;
  if (!user) return <Navigate to="/login" state={{ from: location.pathname + location.search }} replace />;

  return <Outlet />;
}
