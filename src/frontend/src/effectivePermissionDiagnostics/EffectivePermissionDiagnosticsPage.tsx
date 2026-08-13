import React, { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Collapse,
  Descriptions,
  Input,
  List,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
} from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useCompany } from '../auth/CompanyProvider';
import { effectivePermissionDiagnosticsApi } from './effectivePermissionDiagnosticsApi';
import type {
  PermissionDto,
  UserRoleAssignmentDto,
  UserAdminGroupAssignmentDto,
} from './effectivePermissionDiagnosticsApi';
import { getSanitizedErrorMessage } from './errorMessages';

const { Title, Text } = Typography;

const EffectivePermissionDiagnosticsPage: React.FC = () => {
  const navigate = useNavigate();
  const { currentCompanyId } = useCompany();

  const [userIdInput, setUserIdInput] = useState('');
  const [submittedUserId, setSubmittedUserId] = useState<number | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleLookup = () => {
    const trimmed = userIdInput.trim();
    if (!trimmed) {
      setValidationError('Mã người dùng là bắt buộc.');
      return;
    }
    const parsed = Number(trimmed);
    if (!Number.isInteger(parsed) || parsed <= 0) {
      setValidationError('Mã người dùng phải là số nguyên dương.');
      return;
    }
    setValidationError(null);
    setSubmittedUserId(parsed);
  };

  const {
    data: effectivePermissions,
    isLoading: isLoadingEffective,
    isError: isEffectiveError,
    error: effectiveError,
  } = useQuery({
    queryKey: ['effective-permissions', submittedUserId, currentCompanyId],
    queryFn: () =>
      effectivePermissionDiagnosticsApi.fetchEffectivePermissions(
        submittedUserId!,
        currentCompanyId,
      ),
    enabled: submittedUserId !== null,
    retry: false,
  });

  const { data: catalog } = useQuery({
    queryKey: ['permission-catalog-diagnostics'],
    queryFn: effectivePermissionDiagnosticsApi.fetchPermissionCatalog,
    retry: false,
  });

  const {
    data: individualPermissions,
    isLoading: isLoadingIndividual,
  } = useQuery({
    queryKey: ['diagnostics-individual-permissions', submittedUserId],
    queryFn: () =>
      effectivePermissionDiagnosticsApi.fetchUserIndividualPermissions(submittedUserId!),
    enabled: submittedUserId !== null,
    retry: false,
  });

  const {
    data: roleAssignments,
    isLoading: isLoadingRoles,
  } = useQuery({
    queryKey: ['diagnostics-role-assignments', submittedUserId],
    queryFn: () =>
      effectivePermissionDiagnosticsApi.fetchUserRoleAssignments(submittedUserId!),
    enabled: submittedUserId !== null,
    retry: false,
  });

  const {
    data: adminGroupAssignments,
    isLoading: isLoadingAdminGroups,
  } = useQuery({
    queryKey: ['diagnostics-admin-group-assignments', submittedUserId],
    queryFn: () =>
      effectivePermissionDiagnosticsApi.fetchUserAdminGroupAssignments(submittedUserId!),
    enabled: submittedUserId !== null,
    retry: false,
  });

  const catalogMap = new Map<string, PermissionDto>();
  catalog?.forEach((p) => catalogMap.set(p.permissionCode, p));

  const enrichedPermissions = effectivePermissions?.permissionCodes.map((code) => {
    const detail = catalogMap.get(code);
    return {
      code,
      description: detail?.description ?? null,
      moduleCode: detail?.moduleCode ?? null,
      dataScope: detail?.dataScope ?? null,
      isActive: detail?.isActive ?? null,
    };
  });

  return (
    <div data-testid="effective-permission-diagnostics-page">
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={() => navigate(-1)} data-testid="back-button">
          ← Quay lại
        </Button>
      </Space>

      <Title level={3}>Chẩn đoán quyền hiệu lực</Title>
      <Text type="secondary" data-testid="page-description">
        Quyền hiệu lực cuối cùng do backend xác định cho một người dùng.
        Không có thông tin phân bổ theo nguồn.
      </Text>

      <Card title="Chọn người dùng" style={{ marginTop: 16, marginBottom: 16 }} data-testid="user-selection-card">
        <Space>
          <Input
            placeholder="Nhập mã người dùng"
            value={userIdInput}
            onChange={(e) => {
              setUserIdInput(e.target.value);
              setValidationError(null);
            }}
            onPressEnter={handleLookup}
            style={{ width: 200 }}
            data-testid="user-id-input"
          />
          <Button type="primary" onClick={handleLookup} data-testid="lookup-button">
            Tra cứu
          </Button>
        </Space>
        {validationError && (
          <Alert
            type="warning"
            message={validationError}
            style={{ marginTop: 8 }}
            data-testid="validation-error"
          />
        )}
        {currentCompanyId !== null && (
          <div style={{ marginTop: 8 }}>
            <Text type="secondary" data-testid="company-context-indicator">
              Bối cảnh công ty: {currentCompanyId}
            </Text>
          </div>
        )}
        {currentCompanyId === null && submittedUserId !== null && (
          <div style={{ marginTop: 8 }}>
            <Text type="secondary" data-testid="global-context-indicator">
              Đang hiển thị quyền hiệu lực toàn cục (chưa chọn công ty).
            </Text>
          </div>
        )}
      </Card>

      {submittedUserId !== null && isLoadingEffective && (
        <Spin data-testid="effective-loading" />
      )}

      {isEffectiveError && (
        <Alert
          type="error"
          message={getSanitizedErrorMessage(effectiveError, 'Không thể tải quyền hiệu lực.')}
          data-testid="effective-error"
          style={{ marginBottom: 16 }}
        />
      )}

      {effectivePermissions && !isEffectiveError && (
        <>
          <Card
            title="Quyền hiệu lực do Backend xác định"
            style={{ marginBottom: 16 }}
            data-testid="effective-permissions-card"
          >
            <Descriptions bordered size="small" column={2} style={{ marginBottom: 16 }}>
              <Descriptions.Item label="Mã người dùng">{effectivePermissions.userId}</Descriptions.Item>
              <Descriptions.Item label="Mã công ty">
                {effectivePermissions.companyId ?? 'Toàn cục'}
              </Descriptions.Item>
              <Descriptions.Item label="Tổng số quyền">
                {effectivePermissions.permissionCodes.length}
              </Descriptions.Item>
            </Descriptions>

            {effectivePermissions.permissionCodes.length === 0 ? (
              <Text type="secondary" data-testid="no-permissions-message">
                Người dùng này không có quyền hiệu lực nào
                {currentCompanyId !== null ? ' cho công ty đã chọn' : ''}.
              </Text>
            ) : (
              <Table
                dataSource={enrichedPermissions}
                rowKey="code"
                size="small"
                pagination={false}
                data-testid="effective-permissions-table"
                columns={[
                  {
                    title: 'Mã quyền',
                    dataIndex: 'code',
                    key: 'code',
                    render: (code: string) => <Text strong>{code}</Text>,
                  },
                  {
                    title: 'Mô tả',
                    dataIndex: 'description',
                    key: 'description',
                    render: (desc: string | null) => desc ?? <Text type="secondary">—</Text>,
                  },
                  {
                    title: 'Phân hệ',
                    dataIndex: 'moduleCode',
                    key: 'moduleCode',
                    render: (mod: string | null) => mod ?? <Text type="secondary">—</Text>,
                  },
                  {
                    title: 'Phạm vi',
                    dataIndex: 'dataScope',
                    key: 'dataScope',
                    render: (scope: string | null) =>
                      scope ? <Tag>{scope}</Tag> : <Text type="secondary">—</Text>,
                  },
                  {
                    title: 'Hoạt động',
                    dataIndex: 'isActive',
                    key: 'isActive',
                    render: (active: boolean | null) =>
                      active === null ? (
                        <Text type="secondary">—</Text>
                      ) : active ? (
                        <Tag color="green">Có</Tag>
                      ) : (
                        <Tag color="red">Không</Tag>
                      ),
                  },
                ]}
              />
            )}
          </Card>

          <Card
            title="Chỉ mang tính tham khảo — Không phải phân bổ nguồn chính thức"
            style={{ marginBottom: 16 }}
            data-testid="contextual-sections-card"
          >
            <Alert
              type="info"
              message="Các mục bên dưới hiển thị thông tin tham khảo từ các API liên quan. Chúng không thể hiện phân bổ nguồn chính thức cho các quyền hiệu lực nêu trên."
              style={{ marginBottom: 16 }}
              data-testid="context-disclaimer"
            />

            <Collapse
              items={[
                {
                  key: 'individual',
                  label: 'Quyền cá nhân (Tham khảo)',
                  children: (
                    <div data-testid="individual-permissions-context">
                      {isLoadingIndividual && <Spin data-testid="individual-loading" />}
                      {!isLoadingIndividual && individualPermissions && individualPermissions.length === 0 && (
                        <Text type="secondary">Không tìm thấy quyền cá nhân nào.</Text>
                      )}
                      {!isLoadingIndividual && individualPermissions && individualPermissions.length > 0 && (
                        <Table
                          dataSource={individualPermissions}
                          rowKey="id"
                          size="small"
                          pagination={false}
                          data-testid="individual-permissions-table"
                          columns={[
                            { title: 'Quyền', dataIndex: 'permissionCode', key: 'permissionCode' },
                            {
                              title: 'Cấp quyền',
                              dataIndex: 'grantType',
                              key: 'grantType',
                              render: (gt: string) =>
                                gt === 'ALLOW' ? (
                                  <Tag color="green">ALLOW</Tag>
                                ) : (
                                  <Tag color="red">{gt}</Tag>
                                ),
                            },
                            { title: 'Phạm vi', dataIndex: 'scopeType', key: 'scopeType' },
                            { title: 'Trạng thái', dataIndex: 'assignmentStatus', key: 'assignmentStatus' },
                          ]}
                        />
                      )}
                    </div>
                  ),
                },
                {
                  key: 'roles',
                  label: 'Phân vai trò (Tham khảo)',
                  children: (
                    <div data-testid="role-assignments-context">
                      {isLoadingRoles && <Spin data-testid="roles-loading" />}
                      {!isLoadingRoles && roleAssignments && roleAssignments.length === 0 && (
                        <Text type="secondary">Không tìm thấy phân vai trò nào.</Text>
                      )}
                      {!isLoadingRoles && roleAssignments && roleAssignments.length > 0 && (
                        <RoleAssignmentsList assignments={roleAssignments} />
                      )}
                    </div>
                  ),
                },
                {
                  key: 'adminGroups',
                  label: 'Phân nhóm quản trị (Tham khảo)',
                  children: (
                    <div data-testid="admin-group-assignments-context">
                      {isLoadingAdminGroups && <Spin data-testid="admin-groups-loading" />}
                      {!isLoadingAdminGroups && adminGroupAssignments && adminGroupAssignments.length === 0 && (
                        <Text type="secondary">Không tìm thấy phân nhóm quản trị nào.</Text>
                      )}
                      {!isLoadingAdminGroups && adminGroupAssignments && adminGroupAssignments.length > 0 && (
                        <AdminGroupAssignmentsList assignments={adminGroupAssignments} />
                      )}
                    </div>
                  ),
                },
              ]}
            />
          </Card>
        </>
      )}
    </div>
  );
};

const RoleAssignmentsList: React.FC<{ assignments: UserRoleAssignmentDto[] }> = ({ assignments }) => {
  return (
    <List
      dataSource={assignments}
      size="small"
      renderItem={(ra) => (
        <List.Item key={ra.id}>
          <List.Item.Meta
            title={
              <>
                <Text strong>{ra.roleCode}</Text> — {ra.roleName}
                {ra.isActive ? (
                  <Tag color="green" style={{ marginLeft: 8 }}>Hoạt động</Tag>
                ) : (
                  <Tag color="red" style={{ marginLeft: 8 }}>Ngừng hoạt động</Tag>
                )}
              </>
            }
            description={`Phạm vi: ${ra.scopeType}${ra.companyId ? ` (Công ty ${ra.companyId})` : ''}`}
          />
        </List.Item>
      )}
    />
  );
};

const AdminGroupAssignmentsList: React.FC<{ assignments: UserAdminGroupAssignmentDto[] }> = ({ assignments }) => {
  return (
    <List
      dataSource={assignments}
      size="small"
      renderItem={(aga) => (
        <List.Item key={aga.id}>
          <List.Item.Meta
            title={
              <>
                <Text strong>{aga.groupCode}</Text> — {aga.groupName}
                <Tag style={{ marginLeft: 8 }}>{aga.assignmentStatus}</Tag>
              </>
            }
          />
        </List.Item>
      )}
    />
  );
};

export default EffectivePermissionDiagnosticsPage;
