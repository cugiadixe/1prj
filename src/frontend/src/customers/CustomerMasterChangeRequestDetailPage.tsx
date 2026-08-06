import React from 'react';
import { Alert, Button, Card, Descriptions, Space, Spin, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getCustomerMasterChangeRequestById } from './customerMasterChangeApi';
import { getErrorMessage } from './errorMessages';

const { Title } = Typography;

const CustomerMasterChangeRequestDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();

  const { data: request, isLoading, error } = useQuery({
    queryKey: ['customer-master-change-request', id],
    queryFn: () => getCustomerMasterChangeRequestById(Number(id)),
    enabled: !!id,
  });

  if (isLoading) {
    return <Spin data-testid="loading-spinner" />;
  }

  if (error || !request) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(error) || 'Request not found'}
        data-testid="error-alert"
      />
    );
  }

  return (
    <div data-testid="customer-master-change-request-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Change Request {request.id}
        </Title>
        <Space>
          {request.workflowInstanceId && (
            <Button>
              <Link to={`/workflow/instances/${request.workflowInstanceId}`}>
                View Workflow
              </Link>
            </Button>
          )}
          {request.targetCustomerId && (
            <Button type="primary">
              <Link to={`/customers/${request.targetCustomerId}`}>
                View Target Customer
              </Link>
            </Button>
          )}
          <Button>
            <Link to="/customers/change-requests">Back to My Change Requests</Link>
          </Button>
        </Space>
      </Space>

      <Card title="Request Status" style={{ marginBottom: 16 }}>
        <Descriptions bordered column={2}>
          <Descriptions.Item label="Status">
            <Tag color={request.requestStatus === 'EXECUTED' ? 'green' : 'blue'} data-testid="status-tag">
              {request.requestStatus}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Submitted At">
            {new Date(request.createdAt).toLocaleString()}
          </Descriptions.Item>
          <Descriptions.Item label="Last Updated">
            {request.updatedAt ? new Date(request.updatedAt).toLocaleString() : 'N/A'}
          </Descriptions.Item>
          <Descriptions.Item label="Target Customer ID">
            {request.targetCustomerId || 'N/A'}
          </Descriptions.Item>
          <Descriptions.Item label="Company ID">
            {request.companyId || 'None'}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {request.payload && (
        <Card title="Requested Changes (Safe Metadata)">
          <Descriptions bordered column={2}>
            <Descriptions.Item label="Reason">
              {request.payload.reason}
            </Descriptions.Item>
            <Descriptions.Item label="Full Name">
              {request.payload.fullName || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="CCCD">
              {request.payload.cccd || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Phone">
              {request.payload.phone || '—'}
            </Descriptions.Item>
          </Descriptions>
        </Card>
      )}
    </div>
  );
};

export default CustomerMasterChangeRequestDetailPage;
