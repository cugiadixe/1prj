import React, { useState } from 'react';
import { Alert, Button, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { searchServiceTypes } from './serviceTypesApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { ServiceTypeListItem } from './types';

const { Title } = Typography;

const ServiceTypeListPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const { data, isLoading, error } = useQuery({
    queryKey: ['serviceTypes', page, pageSize],
    queryFn: () => searchServiceTypes({ page, pageSize }),
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view service types."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Code',
      dataIndex: 'code',
      key: 'code',
    },
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Standard Price',
      dataIndex: 'standardPrice',
      key: 'standardPrice',
      render: (val: number, record: ServiceTypeListItem) => 
        `${val.toLocaleString()} ${record.standardPriceCurrency}`,
    },
    {
      title: 'Cycle (Months)',
      dataIndex: 'cycleDurationMonths',
      key: 'cycleDurationMonths',
      render: (val: number | null) => val ?? '—',
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'green' : 'red'}>
          {isActive ? 'ACTIVE' : 'INACTIVE'}
        </Tag>
      ),
    },
  ];

  return (
    <div data-testid="service-types-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Service Types</Title>
        <Space>
          {hasPermission('SERVICE_TYPE_MANAGE', 'GLOBAL') && (
            <Button type="primary" data-testid="create-service-type-btn">
              <Link to="/services/types/new">Create Service Type</Link>
            </Button>
          )}
        </Space>
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="service-type-list-error"
        />
      )}

      {isLoading && <Spin data-testid="service-type-list-loading" />}

      {!isLoading && !error && data && data.items.length === 0 && (
        <Alert
          type="info"
          message="No service types found."
          data-testid="service-type-list-empty"
        />
      )}

      {data && data.items.length > 0 && (
        <Table
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="service-type-list-table"
          onRow={(record: ServiceTypeListItem) => ({
            onClick: () => navigate(`/services/types/${record.id}`),
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

export default ServiceTypeListPage;
