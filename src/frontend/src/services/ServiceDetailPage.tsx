import React, { useState } from 'react';
import { Alert, Button, Descriptions, Space, Spin, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { useCompany } from '../auth/CompanyProvider';
import { getServiceById } from './servicesApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import ServiceRenewDialog from './ServiceRenewDialog';
import ServicePriceOverrideDialog from './ServicePriceOverrideDialog';

const { Title } = Typography;

const ServiceDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const serviceId = parseInt(id || '0', 10);

  const { hasPermission } = usePermissions();
  const { currentCompanyId } = useCompany();
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
        message="You do not have permission to view this service."
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

  // Cross-company check: If the service doesn't belong to the current company context, 
  // warn the user but the backend might have already blocked it if it doesn't match the token/auth.
  if (currentCompanyId && data.companyId !== currentCompanyId) {
    return (
      <Alert
        type="warning"
        message="This service belongs to a different company than your currently selected context."
        data-testid="service-company-mismatch"
      />
    );
  }

  const isRenewable = data.status === 'ACTIVE' && hasPermission('SERVICE_RENEW_STANDARD', 'COMPANY');
  const isOverridable = data.status === 'ACTIVE' && hasPermission('SERVICE_PRICE_OVERRIDE_REQUEST', 'COMPANY');

  return (
    <div data-testid="service-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Service Details</Title>
        <Space>
          <Button onClick={() => navigate('/services')}>Back to List</Button>
          {isRenewable && (
            <Button onClick={() => setRenewVisible(true)} data-testid="renew-btn">
              Renew
            </Button>
          )}
          {isOverridable && (
            <Button onClick={() => setOverrideVisible(true)} data-testid="request-override-btn">
              Request Price Override
            </Button>
          )}
        </Space>
      </Space>

      <Descriptions bordered column={1}>
        <Descriptions.Item label="Service ID">{data.id}</Descriptions.Item>
        <Descriptions.Item label="Service Type">
          {data.serviceTypeName || data.serviceTypeCode || data.serviceTypeId}
          {' '}
          <Link to={`/services/types/${data.serviceTypeId}`}>(View Type)</Link>
        </Descriptions.Item>
        <Descriptions.Item label="Customer">
          <Link to={`/customers/${data.customerId}`}>{data.customerId}</Link>
        </Descriptions.Item>
        <Descriptions.Item label="Company ID">{data.companyId}</Descriptions.Item>
        <Descriptions.Item label="Status">
          <Tag color={data.status === 'ACTIVE' ? 'green' : data.status === 'EXPIRED' ? 'orange' : 'red'}>
            {data.status}
          </Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Applied Price">
          <Space>
            {data.appliedPrice.toLocaleString()}
            {data.isOverridePrice && <Tag color="blue">OVERRIDE</Tag>}
          </Space>
        </Descriptions.Item>
        <Descriptions.Item label="Standard Price Snapshot">
          {data.standardPriceSnapshot.toLocaleString()}
        </Descriptions.Item>
        {data.overrideApprovalRequestId && (
          <Descriptions.Item label="Override Request ID">
            {data.overrideApprovalRequestId}
          </Descriptions.Item>
        )}
        <Descriptions.Item label="Valid From">
          {new Date(data.validFrom).toLocaleDateString()}
        </Descriptions.Item>
        <Descriptions.Item label="Valid To">
          {data.validTo ? new Date(data.validTo).toLocaleDateString() : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Cycle Number">{data.cycleNumber}</Descriptions.Item>
        <Descriptions.Item label="Created At">
          {new Date(data.createdAt).toLocaleString()}
        </Descriptions.Item>
        <Descriptions.Item label="Updated At">
          {data.updatedAt ? new Date(data.updatedAt).toLocaleString() : '—'}
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
