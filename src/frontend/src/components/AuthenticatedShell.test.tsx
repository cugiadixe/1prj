import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import AuthenticatedShell from './AuthenticatedShell';

const mockUseAuth = vi.fn();
vi.mock('../auth/AuthProvider', () => ({
  useAuth: () => mockUseAuth(),
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

const mockUseCompany = vi.fn();
vi.mock('../auth/CompanyProvider', () => ({
  useCompany: () => mockUseCompany(),
}));

let mockHasPermission = vi.fn();

const renderShell = () => {
  return render(
    <BrowserRouter>
      <AuthenticatedShell />
    </BrowserRouter>
  );
};

describe('AuthenticatedShell Navigation Gating', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      user: { username: 'testuser', displayName: 'Test User' },
      logout: vi.fn(),
    });
    mockUseCompany.mockReturnValue({
      companies: [],
      currentCompanyId: null,
      switchCompany: vi.fn(),
    });
    mockHasPermission = vi.fn().mockReturnValue(false);
  });

  it('hides SECURITY_ADMIN_MANAGE-gated items when permission is missing', () => {
    mockHasPermission.mockImplementation((perm: string) => perm !== 'SECURITY_ADMIN_MANAGE');
    renderShell();
    expect(screen.queryByTestId('nav-role-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-admin-group-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-department-permissions')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-effective-permissions')).not.toBeInTheDocument();
  });

  it('shows SECURITY_ADMIN_MANAGE-gated items when permission is present', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'SECURITY_ADMIN_MANAGE');
    renderShell();
    expect(screen.getByTestId('nav-role-management')).toBeInTheDocument();
    expect(screen.getByTestId('nav-admin-group-management')).toBeInTheDocument();
    expect(screen.getByTestId('nav-department-permissions')).toBeInTheDocument();
    expect(screen.getByTestId('nav-effective-permissions')).toBeInTheDocument();
  });

  it('does not show SECURITY_ADMIN_MANAGE-gated items for SECURITY_AUDIT_VIEW alone', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'SECURITY_AUDIT_VIEW');
    renderShell();
    expect(screen.queryByTestId('nav-role-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-admin-group-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-department-permissions')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-effective-permissions')).not.toBeInTheDocument();
    expect(screen.getByTestId('nav-audit-viewer')).toBeInTheDocument();
  });

  it('does not show SECURITY_ADMIN_MANAGE-gated items for SECURITY_ACCOUNT_MANAGE alone', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'SECURITY_ACCOUNT_MANAGE');
    renderShell();
    expect(screen.queryByTestId('nav-role-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-admin-group-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-department-permissions')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-effective-permissions')).not.toBeInTheDocument();
    expect(screen.getByTestId('nav-account-management')).toBeInTheDocument();
  });
});
