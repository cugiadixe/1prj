import React, { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  Form,
  Input,
  Typography,
} from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { useAuth } from '../auth/AuthProvider';
import { apiChangePassword } from '../auth/authApi';

const { Title } = Typography;

interface ChangePasswordFormValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

const ChangePasswordPage: React.FC = () => {
  const { isAuthenticated, onPasswordChanged, isBootstrapping } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [form] = Form.useForm<ChangePasswordFormValues>();

  if (isBootstrapping) return null;

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  const onFinish = async (values: ChangePasswordFormValues) => {
    setErrorMessage(null);
    setLoading(true);
    try {
      await apiChangePassword({
        CurrentPassword: values.currentPassword,
        NewPassword: values.newPassword,
      });
      onPasswordChanged();
      navigate('/login', { replace: true });
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response
        ?.status;
      if (status === 400) {
        setErrorMessage(
          'Đổi mật khẩu thất bại. Mật khẩu hiện tại có thể không đúng, hoặc mật khẩu mới không đáp ứng yêu cầu bảo mật.',
        );
      } else if (status === 403) {
        setErrorMessage('Xác thực bảo mật thất bại. Vui lòng tải lại trang và thử lại.');
      } else if (status === 409) {
        setErrorMessage('Xung đột dữ liệu. Vui lòng thử lại.');
      } else {
        setErrorMessage('Đổi mật khẩu thất bại. Vui lòng thử lại sau.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      }}
    >
      <Card style={{ width: 420, boxShadow: '0 8px 32px rgba(0,0,0,0.15)', borderRadius: 12 }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <Title level={3} style={{ marginBottom: 4 }}>
            Đổi mật khẩu
          </Title>
          <Typography.Text type="secondary">
            Nhập mật khẩu hiện tại và mật khẩu mới. Sau khi đổi, bạn cần đăng nhập lại.
          </Typography.Text>
        </div>

        {errorMessage && (
          <Alert
            type="error"
            message={errorMessage}
            showIcon
            style={{ marginBottom: 16 }}
            data-testid="change-password-error"
          />
        )}

        <Form
          form={form}
          name="change-password-form"
          onFinish={onFinish}
          layout="vertical"
          autoComplete="off"
        >
          <Form.Item
            name="currentPassword"
            label="Mật khẩu hiện tại"
            rules={[
              {
                required: true,
                message: 'Vui lòng nhập mật khẩu hiện tại.',
              },
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="Mật khẩu hiện tại"
              data-testid="change-current-password"
              autoFocus
              size="large"
            />
          </Form.Item>

          <Form.Item
            name="newPassword"
            label="Mật khẩu mới"
            rules={[
              { required: true, message: 'Vui lòng nhập mật khẩu mới.' },
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="Mật khẩu mới"
              data-testid="change-new-password"
              size="large"
            />
          </Form.Item>

          <Form.Item
            name="confirmPassword"
            label="Xác nhận mật khẩu mới"
            dependencies={['newPassword']}
            rules={[
              {
                required: true,
                message: 'Vui lòng xác nhận mật khẩu mới.',
              },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('newPassword') === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(
                    new Error('Mật khẩu mới không khớp.'),
                  );
                },
              }),
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="Xác nhận mật khẩu mới"
              data-testid="change-confirm-password"
              size="large"
            />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0 }}>
            <Button
              type="primary"
              htmlType="submit"
              loading={loading}
              block
              size="large"
              data-testid="change-password-submit"
            >
              Đổi mật khẩu
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default ChangePasswordPage;
