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
  const [companyId] = useState<number>(1);
  const [customerId, setCustomerId] = useState<number | undefined>(undefined);
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const { data, isLoading, error } = useQuery({
    queryKey: ['payments', companyId, customerId, statusFilter, page, pageSize],
    queryFn: () =>
      listPayments({ companyId, customerId, status: statusFilter, page, pageSize }),
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem thanh toán."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Mã hóa đơn',
      dataIndex: 'billCode',
      key: 'billCode',
    },
    {
      title: 'Phương thức',
      dataIndex: 'paymentMethod',
      key: 'paymentMethod',
    },
    {
      title: 'Ngày',
      dataIndex: 'paymentDate',
      key: 'paymentDate',
      render: (val: string) => new Date(val).toLocaleDateString('vi-VN'),
    },
    {
      title: 'Tổng tiền',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (val: number) => `${val.toLocaleString('vi-VN')} VND`,
    },
    {
      title: 'Trạng thái',
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
        <Title level={4} style={{ margin: 0 }}>Thanh toán</Title>
        <Space>
          {hasPermission('PAYMENT_CREATE_DRAFT') && (
            <Button type="primary" data-testid="create-payment-btn">
              <Link to="/payments/new">Tạo phiếu nháp</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="Tìm theo mã KH..."
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
          placeholder="Trạng thái"
          allowClear
          style={{ width: 150 }}
          onChange={(val) => { setStatusFilter(val); setPage(1); }}
          value={statusFilter}
          data-testid="payment-status-filter"
          options={[
            { label: 'Nháp', value: 'DRAFT' },
            { label: 'Đã xác nhận', value: 'CONFIRMED' },
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
          message="Không tìm thấy thanh toán."
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
