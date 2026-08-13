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
      title: 'KH nguồn',
      dataIndex: 'sourceCustomerId',
      key: 'sourceCustomerId',
    },
    {
      title: 'KH đích',
      dataIndex: 'targetCustomerId',
      key: 'targetCustomerId',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'requestStatus',
      key: 'requestStatus',
      render: (status: string) => (
        <Tag color={STATUS_COLORS[status] || 'default'} data-testid="status-tag">
          {status}
        </Tag>
      ),
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => new Date(text).toLocaleDateString('vi-VN'),
    },
    {
      title: 'Thao tác',
      key: 'action',
      render: (_: unknown, record: CustomerMergeRequestDto) => (
        <Space size="middle">
          <Link to={`/customers/merge-requests/${record.id}`}>Xem</Link>
          {record.workflowInstanceId && (
            <Link to={`/workflow/instances/${record.workflowInstanceId}`}>
              Quy trình
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
          Yêu cầu gộp
        </Title>
        <Space>
          <Button type="primary">
            <Link to="/customers/merge/search">Tìm trùng lặp</Link>
          </Button>
          <Button>
            <Link to="/customers">Quay lại khách hàng</Link>
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
