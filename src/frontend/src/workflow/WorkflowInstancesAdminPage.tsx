import React, { useState } from 'react';
import { Alert, Badge, Button, Select, Space, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { searchInstances } from './workflowRuntimeApi';
import { getBusinessProcesses } from './workflowApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import { INSTANCE_STATUS_COLORS, INSTANCE_STATUS_LABELS } from './instanceStatus';
import { formatUtcDateTime } from '../utils/datetime';
import type { WorkflowInstance } from './types';

const { Title, Text } = Typography;

/** Trạng thái cần người xử lý — nêu bật để admin không bỏ sót. */
const NEEDS_ATTENTION = ['FAILED', 'PENDING_EXECUTION', 'EXECUTING'];

const WorkflowInstancesAdminPage: React.FC = () => {
  const navigate = useNavigate();
  const [processCode, setProcessCode] = useState<string | undefined>(undefined);
  const [instanceStatus, setInstanceStatus] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);

  const { data: processes } = useQuery({
    queryKey: ['workflow-processes'],
    queryFn: getBusinessProcesses,
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['workflow-instances-admin', processCode, instanceStatus, page],
    queryFn: () => searchInstances({ processCode, instanceStatus, page, pageSize: 20 }),
  });

  // Đếm riêng hồ sơ Thất bại để hiện cảnh báo, không phụ thuộc bộ lọc đang chọn.
  const { data: failed } = useQuery({
    queryKey: ['workflow-instances-failed-count'],
    queryFn: () => searchInstances({ instanceStatus: 'FAILED', page: 1, pageSize: 1 }),
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem danh sách hồ sơ quy trình."
        data-testid="permission-denied"
      />
    );
  }

  const failedCount = failed?.totalCount ?? 0;

  const columns = [
    { title: 'ID', dataIndex: 'id', key: 'id', width: 70 },
    { title: 'Quy trình', dataIndex: 'processCode', key: 'processCode' },
    {
      title: 'Đối tượng',
      key: 'entity',
      render: (_: unknown, r: WorkflowInstance) =>
        r.businessEntityLabel ?? `${r.businessEntityType} #${r.businessEntityId}`,
    },
    {
      title: 'Người đề xuất',
      key: 'requester',
      render: (_: unknown, r: WorkflowInstance) => r.requesterName ?? `Người dùng ${r.requesterId}`,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'instanceStatus',
      key: 'instanceStatus',
      render: (v: string) => (
        <Tag color={INSTANCE_STATUS_COLORS[v] ?? 'default'}>{INSTANCE_STATUS_LABELS[v] ?? v}</Tag>
      ),
    },
    {
      title: 'Đã tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (v: string) => formatUtcDateTime(v),
    },
    {
      title: 'Cập nhật',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
      render: (v: string | null) => formatUtcDateTime(v),
    },
  ];

  return (
    <div data-testid="workflow-instances-admin-page">
      <Title level={4} style={{ marginBottom: 16 }}>Tất cả hồ sơ quy trình</Title>

      {failedCount > 0 && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
          message={`Có ${failedCount} hồ sơ đang ở trạng thái Thất bại`}
          description="Những hồ sơ này đã được duyệt nhưng nghiệp vụ chưa chạy xong. Mở từng hồ sơ và bấm Chạy lại sau khi đã khắc phục nguyên nhân."
          action={
            <Button size="small" onClick={() => { setInstanceStatus('FAILED'); setPage(1); }} data-testid="show-failed-btn">
              Xem
            </Button>
          }
          data-testid="failed-queue-alert"
        />
      )}

      {error && !isPermissionDenied(error) && (
        <Alert type="error" message={getErrorMessage(error)} style={{ marginBottom: 16 }} data-testid="instances-error" />
      )}

      <Space style={{ marginBottom: 16 }} wrap>
        <Select
          allowClear
          placeholder="Lọc theo quy trình"
          style={{ width: 280 }}
          value={processCode}
          onChange={(v) => { setProcessCode(v); setPage(1); }}
          data-testid="filter-processCode"
          options={(processes ?? []).map((p) => ({
            label: `${p.processCode} — ${p.processName}`,
            value: p.processCode,
          }))}
        />
        <Select
          allowClear
          placeholder="Lọc theo trạng thái"
          style={{ width: 200 }}
          value={instanceStatus}
          onChange={(v) => { setInstanceStatus(v); setPage(1); }}
          data-testid="filter-status"
          options={Object.keys(INSTANCE_STATUS_LABELS).map((k) => ({
            label: INSTANCE_STATUS_LABELS[k],
            value: k,
          }))}
        />
        {NEEDS_ATTENTION.includes(instanceStatus ?? '') && (
          <Badge status="warning" text={<Text type="secondary">Đang lọc nhóm cần xử lý</Text>} />
        )}
      </Space>

      <Table<WorkflowInstance>
        rowKey="id"
        loading={isLoading}
        columns={columns}
        dataSource={data?.items ?? []}
        data-testid="instances-table"
        locale={{ emptyText: 'Không có hồ sơ nào khớp bộ lọc.' }}
        rowClassName={(r) => (r.instanceStatus === 'FAILED' ? 'ptkd-row-danger' : '')}
        onRow={(record) => ({
          onClick: () => navigate(`/workflow/instances/${record.id}`),
          style: { cursor: 'pointer' },
        })}
        pagination={{
          current: data?.page ?? 1,
          pageSize: data?.pageSize ?? 20,
          total: data?.totalCount ?? 0,
          showSizeChanger: false,
          onChange: setPage,
        }}
      />
    </div>
  );
};

export default WorkflowInstancesAdminPage;
