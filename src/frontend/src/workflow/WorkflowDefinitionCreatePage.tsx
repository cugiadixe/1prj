import React, { useState } from 'react';
import { Alert, Button, Card, Form, Input, Select, Space, Spin, Typography } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { createDefinition, getBusinessProcesses } from './workflowApi';
import { getErrorMessage } from './errorMessages';
import type { CreateWorkflowDefinitionRequest } from './types';

const { Title } = Typography;

const WorkflowDefinitionCreatePage: React.FC = () => {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const { data: processes, isLoading: processesLoading } = useQuery({
    queryKey: ['workflow-processes'],
    queryFn: getBusinessProcesses,
  });

  const createMutation = useMutation({
    mutationFn: (values: CreateWorkflowDefinitionRequest) => createDefinition(values),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['workflow-definitions'] });
      navigate(`/workflow/definitions/${result.id}`);
    },
    onError: (err) => {
      setSubmitError(getErrorMessage(err));
    },
  });

  const handleSubmit = (values: Record<string, unknown>) => {
    setSubmitError(null);
    const request: CreateWorkflowDefinitionRequest = {
      definitionCode: values.definitionCode as string,
      definitionName: values.definitionName as string,
      description: (values.description as string) || null,
      processCode: values.processCode as string,
    };
    createMutation.mutate(request);
  };

  if (processesLoading) return <Spin data-testid="create-definition-loading" />;

  return (
    <div data-testid="workflow-definition-create-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Tạo định nghĩa quy trình</Title>
        <Button>
          <Link to="/workflow">Quay lại danh sách</Link>
        </Button>
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

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          data-testid="definition-create-form"
        >
          <Form.Item
            name="definitionCode"
            label="Mã định nghĩa"
            rules={[{ required: true, message: 'Mã định nghĩa là bắt buộc' }]}
          >
            <Input data-testid="input-definitionCode" />
          </Form.Item>

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

          <Form.Item
            name="processCode"
            label="Quy trình nghiệp vụ"
            rules={[{ required: true, message: 'Quy trình nghiệp vụ là bắt buộc' }]}
          >
            <Select
              data-testid="input-processCode"
              placeholder="Chọn một quy trình nghiệp vụ"
              options={(processes ?? []).map((p) => ({
                label: `${p.processCode} — ${p.processName}`,
                value: p.processCode,
              }))}
            />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                loading={createMutation.isPending}
                data-testid="submit-create"
              >
                Tạo định nghĩa
              </Button>
              <Button>
                <Link to="/workflow">Hủy</Link>
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default WorkflowDefinitionCreatePage;
