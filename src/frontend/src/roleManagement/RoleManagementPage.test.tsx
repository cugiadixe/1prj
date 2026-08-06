import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import RoleManagementPage from './RoleManagementPage';
import { roleManagementApi } from './roleManagementApi';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';

vi.mock('./roleManagementApi');
const mockedApi = roleManagementApi as any;

// Mock CompanyProvider to control currentCompanyId
let mockCompanyId: number | null = null;
vi.mock('../auth/CompanyProvider', async () => {
  const actual = await vi.importActual('../auth/CompanyProvider');
  return {
    ...(actual as any),
    useCompany: () => ({
      currentCompanyId: mockCompanyId,
      companies: [],
      switchCompany: vi.fn(),
    }),
  };
});

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
});

const renderComponent = () => {
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <RoleManagementPage />
      </BrowserRouter>
    </QueryClientProvider>
  );
};

describe('RoleManagementPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockCompanyId = null;
    queryClient.clear();

    mockedApi.getRoles.mockResolvedValue([
      {
        id: 1,
        roleCode: 'TEST_GLOBAL',
        name: 'Test Global Role',
        description: null,
        scopeType: 'GLOBAL',
        companyId: null,
        isActive: true,
        permissionCodes: ['TEST_PERM_1'],
        rowVersion: 'v1'
      },
      {
        id: 2,
        roleCode: 'TEST_COMPANY',
        name: 'Test Company Role',
        description: null,
        scopeType: 'COMPANY',
        companyId: 10,
        isActive: true,
        permissionCodes: [],
        rowVersion: 'v1'
      }
    ]);

    mockedApi.getPermissions.mockResolvedValue([
      {
        permissionCode: 'TEST_PERM_1',
        moduleCode: 'TEST',
        actionCode: 'VIEW',
        dataScope: 'GLOBAL',
        isSensitive: false,
        isDelegable: false,
        requiresReason: false,
        isActive: true,
        description: 'Test perm 1'
      },
      {
        permissionCode: 'TEST_PERM_2',
        moduleCode: 'TEST',
        actionCode: 'EDIT',
        dataScope: 'GLOBAL',
        isSensitive: false,
        isDelegable: false,
        requiresReason: false,
        isActive: true,
        description: 'Test perm 2'
      }
    ]);
  });

  it('renders loading state initially', () => {
    renderComponent();
    expect(screen.getByTestId('role-management-page')).toBeInTheDocument();
  });

  it('renders roles list after loading', async () => {
    renderComponent();
    await waitFor(() => {
      expect(screen.getByText('TEST_GLOBAL')).toBeInTheDocument();
      expect(screen.getByText('TEST_COMPANY')).toBeInTheDocument();
    });
  });

  it('shows role detail when a role is selected', async () => {
    renderComponent();
    await waitFor(() => {
      expect(screen.getByTestId('select-role-1')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('select-role-1'));

    await waitFor(() => {
      expect(screen.getByTestId('role-detail-card')).toBeInTheDocument();
      expect(screen.getByText('Role Details: TEST_GLOBAL')).toBeInTheDocument();
      expect(screen.getByText('TEST_PERM_1')).toBeInTheDocument();
    });
  });

  it('blocks adding permissions to COMPANY role if no company is selected', async () => {
    mockCompanyId = null;
    renderComponent();

    await waitFor(() => {
      fireEvent.click(screen.getByTestId('select-role-2'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('add-permissions-btn')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('add-permissions-btn'));

    await waitFor(() => {
      expect(screen.getByText(/A specific company must be selected/i)).toBeInTheDocument();
    });
  });

  it('allows adding permissions to COMPANY role if company is selected', async () => {
    mockCompanyId = 10;
    renderComponent();

    await waitFor(() => {
      fireEvent.click(screen.getByTestId('select-role-2'));
    });

    await waitFor(() => {
      fireEvent.click(screen.getByTestId('add-permissions-btn'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('add-permissions-modal')).toBeInTheDocument();
    });
  });

  it('allows adding permissions to GLOBAL role regardless of selected company', async () => {
    mockCompanyId = null;
    renderComponent();

    await waitFor(() => {
      fireEvent.click(screen.getByTestId('select-role-1'));
    });

    await waitFor(() => {
      fireEvent.click(screen.getByTestId('add-permissions-btn'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('add-permissions-modal')).toBeInTheDocument();
    });
  });

  it('renders creation modal', async () => {
    renderComponent();
    await waitFor(() => {
      fireEvent.click(screen.getByTestId('create-role-btn'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('role-form-modal')).toBeInTheDocument();
      expect(screen.getByTestId('role-code-input')).toBeInTheDocument();
    });
  });
});
