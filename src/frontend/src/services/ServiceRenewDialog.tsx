import React, { useState } from 'react';
import { Alert, DatePicker, Form, Modal, message } from 'antd';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { renewService } from './servicesApi';
import { getErrorMessage, isConcurrencyError } from './errorMessages';
import type { RenewServiceRequest, ServiceDetail } from './types';

interface ServiceRenewDialogProps {
  visible: boolean;
  onClose: () => void;
  service: ServiceDetail;
}

const ServiceRenewDialog: React.FC<ServiceRenewDialogProps> = ({ visible, onClose, service }) => {
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [formError, setFormError] = useState<string | null>(null);

  const renewMutation = useMutation({
    mutationFn: (values: RenewServiceRequest) => renewService(service.id, values),
    onSuccess: (data) => {
      message.success('Gia hạn dịch vụ thành công');
      queryClient.setQueryData(['service', service.id], data);
      queryClient.invalidateQueries({ queryKey: ['services'] });
      form.resetFields();
      onClose();
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        setFormError('Bản ghi đã bị thay đổi bởi người dùng khác. Vui lòng tải lại và thử lại.');
      } else {
        setFormError(getErrorMessage(err));
      }
    },
  });

  const handleOk = () => {
    form.validateFields().then((values) => {
      setFormError(null);
      renewMutation.mutate({
        validFrom: values.validFrom.format('YYYY-MM-DD'),
        validTo: values.validTo ? values.validTo.format('YYYY-MM-DD') : undefined,
        rowVersion: service.rowVersion,
      });
    }).catch(() => {});
  };

  const handleCancel = () => {
    form.resetFields();
    setFormError(null);
    onClose();
  };

  return (
    <Modal
      title="Gia hạn dịch vụ"
      open={visible}
      onOk={handleOk}
      onCancel={handleCancel}
      confirmLoading={renewMutation.isPending}
      data-testid="renew-service-dialog"
    >
      {formError && (
        <Alert
          type="error"
          message={formError}
          style={{ marginBottom: 16 }}
          data-testid="renew-service-error"
        />
      )}
      <Form form={form} layout="vertical">
        <Form.Item
          name="validFrom"
          label="Hiệu lực từ"
          rules={[{ required: true, message: 'Vui lòng chọn ngày bắt đầu hiệu lực' }]}
        >
          <DatePicker style={{ width: '100%' }} data-testid="input-valid-from" />
        </Form.Item>
        <Form.Item name="validTo" label="Hiệu lực đến (Tùy chọn)">
          <DatePicker style={{ width: '100%' }} data-testid="input-valid-to" />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default ServiceRenewDialog;
