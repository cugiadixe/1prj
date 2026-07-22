import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider, useAuth } from './auth/AuthProvider';
import { CompanyProvider } from './auth/CompanyProvider';
import ProtectedRoute from './components/ProtectedRoute';
import AuthenticatedShell from './components/AuthenticatedShell';
import LoginPage from './pages/LoginPage';
import ChangePasswordPage from './pages/ChangePasswordPage';
import Home from './pages/Home';
import SystemHealth from './pages/SystemHealth';
import AccountManagementPage from './pages/AccountManagementPage';
import AccountDetailPage from './pages/AccountDetailPage';
import PermissionAssignmentPage from './permissionAssignment/PermissionAssignmentPage';
import AuditViewerPage from './auditViewer/AuditViewerPage';
import RoleManagementPage from './roleManagement/RoleManagementPage';
import AdminGroupManagementPage from './adminGroupManagement/AdminGroupManagementPage';

const queryClient = new QueryClient();

/**
 * ChangePasswordGuard — ensures /change-password is accessible only to
 * authenticated users who are required to change their password.
 * Redirects otherwise (DEC-1B-J-03).
 */
const ChangePasswordGuard: React.FC = () => {
  const { isAuthenticated, mustChangePassword, isBootstrapping } = useAuth();

  if (isBootstrapping) return null;
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (!mustChangePassword) return <Navigate to="/" replace />;

  return <ChangePasswordPage />;
};

const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <AuthProvider>
          <CompanyProvider>
          <Routes>
            {/* Public routes */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/change-password" element={<ChangePasswordGuard />} />

            {/* Authenticated shell — wraps protected pages */}
            <Route
              element={
                <ProtectedRoute>
                  <AuthenticatedShell />
                </ProtectedRoute>
              }
            >
              <Route index element={<Home />} />
              <Route path="system-health" element={<SystemHealth />} />
              {/* Phase 1B.1-K — Account Management UI */}
              <Route path="security/accounts" element={<AccountManagementPage />} />
              <Route path="security/accounts/:accountId" element={<AccountDetailPage />} />
              {/* Phase 1B.1-N — Permission Assignment UI */}
              <Route path="security/permissions/assignments" element={<PermissionAssignmentPage />} />
              {/* Phase 1B.1-O — Audit Viewer UI */}
              <Route path="security/audit" element={<AuditViewerPage />} />
              {/* Phase 1B.1-P1 — Role Management UI */}
              <Route path="security/roles" element={<RoleManagementPage />} />
              {/* Phase 1B.1-P2 — Admin Group Management UI */}
              <Route path="security/admin-groups" element={<AdminGroupManagementPage />} />
            </Route>

            {/* Catch-all */}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </CompanyProvider>
        </AuthProvider>
      </Router>
    </QueryClientProvider>
  );
};

export default App;
