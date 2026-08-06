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
        <Title level={4} style={{ margin: 0 }}>Create Workflow Definition</Title>
        <Button>
          <Link to="/workflow">Back to List</Link>
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
            label="Definition Code"
            rules={[{ required: true, message: 'Definition code is required' }]}
          >
            <Input data-testid="input-definitionCode" />
          </Form.Item>

          <Form.Item
            name="definitionName"
            label="Definition Name"
            rules={[{ required: true, message: 'Definition name is required' }]}
          >
            <Input data-testid="input-definitionName" />
          </Form.Item>

          <Form.Item name="description" label="Description">
            <Input.TextArea rows={3} data-testid="input-description" />
          </Form.Item>

          <Form.Item
            name="processCode"
            label="Business Process"
            rules={[{ required: true, message: 'Business process is required' }]}
          >
            <Select
              data-testid="input-processCode"
              placeholder="Select a business process"
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
                Create Definition
              </Button>
              <Button>
                <Link to="/workflow">Cancel</Link>
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default WorkflowDefinitionCreatePage;
