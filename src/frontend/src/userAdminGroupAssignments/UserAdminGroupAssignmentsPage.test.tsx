import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import '@testing-library/jest-dom';

import UserAdminGroupAssignmentsPage from './UserAdminGroupAssignmentsPage';
import { CompanyProvider } from '../auth/CompanyProvider';
import { userAdminGroupAssignmentsApi } from './userAdminGroupAssignmentsApi';
import { adminGroupManagementApi } from '../adminGroupManagement/adminGroupManagementApi';
import { vi, describe, it, expect, beforeEach } from 'vitest';

vi.mock('./userAdminGroupAssignmentsApi');
vi.mock('../adminGroupManagement/adminGroupManagementApi');
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

const mockedUserAdminGroupAssignmentsApi = vi.mocked(userAdminGroupAssignmentsApi);
const mockedAdminGroupManagementApi = vi.mocked(adminGroupManagementApi);

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

describe('UserAdminGroupAssignmentsPage', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = createTestQueryClient();
    vi.clearAllMocks();

    mockedAdminGroupManagementApi.getAdminGroups.mockResolvedValue([
      {
        id: 1,
        groupCode: 'ADMIN_GROUP',
        name: 'Administrators Group',
        description: null,
        scopeType: 'GLOBAL',
        companyId: null,
        isActive: true,
        permissionCodes: [],
        rowVersion: 'v1',
      },
      {
        id: 2,
        groupCode: 'COMPANY_ADMIN_GROUP',
        name: 'Company Admins',
        description: null,
        scopeType: 'COMPANY',
        companyId: null,
        isActive: true,
        permissionCodes: [],
        rowVersion: 'v1',
      },
    ]);

    mockedUserAdminGroupAssignmentsApi.getUserAdminGroupAssignments.mockResolvedValue([
      {
        id: 101,
        userId: 1,
        adminGroupId: 1,
        groupCode: 'ADMIN_GROUP',
        groupName: 'Administrators Group',
        assignmentStatus: 'Active',
        effectiveFrom: new Date(Date.now() - 10000).toISOString(),
        effectiveTo: null,
        rowVersion: 'v1',
      },
    ]);
  });

  const renderComponent = (userId: string = '1') => {
    return render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[`/security/users/${userId}/admin-group-assignments`]}>
          <CompanyProvider>
            <Routes>
              <Route path="/security/users/:userId/admin-group-assignments" element={<UserAdminGroupAssignmentsPage />} />
            </Routes>
          </CompanyProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );
  };

  it('renders user ID and admin group assignments table', async () => {
    renderComponent('1');

    expect(await screen.findByTestId('user-id-display')).toHaveTextContent('Mã người dùng: 1');
    expect(await screen.findByTestId('assignments-table')).toBeInTheDocument();
    
    await waitFor(() => {
      expect(screen.getByTestId('assignment-group-name-101')).toHaveTextContent('Administrators Group');
      expect(screen.getByTestId('assignment-scope-GLOBAL')).toBeInTheDocument();
      expect(screen.getByTestId('assignment-status-101')).toHaveTextContent(/^HOẠT ĐỘNG$/);
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
    
    const assignBtn = screen.getByTestId('assign-admin-group-button');
    await user.click(assignBtn);

    const modal = await screen.findByTestId('assign-admin-group-modal');
    expect(modal).toBeInTheDocument();

    const select = screen.getByTestId('assign-admin-group-select');
    await user.click(select);
    const option = await screen.findByTestId('admin-group-option-1');
    await user.click(option);

    // Mock successful assign
    mockedUserAdminGroupAssignmentsApi.assignAdminGroupToUser.mockResolvedValue({
      id: 102,
      userId: 1,
      adminGroupId: 1,
      groupCode: 'ADMIN_GROUP',
      groupName: 'Administrators Group',
      assignmentStatus: 'Active',
      effectiveFrom: new Date().toISOString(),
      effectiveTo: null,
      rowVersion: 'v1',
    });

    const submitBtn = within(modal).getByRole('button', { name: /ok/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(mockedUserAdminGroupAssignmentsApi.assignAdminGroupToUser).toHaveBeenCalledWith(1, expect.objectContaining({
        adminGroupId: 1
      }), undefined); // companyId is undefined for GLOBAL
    });
  });

  it('shows error when assigning COMPANY scope without selected company', async () => {
    const user = userEvent.setup();
    renderComponent('1');

    await screen.findByTestId('assignments-table');
    
    const assignBtn = screen.getByTestId('assign-admin-group-button');
    await user.click(assignBtn);

    const modal = await screen.findByTestId('assign-admin-group-modal');
    expect(modal).toBeInTheDocument();

    const select = screen.getByTestId('assign-admin-group-select');
    await user.click(select);
    
    const option = await screen.findByTestId('admin-group-option-2'); // COMPANY admin group
    await user.click(option);

    const submitBtn = within(modal).getByRole('button', { name: /ok/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByTestId('assign-error')).toHaveTextContent('Phải chọn một công ty cụ thể để phân công nhóm quản trị có phạm vi COMPANY.');
    });
    
    expect(mockedUserAdminGroupAssignmentsApi.assignAdminGroupToUser).not.toHaveBeenCalled();
  });

  it('deactivates assignment', async () => {
    const user = userEvent.setup();
    renderComponent('1');

    await screen.findByTestId('assignments-table');
    
    const deactivateBtn = await screen.findByTestId('deactivate-assignment-button-101');
    await user.click(deactivateBtn);

    const modal = await screen.findByTestId('deactivate-assignment-modal');
    expect(modal).toBeInTheDocument();

    mockedUserAdminGroupAssignmentsApi.deactivateUserAdminGroupAssignment.mockResolvedValue(undefined);

    const submitBtn = within(modal).getByRole('button', { name: /vô hiệu hóa/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(mockedUserAdminGroupAssignmentsApi.deactivateUserAdminGroupAssignment).toHaveBeenCalledWith(
        1,
        101,
        { rowVersion: 'v1' },
        undefined
      );
    });
  });

  it('handles permission denied error (403)', async () => {
    mockedUserAdminGroupAssignmentsApi.getUserAdminGroupAssignments.mockRejectedValue({ response: { status: 403 } });
    renderComponent('1');
    expect(await screen.findByTestId('assignments-permission-denied')).toBeInTheDocument();
  });

  it('handles not found error (404)', async () => {
    mockedUserAdminGroupAssignmentsApi.getUserAdminGroupAssignments.mockRejectedValue({ response: { status: 404 } });
    renderComponent('1');
    expect(await screen.findByTestId('assignments-not-found')).toBeInTheDocument();
  });
});
