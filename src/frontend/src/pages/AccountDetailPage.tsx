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
import { usePermissions } from '../auth/AuthProvider';
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

// ── Confirmation modal with optional reason textarea ──────────────────────────

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
        setValidationError('A reason is required.');
        return;
      }
      if (reason.length > MAX_REASON_LENGTH) {
        setValidationError(`Reason must not exceed ${MAX_REASON_LENGTH} characters.`);
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
      okText="Confirm"
      cancelText="Cancel"
      data-testid="confirm-action-modal"
      destroyOnHidden
    >
      <p>{confirmationText}</p>
      {requireReason && (
        <Form layout="vertical">
          <Form.Item
            label="Reason"
            validateStatus={validationError ? 'error' : undefined}
            help={validationError}
          >
            <TextArea
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              maxLength={MAX_REASON_LENGTH}
              placeholder="Enter reason (required)"
              data-testid="reason-input"
              aria-label="Reason"
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

// ── Temporary password display modal ─────────────────────────────────────────

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
      title="Temporary Password"
      footer={
        <Space>
          <Button
            onClick={handleCopy}
            data-testid="copy-temp-password-button"
          >
            Copy to Clipboard
          </Button>
          <Button
            type="primary"
            onClick={onClose}
            data-testid="dismiss-temp-password-button"
          >
            Close
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
        message="Record this password now. It will not be shown again."
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

// ── Main AccountDetailPage ────────────────────────────────────────────────────

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
    title: 'Activate Account',
    confirmationText: 'Are you sure you want to activate this account?',
    requireReason: false,
  },
  disable: {
    title: 'Disable Account',
    confirmationText:
      'This will prevent the user from logging in. Enter a reason.',
    requireReason: true,
  },
  lock: {
    title: 'Lock Account',
    confirmationText: 'This will lock the account. Enter a reason.',
    requireReason: true,
  },
  unlock: {
    title: 'Unlock Account',
    confirmationText: 'Are you sure you want to unlock this account?',
    requireReason: false,
  },
  'reset-password': {
    title: 'Reset Password',
    confirmationText:
      'This will generate a new temporary password and revoke all sessions. Enter a reason.',
    requireReason: true,
  },
  'revoke-sessions': {
    title: 'Revoke All Sessions',
    confirmationText:
      'This will revoke all active sessions for this user. Enter a reason.',
    requireReason: true,
  },
};

const AccountDetailPage: React.FC = () => {
  const { accountId } = useParams<{ accountId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = usePermissions();

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
          // Temporary password handled separately — do NOT log it
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
      // Refetch account detail after successful action
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

  // ── Loading ─────────────────────────────────────────────────────────────────

  if (isNaN(accountIdNum)) {
    return <Alert type="error" message="Invalid account ID." data-testid="invalid-account-id" />;
  }

  if (isLoading) {
    return (
      <div style={{ textAlign: 'center', padding: 48 }} data-testid="account-detail-loading">
        <Spin size="large" />
      </div>
    );
  }

  // ── Error states ────────────────────────────────────────────────────────────

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

  const canActivate = status === 'DISABLED';
  const canDisable = status === 'ACTIVE' || status === 'LOCKED';
  const canLock = status === 'ACTIVE';
  const canUnlock = status === 'LOCKED';
  const canResetPassword = status !== 'DISABLED';
  const canRevokeSessions = true;

  const activeConfig = activeAction ? ACTION_CONFIG[activeAction] : null;

  return (
    <div data-testid="account-detail-page">
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Space>
          <Button onClick={() => navigate('/security/accounts')} data-testid="back-to-list-button">
            ← Back to Account List
          </Button>
        </Space>
        {hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') && account && (
          <Space>
            <Link to={`/security/permissions/assignments?userId=${account.userId}`}>
              <Button data-testid="link-permission-assignment">
                Manage Permissions
              </Button>
            </Link>
          </Space>
        )}
      </div>

      <Title level={3}>Account Detail</Title>

      {/* Status banner */}
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
            message="User must change password on next login."
            data-testid="must-change-password-warning"
            style={{ marginBottom: 0 }}
          />
        )}
        {account.temporaryPasswordExpiresAt && (
          <Alert
            type="info"
            message={`Temporary password expires at: ${new Date(account.temporaryPasswordExpiresAt).toLocaleString()}`}
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
        <Descriptions.Item label="Account ID" data-testid="field-account-id">
          {account.id}
        </Descriptions.Item>
        <Descriptions.Item label="User ID" data-testid="field-user-id">
          {account.userId}
        </Descriptions.Item>
        <Descriptions.Item label="Username" data-testid="field-username">
          {account.username}
        </Descriptions.Item>
        <Descriptions.Item label="Provider Type" data-testid="field-provider-type">
          {account.providerType}
        </Descriptions.Item>
        <Descriptions.Item label="Failed Attempts" data-testid="field-failed-attempts">
          {account.failedAttemptCount}
        </Descriptions.Item>
        <Descriptions.Item label="Manual Lock" data-testid="field-is-manual-lock">
          {account.isManualLock ? 'Yes' : 'No'}
        </Descriptions.Item>
        {account.lockoutEnd && (
          <Descriptions.Item label="Lockout Until" data-testid="field-lockout-end" span={2}>
            {new Date(account.lockoutEnd).toLocaleString()}
          </Descriptions.Item>
        )}
        <Descriptions.Item label="Created At" data-testid="field-created-at">
          {new Date(account.createdAt).toLocaleString()}
        </Descriptions.Item>
        <Descriptions.Item label="Updated At" data-testid="field-updated-at">
          {account.updatedAt ? new Date(account.updatedAt).toLocaleString() : '—'}
        </Descriptions.Item>
      </Descriptions>

      {/* Action buttons */}
      <Space wrap data-testid="account-actions">
        {canActivate && (
          <Button
            type="primary"
            onClick={() => handleActionClick('activate')}
            data-testid="activate-button"
          >
            Activate
          </Button>
        )}
        {canDisable && (
          <Button
            danger
            onClick={() => handleActionClick('disable')}
            data-testid="disable-button"
          >
            Disable
          </Button>
        )}
        {canLock && (
          <Button
            danger
            onClick={() => handleActionClick('lock')}
            data-testid="lock-button"
          >
            Lock
          </Button>
        )}
        {canUnlock && (
          <Button
            onClick={() => handleActionClick('unlock')}
            data-testid="unlock-button"
          >
            Unlock
          </Button>
        )}
        {canResetPassword && (
          <Button
            onClick={() => handleActionClick('reset-password')}
            data-testid="reset-password-button"
          >
            Reset Password
          </Button>
        )}
        {canRevokeSessions && (
          <Button
            danger
            onClick={() => handleActionClick('revoke-sessions')}
            data-testid="revoke-sessions-button"
          >
            Revoke Sessions
          </Button>
        )}
      </Space>

      {/* Confirmation modal */}
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

      {/* Temporary password modal — shown once after reset-password */}
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
