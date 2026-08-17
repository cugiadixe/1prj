import React, { useState } from 'react';
import {
  Alert,
  Button,
  Descriptions,
  Form,
  Input,
  Modal,
  Space,
  Spin,
  Tag,
  Typography,
} from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { usePermissions, useAuth } from '../auth/AuthProvider';
import {
  getAccountDetail,
  activateAccount,
  disableAccount,
  lockAccount,
  unlockAccount,
  resetPassword,
  revokeSessions,
} from '../accountManagement/accountManagementApi';
import {
  getErrorMessage,
  isPermissionDenied,
  isNotFound,
  PERMISSION_DENIED,
  ACCOUNT_NOT_FOUND,
} from '../accountManagement/errorMessages';
import type { AccountStatus } from '../accountManagement/types';

const { Title, Text } = Typography;
const { TextArea } = Input;

const STATUS_COLORS: Record<string, string> = {
  ACTIVE: 'green',
  LOCKED: 'orange',
  DISABLED: 'red',
};

const MAX_REASON_LENGTH = 500;

interface ConfirmActionModalProps {
  open: boolean;
  title: string;
  confirmationText: string;
  requireReason: boolean;
  isLoading: boolean;
  errorMessage: string | null;
  onConfirm: (reason: string) => void;
  onCancel: () => void;
}

const ConfirmActionModal: React.FC<ConfirmActionModalProps> = ({
  open,
  title,
  confirmationText,
  requireReason,
  isLoading,
  errorMessage,
  onConfirm,
  onCancel,
}) => {
  const [reason, setReason] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleOk = () => {
    if (requireReason) {
      if (!reason.trim()) {
        setValidationError('Vui lòng nhập lý do.');
        return;
      }
      if (reason.length > MAX_REASON_LENGTH) {
        setValidationError(`Lý do không được vượt quá ${MAX_REASON_LENGTH} ký tự.`);
        return;
      }
    }
    setValidationError(null);
    onConfirm(reason.trim());
  };

  const handleCancel = () => {
    setReason('');
    setValidationError(null);
    onCancel();
  };

  return (
    <Modal
      open={open}
      title={title}
      onOk={handleOk}
      onCancel={handleCancel}
      confirmLoading={isLoading}
      okText="Xác nhận"
      cancelText="Hủy"
      data-testid="confirm-action-modal"
      destroyOnHidden
    >
      <p>{confirmationText}</p>
      {requireReason && (
        <Form layout="vertical">
          <Form.Item
            label="Lý do"
            validateStatus={validationError ? 'error' : undefined}
            help={validationError}
          >
            <TextArea
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              maxLength={MAX_REASON_LENGTH}
              placeholder="Nhập lý do (bắt buộc)"
              data-testid="reason-input"
              aria-label="Lý do"
            />
          </Form.Item>
        </Form>
      )}
      {errorMessage && (
        <Alert
          type="error"
          message={errorMessage}
          data-testid="action-error-message"
          style={{ marginTop: 8 }}
        />
      )}
    </Modal>
  );
};

interface TempPasswordModalProps {
  open: boolean;
  temporaryPassword: string;
  onClose: () => void;
}

const TempPasswordModal: React.FC<TempPasswordModalProps> = ({
  open,
  temporaryPassword,
  onClose,
}) => {
  const handleCopy = () => {
    void navigator.clipboard.writeText(temporaryPassword);
  };

  return (
    <Modal
      open={open}
      title="Mật khẩu tạm thời"
      footer={
        <Space>
          <Button
            onClick={handleCopy}
            data-testid="copy-temp-password-button"
          >
            Sao chép
          </Button>
          <Button
            type="primary"
            onClick={onClose}
            data-testid="dismiss-temp-password-button"
          >
            Đóng
          </Button>
        </Space>
      }
      onCancel={onClose}
      closable={false}
      data-testid="temp-password-modal"
      destroyOnHidden
    >
      <Alert
        type="warning"
        message="Ghi lại mật khẩu này ngay. Mật khẩu sẽ không hiển thị lại."
        style={{ marginBottom: 12 }}
      />
      <Text
        strong
        copyable={false}
        data-testid="temp-password-display"
        style={{ fontSize: 16, letterSpacing: 2 }}
      >
        {temporaryPassword}
      </Text>
    </Modal>
  );
};

type ActionType =
  | 'activate'
  | 'disable'
  | 'lock'
  | 'unlock'
  | 'reset-password'
  | 'revoke-sessions'
  | null;

interface ActionConfig {
  title: string;
  confirmationText: string;
  requireReason: boolean;
}

const ACTION_CONFIG: Record<NonNullable<ActionType>, ActionConfig> = {
  activate: {
    title: 'Kích hoạt tài khoản',
    confirmationText: 'Bạn có chắc muốn kích hoạt tài khoản này?',
    requireReason: false,
  },
  disable: {
    title: 'Vô hiệu hóa tài khoản',
    confirmationText:
      'Thao tác này sẽ ngăn người dùng đăng nhập. Vui lòng nhập lý do.',
    requireReason: true,
  },
  lock: {
    title: 'Khóa tài khoản',
    confirmationText: 'Thao tác này sẽ khóa tài khoản. Vui lòng nhập lý do.',
    requireReason: true,
  },
  unlock: {
    title: 'Mở khóa tài khoản',
    confirmationText: 'Bạn có chắc muốn mở khóa tài khoản này?',
    requireReason: false,
  },
  'reset-password': {
    title: 'Đặt lại mật khẩu',
    confirmationText:
      'Thao tác này sẽ tạo mật khẩu tạm thời mới và thu hồi tất cả phiên đăng nhập. Vui lòng nhập lý do.',
    requireReason: true,
  },
  'revoke-sessions': {
    title: 'Thu hồi phiên đăng nhập',
    confirmationText:
      'Thao tác này sẽ thu hồi tất cả phiên đăng nhập của người dùng. Vui lòng nhập lý do.',
    requireReason: true,
  },
};

const AccountDetailPage: React.FC = () => {
  const { accountId } = useParams<{ accountId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = usePermissions();
  const { user: currentUser } = useAuth();

  const [activeAction, setActiveAction] = useState<ActionType>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [temporaryPassword, setTemporaryPassword] = useState<string | null>(null);

  const accountIdNum = accountId ? parseInt(accountId, 10) : NaN;

  const {
    data: account,
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ['account-detail', accountIdNum],
    queryFn: () => getAccountDetail(accountIdNum),
    enabled: !isNaN(accountIdNum),
    retry: false,
  });

  const mutation = useMutation({
    mutationFn: async (reason: string) => {
      if (!activeAction || isNaN(accountIdNum)) return;

      switch (activeAction) {
        case 'activate':
          await activateAccount(accountIdNum);
          break;
        case 'disable':
          await disableAccount(accountIdNum, reason);
          break;
        case 'lock':
          await lockAccount(accountIdNum, reason);
          break;
        case 'unlock':
          await unlockAccount(accountIdNum);
          break;
        case 'reset-password': {
          const result = await resetPassword(accountIdNum, reason);
          setTemporaryPassword(result.temporaryPassword);
          break;
        }
        case 'revoke-sessions':
          await revokeSessions(accountIdNum, reason);
          break;
      }
    },
    onSuccess: () => {
      setActiveAction(null);
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ['account-detail', accountIdNum] });
    },
    onError: (err: unknown) => {
      setActionError(getErrorMessage(err));
    },
  });

  const handleActionClick = (action: ActionType) => {
    setActionError(null);
    setActiveAction(action);
  };

  const handleConfirm = (reason: string) => {
    mutation.mutate(reason);
  };

  const handleModalCancel = () => {
    setActiveAction(null);
    setActionError(null);
    mutation.reset();
  };

  const handleDismissTempPassword = () => {
    setTemporaryPassword(null);
  };

  if (isNaN(accountIdNum)) {
    return <Alert type="error" message="ID tài khoản không hợp lệ." data-testid="invalid-account-id" />;
  }

  if (isLoading) {
    return (
      <div style={{ textAlign: 'center', padding: 48 }} data-testid="account-detail-loading">
        <Spin size="large" />
      </div>
    );
  }

  if (isError) {
    if (isPermissionDenied(error)) {
      return (
        <Alert
          type="warning"
          message={PERMISSION_DENIED}
          data-testid="account-detail-permission-denied"
        />
      );
    }
    if (isNotFound(error)) {
      return (
        <Alert
          type="error"
          message={ACCOUNT_NOT_FOUND}
          data-testid="account-detail-not-found"
        />
      );
    }
    return (
      <Alert
        type="error"
        message={getErrorMessage(error)}
        data-testid="account-detail-error"
      />
    );
  }

  if (!account) {
    return <Alert type="error" message={ACCOUNT_NOT_FOUND} data-testid="account-detail-not-found" />;
  }

  const status: AccountStatus = account.status;

  // Chốt tự khoá (rõ ràng trên giao diện): không cho tự vô hiệu/khoá tài khoản của chính mình.
  // Backend cũng chặn (AUTH_CANNOT_MODIFY_SELF) — đây là lớp cho gọn UX, không phải lớp bảo vệ chính.
  const isSelf = currentUser?.userId === account.userId;

  const canActivate = status === 'DISABLED';
  const canDisable = (status === 'ACTIVE' || status === 'LOCKED') && !isSelf;
  const canLock = status === 'ACTIVE' && !isSelf;
  const canUnlock = status === 'LOCKED';
  const canResetPassword = status !== 'DISABLED';
  const canRevokeSessions = true;

  const activeConfig = activeAction ? ACTION_CONFIG[activeAction] : null;

  return (
    <div data-testid="account-detail-page">
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Space>
          <Button onClick={() => navigate('/security/accounts')} data-testid="back-to-list-button">
            ← Quay lại danh sách
          </Button>
        </Space>
        {hasPermission('SECURITY_ADMIN_MANAGE') && account && (
          <Space>
            <Link to={`/security/permissions/assignments?userId=${account.userId}`}>
              <Button data-testid="link-permission-assignment">
                Phân quyền
              </Button>
            </Link>
            <Link to={`/security/users/${account.userId}/role-assignments`}>
              <Button data-testid="link-role-assignment">
                Vai trò
              </Button>
            </Link>
            <Link to={`/security/users/${account.userId}/admin-group-assignments`}>
              <Button data-testid="link-admin-group-assignment">
                Nhóm quản trị
              </Button>
            </Link>
          </Space>
        )}
      </div>

      <Title level={3}>Chi tiết tài khoản</Title>

      <Space style={{ marginBottom: 16 }}>
        <Tag
          color={STATUS_COLORS[status] ?? 'default'}
          style={{ fontSize: 14, padding: '4px 12px' }}
          data-testid="account-status-badge"
        >
          {status}
        </Tag>
        {account.mustChangePassword && (
          <Alert
            type="warning"
            message="Người dùng phải đổi mật khẩu khi đăng nhập lần tới."
            data-testid="must-change-password-warning"
            style={{ marginBottom: 0 }}
          />
        )}
        {account.temporaryPasswordExpiresAt && (
          <Alert
            type="info"
            message={`Mật khẩu tạm hết hạn lúc: ${new Date(account.temporaryPasswordExpiresAt).toLocaleString('vi-VN')}`}
            data-testid="temp-password-expiry-warning"
            style={{ marginBottom: 0 }}
          />
        )}
      </Space>

      <Descriptions
        bordered
        column={2}
        data-testid="account-descriptions"
        style={{ marginBottom: 24 }}
      >
        <Descriptions.Item label="ID tài khoản" data-testid="field-account-id">
          {account.id}
        </Descriptions.Item>
        <Descriptions.Item label="ID người dùng" data-testid="field-user-id">
          {account.userId}
        </Descriptions.Item>
        <Descriptions.Item label="Tên đăng nhập" data-testid="field-username">
          {account.username}
        </Descriptions.Item>
        <Descriptions.Item label="Loại xác thực" data-testid="field-provider-type">
          {account.providerType}
        </Descriptions.Item>
        <Descriptions.Item label="Công ty" data-testid="field-company">
          {account.companyName ?? '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Phòng ban" data-testid="field-department">
          {account.departmentName ?? '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Số lần thất bại" data-testid="field-failed-attempts">
          {account.failedAttemptCount}
        </Descriptions.Item>
        <Descriptions.Item label="Khóa thủ công" data-testid="field-is-manual-lock">
          {account.isManualLock ? 'Có' : 'Không'}
        </Descriptions.Item>
        {account.lockoutEnd && (
          <Descriptions.Item label="Khóa đến" data-testid="field-lockout-end" span={2}>
            {new Date(account.lockoutEnd).toLocaleString('vi-VN')}
          </Descriptions.Item>
        )}
        <Descriptions.Item label="Ngày tạo" data-testid="field-created-at">
          {new Date(account.createdAt).toLocaleString('vi-VN')}
        </Descriptions.Item>
        <Descriptions.Item label="Cập nhật lúc" data-testid="field-updated-at">
          {account.updatedAt ? new Date(account.updatedAt).toLocaleString('vi-VN') : '—'}
        </Descriptions.Item>
      </Descriptions>

      <Space wrap data-testid="account-actions">
        {canActivate && (
          <Button
            type="primary"
            onClick={() => handleActionClick('activate')}
            data-testid="activate-button"
          >
            Kích hoạt
          </Button>
        )}
        {canDisable && (
          <Button
            danger
            onClick={() => handleActionClick('disable')}
            data-testid="disable-button"
          >
            Vô hiệu hóa
          </Button>
        )}
        {canLock && (
          <Button
            danger
            onClick={() => handleActionClick('lock')}
            data-testid="lock-button"
          >
            Khóa
          </Button>
        )}
        {canUnlock && (
          <Button
            onClick={() => handleActionClick('unlock')}
            data-testid="unlock-button"
          >
            Mở khóa
          </Button>
        )}
        {canResetPassword && (
          <Button
            onClick={() => handleActionClick('reset-password')}
            data-testid="reset-password-button"
          >
            Đặt lại mật khẩu
          </Button>
        )}
        {canRevokeSessions && (
          <Button
            danger
            onClick={() => handleActionClick('revoke-sessions')}
            data-testid="revoke-sessions-button"
          >
            Thu hồi phiên
          </Button>
        )}
      </Space>

      {activeConfig && (
        <ConfirmActionModal
          open={activeAction !== null}
          title={activeConfig.title}
          confirmationText={activeConfig.confirmationText}
          requireReason={activeConfig.requireReason}
          isLoading={mutation.isPending}
          errorMessage={actionError}
          onConfirm={handleConfirm}
          onCancel={handleModalCancel}
        />
      )}

      {temporaryPassword && (
        <TempPasswordModal
          open={true}
          temporaryPassword={temporaryPassword}
          onClose={handleDismissTempPassword}
        />
      )}
    </div>
  );
};

export default AccountDetailPage;
