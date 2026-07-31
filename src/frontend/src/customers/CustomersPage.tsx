import React, { useState } from 'react';
import { Alert, Button, Input, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { searchCustomers } from './customersApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { CustomerListItem } from './types';

const { Title } = Typography;

const CustomersPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const { data, isLoading, error } = useQuery({
    queryKey: ['customers', search, statusFilter, page, pageSize],
    queryFn: () =>
      searchCustomers({ search, customerStatus: statusFilter, page, pageSize }),
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view customers."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Code',
      dataIndex: 'customerCode',
      key: 'customerCode',
    },
    {
      title: 'Full Name',
      dataIndex: 'fullName',
      key: 'fullName',
    },
    {
      title: 'CCCD',
      dataIndex: 'cccd',
      key: 'cccd',
      render: (val: string | null) => val ?? '—',
    },
    {
      title: 'Phone',
      dataIndex: 'phone',
      key: 'phone',
      render: (val: string | null) => val ?? '—',
    },
    {
      title: 'Status',
      dataIndex: 'customerStatus',
      key: 'customerStatus',
      render: (status: string) => (
        <Tag color={status === 'ACTIVE' ? 'green' : status === 'INACTIVE' ? 'red' : 'default'}>
          {status}
        </Tag>
      ),
    },
  ];

  return (
    <div data-testid="customers-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Customers</Title>
        {hasPermission('CUSTOMER_CREATE_FINAL', 'GLOBAL') && (
          <Button type="primary" data-testid="create-customer-btn">
            <Link to="/customers/new">Create Customer</Link>
          </Button>
        )}
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="Search by name, code, CCCD..."
          allowClear
          onSearch={(val) => { setSearch(val); setPage(1); }}
          style={{ width: 300 }}
          data-testid="customer-search"
        />
        <Select
          placeholder="Status"
          allowClear
          style={{ width: 150 }}
          onChange={(val) => { setStatusFilter(val); setPage(1); }}
          value={statusFilter}
          data-testid="customer-status-filter"
          options={[
            { label: 'Active', value: 'ACTIVE' },
            { label: 'Inactive', value: 'INACTIVE' },
            { label: 'Merged', value: 'MERGED' },
          ]}
        />
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="customer-list-error"
        />
      )}

      {isLoading && <Spin data-testid="customer-list-loading" />}

      {!isLoading && !error && data && data.items.length === 0 && (
        <Alert
          type="info"
          message="No customers found."
          data-testid="customer-list-empty"
        />
      )}

      {data && data.items.length > 0 && (
        <Table
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="customer-list-table"
          onRow={(record: CustomerListItem) => ({
            onClick: () => navigate(`/customers/${record.id}`),
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

export default CustomersPage;
