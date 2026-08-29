import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { AppLayout } from './components/AppLayout';
import { ResourceSectionPage } from './pages/ResourceSectionPage';
import { SectionDashboardPage } from './pages/SectionDashboardPage';

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/finance" replace />} />
            <Route path="/:section" element={<SectionDashboardPage />} />
            <Route path="/:section/:resourceKey" element={<ResourceSectionPage />} />
          </Route>
        </Route>
      </Routes>
    </AuthProvider>
  );
}
