import React, { useState } from 'react';
import { Alert, Button, Descriptions, Space, Spin, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { getServiceById } from './servicesApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import ServiceRenewDialog from './ServiceRenewDialog';
import ServicePriceOverrideDialog from './ServicePriceOverrideDialog';

const { Title } = Typography;

const ServiceDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const serviceId = parseInt(id || '0', 10);

  const { hasPermission } = usePermissions();
  const navigate = useNavigate();

  const [renewVisible, setRenewVisible] = useState(false);
  const [overrideVisible, setOverrideVisible] = useState(false);

  const hasViewPerm = hasPermission('SERVICE_VIEW', 'COMPANY');

  const { data, isLoading, error } = useQuery({
    queryKey: ['service', serviceId],
    queryFn: () => getServiceById(serviceId),
    enabled: !!serviceId && hasViewPerm,
  });

  if (!hasViewPerm || isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem dịch vụ này."
        data-testid="permission-denied"
      />
    );
  }

  if (error && !isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(error)}
        data-testid="service-detail-error"
      />
    );
  }

  if (isLoading || !data) {
    return <Spin data-testid="service-detail-loading" />;
  }

  // Dịch vụ xem chéo công ty: backend đã kiểm quyền SERVICE_VIEW theo công ty của dịch vụ.
  const isRenewable = data.status === 'ACTIVE' && hasPermission('SERVICE_RENEW_STANDARD', 'COMPANY');
  const isOverridable = data.status === 'ACTIVE' && hasPermission('SERVICE_PRICE_OVERRIDE_REQUEST', 'COMPANY');

  return (
    <div data-testid="service-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Chi tiết dịch vụ</Title>
        <Space>
          <Button onClick={() => navigate('/services')}>Quay lại danh sách</Button>
          {isRenewable && (
            <Button onClick={() => setRenewVisible(true)} data-testid="renew-btn">
              Gia hạn
            </Button>
          )}
          {isOverridable && (
            <Button onClick={() => setOverrideVisible(true)} data-testid="request-override-btn">
              Yêu cầu ghi đè giá
            </Button>
          )}
        </Space>
      </Space>

      <Descriptions bordered column={1}>
        <Descriptions.Item label="Mã dịch vụ">{data.id}</Descriptions.Item>
        <Descriptions.Item label="Loại dịch vụ">
          {data.serviceTypeName || data.serviceTypeCode || data.serviceTypeId}
          {' '}
          <Link to={`/services/types/${data.serviceTypeId}`}>(Xem loại)</Link>
        </Descriptions.Item>
        <Descriptions.Item label="Khách hàng">
          <Link to={`/customers/${data.customerId}`}>{data.customerName ?? data.customerId}</Link>
        </Descriptions.Item>
        <Descriptions.Item label="Mã khách hàng">{data.customerCode ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Công ty">{data.companyName ?? `Mã ${data.companyId}`}</Descriptions.Item>
        <Descriptions.Item label="Trạng thái">
          <Tag color={data.status === 'ACTIVE' ? 'green' : data.status === 'EXPIRED' ? 'orange' : 'red'}>
            {data.status}
          </Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Giá áp dụng">
          <Space>
            {data.appliedPrice.toLocaleString()}
            {data.isOverridePrice && <Tag color="blue">GHI ĐÈ</Tag>}
          </Space>
        </Descriptions.Item>
        <Descriptions.Item label="Giá gốc (snapshot)">
          {data.standardPriceSnapshot.toLocaleString()}
        </Descriptions.Item>
        {data.overrideApprovalRequestId && (
          <Descriptions.Item label="Mã yêu cầu ghi đè">
            {data.overrideApprovalRequestId}
          </Descriptions.Item>
        )}
        <Descriptions.Item label="Hiệu lực từ">
          {new Date(data.validFrom).toLocaleDateString('vi-VN')}
        </Descriptions.Item>
        <Descriptions.Item label="Hiệu lực đến">
          {data.validTo ? new Date(data.validTo).toLocaleDateString('vi-VN') : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Số chu kỳ">{data.cycleNumber}</Descriptions.Item>
        <Descriptions.Item label="Ngày tạo">
          {new Date(data.createdAt).toLocaleString('vi-VN')}
        </Descriptions.Item>
        <Descriptions.Item label="Ngày cập nhật">
          {data.updatedAt ? new Date(data.updatedAt).toLocaleString('vi-VN') : '—'}
        </Descriptions.Item>
      </Descriptions>

      {renewVisible && (
        <ServiceRenewDialog
          visible={renewVisible}
          onClose={() => setRenewVisible(false)}
          service={data}
        />
      )}

      {overrideVisible && (
        <ServicePriceOverrideDialog
          visible={overrideVisible}
          onClose={() => setOverrideVisible(false)}
          service={data}
        />
      )}
    </div>
  );
};

export default ServiceDetailPage;
