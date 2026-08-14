import React, { useState } from 'react';
import { Alert, Button, Card, Form, Input, Modal, Select, Space, Table, Tag, Typography } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createCondition,
  deleteCondition,
  getConditionFields,
  type ConditionField,
} from './workflowApi';
import { getErrorMessage } from './errorMessages';
import type { WorkflowCondition } from './types';

const { Title, Text } = Typography;

const OPERATOR_LABELS: Record<string, string> = {
  EQ: 'bằng',
  NEQ: 'khác',
  GT: 'lớn hơn',
  LT: 'nhỏ hơn',
  GTE: 'lớn hơn hoặc bằng',
  LTE: 'nhỏ hơn hoặc bằng',
  IN: 'thuộc danh sách',
  CONTAINS: 'có chứa',
};

interface Props {
  versionId: number;
  processCode?: string;
  conditions: WorkflowCondition[];
  /** Chỉ bản NHÁP mới sửa được cấu hình. */
  editable: boolean;
  onChanged: () => void;
}

/**
 * Điều kiện áp dụng của một phiên bản quy trình.
 *
 * Khi tạo hồ sơ, engine chọn liên kết đầu tiên (theo thứ hạng) mà điều kiện của phiên bản KHỚP
 * dữ liệu hồ sơ. Nhờ đó khai báo được luật kiểu "tổng tiền > 50 triệu thì dùng quy trình 2 cấp"
 * mà không cần lập trình viên.
 *
 * Ranh giới an toàn: trường chỉ chọn được trong danh mục DEV khai báo trước cho từng quy trình,
 * toán tử bị giới hạn theo kiểu dữ liệu. Không có ô nhập biểu thức hay SQL.
 */
const WorkflowConditionsCard: React.FC<Props> = ({
  versionId, processCode, conditions, editable, onChanged,
}) => {
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedField, setSelectedField] = useState<ConditionField | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);
  const [form] = Form.useForm();

  const { data: fields } = useQuery({
    queryKey: ['workflow-condition-fields', processCode],
    queryFn: () => getConditionFields(processCode!),
    enabled: !!processCode && editable,
    staleTime: 300000,
  });

  const close = () => {
    setModalOpen(false);
    setSelectedField(undefined);
    setError(null);
    form.resetFields();
  };

  const createMutation = useMutation({
    mutationFn: (req: { fieldCode: string; operator: string; value: string }) =>
      createCondition(versionId, req),
    onSuccess: () => { close(); onChanged(); queryClient.invalidateQueries({ queryKey: ['workflow-version', versionId] }); },
    onError: (e) => setError(getErrorMessage(e)),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteCondition(id),
    onSuccess: () => { onChanged(); queryClient.invalidateQueries({ queryKey: ['workflow-version', versionId] }); },
    onError: (e) => setError(getErrorMessage(e)),
  });

  const fieldLabel = (code: string) =>
    fields?.find((f) => f.fieldCode === code)?.fieldLabel ?? code;

  const columns = [
    {
      title: 'Trường',
      dataIndex: 'fieldCode',
      key: 'fieldCode',
      render: (v: string) => fieldLabel(v),
    },
    {
      title: 'Toán tử',
      dataIndex: 'operator',
      key: 'operator',
      render: (v: string) => OPERATOR_LABELS[v] ?? v,
    },
    { title: 'Giá trị', dataIndex: 'value', key: 'value' },
    ...(editable
      ? [{
          title: 'Thao tác',
          key: 'actions',
          width: 100,
          render: (_: unknown, r: WorkflowCondition) => (
            <Button
              size="small"
              danger
              onClick={() => {
                Modal.confirm({
                  title: 'Xoá điều kiện',
                  content: `Xoá điều kiện "${fieldLabel(r.fieldCode)} ${OPERATOR_LABELS[r.operator] ?? r.operator} ${r.value}"?`,
                  onOk: () => deleteMutation.mutateAsync(r.id),
                });
              }}
              data-testid={`delete-condition-${r.id}`}
            >
              Xoá
            </Button>
          ),
        }]
      : []),
  ];

  return (
    <>
      <Space style={{ marginTop: 24, marginBottom: 8, width: '100%', justifyContent: 'space-between' }}>
        <Title level={5} style={{ margin: 0 }}>Điều kiện áp dụng</Title>
        {editable && (
          <Button onClick={() => setModalOpen(true)} data-testid="add-condition-btn">
            Thêm điều kiện
          </Button>
        )}
      </Space>

      <Card data-testid="conditions-display">
        {conditions.length === 0 ? (
          <Text type="secondary">
            Không có điều kiện — phiên bản này áp dụng cho mọi hồ sơ của quy trình.
          </Text>
        ) : (
          <>
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 12 }}
              message="Tất cả điều kiện phải cùng đúng thì phiên bản này mới được áp dụng."
            />
            <Table
              dataSource={conditions}
              rowKey="id"
              pagination={false}
              size="small"
              columns={columns}
            />
          </>
        )}
      </Card>

      <Modal
        title="Thêm điều kiện áp dụng"
        open={modalOpen}
        onCancel={close}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending}
        data-testid="condition-modal"
      >
        {error && <Alert type="error" message={error} style={{ marginBottom: 12 }} />}
        <Form
          form={form}
          layout="vertical"
          onFinish={(v) => createMutation.mutate({
            fieldCode: v.fieldCode as string,
            operator: v.operator as string,
            value: String(v.value ?? '').trim(),
          })}
        >
          <Form.Item name="fieldCode" label="Trường" rules={[{ required: true, message: 'Chọn trường' }]}>
            <Select
              placeholder="Chọn trường của hồ sơ"
              data-testid="input-conditionField"
              onChange={(code: string) => {
                setSelectedField(fields?.find((f) => f.fieldCode === code));
                form.setFieldsValue({ operator: undefined });
              }}
              options={(fields ?? []).map((f) => ({ label: f.fieldLabel, value: f.fieldCode }))}
              notFoundContent="Quy trình này chưa khai báo trường điều kiện nào."
            />
          </Form.Item>

          {selectedField && (
            <Alert
              type="info"
              style={{ marginBottom: 12 }}
              message={
                <span>
                  Kiểu dữ liệu: <Tag>{selectedField.dataType}</Tag>
                  {selectedField.description}
                </span>
              }
            />
          )}

          <Form.Item name="operator" label="Toán tử" rules={[{ required: true, message: 'Chọn toán tử' }]}>
            <Select
              placeholder={selectedField ? 'Chọn toán tử' : 'Chọn trường trước'}
              disabled={!selectedField}
              data-testid="input-conditionOperator"
              options={(selectedField?.allowedOperators ?? []).map((op) => ({
                label: OPERATOR_LABELS[op] ?? op,
                value: op,
              }))}
            />
          </Form.Item>

          <Form.Item
            name="value"
            label="Giá trị so sánh"
            rules={[{ required: true, message: 'Nhập giá trị' }]}
            extra={
              form.getFieldValue('operator') === 'IN'
                ? 'Nhiều giá trị, ngăn cách bằng dấu phẩy.'
                : undefined
            }
          >
            <Input data-testid="input-conditionValue" />
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
};

export default WorkflowConditionsCard;
