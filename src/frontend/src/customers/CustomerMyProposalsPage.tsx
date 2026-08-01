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
      title: 'Customer Code',
      key: 'customerCode',
      render: (_: unknown, record: CustomerProposalDto) => record.summary?.customerCode || 'N/A',
    },
    {
      title: 'Full Name',
      key: 'fullName',
      render: (_: unknown, record: CustomerProposalDto) => record.summary?.fullName || 'N/A',
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
      render: (_: unknown, record: CustomerProposalDto) => (
        <Space size="middle">
          <Link to={`/customers/proposals/${record.id}`}>View Status</Link>
          {record.workflowInstanceId && (
            <Link to={`/workflow/instances/${record.workflowInstanceId}`}>View Workflow</Link>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div data-testid="customer-my-proposals-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>My Customer Proposals</Title>
        <Space>
          <Button type="primary">
            <Link to="/customers/proposals/new">Submit Proposal</Link>
          </Button>
          <Button>
            <Link to="/customers">Back to Customers</Link>
          </Button>
        </Space>
      </Space>

      {error && (
        <Alert
          type="error"
          message={getErrorMessage(error) || 'Failed to load proposals'}
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
