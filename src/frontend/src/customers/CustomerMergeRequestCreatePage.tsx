import React, { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Form,
  InputNumber,
  Space,
  Spin,
  Typography,
} from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { getCustomerById } from './customersApi';
import { createMergeRequest } from './customerMergeApi';
import { getMergeErrorMessage } from './customerMergeErrorMessages';
import type { CreateCustomerMergeRequest } from './customerMergeTypes';

const { Title } = Typography;

const CustomerMergeRequestCreatePage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const preselectedSourceId = searchParams.get('sourceCustomerId');

  const [sourceId, setSourceId] = useState<number | null>(
    preselectedSourceId ? Number(preselectedSourceId) : null,
  );
  const [targetId, setTargetId] = useState<number | null>(null);

  const {
    data: sourceCustomer,
    isLoading: sourceLoading,
    error: sourceError,
  } = useQuery({
    queryKey: ['customer', sourceId],
    queryFn: () => getCustomerById(sourceId!),
    enabled: !!sourceId,
  });

  const {
    data: targetCustomer,
    isLoading: targetLoading,
    error: targetError,
  } = useQuery({
    queryKey: ['customer', targetId],
    queryFn: () => getCustomerById(targetId!),
    enabled: !!targetId,
  });

  const createMutation = useMutation({
    mutationFn: (request: CreateCustomerMergeRequest) =>
      createMergeRequest(request),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['merge-requests'] });
      navigate(`/customers/merge-requests/${result.id}`);
    },
    onError: (err) => {
      setSubmitError(getMergeErrorMessage(err));
    },
  });

  const handleSubmit = () => {
    if (!sourceCustomer || !targetCustomer) return;

    if (sourceCustomer.id === targetCustomer.id) {
      setSubmitError('Khách hàng nguồn và đích không được trùng nhau.');
      return;
    }

    setSubmitError(null);
    const request: CreateCustomerMergeRequest = {
      sourceCustomerId: sourceCustomer.id,
      targetCustomerId: targetCustomer.id,
      survivorshipPayload: JSON.stringify({
        survivorId: targetCustomer.id,
        sourceId: sourceCustomer.id,
      }),
      sourceRowVersionSnapshot: sourceCustomer.rowVersion,
      targetRowVersionSnapshot: targetCustomer.rowVersion,
      candidates: [
        {
          candidateCustomerId: sourceCustomer.id,
          matchType: 'MANUAL',
          matchConfidence: null,
          snapshotPayload: null,
        },
      ],
    };
    createMutation.mutate(request);
  };

  const handleSourceIdChange = (value: number | null) => {
    setSourceId(value);
    setSubmitError(null);
  };

  const handleTargetIdChange = (value: number | null) => {
    setTargetId(value);
    setSubmitError(null);
  };

  return (
    <div data-testid="customer-merge-request-create-page">
      <Space
        style={{
          marginBottom: 16,
          width: '100%',
          justifyContent: 'space-between',
        }}
      >
        <Title level={4} style={{ margin: 0 }}>
          Tạo yêu cầu gộp
        </Title>
        <Space>
          <Button>
            <Link to="/customers/merge/search">Tìm trùng lặp</Link>
          </Button>
          <Button>
            <Link to="/customers/merge-requests">Quay lại yêu cầu gộp</Link>
          </Button>
        </Space>
      </Space>

      {submitError && (
        <Alert
          type="error"
          message={submitError}
          closable
          onClose={() => setSubmitError(null)}
          style={{ marginBottom: 16 }}
          data-testid="create-error"
        />
      )}

      <Card title="Chọn khách hàng" style={{ marginBottom: 16 }}>
        <Form form={form} layout="vertical">
          <Form.Item label="Mã KH nguồn (sẽ được gộp/ngừng)">
            <InputNumber
              style={{ width: '100%' }}
              value={sourceId}
              onChange={handleSourceIdChange}
              placeholder="Nhập mã KH nguồn"
              data-testid="input-source-id"
            />
          </Form.Item>
          <Form.Item label="Mã KH đích (giữ lại)">
            <InputNumber
              style={{ width: '100%' }}
              value={targetId}
              onChange={handleTargetIdChange}
              placeholder="Nhập mã KH đích"
              data-testid="input-target-id"
            />
          </Form.Item>
        </Form>
      </Card>

      {(sourceLoading || targetLoading) && (
        <Spin data-testid="loading-spinner" />
      )}

      {sourceError && (
        <Alert
          type="error"
          message={getMergeErrorMessage(sourceError)}
          style={{ marginBottom: 16 }}
          data-testid="source-error"
        />
      )}

      {targetError && (
        <Alert
          type="error"
          message={getMergeErrorMessage(targetError)}
          style={{ marginBottom: 16 }}
          data-testid="target-error"
        />
      )}

      {sourceCustomer && targetCustomer && (
        <>
          <Card title="So sánh nguồn và đích" style={{ marginBottom: 16 }}>
            <Descriptions bordered column={2}>
              <Descriptions.Item label="KH nguồn">
                {sourceCustomer.profile.fullName} (ID: {sourceCustomer.id})
              </Descriptions.Item>
              <Descriptions.Item label="KH đích (giữ lại)">
                {targetCustomer.profile.fullName} (ID: {targetCustomer.id})
              </Descriptions.Item>
              <Descriptions.Item label="CCCD nguồn">
                {sourceCustomer.profile.cccd || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="CCCD đích">
                {targetCustomer.profile.cccd || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="ĐT nguồn">
                {sourceCustomer.profile.phone || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="ĐT đích">
                {targetCustomer.profile.phone || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái nguồn">
                {sourceCustomer.customerStatus}
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái đích">
                {targetCustomer.customerStatus}
              </Descriptions.Item>
            </Descriptions>
          </Card>

          <Button
            type="primary"
            size="large"
            onClick={handleSubmit}
            loading={createMutation.isPending}
            data-testid="submit-merge-request"
          >
            Gửi yêu cầu gộp
          </Button>
        </>
      )}
    </div>
  );
};

export default CustomerMergeRequestCreatePage;
