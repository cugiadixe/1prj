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

/**
 * MustChangePassword page (Phase 1B.1-J).
 * Only accessible by authenticated users with mustChangePassword=true.
 * After successful change, auth state is cleared and user is redirected to /login
 * (Phase G: backend revokes all sessions; fresh login required).
 */
const ChangePasswordPage: React.FC = () => {
  const { isAuthenticated, mustChangePassword, onPasswordChanged, isBootstrapping } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [form] = Form.useForm<ChangePasswordFormValues>();

  // Wait for bootstrap before enforcing guards
  if (isBootstrapping) return null;

  // Route guard via declarative Navigate — avoids useEffect constraint
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  if (!mustChangePassword) {
    return <Navigate to="/" replace />;
  }

  const onFinish = async (values: ChangePasswordFormValues) => {
    setErrorMessage(null);
    setLoading(true);
    try {
      await apiChangePassword({
        CurrentPassword: values.currentPassword,
        NewPassword: values.newPassword,
      });
      // Phase G: backend revokes all sessions — clear auth and return to /login
      onPasswordChanged();
      navigate('/login', { replace: true });
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response
        ?.status;
      if (status === 400) {
        setErrorMessage(
          'Password change failed. The current password may be incorrect, or the new password does not meet policy requirements.',
        );
      } else if (status === 403) {
        setErrorMessage('Security validation failed. Please refresh the page and try again.');
      } else if (status === 409) {
        setErrorMessage('A concurrency conflict occurred. Please try again.');
      } else {
        setErrorMessage('Password change failed. Please try again later.');
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
        background: '#f0f2f5',
      }}
    >
      <Card style={{ width: 420, boxShadow: '0 4px 24px rgba(0,0,0,0.08)' }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <Title level={3} style={{ marginBottom: 4 }}>
            Change Your Password
          </Title>
          <Typography.Text type="secondary">
            Your account requires a password change before you can continue.
          </Typography.Text>
        </div>

        {errorMessage && (
          <Alert
            type="error"
            title={errorMessage}
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
            label="Current Password"
            rules={[
              {
                required: true,
                message: 'Please enter your current password.',
              },
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="Current password"
              data-testid="change-current-password"
              autoFocus
            />
          </Form.Item>

          <Form.Item
            name="newPassword"
            label="New Password"
            rules={[
              { required: true, message: 'Please enter your new password.' },
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="New password"
              data-testid="change-new-password"
            />
          </Form.Item>

          <Form.Item
            name="confirmPassword"
            label="Confirm New Password"
            dependencies={['newPassword']}
            rules={[
              {
                required: true,
                message: 'Please confirm your new password.',
              },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('newPassword') === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(
                    new Error('New passwords do not match.'),
                  );
                },
              }),
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="Confirm new password"
              data-testid="change-confirm-password"
            />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0 }}>
            <Button
              type="primary"
              htmlType="submit"
              loading={loading}
              block
              data-testid="change-password-submit"
            >
              Change Password
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default ChangePasswordPage;
