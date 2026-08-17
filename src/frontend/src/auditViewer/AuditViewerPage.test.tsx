import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import AuditViewerPage from './AuditViewerPage';
import * as api from './auditViewerApi';

vi.mock('./auditViewerApi', () => ({
  getAuditEvents: vi.fn(),
}));

const MOCK_EVENT: api.SecurityAuditEventDto = {
  id: 1,
  actorUserId: 100,
  actorName: 'Nguyễn Văn A',
  actingAsUserId: null,
  targetUserId: 200,
  targetName: 'Trần Thị B',
  companyId: null,
  eventCode: 'ACCOUNT_LOCKED',
  entityType: 'UserAuthAccount',
  entityId: '200',
  reason: 'Too many failed attempts',
  correlationId: '1234-5678',
  outcome: 'Success',
  policyVersion: null,
  createdAt: '2026-01-01T12:00:00Z',
};

const MOCK_PAGED_RESULT: api.PagedResult<api.SecurityAuditEventDto> = {
  page: 1,
  pageSize: 50,
  totalCount: 1,
  items: [MOCK_EVENT],
};

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/security/audit']}>
        <Routes>
          <Route path="/security/audit" element={<>{children}</>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('AuditViewerPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders audit viewer page', async () => {
    (api.getAuditEvents as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AuditViewerPage />, { wrapper: makeWrapper() });

    expect(screen.getByTestId('audit-viewer-page')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByTestId('audit-list-table')).toBeInTheDocument());
  });

  it('shows loading spinner while fetching', () => {
    (api.getAuditEvents as ReturnType<typeof vi.fn>).mockImplementation(() => new Promise(() => {}));

    render(<AuditViewerPage />, { wrapper: makeWrapper() });
    expect(screen.getByTestId('audit-list-loading')).toBeInTheDocument();
  });

  it('calls getAuditEvents on mount', async () => {
    (api.getAuditEvents as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AuditViewerPage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(api.getAuditEvents).toHaveBeenCalledWith(
        expect.objectContaining({ page: 1, pageSize: 50 })
      );
    });
  });

  it('displays audit data from API response', async () => {
    (api.getAuditEvents as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AuditViewerPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByText('ACCOUNT_LOCKED')).toBeInTheDocument());
    expect(screen.getByText('Nguyễn Văn A (#100)')).toBeInTheDocument();
    expect(screen.getByText('Trần Thị B (#200)')).toBeInTheDocument();
    expect(screen.getByText('Success')).toBeInTheDocument();
    expect(screen.getByText('UserAuthAccount (200)')).toBeInTheDocument();
  });

  it('shows empty state when no results', async () => {
    (api.getAuditEvents as ReturnType<typeof vi.fn>).mockResolvedValue({
      page: 1,
      pageSize: 50,
      totalCount: 0,
      items: [],
    });

    render(<AuditViewerPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByText('Không có sự kiện kiểm toán.')).toBeInTheDocument());
  });

  it('updates request parameters when filters are changed', async () => {
    const user = userEvent.setup();
    (api.getAuditEvents as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AuditViewerPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(api.getAuditEvents).toHaveBeenCalledTimes(1));

    const actorInput = screen.getByTestId('audit-filter-actor');
    await user.type(actorInput, '100');

    // Antd Input doesn't auto-submit without Enter or debounce in our simple implementation, wait, we used onChange directly.
    // So it should trigger the API call as we type.

    await waitFor(() =>
      expect(api.getAuditEvents).toHaveBeenCalledWith(
        expect.objectContaining({ actorUserId: 100, page: 1 })
      )
    );
  });

  it('shows generic error message on server error', async () => {
    (api.getAuditEvents as ReturnType<typeof vi.fn>).mockRejectedValue({
      response: { status: 500, data: { title: 'Something went wrong.' } },
    });

    render(<AuditViewerPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByText('Something went wrong.')).toBeInTheDocument());
    expect(screen.getByTestId('audit-list-error')).toBeInTheDocument();
  });

  it('opens detail drawer with safe fields on View click', async () => {
    const user = userEvent.setup();
    (api.getAuditEvents as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AuditViewerPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId(`view-audit-detail-1`));
    await user.click(screen.getByTestId(`view-audit-detail-1`));

    await waitFor(() => expect(screen.getByTestId('audit-detail-drawer')).toBeInTheDocument());

    // Check safe fields exist
    expect(screen.getByText('Too many failed attempts')).toBeInTheDocument(); // Reason
    expect(screen.getByText('1234-5678')).toBeInTheDocument(); // Correlation ID
  });

  it('does not store sensitive state in localStorage', () => {
    expect(localStorage.length).toBe(0);
  });

  it('does not store sensitive state in sessionStorage', () => {
    expect(sessionStorage.length).toBe(0);
  });
});
