import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CustomerCreatePage from './CustomerCreatePage';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => mockNavigate };
});

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: vi.fn().mockReturnValue(true),
  }),
}));

vi.mock('./customersApi', () => ({
  createCustomer: vi.fn(),
  checkDuplicates: vi.fn(),
}));

import { createCustomer, checkDuplicates } from './customersApi';
const mockCreate = vi.mocked(createCustomer);
const mockCheckDuplicates = vi.mocked(checkDuplicates);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <CustomerCreatePage />
      </BrowserRouter>
    </QueryClientProvider>,
  );

describe('CustomerCreatePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  it('renders create form', () => {
    renderPage();
    expect(screen.getByTestId('customer-create-page')).toBeInTheDocument();
    expect(screen.getByTestId('customer-create-form')).toBeInTheDocument();
  });

  it('has required fields for customerCode and fullName', () => {
    renderPage();
    expect(screen.getByTestId('input-customerCode')).toBeInTheDocument();
    expect(screen.getByTestId('input-fullName')).toBeInTheDocument();
  });

  it('shows duplicate warning when duplicates found on blur', async () => {
    mockCheckDuplicates.mockResolvedValue({
      hasDuplicates: true,
      matches: [
        { id: 2, customerCode: 'C002', fullName: 'Bob', cccd: '999', phone: null, customerStatus: 'ACTIVE', createdAt: '2026-01-01' },
      ],
    });
    renderPage();
    const cccdInput = screen.getByTestId('input-cccd');
    fireEvent.change(cccdInput, { target: { value: '123456' } });
    fireEvent.blur(cccdInput);
    await waitFor(() => {
      expect(screen.getByTestId('duplicate-warning')).toBeInTheDocument();
      expect(screen.getByTestId('duplicate-warning-list')).toBeInTheDocument();
    });
  });

  it('shows error message on create failure', async () => {
    mockCreate.mockRejectedValue({
      response: {
        status: 409,
        data: { extensions: { errorCode: 'CUS_DUPLICATE_CUSTOMER_CODE' } },
      },
    });
    renderPage();

    fireEvent.change(screen.getByTestId('input-customerCode'), { target: { value: 'C001' } });
    fireEvent.change(screen.getByTestId('input-fullName'), { target: { value: 'Test' } });
    fireEvent.click(screen.getByTestId('submit-create'));

    await waitFor(() => {
      expect(screen.getByTestId('create-error')).toBeInTheDocument();
    });
  });

  it('navigates to detail on successful create', async () => {
    mockCreate.mockResolvedValue({
      id: 5,
      customerCode: 'C005',
      customerStatus: 'ACTIVE',
      rowVersion: 'AA',
      createdAt: '2026-01-01',
      updatedAt: null,
      profile: {
        id: 5, fullName: 'New', cccd: null, dob: null, dobPartial: null, dobPrecision: null,
        gender: null, permanentAddress: null, cccdIssueDate: null, cccdIssuePlace: null,
        taxCode: null, phone: null, contactAddress: null, deathDateSolar: null,
        deathDateLunar: null, deathPlace: null, hometown: null, isActive: true, rowVersion: 'BB',
      },
    });
    renderPage();

    fireEvent.change(screen.getByTestId('input-customerCode'), { target: { value: 'C005' } });
    fireEvent.change(screen.getByTestId('input-fullName'), { target: { value: 'New Customer' } });
    fireEvent.click(screen.getByTestId('submit-create'));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/customers/5');
    });
  });
});
