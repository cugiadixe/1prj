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
        message="Bạn không có quyền xem phiên bản quy trình này."
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
    { title: 'Thứ tự', dataIndex: 'stepOrder', key: 'stepOrder', width: 80 },
    { title: 'Tên', dataIndex: 'stepName', key: 'stepName' },
    {
      title: 'Bắt buộc',
      dataIndex: 'isRequired',
      key: 'isRequired',
      render: (v: boolean) => v ? 'Có' : 'Không',
    },
    {
      title: 'Hạn (phút)',
      dataIndex: 'dueDurationMinutes',
      key: 'dueDurationMinutes',
      render: (v: number | null) => v ?? '—',
    },
    {
      title: 'Quy tắc phê duyệt',
      key: 'rules',
      render: (_: unknown, step: WorkflowStep) =>
        step.approverRules.length > 0
          ? step.approverRules.map((r) => (
              <Tag key={r.id}>{r.approverSourceType}: {r.approverSourceValue}</Tag>
            ))
          : <Text type="secondary">Không có</Text>,
    },
    ...(isDraft && canManage
      ? [
          {
            title: 'Thao tác',
            key: 'actions',
            render: (_: unknown, step: WorkflowStep) => (
              <Space>
                <Button size="small" onClick={() => openEditStep(step)} data-testid={`edit-step-${step.id}`}>
                  Sửa
                </Button>
                <Button size="small" onClick={() => openAddRule(step.id)} data-testid={`add-rule-${step.id}`}>
                  Thêm quy tắc
                </Button>
                <Button
                  size="small"
                  danger
                  onClick={() => {
                    Modal.confirm({
                      title: 'Xóa bước',
                      content: `Xóa bước "${step.stepName}"?`,
                      onOk: () => deleteStepMutation.mutate(step.id),
                    });
                  }}
                  data-testid={`delete-step-${step.id}`}
                >
                  Xóa
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
          Phiên bản {version.versionNumber}
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
              Xuất bản
            </Button>
          )}
          {isPublished && canPublish && (
            <Button
              type="primary"
              onClick={() => {
                Modal.confirm({
                  title: 'Kích hoạt phiên bản',
                  content: 'Phiên bản này sẽ trở thành phiên bản hoạt động cho các phiên xử lý quy trình mới.',
                  onOk: () => activateMutation.mutate(),
                });
              }}
              data-testid="activate-btn"
            >
              Kích hoạt
            </Button>
          )}
          {isActive && canPublish && (
            <Button
              danger
              onClick={() => {
                Modal.confirm({
                  title: 'Ngừng sử dụng phiên bản',
                  content: 'Sẽ không có phiên xử lý mới nào sử dụng phiên bản này. Các phiên xử lý đang hoạt động sẽ tiếp tục với bản chụp cố định của chúng.',
                  onOk: () => retireMutation.mutate(),
                });
              }}
              data-testid="retire-btn"
            >
              Ngừng sử dụng
            </Button>
          )}
          {isDraft && canManage && (
            <Button
              danger
              onClick={() => {
                Modal.confirm({
                  title: 'Xóa phiên bản nháp',
                  content: 'Thao tác này sẽ xóa vĩnh viễn phiên bản nháp này.',
                  onOk: () => deleteVersionMutation.mutate(),
                });
              }}
              data-testid="delete-version-btn"
            >
              Xóa bản nháp
            </Button>
          )}
          <Button>
            <Link to={`/workflow/definitions/${defId}`}>Quay lại định nghĩa</Link>
          </Button>
        </Space>
      </Space>

      {(isActive || version.versionStatus === 'RETIRED') && (
        <Alert
          type="info"
          message="Các phiên xử lý đang hoạt động sử dụng bản chụp cố định của phiên bản quy trình tại thời điểm chúng được tạo. Các thay đổi đối với cấu hình này sẽ chỉ ảnh hưởng đến các phiên xử lý mới."
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
                Tải lại
              </Button>
            ) : undefined
          }
        />
      )}

      <Descriptions bordered column={2} style={{ marginBottom: 24 }} data-testid="version-details">
        <Descriptions.Item label="Phiên bản">{version.versionNumber}</Descriptions.Item>
        <Descriptions.Item label="Trạng thái">
          <Tag color={STATUS_COLORS[version.versionStatus] ?? 'default'}>{version.versionStatus}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Hiệu lực từ">
          {version.effectiveFrom ? new Date(version.effectiveFrom).toLocaleDateString('vi-VN') : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Hiệu lực đến">
          {version.effectiveTo ? new Date(version.effectiveTo).toLocaleDateString('vi-VN') : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Ngày xuất bản">
          {version.publishedAt ? new Date(version.publishedAt).toLocaleDateString('vi-VN') : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Đã tạo">
          {new Date(version.createdAt).toLocaleDateString('vi-VN')}
        </Descriptions.Item>
      </Descriptions>

      <Space style={{ marginBottom: 8, width: '100%', justifyContent: 'space-between' }}>
        <Title level={5} style={{ margin: 0 }}>Các bước</Title>
        {isDraft && canManage && (
          <Button onClick={openAddStep} data-testid="add-step-btn">Thêm bước</Button>
        )}
      </Space>

      {version.steps.length === 0 && (
        <Alert type="info" message="Chưa có bước nào được cấu hình." data-testid="steps-empty" />
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
          <Title level={5}>Điều kiện (Chỉ đọc)</Title>
          <Card data-testid="conditions-display">
            <Table
              dataSource={version.conditions}
              rowKey="id"
              pagination={false}
              columns={[
                { title: 'Trường', dataIndex: 'fieldCode', key: 'fieldCode' },
                { title: 'Toán tử', dataIndex: 'operator', key: 'operator' },
                { title: 'Giá trị', dataIndex: 'value', key: 'value' },
              ]}
            />
          </Card>
        </>
      )}

      <Modal
        title={editingStep ? 'Sửa bước' : 'Thêm bước'}
        open={stepModalOpen}
        onCancel={() => { setStepModalOpen(false); setEditingStep(null); stepForm.resetFields(); }}
        onOk={() => stepForm.submit()}
        confirmLoading={createStepMutation.isPending || updateStepMutation.isPending}
        data-testid="step-modal"
      >
        <Form form={stepForm} layout="vertical" onFinish={handleStepSubmit}>
          <Form.Item name="stepName" label="Tên bước" rules={[{ required: true, message: 'Tên bước là bắt buộc' }]}>
            <Input data-testid="input-stepName" />
          </Form.Item>
          <Form.Item name="stepOrder" label="Thứ tự bước" rules={[{ required: true, message: 'Thứ tự bước là bắt buộc' }]}>
            <InputNumber min={1} style={{ width: '100%' }} data-testid="input-stepOrder" />
          </Form.Item>
          <Form.Item name="isRequired" label="Bắt buộc" valuePropName="checked">
            <Switch data-testid="input-isRequired" />
          </Form.Item>
          <Form.Item name="description" label="Mô tả">
            <Input.TextArea rows={2} data-testid="input-stepDescription" />
          </Form.Item>
          <Form.Item name="dueDurationMinutes" label="Thời hạn (phút)">
            <InputNumber min={1} style={{ width: '100%' }} data-testid="input-dueDuration" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Thêm quy tắc phê duyệt"
        open={ruleModalOpen}
        onCancel={() => { setRuleModalOpen(false); ruleForm.resetFields(); }}
        onOk={() => ruleForm.submit()}
        confirmLoading={createRuleMutation.isPending}
        data-testid="rule-modal"
      >
        <Form form={ruleForm} layout="vertical" onFinish={handleRuleSubmit}>
          <Form.Item
            name="approverSourceType"
            label="Loại nguồn"
            rules={[{ required: true, message: 'Loại nguồn là bắt buộc' }]}
          >
            <Select
              data-testid="input-approverSourceType"
              options={[
                { label: 'Người dùng cụ thể', value: 'SPECIFIC_USER' },
                { label: 'Vai trò', value: 'ROLE' },
                { label: 'Nhóm quản trị', value: 'ADMIN_GROUP' },
                { label: 'Phòng ban', value: 'DEPARTMENT' },
                { label: 'Quyền', value: 'PERMISSION' },
              ]}
            />
          </Form.Item>
          <Form.Item
            name="approverSourceValue"
            label="Giá trị nguồn"
            rules={[{ required: true, message: 'Giá trị nguồn là bắt buộc' }]}
          >
            <Input data-testid="input-approverSourceValue" />
          </Form.Item>
          <Form.Item
            name="priority"
            label="Ưu tiên"
            rules={[{ required: true, message: 'Ưu tiên là bắt buộc' }]}
          >
            <InputNumber min={1} style={{ width: '100%' }} data-testid="input-rulePriority" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Xuất bản phiên bản"
        open={publishModalOpen}
        onCancel={() => { setPublishModalOpen(false); publishForm.resetFields(); }}
        onOk={() => publishForm.submit()}
        confirmLoading={publishMutation.isPending}
        data-testid="publish-modal"
      >
        <Alert
          type="warning"
          message="Xuất bản sẽ chuyển phiên bản này từ NHÁP sang ĐÃ XUẤT BẢN. Thao tác này không thể hoàn tác."
          style={{ marginBottom: 16 }}
        />
        <Form form={publishForm} layout="vertical" onFinish={() => publishMutation.mutate()}>
          <Form.Item
            name="effectiveFrom"
            label="Hiệu lực từ"
            rules={[{ required: true, message: 'Ngày hiệu lực từ là bắt buộc' }]}
          >
            <DatePicker style={{ width: '100%' }} data-testid="input-effectiveFrom" />
          </Form.Item>
          <Form.Item name="effectiveTo" label="Hiệu lực đến">
            <DatePicker style={{ width: '100%' }} data-testid="input-effectiveTo" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default WorkflowVersionDetailPage;
