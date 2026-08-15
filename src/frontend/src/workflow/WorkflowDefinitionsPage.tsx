import React, { useState } from 'react';
import { Alert, Button, Input, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { searchDefinitions } from './workflowApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { WorkflowDefinitionListItem } from './types';

const { Title } = Typography;

const WorkflowDefinitionsPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const [processCodeFilter, setProcessCodeFilter] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const { data, isLoading, error } = useQuery({
    queryKey: ['workflow-definitions', processCodeFilter, page, pageSize],
    queryFn: () =>
      searchDefinitions({ processCode: processCodeFilter, page, pageSize }),
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem các định nghĩa quy trình."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Mã',
      dataIndex: 'definitionCode',
      key: 'definitionCode',
    },
    {
      title: 'Tên',
      dataIndex: 'definitionName',
      key: 'definitionName',
    },
    {
      title: 'Quy trình',
      dataIndex: 'processCode',
      key: 'processCode',
    },
    {
      title: 'Hoạt động',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (val: boolean) => (
        <Tag color={val ? 'green' : 'red'}>{val ? 'Hoạt động' : 'Ngừng hoạt động'}</Tag>
      ),
    },
  ];

  return (
    <div data-testid="workflow-definitions-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Định nghĩa quy trình</Title>
        {hasPermission('WORKFLOW_CONFIG_MANAGE') && (
          <Button type="primary" data-testid="create-definition-btn">
            <Link to="/workflow/definitions/new">Tạo định nghĩa</Link>
          </Button>
        )}
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="Lọc theo mã quy trình..."
          allowClear
          onSearch={(val) => { setProcessCodeFilter(val || undefined); setPage(1); }}
          style={{ width: 300 }}
          data-testid="workflow-process-filter"
        />
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="workflow-list-error"
        />
      )}

      {isLoading && <Spin data-testid="workflow-list-loading" />}

      {!isLoading && !error && data && data.items.length === 0 && (
        <Alert
          type="info"
          message="Không tìm thấy định nghĩa quy trình nào."
          data-testid="workflow-list-empty"
        />
      )}

      {data && data.items.length > 0 && (
        <Table
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="workflow-list-table"
          onRow={(record: WorkflowDefinitionListItem) => ({
            onClick: () => navigate(`/workflow/definitions/${record.id}`),
            style: { cursor: 'pointer' },
          })}
          pagination={{
            current: data.page,
            pageSize: data.pageSize,
            total: data.totalCount,
            onChange: (p, ps) => { setPage(p); setPageSize(ps); },
          }}
        />
      )}
    </div>
  );
};

export default WorkflowDefinitionsPage;
