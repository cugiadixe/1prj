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
      title: 'Target Customer ID',
      dataIndex: 'targetCustomerId',
      key: 'targetCustomerId',
    },
    {
      title: 'Status',
      dataIndex: 'requestStatus',
      key: 'requestStatus',
    },
    {
      title: 'Submitted At',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => new Date(text).toLocaleString(),
    },
    {
      title: 'Action',
      key: 'action',
      render: (_: unknown, record: CustomerMasterChangeDto) => (
        <Space size="middle">
          <Link to={`/customers/change-requests/${record.id}`}>View Status</Link>
          {record.workflowInstanceId && (
            <Link to={`/workflow/instances/${record.workflowInstanceId}`}>View Workflow</Link>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div data-testid="customer-master-change-requests-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>My Customer Change Requests</Title>
        <Space>
          <Button>
            <Link to="/customers">Back to Customers</Link>
          </Button>
        </Space>
      </Space>

      {error && (
        <Alert
          type="error"
          message={getErrorMessage(error) || 'Failed to load change requests'}
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
