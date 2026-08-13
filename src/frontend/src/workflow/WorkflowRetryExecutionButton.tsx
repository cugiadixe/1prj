import React from 'react';
import { Button, Modal } from 'antd';

interface WorkflowRetryExecutionButtonProps {
  loading: boolean;
  onRetry: () => void;
}

const WorkflowRetryExecutionButton: React.FC<WorkflowRetryExecutionButtonProps> = ({
  loading,
  onRetry,
}) => {
  const handleClick = () => {
    Modal.confirm({
      title: 'Thử lại thực thi',
      content: 'Thao tác này sẽ thử lại lần thực thi đã thất bại. Hệ thống sẽ cố gắng hoàn tất hành động đã được phê duyệt.',
      onOk: onRetry,
    });
  };

  return (
    <Button
      onClick={handleClick}
      loading={loading}
      data-testid="retry-execution-btn"
    >
      Thử lại thực thi
    </Button>
  );
};

export default WorkflowRetryExecutionButton;
