import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import SystemHealth from './SystemHealth';
import axiosClient from '../api/axiosClient';

vi.mock('../api/axiosClient', () => ({
  default: {
    get: vi.fn()
  }
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } }
});

describe('SystemHealth Component', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('renders loading state initially', () => {
    (axiosClient.get as any).mockImplementation(() => new Promise(() => {}));
    
    render(
      <QueryClientProvider client={queryClient}>
        <SystemHealth />
      </QueryClientProvider>
    );

    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('renders healthy status', async () => {
    (axiosClient.get as any).mockResolvedValue({
      data: {
        status: 'Healthy',
        entries: [
          { name: 'sql_server', status: 'Healthy', description: null, duration: '00:00:00.005' }
        ]
      }
    });

    render(
      <QueryClientProvider client={queryClient}>
        <SystemHealth />
      </QueryClientProvider>
    );

    const statusElement = await screen.findByText('Overall Status:');
    expect(statusElement).toBeInTheDocument();
    const healthyTags = screen.getAllByText('Healthy');
    expect(healthyTags.length).toBeGreaterThanOrEqual(1);
  });

  it('renders error state', async () => {
    (axiosClient.get as any).mockRejectedValue(new Error('Network Error'));

    render(
      <QueryClientProvider client={queryClient}>
        <SystemHealth />
      </QueryClientProvider>
    );

    const errorElement = await screen.findByText(/System Offline or Error/i);
    expect(errorElement).toBeInTheDocument();
  });
});
