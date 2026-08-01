import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import WorkflowRejectDialog from './WorkflowRejectDialog';

describe('WorkflowRejectDialog', () => {
  it('renders when open', () => {
    render(
      <WorkflowRejectDialog
        open={true}
        loading={false}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    );
    expect(screen.getByText('Reject Step')).toBeInTheDocument();
    expect(screen.getByTestId('reject-warning')).toBeInTheDocument();
    expect(screen.getByTestId('reject-reason')).toBeInTheDocument();
    expect(screen.getByTestId('reject-comment')).toBeInTheDocument();
  });

  it('does not render when closed', () => {
    render(
      <WorkflowRejectDialog
        open={false}
        loading={false}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    );
    expect(screen.queryByText('Reject Step')).not.toBeInTheDocument();
  });

  it('shows permanent warning text', () => {
    render(
      <WorkflowRejectDialog
        open={true}
        loading={false}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    );
    expect(screen.getByText(/This action is permanent/)).toBeInTheDocument();
  });
});
