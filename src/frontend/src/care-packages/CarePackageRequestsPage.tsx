import React, { useState } from 'react';
import { Alert, Button, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { useCarePackageRequests } from './hooks';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import { carePackageStatusColors, carePackageStatusLabel, carePackageStatusOrder } from './statusLabels';
import type { CarePackageRequestDto } from './types';

const { Title } = Typography;

const CarePackageRequestsPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);

  const { data, isLoading, error } = useCarePackageRequests(statusFilter ? { status: statusFilter } : undefined);

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem yêu cầu gói chăm sóc."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
    },
    {
      title: 'Gói chăm sóc',
      dataIndex: 'serviceName',
      key: 'serviceName',
      render: (val: string | null) => val ?? '—',
    },
    {
      title: 'Khách hàng',
      dataIndex: 'customerName',
      key: 'customerName',
      render: (val: string | null, record: CarePackageRequestDto) =>
        val ? `${val}${record.customerCode ? ` (${record.customerCode})` : ''}` : `#${record.customerId}`,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => (
        <Tag color={carePackageStatusColors[status] || 'default'}>{carePackageStatusLabel(status)}</Tag>
      ),
    },
    {
      title: 'Tổng số tiền',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (val: number) => val != null ? val.toLocaleString('vi-VN') + ' VND' : '—',
    },
    {
      title: 'Ngày bán',
      dataIndex: 'saleDate',
      key: 'saleDate',
      render: (val: string) => new Date(val).toLocaleDateString('vi-VN'),
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (val: string) => new Date(val).toLocaleString('vi-VN'),
    },
  ];

  return (
    <div data-testid="care-package-requests-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Yêu cầu gói chăm sóc</Title>
        <Space>
          {hasPermission('CARE_PACKAGE_CREATE') && (
            <Button type="primary" data-testid="create-care-package-btn">
              <Link to="/care-packages/new">Tạo yêu cầu</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Select
          placeholder="Lọc theo trạng thái"
          allowClear
          style={{ width: 200 }}
          onChange={(val) => setStatusFilter(val)}
          value={statusFilter}
          data-testid="care-package-status-filter"
          options={carePackageStatusOrder.map((s) => ({ label: carePackageStatusLabel(s), value: s }))}
        />
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="care-package-list-error"
        />
      )}

      {isLoading && <Spin data-testid="care-package-list-loading" />}

      {!isLoading && !error && data && data.items.length === 0 && (
        <Alert
          type="info"
          message="Không tìm thấy yêu cầu gói chăm sóc nào."
          data-testid="care-package-list-empty"
        />
      )}

      {data && data.items.length > 0 && (
        <Table
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="care-package-list-table"
          onRow={(record: CarePackageRequestDto) => ({
            onClick: () => navigate(`/care-packages/${record.id}`),
            style: { cursor: 'pointer' },
          })}
          pagination={{
            total: data.totalCount,
          }}
        />
      )}
    </div>
  );
};

export default CarePackageRequestsPage;
