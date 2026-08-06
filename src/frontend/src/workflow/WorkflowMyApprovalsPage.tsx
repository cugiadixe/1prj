import React from 'react';
import { Alert, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { getMyApprovals } from './workflowRuntimeApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { MyApprovalItem } from './types';

const { Title } = Typography;

const INSTANCE_STATUS_COLORS: Record<string, string> = {
  PENDING_APPROVAL: 'blue',
  RETURNED: 'orange',
  WITHDRAWN: 'red',
  PENDING_EXECUTION: 'cyan',
  COMPLETED: 'green',
  CANCELLED: 'default',
};

const WorkflowMyApprovalsPage: React.FC = () => {
  const navigate = useNavigate();

  const { data: approvals, isLoading, error } = useQuery({
    queryKey: ['workflow-my-approvals'],
    queryFn: getMyApprovals,
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view approvals."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    { title: 'Process', dataIndex: 'processCode', key: 'processCode' },
    { title: 'Entity Type', dataIndex: 'businessEntityType', key: 'businessEntityType' },
    { title: 'Entity ID', dataIndex: 'businessEntityId', key: 'businessEntityId' },
    { title: 'Step', dataIndex: 'stepName', key: 'stepName' },
    {
      title: 'Status',
      dataIndex: 'instanceStatus',
      key: 'instanceStatus',
      render: (val: string) => (
        <Tag color={INSTANCE_STATUS_COLORS[val] ?? 'default'}>{val}</Tag>
      ),
    },
    {
      title: 'Assigned',
      dataIndex: 'assignedAt',
      key: 'assignedAt',
      render: (val: string | null) => val ? new Date(val).toLocaleString() : '—',
    },
  ];

  return (
    <div data-testid="my-approvals-page">
      <Title level={4} style={{ marginBottom: 16 }}>My Approvals</Title>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="my-approvals-error"
        />
      )}

      {isLoading && <Spin data-testid="my-approvals-loading" />}

      {!isLoading && !error && approvals && approvals.length === 0 && (
        <Alert type="info" message="No pending approvals." data-testid="my-approvals-empty" />
      )}

      {approvals && approvals.length > 0 && (
        <Table
          dataSource={approvals}
          columns={columns}
          rowKey={(record: MyApprovalItem) => `${record.instanceId}-${record.stepId}`}
          pagination={false}
          data-testid="my-approvals-table"
          onRow={(record: MyApprovalItem) => ({
            onClick: () => navigate(`/workflow/instances/${record.instanceId}`),
            style: { cursor: 'pointer' },
          })}
        />
      )}
    </div>
  );
};

export default WorkflowMyApprovalsPage;
