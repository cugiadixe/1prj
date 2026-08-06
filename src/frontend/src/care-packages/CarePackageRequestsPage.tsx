import React, { useState } from 'react';
import { Alert, Button, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { useCarePackageRequests } from './hooks';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { CarePackageRequestDto } from './types';

const { Title } = Typography;

const statusColors: Record<string, string> = {
  Draft: 'default',
  PendingApproval: 'orange',
  PaymentEligible: 'blue',
  PendingPayment: 'purple',
  Paid: 'cyan',
  Active: 'green',
  Rejected: 'red',
};

const CarePackageRequestsPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);

  const { data, isLoading, error } = useCarePackageRequests(statusFilter ? { status: statusFilter } : undefined);

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view care package requests."
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
      title: 'Customer ID',
      dataIndex: 'customerId',
      key: 'customerId',
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => (
        <Tag color={statusColors[status] || 'default'}>{status}</Tag>
      ),
    },
    {
      title: 'Total Amount',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (val: number) => val != null ? val.toLocaleString('vi-VN') + ' VND' : '—',
    },
    {
      title: 'Sale Date',
      dataIndex: 'saleDate',
      key: 'saleDate',
      render: (val: string) => new Date(val).toLocaleDateString(),
    },
    {
      title: 'Created At',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (val: string) => new Date(val).toLocaleString(),
    },
  ];

  return (
    <div data-testid="care-package-requests-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Care Package Requests</Title>
        <Space>
          {hasPermission('CARE_PACKAGE_CREATE', 'COMPANY') && (
            <Button type="primary" data-testid="create-care-package-btn">
              <Link to="/care-packages/new">Create Request</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Select
          placeholder="Filter by Status"
          allowClear
          style={{ width: 200 }}
          onChange={(val) => setStatusFilter(val)}
          value={statusFilter}
          data-testid="care-package-status-filter"
          options={[
            { label: 'Draft', value: 'Draft' },
            { label: 'PendingApproval', value: 'PendingApproval' },
            { label: 'PaymentEligible', value: 'PaymentEligible' },
            { label: 'PendingPayment', value: 'PendingPayment' },
            { label: 'Paid', value: 'Paid' },
            { label: 'Active', value: 'Active' },
            { label: 'Rejected', value: 'Rejected' },
          ]}
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
          message="No care package requests found."
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
