import React, { useState } from 'react';
import {
  Alert,
  Button,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
  message,
} from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { searchAccounts, getUsersWithoutAccount, createAccount } from '../accountManagement/accountManagementApi';
import {
  PERMISSION_DENIED,
  GENERIC_ERROR,
  isPermissionDenied,
} from '../accountManagement/errorMessages';
import type { AccountSummaryDto, AccountStatus, UserWithoutAccountDto } from '../accountManagement/types';

const { Title, Text } = Typography;
const { Search } = Input;
const { Option } = Select;

const STATUS_COLORS: Record<string, string> = {
  ACTIVE: 'green',
  LOCKED: 'orange',
  DISABLED: 'red',
};

const PAGE_SIZE = 20;

const AccountManagementPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);
  const [providerTypeFilter, setProviderTypeFilter] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);

  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [creating, setCreating] = useState(false);
  const [tempPassword, setTempPassword] = useState<string | null>(null);
  const [selectedCompanyId, setSelectedCompanyId] = useState<number | undefined>(undefined);
  const [form] = Form.useForm();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['accounts', search, statusFilter, providerTypeFilter, page],
    queryFn: () =>
      searchAccounts({
        search: search || undefined,
        status: statusFilter,
        providerType: providerTypeFilter,
        page,
        pageSize: PAGE_SIZE,
      }),
    retry: false,
  });

  const { data: usersWithoutAccount, isLoading: loadingUsers } = useQuery({
    queryKey: ['users-without-account'],
    queryFn: getUsersWithoutAccount,
    enabled: createModalOpen,
  });

  const handleSearch = (value: string) => {
    setSearch(value);
    setPage(1);
  };

  const handleStatusChange = (value: string | undefined) => {
    setStatusFilter(value);
    setPage(1);
  };

  const handleProviderTypeChange = (value: string | undefined) => {
    setProviderTypeFilter(value);
    setPage(1);
  };

  const handleCreateAccount = async (values: { userId: number; providerSubject: string }) => {
    setCreating(true);
    try {
      const result = await createAccount({
        userId: values.userId,
        providerSubject: values.providerSubject,
      });
      setTempPassword(result.temporaryPassword);
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      queryClient.invalidateQueries({ queryKey: ['users-without-account'] });
      message.success('Tạo tài khoản thành công');
    } catch (err: unknown) {
      const apiErr = err as { response?: { data?: { detail?: string } } };
      message.error(apiErr?.response?.data?.detail || 'Tạo tài khoản thất bại');
    } finally {
      setCreating(false);
    }
  };

  const handleCloseCreateModal = () => {
    setCreateModalOpen(false);
    setTempPassword(null);
    setSelectedCompanyId(undefined);
    form.resetFields();
  };

  const handleUserSelect = (userId: number) => {
    const user = usersWithoutAccount?.find((u: UserWithoutAccountDto) => u.userId === userId);
    if (user?.email) {
      form.setFieldsValue({ providerSubject: user.email });
    }
  };

  const handleCompanyChange = (companyId: number) => {
    setSelectedCompanyId(companyId);
    // Đổi công ty thì bỏ chọn người dùng cũ (có thể không thuộc công ty mới).
    form.setFieldsValue({ userId: undefined });
  };

  // Danh sách công ty (gộp từ các người dùng chưa có tài khoản) cho ô "Công ty".
  const companyOptions = React.useMemo(() => {
    const map = new Map<number, string>();
    usersWithoutAccount?.forEach((u: UserWithoutAccountDto) =>
      u.companies?.forEach((c) => map.set(c.companyId, c.companyName)));
    return Array.from(map, ([value, label]) => ({ value, label }))
      .sort((a, b) => a.label.localeCompare(b.label, 'vi'));
  }, [usersWithoutAccount]);

  // Người dùng lọc theo công ty đang chọn; chưa chọn công ty thì để trống.
  const filteredUserOptions = React.useMemo(() => {
    if (!selectedCompanyId || !usersWithoutAccount) return [];
    return usersWithoutAccount
      .filter((u: UserWithoutAccountDto) =>
        u.companies?.some((c) => c.companyId === selectedCompanyId))
      .map((u: UserWithoutAccountDto) => ({
        value: u.userId,
        label: `${u.fullName}${u.employeeCode ? ` (${u.employeeCode})` : ''}`,
      }));
  }, [usersWithoutAccount, selectedCompanyId]);

  const columns = [
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: AccountStatus) => (
        <Tag color={STATUS_COLORS[status] ?? 'default'} data-testid={`status-badge-${status}`}>
          {status}
        </Tag>
      ),
    },
    {
      title: 'Tên đăng nhập',
      dataIndex: 'username',
      key: 'username',
    },
    {
      title: 'Họ và tên',
      dataIndex: 'fullName',
      key: 'fullName',
    },
    {
      title: 'Mã nhân viên',
      dataIndex: 'employeeCode',
      key: 'employeeCode',
    },
    {
      title: 'Công ty',
      dataIndex: 'companyName',
      key: 'companyName',
      render: (v: string | null) => v ?? '—',
    },
    {
      title: 'Phòng ban',
      dataIndex: 'departmentName',
      key: 'departmentName',
      render: (v: string | null) => v ?? '—',
    },
    {
      title: 'Loại xác thực',
      dataIndex: 'providerType',
      key: 'providerType',
    },
    {
      title: 'Tình trạng NV',
      dataIndex: 'employmentStatus',
      key: 'employmentStatus',
    },
    {
      title: 'Thao tác',
      key: 'actions',
      render: (_: unknown, record: AccountSummaryDto) => (
        <Button
          type="link"
          size="small"
          data-testid={`manage-account-${record.accountId}`}
          onClick={() => navigate(`/security/accounts/${record.accountId}`)}
        >
          Quản lý
        </Button>
      ),
    },
  ];

  if (isError) {
    const msg = isPermissionDenied(error)
      ? PERMISSION_DENIED
      : GENERIC_ERROR;
    return (
      <div data-testid="account-list-error">
        <Alert
          type={isPermissionDenied(error) ? 'warning' : 'error'}
          message={msg}
          data-testid="account-list-error-message"
        />
      </div>
    );
  }

  return (
    <div data-testid="account-management-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={3} style={{ margin: 0 }}>Quản lý tài khoản</Title>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => setCreateModalOpen(true)}
          data-testid="create-account-btn"
        >
          Tạo tài khoản
        </Button>
      </Space>

      <Space style={{ marginBottom: 16 }} wrap>
        <Search
          placeholder="Tìm theo tên đăng nhập, mã NV, hoặc họ tên"
          allowClear
          onSearch={handleSearch}
          style={{ width: 360 }}
          data-testid="account-search-input"
          aria-label="Tìm kiếm tài khoản"
        />

        <Select
          allowClear
          placeholder="Lọc trạng thái"
          style={{ width: 160 }}
          value={statusFilter}
          onChange={handleStatusChange}
          data-testid="status-filter"
          aria-label="Lọc theo trạng thái"
        >
          <Option value="ACTIVE">Hoạt động</Option>
          <Option value="LOCKED">Bị khóa</Option>
          <Option value="DISABLED">Vô hiệu</Option>
        </Select>

        <Select
          allowClear
          placeholder="Lọc loại xác thực"
          style={{ width: 160 }}
          value={providerTypeFilter}
          onChange={handleProviderTypeChange}
          data-testid="provider-type-filter"
          aria-label="Lọc theo loại xác thực"
        >
          <Option value="INTERNAL">Nội bộ</Option>
        </Select>
      </Space>

      {isLoading && (
        <div style={{ textAlign: 'center', padding: 48 }} data-testid="account-list-loading">
          <Spin size="large" />
        </div>
      )}

      {!isLoading && data && (
        <Table<AccountSummaryDto>
          dataSource={data.items}
          columns={columns}
          rowKey="accountId"
          data-testid="account-list-table"
          locale={{ emptyText: 'Không tìm thấy tài khoản nào.' }}
          pagination={{
            current: page,
            pageSize: PAGE_SIZE,
            total: data.totalCount,
            onChange: (p) => setPage(p),
            showTotal: (total) => `Tổng ${total} tài khoản`,
          }}
        />
      )}

      <Modal
        title="Tạo tài khoản mới"
        open={createModalOpen}
        onCancel={handleCloseCreateModal}
        maskClosable={false}
        footer={tempPassword ? [
          <Button key="close" type="primary" onClick={handleCloseCreateModal}>
            Đóng
          </Button>,
        ] : undefined}
        okText="Tạo"
        cancelText="Hủy"
        onOk={() => form.submit()}
        confirmLoading={creating}
        data-testid="create-account-modal"
      >
        {tempPassword ? (
          <Alert
            type="success"
            message="Tạo tài khoản thành công"
            description={
              <div>
                <Text>Mật khẩu tạm thời:</Text>
                <pre style={{ margin: '8px 0', padding: 8, background: '#f5f5f5', borderRadius: 4 }}>
                  {tempPassword}
                </pre>
                <Text type="warning">
                  Vui lòng sao chép mật khẩu này. Mật khẩu chỉ hiển thị một lần.
                  Người dùng sẽ phải đổi mật khẩu khi đăng nhập lần đầu.
                </Text>
              </div>
            }
            data-testid="temp-password-alert"
          />
        ) : (
          <Form
            form={form}
            layout="vertical"
            onFinish={handleCreateAccount}
          >
            <Form.Item
              name="companyId"
              label="Công ty"
              rules={[{ required: true, message: 'Vui lòng chọn công ty' }]}
            >
              <Select
                showSearch
                placeholder="Chọn công ty"
                loading={loadingUsers}
                optionFilterProp="label"
                onChange={handleCompanyChange}
                getPopupContainer={(trigger) => trigger.parentElement!}
                data-testid="company-select"
                options={companyOptions}
              />
            </Form.Item>
            <Form.Item
              name="userId"
              label="Người dùng"
              rules={[{ required: true, message: 'Vui lòng chọn người dùng' }]}
            >
              <Select
                showSearch
                placeholder={selectedCompanyId ? 'Chọn người dùng chưa có tài khoản' : 'Chọn công ty trước'}
                loading={loadingUsers}
                disabled={!selectedCompanyId}
                optionFilterProp="label"
                onChange={handleUserSelect}
                getPopupContainer={(trigger) => trigger.parentElement!}
                data-testid="user-select"
                notFoundContent={selectedCompanyId ? 'Không có người dùng chưa có tài khoản trong công ty này' : 'Vui lòng chọn công ty trước'}
                options={filteredUserOptions}
              />
            </Form.Item>
            <Form.Item
              name="providerSubject"
              label="Tên đăng nhập"
              rules={[
                { required: true, message: 'Vui lòng nhập tên đăng nhập' },
                { max: 200, message: 'Tối đa 200 ký tự' },
              ]}
            >
              <Input placeholder="Ví dụ: ten.dangnhap@domain.com" data-testid="provider-subject-input" />
            </Form.Item>
          </Form>
        )}
      </Modal>
    </div>
  );
};

export default AccountManagementPage;
