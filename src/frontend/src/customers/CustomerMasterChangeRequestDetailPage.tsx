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
        message={getErrorMessage(error) || 'Không tìm thấy yêu cầu'}
        data-testid="error-alert"
      />
    );
  }

  return (
    <div data-testid="customer-master-change-request-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Yêu cầu thay đổi {request.id}
        </Title>
        <Space>
          {request.workflowInstanceId && (
            <Button>
              <Link to={`/workflow/instances/${request.workflowInstanceId}`}>
                Xem quy trình
              </Link>
            </Button>
          )}
          {request.targetCustomerId && (
            <Button type="primary">
              <Link to={`/customers/${request.targetCustomerId}`}>
                Xem khách hàng đích
              </Link>
            </Button>
          )}
          <Button>
            <Link to="/customers/change-requests">Quay lại yêu cầu thay đổi</Link>
          </Button>
        </Space>
      </Space>

      <Card title="Trạng thái yêu cầu" style={{ marginBottom: 16 }}>
        <Descriptions bordered column={2}>
          <Descriptions.Item label="Trạng thái">
            <Tag color={request.requestStatus === 'EXECUTED' ? 'green' : 'blue'} data-testid="status-tag">
              {request.requestStatus}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Ngày gửi">
            {new Date(request.createdAt).toLocaleDateString('vi-VN')}
          </Descriptions.Item>
          <Descriptions.Item label="Cập nhật lần cuối">
            {request.updatedAt ? new Date(request.updatedAt).toLocaleDateString('vi-VN') : 'N/A'}
          </Descriptions.Item>
          <Descriptions.Item label="Mã KH đích">
            {request.targetCustomerId || 'N/A'}
          </Descriptions.Item>
          <Descriptions.Item label="Mã công ty">
            {request.companyId || 'Không có'}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {request.payload && (
        <Card title="Thay đổi yêu cầu (Siêu dữ liệu an toàn)">
          <Descriptions bordered column={2}>
            <Descriptions.Item label="Lý do">
              {request.payload.reason}
            </Descriptions.Item>
            <Descriptions.Item label="Họ tên">
              {request.payload.fullName || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="CCCD">
              {request.payload.cccd || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Điện thoại">
              {request.payload.phone || '—'}
            </Descriptions.Item>
          </Descriptions>
        </Card>
      )}
    </div>
  );
};

export default CustomerMasterChangeRequestDetailPage;
