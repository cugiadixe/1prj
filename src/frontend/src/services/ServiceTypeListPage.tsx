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
        message="Bạn không có quyền xem loại dịch vụ."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Mã',
      dataIndex: 'code',
      key: 'code',
    },
    {
      title: 'Tên',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Giá chuẩn',
      dataIndex: 'standardPrice',
      key: 'standardPrice',
      render: (val: number, record: ServiceTypeListItem) =>
        `${val.toLocaleString()} ${record.standardPriceCurrency}`,
    },
    {
      title: 'Chu kỳ (Tháng)',
      dataIndex: 'cycleDurationMonths',
      key: 'cycleDurationMonths',
      render: (val: number | null) => val ?? '—',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'green' : 'red'}>
          {isActive ? 'Hoạt động' : 'Ngừng hoạt động'}
        </Tag>
      ),
    },
  ];

  return (
    <div data-testid="service-types-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Gói dịch vụ</Title>
        <Space>
          {hasPermission('SERVICE_TYPE_MANAGE', 'GLOBAL') && (
            <Button type="primary" data-testid="create-service-type-btn">
              <Link to="/services/types/new">Tạo loại dịch vụ</Link>
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
          message="Không tìm thấy loại dịch vụ nào."
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
