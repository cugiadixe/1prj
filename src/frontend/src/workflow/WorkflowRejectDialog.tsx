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
      title="Reject Step"
      open={open}
      onCancel={handleCancel}
      onOk={handleOk}
      confirmLoading={loading}
      data-testid="reject-modal"
    >
      <Alert
        type="warning"
        message="This action is permanent. The request will be rejected and cannot be resubmitted."
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
          label="Reason"
          rules={[{ required: true, message: 'Reason is required' }]}
        >
          <Input.TextArea rows={2} data-testid="reject-reason" maxLength={500} />
        </Form.Item>
        <Form.Item name="comment" label="Comment (optional)">
          <Input.TextArea rows={2} data-testid="reject-comment" />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default WorkflowRejectDialog;
