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
      title: 'Retry Execution',
      content: 'This will retry the failed execution. The system will attempt to complete the approved action.',
      onOk: onRetry,
    });
  };

  return (
    <Button
      onClick={handleClick}
      loading={loading}
      data-testid="retry-execution-btn"
    >
      Retry Execution
    </Button>
  );
};

export default WorkflowRetryExecutionButton;
