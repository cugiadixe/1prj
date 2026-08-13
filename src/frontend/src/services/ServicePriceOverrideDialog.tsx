import React, { useState } from 'react';
import { Alert, Form, Input, InputNumber, Modal, message } from 'antd';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { requestPriceOverride } from './servicesApi';
import { getErrorMessage, isConcurrencyError } from './errorMessages';
import type { RequestPriceOverrideRequest, ServiceDetail } from './types';

interface ServicePriceOverrideDialogProps {
  visible: boolean;
  onClose: () => void;
  service: ServiceDetail;
}

const ServicePriceOverrideDialog: React.FC<ServicePriceOverrideDialogProps> = ({ visible, onClose, service }) => {
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [formError, setFormError] = useState<string | null>(null);

  const overrideMutation = useMutation({
    mutationFn: (values: RequestPriceOverrideRequest) => requestPriceOverride(service.id, values),
    onSuccess: () => {
      message.success('Yêu cầu ghi đè giá thành công');
      queryClient.invalidateQueries({ queryKey: ['service', service.id] });
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
      overrideMutation.mutate({
        requestedPrice: values.requestedPrice,
        reason: values.reason,
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
      title="Yêu cầu ghi đè giá"
      open={visible}
      onOk={handleOk}
      onCancel={handleCancel}
      confirmLoading={overrideMutation.isPending}
      data-testid="price-override-dialog"
    >
      {formError && (
        <Alert
          type="error"
          message={formError}
          style={{ marginBottom: 16 }}
          data-testid="price-override-error"
        />
      )}
      <Form form={form} layout="vertical">
        <Form.Item
          name="requestedPrice"
          label="Giá yêu cầu"
          rules={[{ required: true, message: 'Vui lòng nhập giá yêu cầu' }]}
        >
          <InputNumber
            style={{ width: '100%' }}
            min={0}
            step={1000}
            data-testid="input-requested-price"
          />
        </Form.Item>
        <Form.Item
          name="reason"
          label="Lý do"
          rules={[{ required: true, message: 'Vui lòng nhập lý do' }]}
        >
          <Input.TextArea rows={4} data-testid="input-reason" />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default ServicePriceOverrideDialog;
