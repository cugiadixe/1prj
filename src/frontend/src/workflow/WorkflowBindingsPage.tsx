import React, { useState } from 'react';
import {
  Alert,
  Button,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
} from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { usePermissions } from '../auth/AuthProvider';
import {
  createBinding,
  deactivateBinding,
  getBindableVersions,
  getBindings,
  getBusinessProcesses,
  updateBinding,
} from './workflowApi';
import { listCompanies } from '../approvalAuthority/api';
import { getErrorMessage, isConcurrencyError, isPermissionDenied } from './errorMessages';
import type {
  CreateWorkflowBindingRequest,
  UpdateWorkflowBindingRequest,
  WorkflowBindingListItem,
} from './types';

const { Title } = Typography;

const WorkflowBindingsPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const queryClient = useQueryClient();
  const [processCodeFilter, setProcessCodeFilter] = useState<string | undefined>(undefined);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingBinding, setEditingBinding] = useState<WorkflowBindingListItem | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [showConcurrencyRefresh, setShowConcurrencyRefresh] = useState(false);
  const [form] = Form.useForm();

  const { data: bindings, isLoading, error, refetch } = useQuery({
    queryKey: ['workflow-bindings', processCodeFilter],
    queryFn: () => getBindings(processCodeFilter),
  });

  const { data: processes } = useQuery({
    queryKey: ['workflow-processes'],
    queryFn: getBusinessProcesses,
  });

  const scopeType = Form.useWatch('scopeType', form);
  const formProcessCode = Form.useWatch('processCode', form);

  // Danh sách phiên bản gán được cho mã quy trình đang chọn (thay cho việc gõ số ID).
  const { data: bindableVersions, isLoading: versionsLoading } = useQuery({
    queryKey: ['workflow-bindable-versions', formProcessCode],
    queryFn: () => getBindableVersions(formProcessCode as string),
    enabled: !!formProcessCode && !editingBinding,
  });

  const { data: companies } = useQuery({
    queryKey: ['org-companies'],
    queryFn: listCompanies,
    staleTime: 300000,
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

  const createMutation = useMutation({
    mutationFn: (req: CreateWorkflowBindingRequest) => createBinding(req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workflow-bindings'] });
      setModalOpen(false);
      form.resetFields();
    },
    onError: handleError,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, req }: { id: number; req: UpdateWorkflowBindingRequest }) =>
      updateBinding(id, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workflow-bindings'] });
      setModalOpen(false);
      setEditingBinding(null);
      form.resetFields();
    },
    onError: handleError,
  });

  const deactivateMutation = useMutation({
    mutationFn: ({ id, rowVersion }: { id: number; rowVersion: string }) =>
      deactivateBinding(id, rowVersion),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['workflow-bindings'] }),
    onError: handleError,
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem liên kết quy trình."
        data-testid="permission-denied"
      />
    );
  }

  const openCreate = () => {
    setEditingBinding(null);
    form.resetFields();
    form.setFieldsValue({ scopeType: 'GLOBAL', priority: 1 });
    setModalOpen(true);
  };

  const openEdit = (binding: WorkflowBindingListItem) => {
    setEditingBinding(binding);
    // Trước đây chỉ nạp priority, trong khi form vẫn bắt buộc "Hiệu lực từ" → mở Sửa là ô ngày
    // trống, người dùng phải gõ lại ngày hiệu lực mỗi lần chỉ muốn đổi mức ưu tiên.
    form.setFieldsValue({
      priority: binding.priority,
      effectiveFrom: binding.effectiveFrom ? dayjs(binding.effectiveFrom) : undefined,
      effectiveTo: binding.effectiveTo ? dayjs(binding.effectiveTo) : undefined,
    });
    setModalOpen(true);
  };

  const handleSubmit = (values: Record<string, unknown>) => {
    if (editingBinding) {
      const req: UpdateWorkflowBindingRequest = {
        effectiveFrom: (values.effectiveFrom as { toISOString: () => string }).toISOString(),
        effectiveTo: values.effectiveTo
          ? (values.effectiveTo as { toISOString: () => string }).toISOString()
          : null,
        priority: values.priority as number,
        targetVersion: editingBinding.rowVersion,
      };
      updateMutation.mutate({ id: editingBinding.id, req });
    } else {
      const req: CreateWorkflowBindingRequest = {
        workflowVersionId: values.workflowVersionId as number,
        processCode: values.processCode as string,
        scopeType: values.scopeType as string,
        companyId: values.scopeType === 'COMPANY' ? (values.companyId as number) : null,
        priority: values.priority as number,
        effectiveFrom: (values.effectiveFrom as { toISOString: () => string }).toISOString(),
        effectiveTo: values.effectiveTo
          ? (values.effectiveTo as { toISOString: () => string }).toISOString()
          : null,
      };
      createMutation.mutate(req);
    }
  };

  const columns = [
    { title: 'Quy trình', dataIndex: 'processCode', key: 'processCode' },
    {
      title: 'ID Phiên bản',
      dataIndex: 'workflowVersionId',
      key: 'workflowVersionId',
    },
    {
      title: 'Phạm vi',
      dataIndex: 'scopeType',
      key: 'scopeType',
      render: (val: string, record: WorkflowBindingListItem) =>
        val === 'COMPANY' ? `COMPANY (${record.companyId})` : val,
    },
    { title: 'Ưu tiên', dataIndex: 'priority', key: 'priority' },
    {
      title: 'Hiệu lực từ',
      dataIndex: 'effectiveFrom',
      key: 'effectiveFrom',
      render: (val: string) => new Date(val).toLocaleDateString('vi-VN'),
    },
    {
      title: 'Hoạt động',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (val: boolean) => (
        <Tag color={val ? 'green' : 'red'}>{val ? 'Hoạt động' : 'Ngừng hoạt động'}</Tag>
      ),
    },
    ...(hasPermission('WORKFLOW_BIND_PROCESS', 'GLOBAL')
      ? [
          {
            title: 'Thao tác',
            key: 'actions',
            render: (_: unknown, record: WorkflowBindingListItem) => (
              <Space>
                <Button size="small" onClick={() => openEdit(record)} data-testid={`edit-binding-${record.id}`}>
                  Sửa
                </Button>
                {/* Trước đây không có cách nào ngừng một liên kết qua giao diện — cột "Hoạt động"
                    chỉ để xem. Liên kết cũ còn sống là nguyên nhân gây trùng hạng (WF_BINDING_AMBIGUOUS). */}
                {record.isActive && (
                  <Button
                    size="small"
                    danger
                    onClick={() => {
                      Modal.confirm({
                        title: 'Ngừng liên kết',
                        content: `Ngừng liên kết quy trình "${record.processCode}"? Hồ sơ MỚI sẽ không dùng liên kết này nữa; hồ sơ đang chạy dở không bị ảnh hưởng.`,
                        okText: 'Ngừng',
                        cancelText: 'Huỷ',
                        onOk: () => deactivateMutation.mutateAsync({ id: record.id, rowVersion: record.rowVersion }),
                      });
                    }}
                    data-testid={`deactivate-binding-${record.id}`}
                  >
                    Ngừng
                  </Button>
                )}
              </Space>
            ),
          },
        ]
      : []),
  ];

  return (
    <div data-testid="workflow-bindings-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Liên kết quy trình</Title>
        {hasPermission('WORKFLOW_BIND_PROCESS', 'GLOBAL') && (
          <Button type="primary" onClick={openCreate} data-testid="create-binding-btn">
            Tạo liên kết
          </Button>
        )}
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="Lọc theo mã quy trình..."
          allowClear
          onSearch={(val) => setProcessCodeFilter(val || undefined)}
          style={{ width: 300 }}
          data-testid="binding-process-filter"
        />
      </Space>

      {actionError && (
        <Alert
          type="error"
          message={actionError}
          closable={!showConcurrencyRefresh}
          onClose={() => setActionError(null)}
          style={{ marginBottom: 16 }}
          data-testid="binding-error"
          action={
            showConcurrencyRefresh ? (
              <Button size="small" type="primary" onClick={handleRefresh} data-testid="refresh-btn">
                Tải lại
              </Button>
            ) : undefined
          }
        />
      )}

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="binding-list-error"
        />
      )}

      {isLoading && <Spin data-testid="binding-list-loading" />}

      {!isLoading && !error && bindings && bindings.length === 0 && (
        <Alert type="info" message="Không tìm thấy liên kết quy trình nào." data-testid="binding-list-empty" />
      )}

      {bindings && bindings.length > 0 && (
        <Table
          dataSource={bindings}
          columns={columns}
          rowKey="id"
          pagination={false}
          data-testid="binding-list-table"
        />
      )}

      <Modal
        title={editingBinding ? 'Sửa liên kết' : 'Tạo liên kết'}
        open={modalOpen}
        onCancel={() => { setModalOpen(false); setEditingBinding(null); form.resetFields(); }}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
        data-testid="binding-modal"
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          {!editingBinding && (
            <>
              <Form.Item
                name="processCode"
                label="Quy trình nghiệp vụ"
                rules={[{ required: true, message: 'Quy trình là bắt buộc' }]}
              >
                <Select
                  data-testid="input-bindingProcessCode"
                  onChange={() => form.setFieldsValue({ workflowVersionId: undefined })}
                  options={(processes ?? []).map((p) => ({
                    label: `${p.processCode} — ${p.processName}`,
                    value: p.processCode,
                  }))}
                />
              </Form.Item>
              {/* Chọn phiên bản từ danh sách thay vì gõ số ID: gõ nhầm sang phiên bản chưa
                  kích hoạt sẽ chặn toàn bộ quy trình mà không có cảnh báo lúc cấu hình. */}
              <Form.Item
                name="workflowVersionId"
                label="Phiên bản quy trình"
                rules={[{ required: true, message: 'Phiên bản là bắt buộc' }]}
                extra={
                  formProcessCode && !versionsLoading && (bindableVersions ?? []).length === 0
                    ? 'Quy trình này chưa có phiên bản nào được KÍCH HOẠT. Hãy xuất bản và kích hoạt một phiên bản trước.'
                    : undefined
                }
              >
                <Select
                  data-testid="input-versionId"
                  disabled={!formProcessCode}
                  loading={versionsLoading}
                  placeholder={formProcessCode ? 'Chọn phiên bản' : 'Chọn quy trình trước'}
                  options={(bindableVersions ?? []).map((v) => ({
                    label: v.hint ? `${v.label} (${v.hint})` : v.label,
                    value: Number(v.value),
                  }))}
                />
              </Form.Item>
              <Form.Item
                name="scopeType"
                label="Phạm vi"
                rules={[{ required: true, message: 'Phạm vi là bắt buộc' }]}
              >
                <Select
                  data-testid="input-scopeType"
                  options={[
                    { label: 'Toàn cục', value: 'GLOBAL' },
                    { label: 'Công ty', value: 'COMPANY' },
                  ]}
                />
              </Form.Item>
              {scopeType === 'COMPANY' && (
                <Form.Item
                  name="companyId"
                  label="Công ty"
                  rules={[{ required: true, message: 'Công ty là bắt buộc đối với phạm vi Công ty' }]}
                >
                  <Select
                    showSearch
                    optionFilterProp="label"
                    data-testid="input-companyId"
                    placeholder="Chọn công ty"
                    options={(companies ?? []).map((c) => ({ label: c.name, value: c.id }))}
                  />
                </Form.Item>
              )}
            </>
          )}
          <Form.Item
            name="effectiveFrom"
            label="Hiệu lực từ"
            rules={[{ required: true, message: 'Ngày hiệu lực từ là bắt buộc' }]}
          >
            <DatePicker style={{ width: '100%' }} data-testid="input-bindingEffectiveFrom" />
          </Form.Item>
          <Form.Item name="effectiveTo" label="Hiệu lực đến">
            <DatePicker style={{ width: '100%' }} data-testid="input-bindingEffectiveTo" />
          </Form.Item>
          <Form.Item
            name="priority"
            label="Ưu tiên"
            rules={[{ required: true, message: 'Ưu tiên là bắt buộc' }]}
          >
            <InputNumber min={1} style={{ width: '100%' }} data-testid="input-bindingPriority" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default WorkflowBindingsPage;
