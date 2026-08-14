import React from 'react';
import { Alert, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { getMyApprovals } from './workflowRuntimeApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { MyApprovalItem } from './types';
import { formatUtcDateTime } from '../utils/datetime';
import { INSTANCE_STATUS_COLORS, INSTANCE_STATUS_LABELS } from './instanceStatus';

const { Title } = Typography;

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
        message="Bạn không có quyền xem các phê duyệt."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    { title: 'Quy trình', dataIndex: 'processCode', key: 'processCode' },
    {
      title: 'Đối tượng',
      key: 'businessEntity',
      render: (_: unknown, r: MyApprovalItem) =>
        r.businessEntityLabel ?? `${r.businessEntityType} #${r.businessEntityId}`,
    },
    { title: 'Bước', dataIndex: 'stepName', key: 'stepName' },
    {
      title: 'Người đề xuất',
      key: 'requester',
      render: (_: unknown, r: MyApprovalItem) => r.requesterName ?? `Người dùng ${r.requesterId}`,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'instanceStatus',
      key: 'instanceStatus',
      render: (val: string) => (
        <Tag color={INSTANCE_STATUS_COLORS[val] ?? 'default'}>{INSTANCE_STATUS_LABELS[val] ?? val}</Tag>
      ),
    },
    {
      title: 'Đã phân công',
      dataIndex: 'assignedAt',
      key: 'assignedAt',
      render: (val: string | null) => formatUtcDateTime(val),
    },
  ];

  return (
    <div data-testid="my-approvals-page">
      <Title level={4} style={{ marginBottom: 16 }}>Phê duyệt của tôi</Title>

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
        <Alert type="info" message="Không có phê duyệt đang chờ." data-testid="my-approvals-empty" />
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
