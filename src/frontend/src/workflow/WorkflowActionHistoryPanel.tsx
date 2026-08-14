import React from 'react';
import { Alert, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { getInstanceActions } from './workflowRuntimeApi';
import { getErrorMessage } from './errorMessages';
import type { WorkflowActionDto } from './types';
import { formatUtcDateTime } from '../utils/datetime';

const { Title } = Typography;

// Khóa phải khớp giá trị ActionType backend thực sự ghi (thì hiện tại), không phải thì quá khứ.
const ACTION_TYPE_COLORS: Record<string, string> = {
  APPROVE: 'green',
  RETURN: 'orange',
  REJECT: 'volcano',
  REASSIGN: 'purple',
  RETRY: 'geekblue',
};

const ACTION_TYPE_LABELS: Record<string, string> = {
  APPROVE: 'Duyệt',
  RETURN: 'Trả lại',
  REJECT: 'Từ chối',
  REASSIGN: 'Chuyển duyệt',
  RETRY: 'Chạy lại',
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
      title: 'Hành động',
      dataIndex: 'actionType',
      key: 'actionType',
      render: (val: string) => <Tag color={ACTION_TYPE_COLORS[val] ?? 'default'}>{ACTION_TYPE_LABELS[val] ?? val}</Tag>,
    },
    {
      title: 'Người thực hiện',
      key: 'actedBy',
      render: (_: unknown, r: WorkflowActionDto) => (
        <span>
          {r.actedByName ?? `Người dùng ${r.actedBy}`}
          {r.onBehalfOf != null && (
            <span> (thay mặt {r.onBehalfOfName ?? `Người dùng ${r.onBehalfOf}`})</span>
          )}
        </span>
      ),
    },
    {
      title: 'Lý do',
      dataIndex: 'reason',
      key: 'reason',
      render: (val: string | null) => val ?? '—',
    },
    {
      title: 'Ghi chú',
      dataIndex: 'comment',
      key: 'comment',
      render: (val: string | null) => val ?? '—',
    },
    {
      title: 'Thời gian',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (val: string) => formatUtcDateTime(val),
    },
  ];

  return (
    <div data-testid="action-history">
      <Title level={5} style={{ marginBottom: 8, marginTop: 24 }}>Lịch sử hành động</Title>

      {isLoading && <Spin data-testid="action-history-loading" />}

      {error && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          data-testid="action-history-error"
        />
      )}

      {!isLoading && !error && actions && actions.length === 0 && (
        <Alert type="info" message="Chưa có hành động nào được ghi nhận." data-testid="action-history-empty" />
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
