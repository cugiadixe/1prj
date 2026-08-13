import React, { useEffect, useState } from 'react';
import { Alert, Button, Card, Form, Input, Space, Spin, Typography } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getDefinitionById, updateDefinition } from './workflowApi';
import { getErrorMessage, isConcurrencyError, isPermissionDenied } from './errorMessages';
import type { UpdateWorkflowDefinitionRequest } from './types';

const { Title } = Typography;

const WorkflowDefinitionEditPage: React.FC = () => {
  const { definitionId } = useParams<{ definitionId: string }>();
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const id = Number(definitionId);

  const [submitError, setSubmitError] = useState<string | null>(null);
  const [showConcurrencyRefresh, setShowConcurrencyRefresh] = useState(false);

  const {
    data: definition,
    isLoading,
    error: fetchError,
    refetch,
  } = useQuery({
    queryKey: ['workflow-definition', id],
    queryFn: () => getDefinitionById(id),
    enabled: !isNaN(id),
  });

  useEffect(() => {
    if (definition) {
      form.setFieldsValue({
        definitionName: definition.definitionName,
        description: definition.description,
      });
    }
  }, [definition, form]);

  const updateMutation = useMutation({
    mutationFn: (values: UpdateWorkflowDefinitionRequest) => updateDefinition(id, values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workflow-definitions'] });
      queryClient.invalidateQueries({ queryKey: ['workflow-definition', id] });
      navigate(`/workflow/definitions/${id}`);
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        setShowConcurrencyRefresh(true);
      }
      setSubmitError(getErrorMessage(err));
    },
  });

  const handleRefresh = async () => {
    setShowConcurrencyRefresh(false);
    setSubmitError(null);
    await refetch();
  };

  const handleSubmit = (values: Record<string, unknown>) => {
    if (!definition) return;
    setSubmitError(null);
    setShowConcurrencyRefresh(false);

    const request: UpdateWorkflowDefinitionRequest = {
      definitionName: values.definitionName as string,
      description: (values.description as string) || null,
      targetVersion: definition.rowVersion,
    };
    updateMutation.mutate(request);
  };

  if (isLoading) return <Spin data-testid="definition-edit-loading" />;

  if (isPermissionDenied(fetchError)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền sửa định nghĩa quy trình này."
        data-testid="permission-denied"
      />
    );
  }

  if (fetchError) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(fetchError)}
        data-testid="definition-edit-fetch-error"
      />
    );
  }

  if (!definition) return null;

  return (
    <div data-testid="workflow-definition-edit-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Sửa: {definition.definitionCode}
        </Title>
        <Button>
          <Link to={`/workflow/definitions/${id}`}>Quay lại chi tiết</Link>
        </Button>
      </Space>

      {submitError && (
        <Alert
          type="error"
          message={submitError}
          closable={!showConcurrencyRefresh}
          onClose={() => setSubmitError(null)}
          style={{ marginBottom: 16 }}
          data-testid="edit-error"
          action={
            showConcurrencyRefresh ? (
              <Button size="small" type="primary" onClick={handleRefresh} data-testid="refresh-btn">
                Tải lại
              </Button>
            ) : undefined
          }
        />
      )}

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          data-testid="definition-edit-form"
        >
          <Form.Item
            name="definitionName"
            label="Tên định nghĩa"
            rules={[{ required: true, message: 'Tên định nghĩa là bắt buộc' }]}
          >
            <Input data-testid="input-definitionName" />
          </Form.Item>

          <Form.Item name="description" label="Mô tả">
            <Input.TextArea rows={3} data-testid="input-description" />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                loading={updateMutation.isPending}
                data-testid="submit-update"
              >
                Cập nhật định nghĩa
              </Button>
              <Button>
                <Link to={`/workflow/definitions/${id}`}>Hủy</Link>
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default WorkflowDefinitionEditPage;
