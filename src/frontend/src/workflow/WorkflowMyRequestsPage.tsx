import React from 'react';
import { Alert, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { getMyRequests } from './workflowRuntimeApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { WorkflowInstance } from './types';

const { Title } = Typography;

const INSTANCE_STATUS_COLORS: Record<string, string> = {
  PENDING_APPROVAL: 'blue',
  RETURNED: 'orange',
  WITHDRAWN: 'red',
  REJECTED: 'volcano',
  PENDING_EXECUTION: 'cyan',
  EXECUTING: 'geekblue',
  EXECUTED: 'green',
  FAILED: 'magenta',
  COMPLETED: 'green',
  CANCELLED: 'default',
};

const WorkflowMyRequestsPage: React.FC = () => {
  const navigate = useNavigate();

  const { data: requests, isLoading, error } = useQuery({
    queryKey: ['workflow-my-requests'],
    queryFn: getMyRequests,
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view requests."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    { title: 'ID', dataIndex: 'id', key: 'id' },
    { title: 'Process', dataIndex: 'processCode', key: 'processCode' },
    { title: 'Entity Type', dataIndex: 'businessEntityType', key: 'businessEntityType' },
    {
      title: 'Status',
      dataIndex: 'instanceStatus',
      key: 'instanceStatus',
      render: (val: string) => (
        <Tag color={INSTANCE_STATUS_COLORS[val] ?? 'default'}>{val}</Tag>
      ),
    },
    { title: 'Round', dataIndex: 'roundNo', key: 'roundNo' },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (val: string) => new Date(val).toLocaleString(),
    },
    {
      title: 'Updated',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
      render: (val: string | null) => val ? new Date(val).toLocaleString() : '—',
    },
  ];

  return (
    <div data-testid="my-requests-page">
      <Title level={4} style={{ marginBottom: 16 }}>My Requests</Title>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="my-requests-error"
        />
      )}

      {isLoading && <Spin data-testid="my-requests-loading" />}

      {!isLoading && !error && requests && requests.length === 0 && (
        <Alert type="info" message="You have no workflow requests." data-testid="my-requests-empty" />
      )}

      {requests && requests.length > 0 && (
        <Table
          dataSource={requests}
          columns={columns}
          rowKey="id"
          pagination={false}
          data-testid="my-requests-table"
          onRow={(record: WorkflowInstance) => ({
            onClick: () => navigate(`/workflow/instances/${record.id}`),
            style: { cursor: 'pointer' },
          })}
        />
      )}
    </div>
  );
};

export default WorkflowMyRequestsPage;
