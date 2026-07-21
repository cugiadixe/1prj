import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider, useAuth } from './auth/AuthProvider';
import ProtectedRoute from './components/ProtectedRoute';
import AuthenticatedShell from './components/AuthenticatedShell';
import LoginPage from './pages/LoginPage';
import ChangePasswordPage from './pages/ChangePasswordPage';
import Home from './pages/Home';
import SystemHealth from './pages/SystemHealth';

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
            </Route>

            {/* Catch-all */}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </AuthProvider>
      </Router>
    </QueryClientProvider>
  );
};

export default App;
