import { Routes, Route, Navigate, Link, useLocation } from 'react-router-dom';
import { Account } from './pages/Account';
import { Reports } from './pages/Reports';
import { Users } from './pages/Users';
import { AuthProvider } from './auth/AuthContext';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { AppLayout } from './components/AppLayout';
import { ResourceSectionPage } from './pages/ResourceSectionPage';
import { SectionDashboardPage } from './pages/SectionDashboardPage';

export default function App() {
  const location = useLocation();
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/finance" replace />} />
            <Route path="/account" element={<Account />} />
            <Route path="/reports" element={<Reports />} />
            <Route path="/admin/users" element={<Users />} />
            <Route path="/:section" element={<SectionDashboardPage />} />
            <Route path="/:section/:resourceKey" element={<ResourceSectionPage key={location.pathname} />} />
            <Route path="*" element={<div><h1>Page not found</h1><Link to="/finance">Go to dashboard</Link></div>} />
          </Route>
        </Route>
      </Routes>
    </AuthProvider>
  );
}
