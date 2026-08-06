import React from 'react';
import { Alert, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { getInstanceActions } from './workflowRuntimeApi';
import { getErrorMessage } from './errorMessages';

const { Title } = Typography;

const ACTION_TYPE_COLORS: Record<string, string> = {
  APPROVED: 'green',
  RETURNED: 'orange',
  REJECTED: 'volcano',
  RESUBMITTED: 'blue',
  WITHDRAWN: 'red',
  REASSIGNED: 'purple',
  EXECUTION_STARTED: 'cyan',
  EXECUTION_COMPLETED: 'green',
  EXECUTION_FAILED: 'magenta',
  EXECUTION_RETRIED: 'geekblue',
};

interface WorkflowActionHistoryPanelProps {
  instanceId: number;
}

const WorkflowActionHistoryPanel: React.FC<WorkflowActionHistoryPanelProps> = ({ instanceId }) => {
  const { data: actions, isLoading, error } = useQuery({
    queryKey: ['workflow-instance-actions', instanceId],
    queryFn: () => getInstanceActions(instanceId),
    enabled: instanceId > 0,
  });

  const columns = [
    {
      title: 'Action',
      dataIndex: 'actionType',
      key: 'actionType',
      render: (val: string) => <Tag color={ACTION_TYPE_COLORS[val] ?? 'default'}>{val}</Tag>,
    },
    {
      title: 'Actor',
      dataIndex: 'actedBy',
      key: 'actedBy',
      render: (val: number) => `User ${val}`,
    },
    {
      title: 'Reason',
      dataIndex: 'reason',
      key: 'reason',
      render: (val: string | null) => val ?? '—',
    },
    {
      title: 'Comment',
      dataIndex: 'comment',
      key: 'comment',
      render: (val: string | null) => val ?? '—',
    },
    {
      title: 'Time',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (val: string) => new Date(val).toLocaleString(),
    },
  ];

  return (
    <div data-testid="action-history">
      <Title level={5} style={{ marginBottom: 8, marginTop: 24 }}>Action History</Title>

      {isLoading && <Spin data-testid="action-history-loading" />}

      {error && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          data-testid="action-history-error"
        />
      )}

      {!isLoading && !error && actions && actions.length === 0 && (
        <Alert type="info" message="No actions recorded." data-testid="action-history-empty" />
      )}

      {actions && actions.length > 0 && (
        <Table
          dataSource={actions}
          columns={columns}
          rowKey="id"
          pagination={false}
          size="small"
          data-testid="action-history-table"
        />
      )}
    </div>
  );
};

export default WorkflowActionHistoryPanel;
