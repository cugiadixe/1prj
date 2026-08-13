/**
 * PermissionAssignmentPage — Phase 1B.1-N (giao diện làm lại).
 *
 * Giao diện quản trị bảo mật để cấp/thu hồi quyền cá nhân cho từng người dùng.
 * Cổng: SECURITY_ADMIN_MANAGE GLOBAL.
 * Chỉ hỗ trợ phạm vi GLOBAL và COMPANY (ENTITY để sau).
 * Phân quyền theo COMPANY cần chọn công ty hiện hành ở thanh tiêu đề (CompanyProvider).
 * Backend vẫn là nơi quyết định cuối cùng — đây là phần frontend.
 *
 * Nguyên tắc hiển thị (làm lại theo phản hồi người dùng):
 *  - Không phô mã số thô: hiển thị họ tên người dùng, tên công ty, tên quyền.
 *  - Việt hóa toàn bộ nhãn kỹ thuật (GLOBAL/COMPANY, ALLOW/DENY...).
 *  - Chia luồng thành 3 bước rõ ràng: chọn người → cấp/thu hồi quyền → xem quyền tổng hợp.
 */

import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Empty,
  Form,
  Input,
  Modal,
  Radio,
  Select,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useCompany } from '../auth/CompanyProvider';
import {
  fetchPermissionCatalog,
  fetchUserIndividualPermissions,
  fetchEffectivePermissions,
  grantIndividualPermission,
  deactivateIndividualPermission,
} from './permissionAssignmentApi';
import type {
  PermissionDto,
  UserIndividualPermissionDto,
  CreateUserIndividualPermissionRequest,
} from './permissionAssignmentApi';
import {
  getAssignmentErrorMessage,
  isPermissionDenied,
  PERMISSION_DENIED_MSG,
  GENERIC_ERROR,
} from './errorMessages';
import { searchAccounts, getAccountsByUserId } from '../accountManagement/accountManagementApi';
import type { AccountSummaryDto } from '../accountManagement/types';

const { Title, Text, Paragraph } = Typography;

// ── Nhãn Việt hóa cho mã kỹ thuật ─────────────────────────────────────────────
const SCOPE_LABELS: Record<string, string> = {
  GLOBAL: 'Toàn hệ thống',
  COMPANY: 'Theo công ty',
};
const GRANT_LABELS: Record<string, string> = {
  ALLOW: 'Cho phép',
  DENY: 'Từ chối',
};
const scopeLabel = (s: string): string => SCOPE_LABELS[s] ?? s;
const grantLabel = (g: string): string => GRANT_LABELS[g] ?? g;

// Nhãn hiển thị cho một tài khoản: "Họ tên — tên đăng nhập · mã NV"
const accountLabel = (a: AccountSummaryDto): string => {
  const name = a.fullName?.trim() || a.username;
  const emp = a.employeeCode ? ` · ${a.employeeCode}` : '';
  return `${name} — ${a.username}${emp}`;
};

// Hook debounce nhỏ gọn cho ô tìm kiếm người dùng.
function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(t);
  }, [value, delayMs]);
  return debounced;
}

// ── Modal cấp quyền ───────────────────────────────────────────────────────────

interface GrantModalProps {
  open: boolean;
  permissions: PermissionDto[];
  currentCompanyId: number | null;
  currentCompanyName: string | null;
  isLoading: boolean;
  errorMessage: string | null;
  onGrant: (request: CreateUserIndividualPermissionRequest) => void;
  onCancel: () => void;
}

const GrantPermissionModal: React.FC<GrantModalProps> = ({
  open,
  permissions,
  currentCompanyId,
  currentCompanyName,
  isLoading,
  errorMessage,
  onGrant,
  onCancel,
}) => {
  const [permissionCode, setPermissionCode] = useState<string | undefined>(undefined);
  const [scopeType, setScopeType] = useState<'GLOBAL' | 'COMPANY'>('GLOBAL');
  const [grantType, setGrantType] = useState<'ALLOW' | 'DENY'>('ALLOW');
  const [reason, setReason] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const activePermissions = useMemo(
    () => permissions.filter((p) => p.isActive),
    [permissions],
  );
  const selectedPerm = activePermissions.find((p) => p.permissionCode === permissionCode) ?? null;
  const reasonRequired = selectedPerm?.requiresReason ?? false;

  const reset = () => {
    setPermissionCode(undefined);
    setScopeType('GLOBAL');
    setGrantType('ALLOW');
    setReason('');
    setValidationError(null);
  };

  const handleOk = () => {
    if (!permissionCode) {
      setValidationError('Vui lòng chọn một quyền.');
      return;
    }
    if (scopeType === 'COMPANY' && currentCompanyId === null) {
      setValidationError('Cần chọn một công ty ở thanh tiêu đề trước khi phân quyền theo công ty.');
      return;
    }
    if (reasonRequired && !reason.trim()) {
      setValidationError('Quyền này yêu cầu nhập lý do.');
      return;
    }
    setValidationError(null);
    const request: CreateUserIndividualPermissionRequest = {
      permissionCode,
      scopeType,
      companyId: scopeType === 'COMPANY' ? currentCompanyId : null,
      grantType,
      effectiveFrom: new Date().toISOString(),
      effectiveTo: null,
      reason: reason.trim() || null,
    };
    onGrant(request);
  };

  const handleCancel = () => {
    reset();
    onCancel();
  };

  return (
    <Modal
      open={open}
      title="Cấp quyền cá nhân"
      onOk={handleOk}
      onCancel={handleCancel}
      confirmLoading={isLoading}
      okText="Cấp quyền"
      cancelText="Hủy"
      data-testid="grant-permission-modal"
      destroyOnHidden
    >
      <Form layout="vertical">
        <Form.Item label="Quyền cần cấp" required>
          <Select
            showSearch
            placeholder="Tìm và chọn quyền"
            value={permissionCode}
            onChange={(v) => {
              setPermissionCode(v);
              setValidationError(null);
            }}
            filterOption={(input, option) =>
              (option?.label as string ?? '').toLowerCase().includes(input.toLowerCase())
            }
            options={activePermissions.map((p) => ({
              label: p.description ? `${p.description} (${p.permissionCode})` : p.permissionCode,
              value: p.permissionCode,
            }))}
            data-testid="permission-select"
            aria-label="Chọn quyền"
            style={{ width: '100%' }}
          />
        </Form.Item>

        {selectedPerm && (
          <div style={{ marginTop: -8, marginBottom: 12 }}>
            <Space size={4} wrap>
              <Tag>{selectedPerm.permissionCode}</Tag>
              {selectedPerm.isSensitive && <Tag color="volcano">Quyền nhạy cảm</Tag>}
              {selectedPerm.requiresReason && <Tag color="gold">Bắt buộc lý do</Tag>}
            </Space>
          </div>
        )}

        <Form.Item label="Phạm vi áp dụng">
          <Radio.Group
            value={scopeType}
            onChange={(e) => setScopeType(e.target.value)}
            optionType="button"
            buttonStyle="solid"
            data-testid="scope-select"
          >
            <Radio.Button value="GLOBAL">Toàn hệ thống</Radio.Button>
            <Radio.Button value="COMPANY" disabled={currentCompanyId === null}>
              {currentCompanyName ? `Theo công ty: ${currentCompanyName}` : 'Theo công ty'}
            </Radio.Button>
          </Radio.Group>
        </Form.Item>

        {scopeType === 'COMPANY' && currentCompanyId === null && (
          <Alert
            type="warning"
            message="Vui lòng chọn công ty ở thanh tiêu đề để phân quyền theo công ty."
            data-testid="company-required-warning"
            style={{ marginBottom: 12 }}
          />
        )}

        <Form.Item label="Kiểu cấp">
          <Radio.Group
            value={grantType}
            onChange={(e) => setGrantType(e.target.value)}
            optionType="button"
            data-testid="grant-type-select"
          >
            <Radio.Button value="ALLOW">Cho phép</Radio.Button>
            <Radio.Button value="DENY">Từ chối</Radio.Button>
          </Radio.Group>
        </Form.Item>

        {grantType === 'DENY' && (
          <Alert
            type="info"
            message='Quy tắc: "Từ chối" luôn thắng "Cho phép". Quyền bị từ chối sẽ bị chặn kể cả khi được cấp qua vai trò khác.'
            style={{ marginBottom: 12 }}
          />
        )}

        <Form.Item label={reasonRequired ? 'Lý do (bắt buộc)' : 'Lý do (tùy chọn)'} required={reasonRequired}>
          <Input.TextArea
            rows={2}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            maxLength={500}
            showCount
            placeholder="Nhập lý do cấp quyền"
            data-testid="grant-reason-input"
            aria-label="Lý do"
          />
        </Form.Item>
      </Form>

      {validationError && (
        <Alert
          type="error"
          message={validationError}
          data-testid="grant-validation-error"
          style={{ marginTop: 8 }}
        />
      )}
      {errorMessage && (
        <Alert
          type="error"
          message={errorMessage}
          data-testid="grant-api-error"
          style={{ marginTop: 8 }}
        />
      )}
    </Modal>
  );
};

// ── Trang chính ───────────────────────────────────────────────────────────────

const PermissionAssignmentPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { currentCompanyId, companies } = useCompany();

  // Trạng thái chọn người dùng
  const initialUserId = searchParams.get('userId');
  const [selectedUserId, setSelectedUserId] = useState<number | null>(
    initialUserId ? parseInt(initialUserId, 10) : null,
  );
  const [selectedAccount, setSelectedAccount] = useState<AccountSummaryDto | null>(null);
  const [rawUserSearch, setRawUserSearch] = useState('');
  const userSearch = useDebouncedValue(rawUserSearch, 250);

  const [showGrantModal, setShowGrantModal] = useState(false);
  const [grantError, setGrantError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Tra tên công ty từ danh sách công ty của quản trị viên hiện hành.
  const companyName = (id: number | null): string => {
    if (id === null) return '';
    const c = companies.find((x) => x.companyId === id);
    return c ? c.companyName : `Công ty #${id}`;
  };
  const currentCompanyName = currentCompanyId !== null ? companyName(currentCompanyId) : null;

  // ── Tìm kiếm tài khoản (luôn bật để danh sách không trống khi mới vào) ────────
  const {
    data: accountsData,
    isFetching: isFetchingAccounts,
    isError: isAccountsError,
    error: accountsError,
  } = useQuery({
    queryKey: ['permission-assignment-accounts', userSearch],
    queryFn: () => searchAccounts({ search: userSearch || undefined, page: 1, pageSize: 20 }),
    retry: false,
  });

  // ── Hydrate tên người dùng khi mở bằng liên kết ?userId= ─────────────────────
  const { data: hydratedAccounts } = useQuery({
    queryKey: ['permission-assignment-hydrate', selectedUserId],
    queryFn: () => getAccountsByUserId(selectedUserId!),
    enabled: selectedUserId !== null && selectedAccount === null,
    retry: false,
  });
  useEffect(() => {
    if (selectedAccount === null && hydratedAccounts && hydratedAccounts.length > 0) {
      setSelectedAccount(hydratedAccounts[0]);
    }
  }, [hydratedAccounts, selectedAccount]);

  // ── Danh mục quyền ───────────────────────────────────────────────────────────
  const {
    data: catalog,
    isLoading: isLoadingCatalog,
    isError: isCatalogError,
    error: catalogError,
  } = useQuery({
    queryKey: ['permission-catalog'],
    queryFn: fetchPermissionCatalog,
    retry: false,
  });
  const catalogMap = useMemo(() => {
    const m = new Map<string, PermissionDto>();
    (catalog ?? []).forEach((p) => m.set(p.permissionCode, p));
    return m;
  }, [catalog]);
  const permissionName = (code: string): string => catalogMap.get(code)?.description || code;

  // ── Quyền cá nhân của người dùng đã chọn ─────────────────────────────────────
  const {
    data: assignments,
    isLoading: isLoadingAssignments,
    isError: isAssignmentsError,
    error: assignmentsError,
  } = useQuery({
    queryKey: ['user-individual-permissions', selectedUserId],
    queryFn: () => fetchUserIndividualPermissions(selectedUserId!),
    enabled: selectedUserId !== null,
    retry: false,
  });

  // ── Quyền hiệu lực (tổng hợp) ────────────────────────────────────────────────
  const {
    data: effectivePermissions,
    isLoading: isLoadingEffective,
    isError: isEffectiveError,
  } = useQuery({
    queryKey: ['user-effective-permissions', selectedUserId, currentCompanyId],
    queryFn: () => fetchEffectivePermissions(selectedUserId!, currentCompanyId),
    enabled: selectedUserId !== null,
    retry: false,
  });

  // ── Mutation cấp quyền ───────────────────────────────────────────────────────
  const grantMutation = useMutation({
    mutationFn: (request: CreateUserIndividualPermissionRequest) =>
      grantIndividualPermission(selectedUserId!, request),
    onSuccess: () => {
      setShowGrantModal(false);
      setGrantError(null);
      setSuccessMessage('Cấp quyền thành công.');
      void queryClient.invalidateQueries({ queryKey: ['user-individual-permissions', selectedUserId] });
      void queryClient.invalidateQueries({ queryKey: ['user-effective-permissions', selectedUserId] });
    },
    onError: (err: unknown) => {
      setGrantError(getAssignmentErrorMessage(err));
    },
  });

  // ── Mutation thu hồi ─────────────────────────────────────────────────────────
  const deactivateMutation = useMutation({
    mutationFn: (assignment: UserIndividualPermissionDto) =>
      deactivateIndividualPermission(selectedUserId!, assignment.id, {
        rowVersion: assignment.rowVersion,
      }),
    onSuccess: () => {
      setSuccessMessage('Thu hồi phân quyền thành công.');
      void queryClient.invalidateQueries({ queryKey: ['user-individual-permissions', selectedUserId] });
      void queryClient.invalidateQueries({ queryKey: ['user-effective-permissions', selectedUserId] });
    },
    onError: (err: unknown) => {
      setSuccessMessage(null);
      Modal.error({
        title: 'Thu hồi thất bại',
        content: getAssignmentErrorMessage(err),
      });
    },
  });

  // ── Handlers ─────────────────────────────────────────────────────────────────
  const handleSelectUser = (userId: number, account: AccountSummaryDto | null) => {
    setSelectedUserId(userId);
    setSelectedAccount(account);
    setSuccessMessage(null);
  };

  const handleClearUser = () => {
    setSelectedUserId(null);
    setSelectedAccount(null);
    setSuccessMessage(null);
  };

  const handleOpenGrant = () => {
    setGrantError(null);
    setShowGrantModal(true);
  };

  const handleGrantSubmit = (request: CreateUserIndividualPermissionRequest) => {
    grantMutation.mutate(request);
  };

  const handleGrantCancel = () => {
    setShowGrantModal(false);
    setGrantError(null);
    grantMutation.reset();
  };

  const handleDeactivate = (assignment: UserIndividualPermissionDto) => {
    setSuccessMessage(null);
    Modal.confirm({
      title: 'Thu hồi phân quyền',
      content: `Bạn có chắc chắn muốn thu hồi quyền "${permissionName(assignment.permissionCode)}" (${grantLabel(assignment.grantType)}) khỏi người dùng này?`,
      okText: 'Thu hồi',
      okButtonProps: { danger: true },
      cancelText: 'Hủy',
      onOk: () => deactivateMutation.mutate(assignment),
    });
  };

  // Danh sách người dùng cho ô chọn (khử trùng theo userId, ghim người đang chọn).
  const userOptions = useMemo(() => {
    const seen = new Map<number, { value: number; label: string; account: AccountSummaryDto }>();
    if (selectedAccount) {
      seen.set(selectedAccount.userId, {
        value: selectedAccount.userId,
        label: accountLabel(selectedAccount),
        account: selectedAccount,
      });
    }
    (accountsData?.items ?? []).forEach((a) => {
      if (!seen.has(a.userId)) {
        seen.set(a.userId, { value: a.userId, label: accountLabel(a), account: a });
      }
    });
    return Array.from(seen.values());
  }, [accountsData, selectedAccount]);

  // ── Lỗi danh mục quyền ───────────────────────────────────────────────────────
  if (isCatalogError) {
    if (isPermissionDenied(catalogError)) {
      return (
        <div data-testid="permission-assignment-page">
          <Alert type="warning" message={PERMISSION_DENIED_MSG} data-testid="permission-denied-error" />
        </div>
      );
    }
    return (
      <div data-testid="permission-assignment-page">
        <Alert type="error" message={GENERIC_ERROR} data-testid="catalog-error" />
      </div>
    );
  }

  if (isLoadingCatalog) {
    return (
      <div style={{ textAlign: 'center', padding: 48 }} data-testid="permission-assignment-loading">
        <Spin size="large" />
      </div>
    );
  }

  const activeAssignments = (assignments ?? []).filter((a) => a.assignmentStatus === 'ACTIVE');

  const assignmentColumns: ColumnsType<UserIndividualPermissionDto> = [
    {
      title: 'Quyền',
      key: 'permission',
      render: (_, a) => (
        <Space direction="vertical" size={0}>
          <Text strong>{permissionName(a.permissionCode)}</Text>
          <Text type="secondary" style={{ fontSize: 12 }}>{a.permissionCode}</Text>
        </Space>
      ),
    },
    {
      title: 'Kiểu',
      dataIndex: 'grantType',
      key: 'grantType',
      width: 110,
      render: (g: string) => <Tag color={g === 'ALLOW' ? 'green' : 'red'}>{grantLabel(g)}</Tag>,
    },
    {
      title: 'Phạm vi',
      key: 'scope',
      render: (_, a) => (
        <Space size={4} wrap>
          <Tag>{scopeLabel(a.scopeType)}</Tag>
          {a.scopeType === 'COMPANY' && a.companyId != null && (
            <Tag color="blue">{companyName(a.companyId)}</Tag>
          )}
        </Space>
      ),
    },
    {
      title: 'Lý do',
      dataIndex: 'reason',
      key: 'reason',
      render: (r: string | null) => (r ? <Text>{r}</Text> : <Text type="secondary">—</Text>),
    },
    {
      title: 'Hành động',
      key: 'action',
      width: 120,
      render: (_, a) => (
        <Button
          danger
          size="small"
          onClick={() => handleDeactivate(a)}
          loading={deactivateMutation.isPending}
          data-testid={`revoke-${a.id}`}
        >
          Thu hồi
        </Button>
      ),
    },
  ];

  const enrichedEffective = (effectivePermissions?.permissionCodes ?? []).map((code) => {
    const d = catalogMap.get(code);
    return {
      code,
      name: d?.description || code,
      moduleCode: d?.moduleCode ?? null,
    };
  });

  const effectiveColumns: ColumnsType<{ code: string; name: string; moduleCode: string | null }> = [
    {
      title: 'Tên quyền',
      dataIndex: 'name',
      key: 'name',
      render: (name: string) => <Text strong>{name}</Text>,
    },
    {
      title: 'Mã quyền',
      dataIndex: 'code',
      key: 'code',
      render: (code: string) => <Text type="secondary">{code}</Text>,
    },
    {
      title: 'Phân hệ',
      dataIndex: 'moduleCode',
      key: 'moduleCode',
      width: 160,
      render: (mod: string | null) => (mod ? <Tag>{mod}</Tag> : <Text type="secondary">—</Text>),
    },
  ];

  return (
    <div data-testid="permission-assignment-page">
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={() => navigate(-1)} data-testid="back-button">
          ← Quay lại
        </Button>
      </Space>

      <Title level={3} style={{ marginBottom: 4 }}>Phân quyền cá nhân</Title>
      <Paragraph type="secondary" style={{ marginBottom: 16, maxWidth: 720 }}>
        Cấp hoặc thu hồi quyền riêng cho từng người dùng. Quyền cá nhân được cộng thêm vào quyền
        có sẵn từ vai trò; quyền "Từ chối" luôn được ưu tiên. Chọn người dùng để bắt đầu.
      </Paragraph>

      {successMessage && (
        <Alert
          type="success"
          message={successMessage}
          closable
          onClose={() => setSuccessMessage(null)}
          data-testid="success-message"
          style={{ marginBottom: 16 }}
        />
      )}

      {/* ── Bước 1: Chọn người dùng ─────────────────────────────────────────── */}
      <Card title="1. Chọn người dùng" style={{ marginBottom: 16 }} data-testid="user-selection-card">
        <Select
          showSearch
          allowClear
          value={selectedUserId ?? undefined}
          placeholder="Tìm theo họ tên, tên đăng nhập hoặc mã nhân viên"
          style={{ width: '100%', maxWidth: 480 }}
          filterOption={false}
          onSearch={setRawUserSearch}
          onChange={(val, option) => {
            if (val === undefined || val === null) {
              handleClearUser();
              return;
            }
            const acc =
              (option as { account?: AccountSummaryDto } | undefined)?.account ??
              accountsData?.items.find((a) => a.userId === val) ??
              null;
            handleSelectUser(val as number, acc);
          }}
          notFoundContent={
            isFetchingAccounts ? (
              <div style={{ textAlign: 'center', padding: 8 }}>
                <Spin size="small" />
              </div>
            ) : (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Không tìm thấy người dùng" />
            )
          }
          options={userOptions}
          data-testid="user-search-input"
          aria-label="Chọn người dùng"
        />

        {isAccountsError && (
          <Alert
            type="error"
            message={isPermissionDenied(accountsError) ? PERMISSION_DENIED_MSG : GENERIC_ERROR}
            data-testid="user-search-error"
            style={{ marginTop: 12 }}
          />
        )}

        {selectedAccount && (
          <Descriptions
            bordered
            size="small"
            column={{ xs: 1, sm: 2, md: 3 }}
            style={{ marginTop: 16 }}
            data-testid="selected-user-info"
          >
            <Descriptions.Item label="Họ tên">{selectedAccount.fullName?.trim() || '—'}</Descriptions.Item>
            <Descriptions.Item label="Tên đăng nhập">{selectedAccount.username}</Descriptions.Item>
            <Descriptions.Item label="Mã nhân viên">{selectedAccount.employeeCode || '—'}</Descriptions.Item>
          </Descriptions>
        )}

        {!selectedAccount && selectedUserId !== null && (
          <div style={{ marginTop: 12 }}>
            <Space>
              <Spin size="small" />
              <Text type="secondary" data-testid="selected-user-loading">
                Đang tải thông tin người dùng…
              </Text>
            </Space>
          </div>
        )}
      </Card>

      {/* ── Bước 2: Quyền cá nhân ───────────────────────────────────────────── */}
      {selectedUserId !== null && (
        <Card
          title="2. Quyền cá nhân đã cấp"
          style={{ marginBottom: 16 }}
          extra={
            <Button type="primary" onClick={handleOpenGrant} data-testid="grant-permission-button">
              Cấp quyền mới
            </Button>
          }
          data-testid="assignments-card"
        >
          {isAssignmentsError ? (
            <Alert
              type="error"
              message={isPermissionDenied(assignmentsError) ? PERMISSION_DENIED_MSG : GENERIC_ERROR}
              data-testid="assignments-error"
            />
          ) : (
            <Table
              rowKey="id"
              size="small"
              columns={assignmentColumns}
              dataSource={activeAssignments}
              loading={isLoadingAssignments}
              pagination={false}
              data-testid="assignments-list"
              locale={{
                emptyText: (
                  <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description={'Chưa có quyền cá nhân nào. Bấm "Cấp quyền mới" để thêm.'}
                  />
                ),
              }}
            />
          )}
        </Card>
      )}

      {/* ── Bước 3: Quyền hiệu lực (chỉ xem) ────────────────────────────────── */}
      {selectedUserId !== null && (
        <Card
          title="3. Quyền hiệu lực (tổng hợp, chỉ xem)"
          style={{ marginBottom: 16 }}
          data-testid="effective-permissions-card"
        >
          {isLoadingEffective && (
            <div style={{ textAlign: 'center', padding: 24 }} data-testid="effective-loading">
              <Spin />
            </div>
          )}

          {isEffectiveError && (
            <Alert type="warning" message="Không thể tải quyền hiệu lực." data-testid="effective-error" />
          )}

          {!isLoadingEffective && !isEffectiveError && effectivePermissions && (
            <>
              <Alert
                type="info"
                message={
                  effectivePermissions.companyId !== null
                    ? `Quyền hiệu lực trong phạm vi công ty "${companyName(effectivePermissions.companyId)}". Quy tắc "Từ chối thắng" do hệ thống áp dụng.`
                    : 'Quyền hiệu lực toàn hệ thống. Quy tắc "Từ chối thắng" do hệ thống áp dụng.'
                }
                style={{ marginBottom: 12 }}
                data-testid="effective-scope-info"
              />
              <Table
                rowKey="code"
                size="small"
                columns={effectiveColumns}
                dataSource={enrichedEffective}
                pagination={false}
                data-testid="effective-permissions-list"
                locale={{
                  emptyText: (
                    <Empty
                      image={Empty.PRESENTED_IMAGE_SIMPLE}
                      description="Người dùng chưa có quyền hiệu lực nào."
                    />
                  ),
                }}
              />
            </>
          )}
        </Card>
      )}

      {/* ── Modal cấp quyền ─────────────────────────────────────────────────── */}
      <GrantPermissionModal
        open={showGrantModal}
        permissions={catalog ?? []}
        currentCompanyId={currentCompanyId}
        currentCompanyName={currentCompanyName}
        isLoading={grantMutation.isPending}
        errorMessage={grantError}
        onGrant={handleGrantSubmit}
        onCancel={handleGrantCancel}
      />
    </div>
  );
};

export default PermissionAssignmentPage;
