import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@testing-library/jest-dom';
import React from 'react';
import UserRoleAssignmentsPage from './UserRoleAssignmentsPage';
import { AuthProvider } from '../auth/AuthProvider';
import { CompanyProvider } from '../auth/CompanyProvider';
import { userRoleAssignmentsApi } from './userRoleAssignmentsApi';
import { roleManagementApi } from '../roleManagement/roleManagementApi';
import { vi, describe, it, expect, beforeEach } from 'vitest';

vi.mock('./userRoleAssignmentsApi');
vi.mock('../roleManagement/roleManagementApi');
vi.mock('../auth/AuthProvider', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../auth/AuthProvider')>();
  return {
    ...actual,
    useAuth: vi.fn(() => ({
      isAuthenticated: true,
      user: { userId: 1, username: 'admin' },
      mustChangePassword: false,
    })),
    usePermissions: vi.fn(() => ({
      hasPermission: vi.fn(() => true),
    }))
  };
});

const mockedUserRoleAssignmentsApi = vi.mocked(userRoleAssignmentsApi);
const mockedRoleManagementApi = vi.mocked(roleManagementApi);

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

describe('UserRoleAssignmentsPage', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = createTestQueryClient();
    vi.clearAllMocks();

    mockedRoleManagementApi.getRoles.mockResolvedValue([
      {
        id: 1,
        roleCode: 'ADMIN',
        name: 'Administrator',
        description: null,
        scopeType: 'GLOBAL',
        companyId: null,
        isActive: true,
        permissionCodes: [],
        rowVersion: 'v1',
      },
      {
        id: 2,
        roleCode: 'USER',
        name: 'Regular User',
        description: null,
        scopeType: 'COMPANY',
        companyId: null,
        isActive: true,
        permissionCodes: [],
        rowVersion: 'v1',
      },
    ]);

    mockedUserRoleAssignmentsApi.getUserRoleAssignments.mockResolvedValue([
      {
        id: 101,
        userId: 1,
        roleId: 1,
        roleCode: 'ADMIN',
        roleName: 'Administrator',
        scopeType: 'GLOBAL',
        companyId: null,
        effectiveFrom: new Date(Date.now() - 10000).toISOString(),
        effectiveTo: null,
        isActive: true,
        rowVersion: 'v1',
      },
    ]);
  });

  const renderComponent = (userId: string = '1') => {
    return render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[`/security/users/${userId}/role-assignments`]}>
          <CompanyProvider>
            <Routes>
              <Route path="/security/users/:userId/role-assignments" element={<UserRoleAssignmentsPage />} />
            </Routes>
          </CompanyProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );
  };

  it('renders user ID and role assignments table', async () => {
    renderComponent('1');

    expect(await screen.findByTestId('user-id-display')).toHaveTextContent('User ID: 1');
    expect(await screen.findByTestId('assignments-table')).toBeInTheDocument();
    
    await waitFor(() => {
      expect(screen.getByTestId('assignment-role-name-101')).toHaveTextContent('Administrator');
      expect(screen.getByTestId('assignment-scope-GLOBAL')).toBeInTheDocument();
      expect(screen.getByTestId('assignment-status-101')).toHaveTextContent('ACTIVE');
    });
  });

  it('shows error if User ID is missing/invalid', async () => {
    renderComponent('invalid');
    expect(await screen.findByTestId('invalid-user-id')).toBeInTheDocument();
  });

  it('opens assign modal and allows submitting', async () => {
    const user = userEvent.setup();
    renderComponent('1');

    await screen.findByTestId('assignments-table');
    
    const assignBtn = screen.getByTestId('assign-role-button');
    await user.click(assignBtn);

    const modal = await screen.findByTestId('assign-role-modal');
    expect(modal).toBeInTheDocument();

    const select = screen.getByTestId('assign-role-select');
    await user.click(select);
    const option = await screen.findByTestId('role-option-1');
    await user.click(option);

    // Mock successful assign
    mockedUserRoleAssignmentsApi.assignRoleToUser.mockResolvedValue({
      id: 102,
      userId: 1,
      roleId: 1,
      roleCode: 'ADMIN',
      roleName: 'Administrator',
      scopeType: 'GLOBAL',
      companyId: null,
      effectiveFrom: new Date().toISOString(),
      effectiveTo: null,
      isActive: true,
      rowVersion: 'v1',
    });

    const submitBtn = within(modal).getByRole('button', { name: /ok/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(mockedUserRoleAssignmentsApi.assignRoleToUser).toHaveBeenCalledWith(1, expect.objectContaining({
        roleId: 1
      }), undefined); // companyId is undefined for GLOBAL
    });
  });

  it('shows error when assigning COMPANY scope without selected company', async () => {
    const user = userEvent.setup();
    renderComponent('1');

    await screen.findByTestId('assignments-table');
    
    const assignBtn = screen.getByTestId('assign-role-button');
    await user.click(assignBtn);

    const modal = await screen.findByTestId('assign-role-modal');
    expect(modal).toBeInTheDocument();

    const select = screen.getByTestId('assign-role-select');
    await user.click(select);
    
    const option = await screen.findByTestId('role-option-2'); // COMPANY role
    await user.click(option);

    const submitBtn = within(modal).getByRole('button', { name: /ok/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByTestId('assign-error')).toHaveTextContent('A specific company must be selected to assign a COMPANY-scoped role.');
    });
    
    expect(mockedUserRoleAssignmentsApi.assignRoleToUser).not.toHaveBeenCalled();
  });

  it('deactivates assignment', async () => {
    const user = userEvent.setup();
    renderComponent('1');

    await screen.findByTestId('assignments-table');
    
    const deactivateBtn = await screen.findByTestId('deactivate-assignment-button-101');
    await user.click(deactivateBtn);

    const modal = await screen.findByTestId('deactivate-assignment-modal');
    expect(modal).toBeInTheDocument();

    mockedUserRoleAssignmentsApi.deactivateUserRoleAssignment.mockResolvedValue(undefined);

    const submitBtn = within(modal).getByRole('button', { name: /deactivate/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(mockedUserRoleAssignmentsApi.deactivateUserRoleAssignment).toHaveBeenCalledWith(
        1,
        101,
        { rowVersion: 'v1' },
        undefined
      );
    });
  });

  it('handles permission denied error (403)', async () => {
    mockedUserRoleAssignmentsApi.getUserRoleAssignments.mockRejectedValue({ response: { status: 403 } });
    renderComponent('1');
    expect(await screen.findByTestId('assignments-permission-denied')).toBeInTheDocument();
  });

  it('handles not found error (404)', async () => {
    mockedUserRoleAssignmentsApi.getUserRoleAssignments.mockRejectedValue({ response: { status: 404 } });
    renderComponent('1');
    expect(await screen.findByTestId('assignments-not-found')).toBeInTheDocument();
  });
});
