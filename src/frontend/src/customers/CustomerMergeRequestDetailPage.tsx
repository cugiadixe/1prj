import React from 'react';
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
} from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getMergeRequestById } from './customerMergeApi';
import { getMergeErrorMessage } from './customerMergeErrorMessages';
const { Title } = Typography;

const STATUS_COLORS: Record<string, string> = {
  DRAFT: 'default',
  SUBMITTED: 'processing',
  APPROVED: 'blue',
  EXECUTED: 'green',
  REJECTED: 'red',
  WITHDRAWN: 'orange',
};

const CustomerMergeRequestDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();

  const {
    data: request,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['merge-request', id],
    queryFn: () => getMergeRequestById(id!),
    enabled: !!id,
  });

  if (isLoading) {
    return <Spin data-testid="loading-spinner" />;
  }

  if (error || !request) {
    return (
      <Alert
        type="error"
        message={getMergeErrorMessage(error)}
        data-testid="error-alert"
      />
    );
  }

  const candidateColumns = [
    {
      title: 'Candidate Customer ID',
      dataIndex: 'candidateCustomerId',
      key: 'candidateCustomerId',
    },
    {
      title: 'Match Type',
      dataIndex: 'matchType',
      key: 'matchType',
    },
    {
      title: 'Confidence',
      dataIndex: 'matchConfidence',
      key: 'matchConfidence',
      render: (val: number | null) =>
        val !== null ? `${val}%` : '—',
    },
  ];

  return (
    <div data-testid="customer-merge-request-detail-page">
      <Space
        style={{
          marginBottom: 16,
          width: '100%',
          justifyContent: 'space-between',
        }}
      >
        <Title level={4} style={{ margin: 0 }}>
          Merge Request Detail
        </Title>
        <Space>
          {request.workflowInstanceId && (
            <Button>
              <Link
                to={`/workflow/instances/${request.workflowInstanceId}`}
              >
                View Workflow
              </Link>
            </Button>
          )}
          <Button>
            <Link to={`/customers/${request.sourceCustomerId}`}>
              View Source Customer
            </Link>
          </Button>
          <Button type="primary">
            <Link to={`/customers/${request.targetCustomerId}`}>
              View Target Customer
            </Link>
          </Button>
          <Button>
            <Link to="/customers/merge-requests">
              Back to Merge Requests
            </Link>
          </Button>
        </Space>
      </Space>

      <Card title="Request Status" style={{ marginBottom: 16 }}>
        <Descriptions bordered column={2}>
          <Descriptions.Item label="Request ID">
            {request.id}
          </Descriptions.Item>
          <Descriptions.Item label="Status">
            <Tag
              color={STATUS_COLORS[request.requestStatus] || 'default'}
              data-testid="status-tag"
            >
              {request.requestStatus}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Source Customer ID">
            {request.sourceCustomerId}
          </Descriptions.Item>
          <Descriptions.Item label="Target Customer ID">
            {request.targetCustomerId}
          </Descriptions.Item>
          <Descriptions.Item label="Requester ID">
            {request.requesterId}
          </Descriptions.Item>
          <Descriptions.Item label="Created At">
            {new Date(request.createdAt).toLocaleString()}
          </Descriptions.Item>
          <Descriptions.Item label="Last Updated">
            {request.updatedAt
              ? new Date(request.updatedAt).toLocaleString()
              : 'N/A'}
          </Descriptions.Item>
          <Descriptions.Item label="Workflow Instance">
            {request.workflowInstanceId || 'Not linked'}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {request.candidates && request.candidates.length > 0 && (
        <Card title="Candidates" style={{ marginBottom: 16 }}>
          <Table
            columns={candidateColumns}
            dataSource={request.candidates}
            rowKey="candidateCustomerId"
            pagination={false}
            data-testid="candidates-table"
          />
        </Card>
      )}
    </div>
  );
};

export default CustomerMergeRequestDetailPage;
