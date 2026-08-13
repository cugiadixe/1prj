import React from 'react';
import { Alert, Button, Descriptions, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { getDefinitionById, getVersionsByDefinition } from './workflowApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { WorkflowVersionListItem } from './types';

const { Title } = Typography;

const STATUS_COLORS: Record<string, string> = {
  DRAFT: 'default',
  PUBLISHED: 'blue',
  ACTIVE: 'green',
  RETIRED: 'red',
};

const WorkflowDefinitionDetailPage: React.FC = () => {
  const { definitionId } = useParams<{ definitionId: string }>();
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const id = Number(definitionId);

  const {
    data: definition,
    isLoading,
    error: fetchError,
  } = useQuery({
    queryKey: ['workflow-definition', id],
    queryFn: () => getDefinitionById(id),
    enabled: !isNaN(id),
  });

  const { data: versions } = useQuery({
    queryKey: ['workflow-versions', id],
    queryFn: () => getVersionsByDefinition(id),
    enabled: !isNaN(id),
  });

  if (isLoading) return <Spin data-testid="definition-detail-loading" />;

  if (isPermissionDenied(fetchError)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem định nghĩa quy trình này."
        data-testid="permission-denied"
      />
    );
  }

  if (fetchError) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(fetchError)}
        data-testid="definition-detail-error"
      />
    );
  }

  if (!definition) return null;

  const versionColumns = [
    {
      title: 'Phiên bản',
      dataIndex: 'versionNumber',
      key: 'versionNumber',
      render: (v: number) => `v${v}`,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'versionStatus',
      key: 'versionStatus',
      render: (status: string) => (
        <Tag color={STATUS_COLORS[status] ?? 'default'}>{status}</Tag>
      ),
    },
    {
      title: 'Hiệu lực từ',
      dataIndex: 'effectiveFrom',
      key: 'effectiveFrom',
      render: (val: string | null) => val ? new Date(val).toLocaleDateString('vi-VN') : '—',
    },
    {
      title: 'Hiệu lực đến',
      dataIndex: 'effectiveTo',
      key: 'effectiveTo',
      render: (val: string | null) => val ? new Date(val).toLocaleDateString('vi-VN') : '—',
    },
  ];

  return (
    <div data-testid="workflow-definition-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Quy trình: {definition.definitionName}
        </Title>
        <Space>
          {hasPermission('WORKFLOW_CONFIG_MANAGE', 'GLOBAL') && (
            <Button data-testid="edit-definition-btn">
              <Link to={`/workflow/definitions/${id}/edit`}>Sửa</Link>
            </Button>
          )}
          {hasPermission('WORKFLOW_CONFIG_MANAGE', 'GLOBAL') && (
            <Button type="primary" data-testid="create-version-btn">
              <Link to={`/workflow/definitions/${id}/versions/new`}>Phiên bản mới</Link>
            </Button>
          )}
          <Button>
            <Link to="/workflow">Quay lại danh sách</Link>
          </Button>
        </Space>
      </Space>

      <Descriptions bordered column={2} style={{ marginBottom: 24 }} data-testid="definition-details">
        <Descriptions.Item label="Mã">{definition.definitionCode}</Descriptions.Item>
        <Descriptions.Item label="Quy trình">{definition.processCode}</Descriptions.Item>
        <Descriptions.Item label="Hoạt động">
          <Tag color={definition.isActive ? 'green' : 'red'}>
            {definition.isActive ? 'Hoạt động' : 'Ngừng hoạt động'}
          </Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Đã tạo">{new Date(definition.createdAt).toLocaleDateString('vi-VN')}</Descriptions.Item>
        {definition.description && (
          <Descriptions.Item label="Mô tả" span={2}>{definition.description}</Descriptions.Item>
        )}
      </Descriptions>

      <Title level={5}>Các phiên bản</Title>
      {versions && versions.length === 0 && (
        <Alert type="info" message="Chưa có phiên bản nào." data-testid="versions-empty" />
      )}
      {versions && versions.length > 0 && (
        <Table
          dataSource={versions}
          columns={versionColumns}
          rowKey="id"
          data-testid="versions-table"
          pagination={false}
          onRow={(record: WorkflowVersionListItem) => ({
            onClick: () => navigate(`/workflow/definitions/${id}/versions/${record.id}`),
            style: { cursor: 'pointer' },
          })}
        />
      )}
    </div>
  );
};

export default WorkflowDefinitionDetailPage;
