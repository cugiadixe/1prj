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
import { LockOutlined, UserOutlined } from '@ant-design/icons';
import { useAuth } from '../auth/AuthProvider';

const { Title } = Typography;

interface LoginFormValues {
  username: string;
  password: string;
}

/**
 * Login page (Phase 1B.1-J).
 * - Authenticated users without mustChangePassword are redirected to /.
 * - Authenticated users with mustChangePassword are redirected to /change-password.
 * - Displays sanitized error on failure — no raw backend detail exposed.
 */
const LoginPage: React.FC = () => {
  const { login, isAuthenticated, mustChangePassword, isBootstrapping } =
    useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // While bootstrapping, render nothing to avoid flash
  if (isBootstrapping) return null;

  // Already authenticated
  if (isAuthenticated) {
    return mustChangePassword ? (
      <Navigate to="/change-password" replace />
    ) : (
      <Navigate to="/" replace />
    );
  }

  const onFinish = async (values: LoginFormValues) => {
    setErrorMessage(null);
    setLoading(true);
    try {
      await login(values.username, values.password);
      // Navigation is handled after auth state updates — navigate is used
      // only as a fallback; AuthProvider triggers re-render naturally.
      navigate(mustChangePassword ? '/change-password' : '/', { replace: true });
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response
        ?.status;
      if (status === 401 || status === 403) {
        setErrorMessage('Invalid credentials. Please try again.');
      } else if (status === 503) {
        setErrorMessage('Authentication service is temporarily unavailable.');
      } else {
        setErrorMessage('Login failed. Please try again later.');
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
      <Card style={{ width: 380, boxShadow: '0 4px 24px rgba(0,0,0,0.08)' }}>
        <div style={{ textAlign: 'center', marginBottom: 32 }}>
          <Title level={3} style={{ marginBottom: 4 }}>
            PTKD ERP
          </Title>
          <Typography.Text type="secondary">
            Sign in to your account
          </Typography.Text>
        </div>

        {errorMessage && (
          <Alert
            type="error"
            title={errorMessage}
            showIcon
            style={{ marginBottom: 16 }}
            data-testid="login-error"
          />
        )}

        <Form
          name="login-form"
          onFinish={onFinish}
          layout="vertical"
          autoComplete="off"
        >
          <Form.Item
            name="username"
            label="Username"
            rules={[{ required: true, message: 'Please enter your username.' }]}
          >
            <Input
              prefix={<UserOutlined />}
              placeholder="Username"
              data-testid="login-username"
              autoFocus
            />
          </Form.Item>

          <Form.Item
            name="password"
            label="Password"
            rules={[{ required: true, message: 'Please enter your password.' }]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="Password"
              data-testid="login-password"
            />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0 }}>
            <Button
              type="primary"
              htmlType="submit"
              loading={loading}
              block
              data-testid="login-submit"
            >
              Sign In
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default LoginPage;
