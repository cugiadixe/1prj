import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import DepartmentPermissionsPage from './DepartmentPermissionsPage';
import { departmentPermissionsApi } from './departmentPermissionsApi';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';

vi.mock('./departmentPermissionsApi');
const mockedApi = departmentPermissionsApi as any;

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
        <DepartmentPermissionsPage />
      </BrowserRouter>
    </QueryClientProvider>
  );
};

describe('DepartmentPermissionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockCompanyId = null;
    queryClient.clear();

    mockedApi.getDepartments.mockResolvedValue([
      {
        id: 1,
        departmentCode: 'DEPT_IT',
        name: 'IT Department',
        companyId: 10,
        isActive: true,
      },
      {
        id: 2,
        departmentCode: 'DEPT_HR',
        name: 'HR Department',
        companyId: 10,
        isActive: false,
      }
    ]);

    mockedApi.getPermissions.mockResolvedValue([
      {
        permissionCode: 'TEST_PERM_GLOBAL',
        moduleCode: 'TEST',
        actionCode: 'VIEW',
        dataScope: 'GLOBAL',
        isSensitive: false,
        isDelegable: false,
        requiresReason: false,
        isActive: true,
        description: 'Test global perm',
        scope: 'GLOBAL'
      },
      {
        permissionCode: 'TEST_PERM_COMPANY',
        moduleCode: 'TEST',
        actionCode: 'EDIT',
        dataScope: 'COMPANY',
        isSensitive: false,
        isDelegable: false,
        requiresReason: false,
        isActive: true,
        description: 'Test company perm',
        scope: 'COMPANY'
      },
      {
        permissionCode: 'TEST_PERM_ENTITY',
        moduleCode: 'TEST',
        actionCode: 'EDIT',
        dataScope: 'ENTITY',
        isSensitive: false,
        isDelegable: false,
        requiresReason: false,
        isActive: true,
        description: 'Test entity perm',
        scope: 'ENTITY' // Should be filtered out
      }
    ]);

    mockedApi.getDepartmentPermissions.mockResolvedValue([
      { permissionCode: 'TEST_PERM_GLOBAL' }
    ]);
  });

  it('renders loading state initially or list after loading', async () => {
    renderComponent();
    expect(screen.getByTestId('department-permissions-page')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText('DEPT_IT')).toBeInTheDocument();
      expect(screen.getByText('DEPT_HR')).toBeInTheDocument();
    });
  });

  it('shows department detail when a department is selected', async () => {
    renderComponent();
    await waitFor(() => {
      expect(screen.getByTestId('select-department-1')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('select-department-1'));

    await waitFor(() => {
      expect(screen.getByTestId('department-detail-card')).toBeInTheDocument();
      expect(screen.getByText('Department Details: DEPT_IT')).toBeInTheDocument();
      expect(screen.getByText('TEST_PERM_GLOBAL')).toBeInTheDocument();
    });
  });

  it('blocks adding COMPANY permissions if no company is selected', async () => {
    mockCompanyId = null;
    renderComponent();

    await waitFor(() => { expect(screen.getByTestId('select-department-1')).toBeInTheDocument(); }); fireEvent.click(screen.getByTestId('select-department-1'));

    await waitFor(() => {
      expect(screen.getByTestId('add-permissions-btn')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('add-permissions-btn'));

    await waitFor(() => {
      expect(screen.getByText('Add Permissions to Department Baseline')).toBeInTheDocument();
    });
  });

  it('filters out ENTITY scoped permissions from the catalog', async () => {
    renderComponent();
    await waitFor(() => { expect(screen.getByTestId('select-department-1')).toBeInTheDocument(); }); fireEvent.click(screen.getByTestId('select-department-1'));

    await waitFor(() => { expect(screen.getByTestId('add-permissions-btn')).toBeInTheDocument(); }); fireEvent.click(screen.getByTestId('add-permissions-btn'));

    await waitFor(() => {
      expect(screen.getByText('Add Permissions to Department Baseline')).toBeInTheDocument();
    });
    
    await new Promise(r => setTimeout(r, 100));
    fireEvent.mouseDown(screen.getByRole('combobox'));
    
    await waitFor(() => {
      expect(screen.getAllByText('TEST_PERM_GLOBAL').length).toBeGreaterThan(0);
      expect(screen.getAllByText('TEST_PERM_COMPANY').length).toBeGreaterThan(0);
      // ENTITY should be hidden
      expect(screen.queryByText('TEST_PERM_ENTITY')).not.toBeInTheDocument();
    });
  });

  it('does not treat PUT as append-only single-permission add', async () => {
    mockedApi.setDepartmentPermissions.mockResolvedValue();
    renderComponent();
    
    await waitFor(() => { expect(screen.getByTestId('select-department-1')).toBeInTheDocument(); }); fireEvent.click(screen.getByTestId('select-department-1'));

    await waitFor(() => { expect(screen.getByTestId('add-permissions-btn')).toBeInTheDocument(); }); fireEvent.click(screen.getByTestId('add-permissions-btn'));

    await waitFor(() => {
      expect(screen.getByText('Add Permissions to Department Baseline')).toBeInTheDocument();
    });
  });

  it('removes permission through existing DELETE API', async () => {
    const { Modal: AntdModal } = await import('antd');
    const confirmSpy = vi.spyOn(AntdModal, 'confirm').mockImplementation((config: any) => {
      config.onOk?.();
      return { destroy: vi.fn(), update: vi.fn(), then: vi.fn() };
    });

    mockedApi.removeDepartmentPermission.mockResolvedValue();
    renderComponent();

    await waitFor(() => { expect(screen.getByTestId('select-department-1')).toBeInTheDocument(); }); fireEvent.click(screen.getByTestId('select-department-1'));

    await waitFor(() => {
      expect(screen.getByTestId('remove-permission-TEST_PERM_GLOBAL')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('remove-permission-TEST_PERM_GLOBAL'));

    await waitFor(() => {
      expect(confirmSpy).toHaveBeenCalledWith(expect.objectContaining({
        title: 'Remove Permission',
        okText: 'Remove',
      }));
      expect(mockedApi.removeDepartmentPermission).toHaveBeenCalledWith(1, 'TEST_PERM_GLOBAL');
    });

    confirmSpy.mockRestore();
  });
});










