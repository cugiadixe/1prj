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
      message.success('Ngừng hoạt động loại dịch vụ thành công');
      queryClient.setQueryData(['serviceType', serviceTypeId], updatedData);
      setDeactivateError(null);
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        setDeactivateError('Bản ghi đã bị thay đổi bởi người dùng khác. Vui lòng tải lại trang và thử lại.');
      } else {
        setDeactivateError(getErrorMessage(err));
      }
    },
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem loại dịch vụ."
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
      title: 'Ngừng hoạt động loại dịch vụ',
      content: 'Bạn có chắc chắn muốn ngừng hoạt động loại dịch vụ này không?',
      okText: 'Có',
      okType: 'danger',
      cancelText: 'Không',
      onOk: () => deactivateMutation.mutate(data.rowVersion),
    });
  };

  return (
    <div data-testid="service-type-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Chi tiết gói dịch vụ</Title>
        <Space>
          <Button onClick={() => navigate('/services/types')}>Quay lại danh sách</Button>
          {hasPermission('SERVICE_TYPE_MANAGE') && (
            <>
              <Button type="primary" data-testid="edit-service-type-btn">
                <Link to={`/services/types/${serviceTypeId}/edit`}>Sửa</Link>
              </Button>
              {data.isActive && (
                <Button
                  danger
                  onClick={handleDeactivate}
                  loading={deactivateMutation.isPending}
                  data-testid="deactivate-service-type-btn"
                >
                  Ngừng hoạt động
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
                 Tải lại
               </Button>
             ) : undefined
          }
        />
      )}

      <Descriptions bordered column={1}>
        <Descriptions.Item label="Mã">{data.code}</Descriptions.Item>
        <Descriptions.Item label="Tên">{data.name}</Descriptions.Item>
        <Descriptions.Item label="Mô tả">{data.description || '—'}</Descriptions.Item>
        <Descriptions.Item label="Giá chuẩn">
          {data.standardPrice.toLocaleString()} {data.standardPriceCurrency}
        </Descriptions.Item>
        <Descriptions.Item label="Chu kỳ (Tháng)">
          {data.cycleDurationMonths ?? '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Trạng thái">
          <Tag color={data.isActive ? 'green' : 'red'}>
            {data.isActive ? 'Hoạt động' : 'Ngừng hoạt động'}
          </Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Ngày tạo">
          {new Date(data.createdAt).toLocaleString('vi-VN')}
        </Descriptions.Item>
        <Descriptions.Item label="Ngày cập nhật">
          {data.updatedAt ? new Date(data.updatedAt).toLocaleString('vi-VN') : '—'}
        </Descriptions.Item>
      </Descriptions>
    </div>
  );
};

export default ServiceTypeDetailPage;
