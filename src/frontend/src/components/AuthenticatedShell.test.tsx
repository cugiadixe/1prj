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

  it('hides Role Management, Admin Group Management and Department Permissions when SECURITY_ADMIN_MANAGE GLOBAL is missing', () => {
    mockHasPermission.mockImplementation((perm) => perm !== 'SECURITY_ADMIN_MANAGE');
    renderShell();
    expect(screen.queryByTestId('nav-role-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-admin-group-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-department-permissions')).not.toBeInTheDocument();
  });

  it('shows Role Management, Admin Group Management and Department Permissions when SECURITY_ADMIN_MANAGE GLOBAL is present', () => {
    mockHasPermission.mockImplementation((perm) => perm === 'SECURITY_ADMIN_MANAGE');
    renderShell();
    expect(screen.getByTestId('nav-role-management')).toBeInTheDocument();
    expect(screen.getByTestId('nav-admin-group-management')).toBeInTheDocument();
    expect(screen.getByTestId('nav-department-permissions')).toBeInTheDocument();
  });

  it('does not show Role Management, Admin Group Management or Department Permissions for SECURITY_AUDIT_VIEW alone', () => {
    mockHasPermission.mockImplementation((perm) => perm === 'SECURITY_AUDIT_VIEW');
    renderShell();
    expect(screen.queryByTestId('nav-role-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-admin-group-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-department-permissions')).not.toBeInTheDocument();
    expect(screen.getByTestId('nav-audit-viewer')).toBeInTheDocument();
  });

  it('does not show Role Management, Admin Group Management or Department Permissions for SECURITY_ACCOUNT_MANAGE alone', () => {
    mockHasPermission.mockImplementation((perm) => perm === 'SECURITY_ACCOUNT_MANAGE');
    renderShell();
    expect(screen.queryByTestId('nav-role-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-admin-group-management')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-department-permissions')).not.toBeInTheDocument();
    expect(screen.getByTestId('nav-account-management')).toBeInTheDocument();
  });
});
