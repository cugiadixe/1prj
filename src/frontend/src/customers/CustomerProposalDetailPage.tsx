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
        message={getErrorMessage(error) || 'Không tìm thấy đề xuất'}
        data-testid="error-alert"
      />
    );
  }

  return (
    <div data-testid="customer-proposal-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Đề xuất {proposal.id}
        </Title>
        <Space>
          {proposal.workflowInstanceId && (
            <Button>
              <Link to={`/workflow/instances/${proposal.workflowInstanceId}`}>
                Xem quy trình
              </Link>
            </Button>
          )}
          {proposal.createdCustomerId && (
            <Button type="primary">
              <Link to={`/customers/${proposal.createdCustomerId}`}>
                Xem hồ sơ khách hàng
              </Link>
            </Button>
          )}
          <Button>
            <Link to="/customers/proposals">Quay lại đề xuất của tôi</Link>
          </Button>
        </Space>
      </Space>

      <Card title="Trạng thái đề xuất" style={{ marginBottom: 16 }}>
        <Descriptions bordered column={2}>
          <Descriptions.Item label="Trạng thái">
            <Tag color={proposal.requestStatus === 'EXECUTED' ? 'green' : 'blue'} data-testid="status-tag">
              {proposal.requestStatus}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Ngày gửi">
            {new Date(proposal.createdAt).toLocaleDateString('vi-VN')}
          </Descriptions.Item>
          <Descriptions.Item label="Cập nhật lần cuối">
            {proposal.updatedAt ? new Date(proposal.updatedAt).toLocaleDateString('vi-VN') : 'N/A'}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {proposal.summary && (
        <Card title="Tóm tắt khách hàng (Siêu dữ liệu an toàn)">
          <Descriptions bordered column={2}>
            <Descriptions.Item label="Mã khách hàng">
              {proposal.summary.customerCode}
            </Descriptions.Item>
            <Descriptions.Item label="Họ tên">
              {proposal.summary.fullName}
            </Descriptions.Item>
            <Descriptions.Item label="Mã công ty">
              {proposal.summary.companyId || 'Không có'}
            </Descriptions.Item>
          </Descriptions>
        </Card>
      )}
    </div>
  );
};

export default CustomerProposalDetailPage;
