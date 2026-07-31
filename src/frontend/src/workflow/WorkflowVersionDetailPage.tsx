import React, { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  DatePicker,
  Descriptions,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Spin,
  Switch,
  Table,
  Tag,
  Typography,
} from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import {
  activateVersion,
  createApproverRule,
  createStep,
  deleteStep,
  deleteVersion,
  getVersionById,
  publishVersion,
  retireVersion,
  updateStep,
} from './workflowApi';
import { getErrorMessage, isConcurrencyError, isPermissionDenied } from './errorMessages';
import type {
  CreateApproverRuleRequest,
  CreateWorkflowStepRequest,
  UpdateWorkflowStepRequest,
  WorkflowStep,
} from './types';

const { Title, Text } = Typography;

const STATUS_COLORS: Record<string, string> = {
  DRAFT: 'default',
  PUBLISHED: 'blue',
  ACTIVE: 'green',
  RETIRED: 'red',
};

const WorkflowVersionDetailPage: React.FC = () => {
  const { definitionId, versionId } = useParams<{ definitionId: string; versionId: string }>();
  const { hasPermission } = usePermissions();
  const queryClient = useQueryClient();
  const vId = Number(versionId);
  const defId = Number(definitionId);

  const [actionError, setActionError] = useState<string | null>(null);
  const [showConcurrencyRefresh, setShowConcurrencyRefresh] = useState(false);
  const [stepModalOpen, setStepModalOpen] = useState(false);
  const [editingStep, setEditingStep] = useState<WorkflowStep | null>(null);
  const [ruleModalOpen, setRuleModalOpen] = useState(false);
  const [ruleStepId, setRuleStepId] = useState<number | null>(null);
  const [publishModalOpen, setPublishModalOpen] = useState(false);

  const [stepForm] = Form.useForm();
  const [ruleForm] = Form.useForm();
  const [publishForm] = Form.useForm();

  const {
    data: version,
    isLoading,
    error: fetchError,
    refetch,
  } = useQuery({
    queryKey: ['workflow-version', vId],
    queryFn: () => getVersionById(vId),
    enabled: !isNaN(vId),
  });

  const handleError = (err: unknown) => {
    if (isConcurrencyError(err)) {
      setShowConcurrencyRefresh(true);
    }
    setActionError(getErrorMessage(err));
  };

  const handleRefresh = async () => {
    setShowConcurrencyRefresh(false);
    setActionError(null);
    await refetch();
  };

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['workflow-version', vId] });
    queryClient.invalidateQueries({ queryKey: ['workflow-versions', defId] });
  };

  const createStepMutation = useMutation({
    mutationFn: (req: CreateWorkflowStepRequest) => createStep(vId, req),
    onSuccess: () => { invalidate(); setStepModalOpen(false); stepForm.resetFields(); },
    onError: handleError,
  });

  const updateStepMutation = useMutation({
    mutationFn: ({ stepId, req }: { stepId: number; req: UpdateWorkflowStepRequest }) =>
      updateStep(stepId, req),
    onSuccess: () => { invalidate(); setStepModalOpen(false); setEditingStep(null); stepForm.resetFields(); },
    onError: handleError,
  });

  const deleteStepMutation = useMutation({
    mutationFn: (stepId: number) => deleteStep(stepId),
    onSuccess: invalidate,
    onError: handleError,
  });

  const createRuleMutation = useMutation({
    mutationFn: ({ stepId, req }: { stepId: number; req: CreateApproverRuleRequest }) =>
      createApproverRule(stepId, req),
    onSuccess: () => { invalidate(); setRuleModalOpen(false); ruleForm.resetFields(); },
    onError: handleError,
  });

  const publishMutation = useMutation({
    mutationFn: () => {
      const vals = publishForm.getFieldsValue();
      return publishVersion(vId, {
        effectiveFrom: (vals.effectiveFrom as { toISOString: () => string }).toISOString(),
        effectiveTo: vals.effectiveTo ? (vals.effectiveTo as { toISOString: () => string }).toISOString() : null,
        targetVersion: version!.rowVersion,
      });
    },
    onSuccess: () => { invalidate(); setPublishModalOpen(false); publishForm.resetFields(); },
    onError: handleError,
  });

  const activateMutation = useMutation({
    mutationFn: () => activateVersion(vId, { targetVersion: version!.rowVersion }),
    onSuccess: invalidate,
    onError: handleError,
  });

  const retireMutation = useMutation({
    mutationFn: () => retireVersion(vId, { targetVersion: version!.rowVersion }),
    onSuccess: invalidate,
    onError: handleError,
  });

  const deleteVersionMutation = useMutation({
    mutationFn: () => deleteVersion(vId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workflow-versions', defId] });
    },
    onError: handleError,
  });

  if (isLoading) return <Spin data-testid="version-detail-loading" />;

  if (isPermissionDenied(fetchError)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view this workflow version."
        data-testid="permission-denied"
      />
    );
  }

  if (fetchError) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(fetchError)}
        data-testid="version-detail-error"
      />
    );
  }

  if (!version) return null;

  const isDraft = version.versionStatus === 'DRAFT';
  const isPublished = version.versionStatus === 'PUBLISHED';
  const isActive = version.versionStatus === 'ACTIVE';
  const canManage = hasPermission('WORKFLOW_CONFIG_MANAGE', 'GLOBAL');
  const canPublish = hasPermission('WORKFLOW_PUBLISH', 'GLOBAL');

  const handleStepSubmit = (values: Record<string, unknown>) => {
    if (editingStep) {
      updateStepMutation.mutate({
        stepId: editingStep.id,
        req: {
          stepName: values.stepName as string,
          stepOrder: values.stepOrder as number,
          isRequired: values.isRequired as boolean,
          description: (values.description as string) || null,
          dueDurationMinutes: (values.dueDurationMinutes as number) || null,
          targetVersion: editingStep.rowVersion,
        },
      });
    } else {
      createStepMutation.mutate({
        stepName: values.stepName as string,
        stepOrder: values.stepOrder as number,
        isRequired: (values.isRequired as boolean) ?? true,
        description: (values.description as string) || null,
        dueDurationMinutes: (values.dueDurationMinutes as number) || null,
      });
    }
  };

  const handleRuleSubmit = (values: Record<string, unknown>) => {
    if (ruleStepId === null) return;
    createRuleMutation.mutate({
      stepId: ruleStepId,
      req: {
        approverSourceType: values.approverSourceType as string,
        approverSourceValue: values.approverSourceValue as string,
        priority: values.priority as number,
      },
    });
  };

  const openEditStep = (step: WorkflowStep) => {
    setEditingStep(step);
    stepForm.setFieldsValue({
      stepName: step.stepName,
      stepOrder: step.stepOrder,
      isRequired: step.isRequired,
      description: step.description,
      dueDurationMinutes: step.dueDurationMinutes,
    });
    setStepModalOpen(true);
  };

  const openAddStep = () => {
    setEditingStep(null);
    stepForm.resetFields();
    stepForm.setFieldsValue({ isRequired: true, stepOrder: (version.steps.length + 1) });
    setStepModalOpen(true);
  };

  const openAddRule = (stepId: number) => {
    setRuleStepId(stepId);
    ruleForm.resetFields();
    setRuleModalOpen(true);
  };

  const stepColumns = [
    { title: 'Order', dataIndex: 'stepOrder', key: 'stepOrder', width: 80 },
    { title: 'Name', dataIndex: 'stepName', key: 'stepName' },
    {
      title: 'Required',
      dataIndex: 'isRequired',
      key: 'isRequired',
      render: (v: boolean) => v ? 'Yes' : 'No',
    },
    {
      title: 'Due (min)',
      dataIndex: 'dueDurationMinutes',
      key: 'dueDurationMinutes',
      render: (v: number | null) => v ?? '—',
    },
    {
      title: 'Approver Rules',
      key: 'rules',
      render: (_: unknown, step: WorkflowStep) =>
        step.approverRules.length > 0
          ? step.approverRules.map((r) => (
              <Tag key={r.id}>{r.approverSourceType}: {r.approverSourceValue}</Tag>
            ))
          : <Text type="secondary">None</Text>,
    },
    ...(isDraft && canManage
      ? [
          {
            title: 'Actions',
            key: 'actions',
            render: (_: unknown, step: WorkflowStep) => (
              <Space>
                <Button size="small" onClick={() => openEditStep(step)} data-testid={`edit-step-${step.id}`}>
                  Edit
                </Button>
                <Button size="small" onClick={() => openAddRule(step.id)} data-testid={`add-rule-${step.id}`}>
                  Add Rule
                </Button>
                <Button
                  size="small"
                  danger
                  onClick={() => {
                    Modal.confirm({
                      title: 'Delete Step',
                      content: `Delete step "${step.stepName}"?`,
                      onOk: () => deleteStepMutation.mutate(step.id),
                    });
                  }}
                  data-testid={`delete-step-${step.id}`}
                >
                  Delete
                </Button>
              </Space>
            ),
          },
        ]
      : []),
  ];

  return (
    <div data-testid="workflow-version-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Version {version.versionNumber}
          <Tag color={STATUS_COLORS[version.versionStatus] ?? 'default'} style={{ marginLeft: 8 }}>
            {version.versionStatus}
          </Tag>
        </Title>
        <Space>
          {isDraft && canPublish && (
            <Button
              type="primary"
              onClick={() => setPublishModalOpen(true)}
              data-testid="publish-btn"
            >
              Publish
            </Button>
          )}
          {isPublished && canPublish && (
            <Button
              type="primary"
              onClick={() => {
                Modal.confirm({
                  title: 'Activate Version',
                  content: 'This version will become the active version for new workflow instances.',
                  onOk: () => activateMutation.mutate(),
                });
              }}
              data-testid="activate-btn"
            >
              Activate
            </Button>
          )}
          {isActive && canPublish && (
            <Button
              danger
              onClick={() => {
                Modal.confirm({
                  title: 'Retire Version',
                  content: 'No new instances will use this version. Existing active instances will continue with their frozen snapshot.',
                  onOk: () => retireMutation.mutate(),
                });
              }}
              data-testid="retire-btn"
            >
              Retire
            </Button>
          )}
          {isDraft && canManage && (
            <Button
              danger
              onClick={() => {
                Modal.confirm({
                  title: 'Delete Draft Version',
                  content: 'This will permanently delete this draft version.',
                  onOk: () => deleteVersionMutation.mutate(),
                });
              }}
              data-testid="delete-version-btn"
            >
              Delete Draft
            </Button>
          )}
          <Button>
            <Link to={`/workflow/definitions/${defId}`}>Back to Definition</Link>
          </Button>
        </Space>
      </Space>

      {(isActive || version.versionStatus === 'RETIRED') && (
        <Alert
          type="info"
          message="Active instances use a frozen snapshot of the workflow version at the time they were created. Changes to this configuration will only affect new instances."
          style={{ marginBottom: 16 }}
          data-testid="version-freeze-notice"
        />
      )}

      {actionError && (
        <Alert
          type="error"
          message={actionError}
          closable={!showConcurrencyRefresh}
          onClose={() => setActionError(null)}
          style={{ marginBottom: 16 }}
          data-testid="action-error"
          action={
            showConcurrencyRefresh ? (
              <Button size="small" type="primary" onClick={handleRefresh} data-testid="refresh-btn">
                Refresh
              </Button>
            ) : undefined
          }
        />
      )}

      <Descriptions bordered column={2} style={{ marginBottom: 24 }} data-testid="version-details">
        <Descriptions.Item label="Version">{version.versionNumber}</Descriptions.Item>
        <Descriptions.Item label="Status">
          <Tag color={STATUS_COLORS[version.versionStatus] ?? 'default'}>{version.versionStatus}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Effective From">
          {version.effectiveFrom ? new Date(version.effectiveFrom).toLocaleDateString() : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Effective To">
          {version.effectiveTo ? new Date(version.effectiveTo).toLocaleDateString() : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Published At">
          {version.publishedAt ? new Date(version.publishedAt).toLocaleDateString() : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Created">
          {new Date(version.createdAt).toLocaleDateString()}
        </Descriptions.Item>
      </Descriptions>

      <Space style={{ marginBottom: 8, width: '100%', justifyContent: 'space-between' }}>
        <Title level={5} style={{ margin: 0 }}>Steps</Title>
        {isDraft && canManage && (
          <Button onClick={openAddStep} data-testid="add-step-btn">Add Step</Button>
        )}
      </Space>

      {version.steps.length === 0 && (
        <Alert type="info" message="No steps configured." data-testid="steps-empty" />
      )}

      {version.steps.length > 0 && (
        <Table
          dataSource={version.steps}
          columns={stepColumns}
          rowKey="id"
          pagination={false}
          data-testid="steps-table"
          style={{ marginBottom: 24 }}
        />
      )}

      {version.conditions.length > 0 && (
        <>
          <Title level={5}>Conditions (Read-Only)</Title>
          <Card data-testid="conditions-display">
            <Table
              dataSource={version.conditions}
              rowKey="id"
              pagination={false}
              columns={[
                { title: 'Field', dataIndex: 'fieldCode', key: 'fieldCode' },
                { title: 'Operator', dataIndex: 'operator', key: 'operator' },
                { title: 'Value', dataIndex: 'value', key: 'value' },
              ]}
            />
          </Card>
        </>
      )}

      <Modal
        title={editingStep ? 'Edit Step' : 'Add Step'}
        open={stepModalOpen}
        onCancel={() => { setStepModalOpen(false); setEditingStep(null); stepForm.resetFields(); }}
        onOk={() => stepForm.submit()}
        confirmLoading={createStepMutation.isPending || updateStepMutation.isPending}
        data-testid="step-modal"
      >
        <Form form={stepForm} layout="vertical" onFinish={handleStepSubmit}>
          <Form.Item name="stepName" label="Step Name" rules={[{ required: true, message: 'Step name is required' }]}>
            <Input data-testid="input-stepName" />
          </Form.Item>
          <Form.Item name="stepOrder" label="Step Order" rules={[{ required: true, message: 'Step order is required' }]}>
            <InputNumber min={1} style={{ width: '100%' }} data-testid="input-stepOrder" />
          </Form.Item>
          <Form.Item name="isRequired" label="Required" valuePropName="checked">
            <Switch data-testid="input-isRequired" />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea rows={2} data-testid="input-stepDescription" />
          </Form.Item>
          <Form.Item name="dueDurationMinutes" label="Due Duration (minutes)">
            <InputNumber min={1} style={{ width: '100%' }} data-testid="input-dueDuration" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Add Approver Rule"
        open={ruleModalOpen}
        onCancel={() => { setRuleModalOpen(false); ruleForm.resetFields(); }}
        onOk={() => ruleForm.submit()}
        confirmLoading={createRuleMutation.isPending}
        data-testid="rule-modal"
      >
        <Form form={ruleForm} layout="vertical" onFinish={handleRuleSubmit}>
          <Form.Item
            name="approverSourceType"
            label="Source Type"
            rules={[{ required: true, message: 'Source type is required' }]}
          >
            <Select
              data-testid="input-approverSourceType"
              options={[
                { label: 'Specific User', value: 'SPECIFIC_USER' },
                { label: 'Role', value: 'ROLE' },
                { label: 'Admin Group', value: 'ADMIN_GROUP' },
                { label: 'Department', value: 'DEPARTMENT' },
                { label: 'Permission', value: 'PERMISSION' },
              ]}
            />
          </Form.Item>
          <Form.Item
            name="approverSourceValue"
            label="Source Value"
            rules={[{ required: true, message: 'Source value is required' }]}
          >
            <Input data-testid="input-approverSourceValue" />
          </Form.Item>
          <Form.Item
            name="priority"
            label="Priority"
            rules={[{ required: true, message: 'Priority is required' }]}
          >
            <InputNumber min={1} style={{ width: '100%' }} data-testid="input-rulePriority" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Publish Version"
        open={publishModalOpen}
        onCancel={() => { setPublishModalOpen(false); publishForm.resetFields(); }}
        onOk={() => publishForm.submit()}
        confirmLoading={publishMutation.isPending}
        data-testid="publish-modal"
      >
        <Alert
          type="warning"
          message="Publishing will transition this version from DRAFT to PUBLISHED. This action cannot be undone."
          style={{ marginBottom: 16 }}
        />
        <Form form={publishForm} layout="vertical" onFinish={() => publishMutation.mutate()}>
          <Form.Item
            name="effectiveFrom"
            label="Effective From"
            rules={[{ required: true, message: 'Effective from date is required' }]}
          >
            <DatePicker style={{ width: '100%' }} data-testid="input-effectiveFrom" />
          </Form.Item>
          <Form.Item name="effectiveTo" label="Effective To">
            <DatePicker style={{ width: '100%' }} data-testid="input-effectiveTo" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default WorkflowVersionDetailPage;
