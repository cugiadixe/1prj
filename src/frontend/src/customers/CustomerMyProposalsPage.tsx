import React from 'react';
import { Button, Space, Table, Typography, Alert } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getMyCustomerProposals } from './customerProposalApi';
import type { CustomerProposalDto } from './customerProposalTypes';
import { getErrorMessage } from './errorMessages';

const { Title } = Typography;

const CustomerMyProposalsPage: React.FC = () => {
  const { data: proposals, isLoading, error } = useQuery({
    queryKey: ['my-customer-proposals'],
    queryFn: getMyCustomerProposals,
  });

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
    },
    {
      title: 'Mã khách hàng',
      key: 'customerCode',
      render: (_: unknown, record: CustomerProposalDto) => record.summary?.customerCode || 'N/A',
    },
    {
      title: 'Họ tên',
      key: 'fullName',
      render: (_: unknown, record: CustomerProposalDto) => record.summary?.fullName || 'N/A',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'requestStatus',
      key: 'requestStatus',
    },
    {
      title: 'Ngày gửi',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => new Date(text).toLocaleDateString('vi-VN'),
    },
    {
      title: 'Thao tác',
      key: 'action',
      render: (_: unknown, record: CustomerProposalDto) => (
        <Space size="middle">
          <Link to={`/customers/proposals/${record.id}`}>Xem trạng thái</Link>
          {record.workflowInstanceId && (
            <Link to={`/workflow/instances/${record.workflowInstanceId}`}>Xem quy trình</Link>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div data-testid="customer-my-proposals-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Đề xuất khách hàng của tôi</Title>
        <Space>
          <Button type="primary">
            <Link to="/customers/proposals/new">Gửi đề xuất</Link>
          </Button>
          <Button>
            <Link to="/customers">Quay lại khách hàng</Link>
          </Button>
        </Space>
      </Space>

      {error && (
        <Alert
          type="error"
          message={getErrorMessage(error) || 'Không thể tải danh sách đề xuất'}
          style={{ marginBottom: 16 }}
        />
      )}

      <Table
        columns={columns}
        dataSource={proposals}
        rowKey="id"
        loading={isLoading}
        pagination={{ pageSize: 20 }}
      />
    </div>
  );
};

export default CustomerMyProposalsPage;
