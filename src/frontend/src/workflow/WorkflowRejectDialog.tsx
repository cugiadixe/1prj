import React from 'react';
import { Alert, Form, Input, Modal } from 'antd';

interface WorkflowRejectDialogProps {
  open: boolean;
  loading: boolean;
  onCancel: () => void;
  onSubmit: (values: { reason: string; comment?: string }) => void;
}

const WorkflowRejectDialog: React.FC<WorkflowRejectDialogProps> = ({
  open,
  loading,
  onCancel,
  onSubmit,
}) => {
  const [form] = Form.useForm();

  const handleOk = () => form.submit();

  const handleCancel = () => {
    form.resetFields();
    onCancel();
  };

  return (
    <Modal
      title="Từ chối bước"
      open={open}
      onCancel={handleCancel}
      onOk={handleOk}
      confirmLoading={loading}
      data-testid="reject-modal"
    >
      <Alert
        type="warning"
        message="Thao tác này là vĩnh viễn. Yêu cầu sẽ bị từ chối và không thể gửi lại."
        style={{ marginBottom: 16 }}
        data-testid="reject-warning"
      />
      <Form
        form={form}
        layout="vertical"
        onFinish={onSubmit}
      >
        <Form.Item
          name="reason"
          label="Lý do"
          rules={[{ required: true, message: 'Lý do là bắt buộc' }]}
        >
          <Input.TextArea rows={2} data-testid="reject-reason" maxLength={500} />
        </Form.Item>
        <Form.Item name="comment" label="Ghi chú (tùy chọn)">
          <Input.TextArea rows={2} data-testid="reject-comment" />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default WorkflowRejectDialog;
