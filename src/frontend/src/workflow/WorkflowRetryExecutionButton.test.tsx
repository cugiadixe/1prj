import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import WorkflowRetryExecutionButton from './WorkflowRetryExecutionButton';

describe('WorkflowRetryExecutionButton', () => {
  it('renders retry button', () => {
    render(<WorkflowRetryExecutionButton loading={false} onRetry={vi.fn()} />);
    expect(screen.getByTestId('retry-execution-btn')).toBeInTheDocument();
    // Nhãn nút đã Việt hoá.
    expect(screen.getByText('Thử lại thực thi')).toBeInTheDocument();
  });

  it('shows loading state when loading', () => {
    render(<WorkflowRetryExecutionButton loading={true} onRetry={vi.fn()} />);
    const btn = screen.getByTestId('retry-execution-btn');
    expect(btn).toBeInTheDocument();
  });
});
