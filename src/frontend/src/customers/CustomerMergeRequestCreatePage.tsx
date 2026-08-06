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
      setSubmitError('Source and target customer cannot be the same.');
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
          Create Merge Request
        </Title>
        <Space>
          <Button>
            <Link to="/customers/merge/search">Find Duplicates</Link>
          </Button>
          <Button>
            <Link to="/customers/merge-requests">Back to Merge Requests</Link>
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

      <Card title="Select Customers" style={{ marginBottom: 16 }}>
        <Form form={form} layout="vertical">
          <Form.Item label="Source Customer ID (to be merged/retired)">
            <InputNumber
              style={{ width: '100%' }}
              value={sourceId}
              onChange={handleSourceIdChange}
              placeholder="Enter source customer ID"
              data-testid="input-source-id"
            />
          </Form.Item>
          <Form.Item label="Target Customer ID (survivor)">
            <InputNumber
              style={{ width: '100%' }}
              value={targetId}
              onChange={handleTargetIdChange}
              placeholder="Enter target customer ID"
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
          <Card title="Source vs Survivor Comparison" style={{ marginBottom: 16 }}>
            <Descriptions bordered column={2}>
              <Descriptions.Item label="Source Customer">
                {sourceCustomer.profile.fullName} (ID: {sourceCustomer.id})
              </Descriptions.Item>
              <Descriptions.Item label="Target (Survivor) Customer">
                {targetCustomer.profile.fullName} (ID: {targetCustomer.id})
              </Descriptions.Item>
              <Descriptions.Item label="Source CCCD">
                {sourceCustomer.profile.cccd || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Target CCCD">
                {targetCustomer.profile.cccd || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Source Phone">
                {sourceCustomer.profile.phone || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Target Phone">
                {targetCustomer.profile.phone || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Source Status">
                {sourceCustomer.customerStatus}
              </Descriptions.Item>
              <Descriptions.Item label="Target Status">
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
            Submit Merge Request
          </Button>
        </>
      )}
    </div>
  );
};

export default CustomerMergeRequestCreatePage;
