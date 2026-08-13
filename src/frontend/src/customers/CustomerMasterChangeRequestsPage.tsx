import React from 'react';
import { Button, Space, Table, Typography, Alert } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getMyCustomerMasterChangeRequests } from './customerMasterChangeApi';
import type { CustomerMasterChangeDto } from './customerMasterChangeTypes';
import { getErrorMessage } from './errorMessages';

const { Title } = Typography;

const CustomerMasterChangeRequestsPage: React.FC = () => {
  const { data: requests, isLoading, error } = useQuery({
    queryKey: ['my-change-requests'],
    queryFn: getMyCustomerMasterChangeRequests,
  });

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
    },
    {
      title: 'Mã KH đích',
      dataIndex: 'targetCustomerId',
      key: 'targetCustomerId',
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
      render: (_: unknown, record: CustomerMasterChangeDto) => (
        <Space size="middle">
          <Link to={`/customers/change-requests/${record.id}`}>Xem trạng thái</Link>
          {record.workflowInstanceId && (
            <Link to={`/workflow/instances/${record.workflowInstanceId}`}>Xem quy trình</Link>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div data-testid="customer-master-change-requests-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Yêu cầu thay đổi khách hàng của tôi</Title>
        <Space>
          <Button>
            <Link to="/customers">Quay lại khách hàng</Link>
          </Button>
        </Space>
      </Space>

      {error && (
        <Alert
          type="error"
          message={getErrorMessage(error) || 'Không thể tải danh sách yêu cầu thay đổi'}
          style={{ marginBottom: 16 }}
        />
      )}

      <Table
        columns={columns}
        dataSource={requests}
        rowKey="id"
        loading={isLoading}
        pagination={{ pageSize: 20 }}
      />
    </div>
  );
};

export default CustomerMasterChangeRequestsPage;
