import React, { useState } from 'react';
import { Alert, Button, Input, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../../auth/AuthProvider';
import { listPayments } from '../paymentApi';
import { getErrorMessage, isPermissionDenied } from '../errorMessages';
import type { PaymentTransactionListDto } from '../types';

const { Title } = Typography;

const PaymentListPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const [companyId] = useState<number>(1); // In a real app this might come from context
  const [customerId, setCustomerId] = useState<number | undefined>(undefined);
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const { data, isLoading, error } = useQuery({
    queryKey: ['payments', companyId, customerId, statusFilter, page, pageSize],
    queryFn: () =>
      listPayments({ companyId, customerId, status: statusFilter, page, pageSize }),
  });

  // Only allow list access if user has PAYMENT_CREATE_DRAFT (or some read permission, but using accepted UI gating)
  // According to accepted plan, UI gating relies on specific actions. List page is usually gated by generic or PAYMENT_CREATE_DRAFT.
  // We'll gate it with PAYMENT_CREATE_DRAFT as minimum access, but if the API returns 403, we show permission denied.
  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view payments."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Bill Code',
      dataIndex: 'billCode',
      key: 'billCode',
    },
    {
      title: 'Payment Method',
      dataIndex: 'paymentMethod',
      key: 'paymentMethod',
    },
    {
      title: 'Date',
      dataIndex: 'paymentDate',
      key: 'paymentDate',
      render: (val: string) => new Date(val).toLocaleDateString(),
    },
    {
      title: 'Total Amount',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (val: number) => `${val.toLocaleString()} VND`,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        let color = 'default';
        if (status === 'CONFIRMED') color = 'green';
        if (status === 'DRAFT') color = 'blue';
        return <Tag color={color}>{status}</Tag>;
      },
    },
  ];

  return (
    <div data-testid="payment-list-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Payments</Title>
        <Space>
          {hasPermission('PAYMENT_CREATE_DRAFT', 'GLOBAL') && (
            <Button type="primary" data-testid="create-payment-btn">
              <Link to="/payments/new">Create Draft Payment</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="Search by customer ID..."
          allowClear
          onSearch={(val) => {
            const parsed = parseInt(val, 10);
            setCustomerId(isNaN(parsed) ? undefined : parsed);
            setPage(1);
          }}
          style={{ width: 300 }}
          data-testid="payment-customer-search"
        />
        <Select
          placeholder="Status"
          allowClear
          style={{ width: 150 }}
          onChange={(val) => { setStatusFilter(val); setPage(1); }}
          value={statusFilter}
          data-testid="payment-status-filter"
          options={[
            { label: 'Draft', value: 'DRAFT' },
            { label: 'Confirmed', value: 'CONFIRMED' },
          ]}
        />
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="payment-list-error"
        />
      )}

      {isLoading && <Spin data-testid="payment-list-loading" />}

      {!isLoading && !error && data && data.items.length === 0 && (
        <Alert
          type="info"
          message="No payments found."
          data-testid="payment-list-empty"
        />
      )}

      {data && data.items.length > 0 && (
        <Table
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="payment-list-table"
          onRow={(record: PaymentTransactionListDto) => ({
            onClick: () => navigate(`/payments/${record.id}`),
            style: { cursor: 'pointer' },
          })}
          pagination={{
            current: page,
            pageSize,
            total: data.totalCount,
            onChange: (p, ps) => { setPage(p); setPageSize(ps); },
          }}
        />
      )}
    </div>
  );
};

export default PaymentListPage;
