import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { Spin } from 'antd';
import { useAuth } from '../auth/AuthProvider';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

/**
 * ProtectedRoute guards all authenticated routes.
 *
 * Rules (DEC-1B-J-03 / DEC-1B-J-05):
 * - While bootstrapping (silent refresh pending) → show spinner.
 * - Unauthenticated → redirect to /login.
 * - Authenticated + mustChangePassword=true → redirect to /change-password.
 * - Authenticated + mustChangePassword=false → render children.
 */
const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const { isAuthenticated, mustChangePassword, isBootstrapping } = useAuth();
  const location = useLocation();

  if (isBootstrapping) {
    return (
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: '100vh',
        }}
        data-testid="bootstrap-spinner"
      >
        <Spin size="large" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (mustChangePassword) {
    return <Navigate to="/change-password" replace />;
  }

  return <>{children}</>;
};

export default ProtectedRoute;
