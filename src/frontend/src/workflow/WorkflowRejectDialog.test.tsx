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
    // Tiêu đề hộp thoại đã Việt hoá.
    expect(screen.getByText('Từ chối bước')).toBeInTheDocument();
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
    expect(screen.queryByText('Từ chối bước')).not.toBeInTheDocument();
    expect(screen.queryByTestId('reject-warning')).not.toBeInTheDocument();
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
    // Cảnh báo "không thể hoàn tác" nay bằng tiếng Việt.
    expect(screen.getByText(/Thao tác này là vĩnh viễn/)).toBeInTheDocument();
  });
});
