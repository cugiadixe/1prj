import React, { useState } from 'react';
import { Alert, Button, Space, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { listMergeRequests } from './customerMergeApi';
import { getMergeErrorMessage } from './customerMergeErrorMessages';
import type { CustomerMergeRequestDto } from './customerMergeTypes';

const { Title } = Typography;

const STATUS_COLORS: Record<string, string> = {
  DRAFT: 'default',
  SUBMITTED: 'processing',
  APPROVED: 'blue',
  EXECUTED: 'green',
  REJECTED: 'red',
  WITHDRAWN: 'orange',
};

const CustomerMergeRequestsPage: React.FC = () => {
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading, error } = useQuery({
    queryKey: ['merge-requests', page, pageSize],
    queryFn: () => listMergeRequests({ page, pageSize }),
  });

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      render: (text: string) => text.substring(0, 8) + '...',
    },
    {
      title: 'Source Customer',
      dataIndex: 'sourceCustomerId',
      key: 'sourceCustomerId',
    },
    {
      title: 'Target Customer',
      dataIndex: 'targetCustomerId',
      key: 'targetCustomerId',
    },
    {
      title: 'Status',
      dataIndex: 'requestStatus',
      key: 'requestStatus',
      render: (status: string) => (
        <Tag color={STATUS_COLORS[status] || 'default'} data-testid="status-tag">
          {status}
        </Tag>
      ),
    },
    {
      title: 'Created At',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => new Date(text).toLocaleString(),
    },
    {
      title: 'Action',
      key: 'action',
      render: (_: unknown, record: CustomerMergeRequestDto) => (
        <Space size="middle">
          <Link to={`/customers/merge-requests/${record.id}`}>View</Link>
          {record.workflowInstanceId && (
            <Link to={`/workflow/instances/${record.workflowInstanceId}`}>
              Workflow
            </Link>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div data-testid="customer-merge-requests-page">
      <Space
        style={{
          marginBottom: 16,
          width: '100%',
          justifyContent: 'space-between',
        }}
      >
        <Title level={4} style={{ margin: 0 }}>
          Merge Requests
        </Title>
        <Space>
          <Button type="primary">
            <Link to="/customers/merge/search">Find Duplicates</Link>
          </Button>
          <Button>
            <Link to="/customers">Back to Customers</Link>
          </Button>
        </Space>
      </Space>

      {error && (
        <Alert
          type="error"
          message={getMergeErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="list-error"
        />
      )}

      <Table
        columns={columns}
        dataSource={data?.items}
        rowKey="id"
        loading={isLoading}
        pagination={{
          current: page,
          pageSize,
          total: data?.totalCount,
          onChange: (p) => setPage(p),
        }}
      />
    </div>
  );
};

export default CustomerMergeRequestsPage;
