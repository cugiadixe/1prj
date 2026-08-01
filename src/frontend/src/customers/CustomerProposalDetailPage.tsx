import React from 'react';
import { Alert, Button, Card, Descriptions, Space, Spin, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getCustomerProposalById } from './customerProposalApi';
import { getErrorMessage } from './errorMessages';

const { Title } = Typography;

const CustomerProposalDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();

  const { data: proposal, isLoading, error } = useQuery({
    queryKey: ['customer-proposal', id],
    queryFn: () => getCustomerProposalById(Number(id)),
    enabled: !!id,
  });

  if (isLoading) {
    return <Spin data-testid="loading-spinner" />;
  }

  if (error || !proposal) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(error) || 'Proposal not found'}
        data-testid="error-alert"
      />
    );
  }

  return (
    <div data-testid="customer-proposal-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Proposal {proposal.id}
        </Title>
        <Space>
          {proposal.workflowInstanceId && (
            <Button>
              <Link to={`/workflow/instances/${proposal.workflowInstanceId}`}>
                View Workflow
              </Link>
            </Button>
          )}
          {proposal.createdCustomerId && (
            <Button type="primary">
              <Link to={`/customers/${proposal.createdCustomerId}`}>
                View Customer Profile
              </Link>
            </Button>
          )}
          <Button>
            <Link to="/customers/proposals">Back to My Proposals</Link>
          </Button>
        </Space>
      </Space>

      <Card title="Proposal Status" style={{ marginBottom: 16 }}>
        <Descriptions bordered column={2}>
          <Descriptions.Item label="Status">
            <Tag color={proposal.requestStatus === 'EXECUTED' ? 'green' : 'blue'} data-testid="status-tag">
              {proposal.requestStatus}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Submitted At">
            {new Date(proposal.createdAt).toLocaleString()}
          </Descriptions.Item>
          <Descriptions.Item label="Last Updated">
            {proposal.updatedAt ? new Date(proposal.updatedAt).toLocaleString() : 'N/A'}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {proposal.summary && (
        <Card title="Customer Summary (Safe Metadata)">
          <Descriptions bordered column={2}>
            <Descriptions.Item label="Customer Code">
              {proposal.summary.customerCode}
            </Descriptions.Item>
            <Descriptions.Item label="Full Name">
              {proposal.summary.fullName}
            </Descriptions.Item>
            <Descriptions.Item label="Company ID">
              {proposal.summary.companyId || 'None'}
            </Descriptions.Item>
          </Descriptions>
        </Card>
      )}
    </div>
  );
};

export default CustomerProposalDetailPage;
