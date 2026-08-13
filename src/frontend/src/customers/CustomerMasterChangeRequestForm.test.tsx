
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import CustomerMasterChangeRequestForm from './CustomerMasterChangeRequestForm';
import { createCustomerMasterChangeRequest } from './customerMasterChangeApi';

vi.mock('./customerMasterChangeApi', () => ({
  createCustomerMasterChangeRequest: vi.fn(),
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe('CustomerMasterChangeRequestForm', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.resetAllMocks();
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
  });

  const mockProfile = {
    id: 1,
    fullName: 'John Doe',
    cccd: null,
    dob: null,
    dobPartial: null,
    dobPrecision: null,
    gender: null,
    permanentAddress: null,
    cccdIssueDate: null,
    cccdIssuePlace: null,
    taxCode: null,
    phone: null,
    contactAddress: null,
    deathDateSolar: null,
    deathDateLunar: null,
    deathPlace: null,
    hometown: null,
    isActive: true,
    rowVersion: 'v1',
  };

  const renderComponent = () => {
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <CustomerMasterChangeRequestForm
            customerId={123}
            customerName="John Doe"
            targetRowVersion="v1"
            profile={mockProfile}
            onCancel={vi.fn()}
          />
        </MemoryRouter>
      </QueryClientProvider>
    );
  };

  it('renders correctly with customer name', () => {
    renderComponent();
    expect(screen.getByText('Request Change for Customer: John Doe')).toBeInTheDocument();
    expect(screen.getByTestId('input-reason')).toBeInTheDocument();
    expect(screen.getByTestId('input-fullName')).toBeInTheDocument();
  });

  it('shows error if reason is not provided', async () => {
    renderComponent();
    const submitBtn = screen.getByTestId('submit-change-request');
    await userEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Reason is required')).toBeInTheDocument();
    });
    expect(createCustomerMasterChangeRequest).not.toHaveBeenCalled();
  });

  it('submits successfully and navigates', async () => {
    vi.mocked(createCustomerMasterChangeRequest).mockResolvedValueOnce({
      id: 456,
      processCode: 'CUSTOMER_MASTER_CHANGE',
      requesterId: 1,
      companyId: null,
      requestStatus: 'Draft',
      workflowInstanceId: null,
      targetCustomerId: 123,
      targetRowVersion: 'v1',
      createdAt: '2023-01-01T00:00:00Z',
      updatedAt: null,
      rowVersion: 'v2',
      payload: null,
    });

    renderComponent();

    await userEvent.type(screen.getByTestId('input-reason'), 'Change name');
    // Form đã đổ sẵn 'John Doe' — xóa rồi nhập tên mới để phần delta ghi nhận thay đổi.
    await userEvent.clear(screen.getByTestId('input-fullName'));
    await userEvent.type(screen.getByTestId('input-fullName'), 'New Name');

    const submitBtn = screen.getByTestId('submit-change-request');
    await userEvent.click(submitBtn);

    await waitFor(() => {
      expect(createCustomerMasterChangeRequest).toHaveBeenCalledWith(123, {
        targetCustomerId: 123,
        targetRowVersion: 'v1',
        reason: 'Change name',
        fullName: 'New Name',
        cccd: null,
        dob: null,
        dobPartial: null,
        dobPrecision: null,
        gender: null,
        permanentAddress: null,
        cccdIssueDate: null,
        cccdIssuePlace: null,
        taxCode: null,
        phone: null,
        contactAddress: null,
        deathDateSolar: null,
        deathDateLunar: null,
        deathPlace: null,
        hometown: null,
      });
      expect(mockNavigate).toHaveBeenCalledWith('/customers/change-requests/456');
    });
  });

  it('displays API error safely', async () => {
    vi.mocked(createCustomerMasterChangeRequest).mockRejectedValueOnce({
      isAxiosError: true,
      response: {
        data: {
          extensions: {
            errorCode: 'CUS_INVALID_ROW_VERSION',
          },
        },
      },
    });

    renderComponent();

    await userEvent.type(screen.getByTestId('input-reason'), 'Change name');
    const submitBtn = screen.getByTestId('submit-change-request');
    await userEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByTestId('create-error')).toHaveTextContent(
        'This customer was modified by another user. Please refresh and try again.'
      );
    });
  });
});
