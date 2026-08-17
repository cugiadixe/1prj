import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CemeteryWatermarkPage from './CemeteryWatermarkPage';
import * as cemeteriesApi from './cemeteriesApi';
import * as cardsApi from '../cards/cardsApi';
import * as auth from '../auth/AuthProvider';

vi.mock('./cemeteriesApi', async () => {
  const actual = await vi.importActual<typeof import('./cemeteriesApi')>('./cemeteriesApi');
  return { ...actual, getCemeteries: vi.fn(), setCemeteryWatermark: vi.fn() };
});
vi.mock('../cards/cardsApi');
vi.mock('../auth/AuthProvider');
vi.mock('../auth/CompanyProvider', () => ({
  useCompany: () => ({ currentCompanyId: 1, companies: [], switchCompany: vi.fn() }),
}));

const renderPage = () => {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <CemeteryWatermarkPage />
    </QueryClientProvider>
  );
};

describe('CemeteryWatermarkPage', () => {
  beforeEach(() => {
    vi.spyOn(auth, 'usePermissions').mockReturnValue({ permissions: [], hasPermission: vi.fn().mockReturnValue(true) } as any);
    vi.spyOn(cemeteriesApi, 'getCemeteries').mockResolvedValue([
      { id: 5, cemeteryCode: 'KM15', name: 'KM 15 Quang Hanh', address: null, isActive: true, cardWatermarkCode: 'LOTUS' },
    ]);
    vi.spyOn(cardsApi, 'getCards').mockResolvedValue([
      { id: 1, companyId: 1, graveId: 'A-01', cardNumber: '1', serviceId: null, printCount: 0, status: 'ACTIVE', createdAt: '' },
    ] as any);
    vi.spyOn(cemeteriesApi, 'setCemeteryWatermark').mockResolvedValue(undefined);
  });

  it('lists cemeteries with a watermark selector and preview', async () => {
    renderPage();
    expect(await screen.findByText('KM 15 Quang Hanh')).toBeInTheDocument();
    expect(screen.getByTestId('wm-select-5')).toBeInTheDocument();
    expect(screen.getByTestId('wm-preview-5')).toBeInTheDocument();
  });
});
