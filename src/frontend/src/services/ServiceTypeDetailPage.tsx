import React, { useState } from 'react';
import { Alert, Button, Descriptions, Space, Spin, Tag, Typography, message, Modal } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { deactivateServiceType, getServiceTypeById } from './serviceTypesApi';
import { getErrorMessage, isPermissionDenied, isConcurrencyError } from './errorMessages';

const { Title } = Typography;

const ServiceTypeDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const serviceTypeId = parseInt(id || '0', 10);
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [deactivateError, setDeactivateError] = useState<string | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['serviceType', serviceTypeId],
    queryFn: () => getServiceTypeById(serviceTypeId),
    enabled: !!serviceTypeId,
  });

  const deactivateMutation = useMutation({
    mutationFn: (rowVersion: string) => deactivateServiceType(serviceTypeId, rowVersion),
    onSuccess: (updatedData) => {
      message.success('Service type deactivated successfully');
      queryClient.setQueryData(['serviceType', serviceTypeId], updatedData);
      setDeactivateError(null);
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        setDeactivateError('This record was modified by another user. Please refresh the page and try again.');
      } else {
        setDeactivateError(getErrorMessage(err));
      }
    },
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view service types."
        data-testid="permission-denied"
      />
    );
  }

  if (error && !isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(error)}
        data-testid="service-type-detail-error"
      />
    );
  }

  if (isLoading || !data) {
    return <Spin data-testid="service-type-detail-loading" />;
  }

  const handleDeactivate = () => {
    Modal.confirm({
      title: 'Deactivate Service Type',
      content: 'Are you sure you want to deactivate this service type?',
      okText: 'Yes',
      okType: 'danger',
      cancelText: 'No',
      onOk: () => deactivateMutation.mutate(data.rowVersion),
    });
  };

  return (
    <div data-testid="service-type-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Service Type Details</Title>
        <Space>
          <Button onClick={() => navigate('/services/types')}>Back to List</Button>
          {hasPermission('SERVICE_TYPE_MANAGE', 'GLOBAL') && (
            <>
              <Button type="primary" data-testid="edit-service-type-btn">
                <Link to={`/services/types/${serviceTypeId}/edit`}>Edit</Link>
              </Button>
              {data.isActive && (
                <Button 
                  danger 
                  onClick={handleDeactivate} 
                  loading={deactivateMutation.isPending}
                  data-testid="deactivate-service-type-btn"
                >
                  Deactivate
                </Button>
              )}
            </>
          )}
        </Space>
      </Space>

      {deactivateError && (
        <Alert
          type="error"
          message={deactivateError}
          style={{ marginBottom: 16 }}
          data-testid="service-type-deactivate-error"
          action={
             isConcurrencyError(deactivateError) ? (
               <Button size="small" type="primary" onClick={() => queryClient.invalidateQueries({ queryKey: ['serviceType', serviceTypeId] })}>
                 Refresh
               </Button>
             ) : undefined
          }
        />
      )}

      <Descriptions bordered column={1}>
        <Descriptions.Item label="Code">{data.code}</Descriptions.Item>
        <Descriptions.Item label="Name">{data.name}</Descriptions.Item>
        <Descriptions.Item label="Description">{data.description || '—'}</Descriptions.Item>
        <Descriptions.Item label="Standard Price">
          {data.standardPrice.toLocaleString()} {data.standardPriceCurrency}
        </Descriptions.Item>
        <Descriptions.Item label="Cycle Duration (Months)">
          {data.cycleDurationMonths ?? '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Status">
          <Tag color={data.isActive ? 'green' : 'red'}>
            {data.isActive ? 'ACTIVE' : 'INACTIVE'}
          </Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Created At">
          {new Date(data.createdAt).toLocaleString()}
        </Descriptions.Item>
        <Descriptions.Item label="Updated At">
          {data.updatedAt ? new Date(data.updatedAt).toLocaleString() : '—'}
        </Descriptions.Item>
      </Descriptions>
    </div>
  );
};

export default ServiceTypeDetailPage;
