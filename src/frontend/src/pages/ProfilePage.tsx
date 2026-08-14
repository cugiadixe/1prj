import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Card,
  Descriptions,
  Table,
  Tag,
  Typography,
  Button,
  Space,
  Alert,
} from 'antd';
import { KeyOutlined, UserOutlined, HistoryOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { useAuth } from '../auth/AuthProvider';
import {
  apiFetchMyActivity,
  apiFetchMyProfile,
  type MyActivityEventDto,
} from '../auth/authApi';

const { Title, Text } = Typography;

/** Nhãn tiếng Việt cho mã sự kiện audit. Không khớp thì hiển thị mã gốc. */
const EVENT_LABELS: Record<string, string> = {
  LOGIN_SUCCEEDED: 'Đăng nhập thành công',
  LOGIN_SUCCESS: 'Đăng nhập thành công',
  LOGIN_FAILED: 'Đăng nhập thất bại',
  LOGOUT: 'Đăng xuất',
  PASSWORD_CHANGED: 'Đổi mật khẩu',
  PASSWORD_CHANGE: 'Đổi mật khẩu',
  TOKEN_REFRESHED: 'Làm mới phiên',
  PERMISSION_GRANTED: 'Cấp quyền',
  PERMISSION_REVOKED: 'Thu hồi quyền',
  ROLE_ASSIGNED: 'Gán vai trò',
  ROLE_REMOVED: 'Gỡ vai trò',
  USER_CREATED: 'Tạo người dùng',
  USER_UPDATED: 'Cập nhật người dùng',
  ACCOUNT_CREATED: 'Tạo tài khoản',
  // Quy trình phê duyệt
  WORKFLOW_SUBMITTED: 'Gửi yêu cầu phê duyệt',
  WORKFLOW_INSTANCE_CREATED: 'Tạo quy trình phê duyệt',
  WORKFLOW_APPROVED: 'Phê duyệt',
  WORKFLOW_REJECTED: 'Từ chối duyệt',
  WORKFLOW_EXECUTED: 'Thực thi quy trình',
  // Gói dịch vụ / chăm sóc khách hàng
  CARE_PACKAGE_ASSIGNED: 'Gán gói dịch vụ',
  CARE_PACKAGE_SUBMIT_APPROVAL: 'Gửi duyệt gán gói dịch vụ',
  CARE_PACKAGE_APPROVAL_EXECUTED: 'Đã duyệt gán gói dịch vụ',
  CARE_PACKAGE_ASSIGN_GRAVE: 'Gán gói dịch vụ vào phần mộ',
};

function eventLabel(code: string): string {
  return EVENT_LABELS[code] ?? code;
}

/** Nhãn tiếng Việt cho loại đối tượng (entity_type). Không khớp thì giữ nguyên. */
const ENTITY_LABELS: Record<string, string> = {
  CustomerCarePackage: 'Gói dịch vụ của khách',
  WorkflowInstance: 'Quy trình phê duyệt',
  AUTH_ACCOUNT: 'Tài khoản',
  AuthAccount: 'Tài khoản',
  User: 'Người dùng',
  Customer: 'Khách hàng',
  Grave: 'Phần mộ',
  Permission: 'Quyền',
  Role: 'Vai trò',
};

function entityText(entityType: string, entityId: string | null): string {
  if (!entityType) return '—';
  const label = ENTITY_LABELS[entityType] ?? entityType;
  return entityId ? `${label} #${entityId}` : label;
}

/**
 * created_at là UTC. Backend serialize DateTime (Kind=Unspecified) KHÔNG kèm 'Z',
 * nên JS sẽ hiểu nhầm là giờ local → lệch. Thêm 'Z' khi chuỗi chưa có múi giờ.
 */
function parseUtc(iso: string): Date {
  const hasTz = /[zZ]$|[+-]\d{2}:?\d{2}$/.test(iso);
  return new Date(hasTz ? iso : `${iso}Z`);
}

function outcomeTag(outcome: string): React.ReactNode {
  const up = (outcome || '').toUpperCase();
  if (up === 'SUCCESS' || up === 'SUCCEEDED' || up === 'ALLOWED') {
    return <Tag color="green">Thành công</Tag>;
  }
  if (up === 'FAILURE' || up === 'FAILED' || up === 'DENIED') {
    return <Tag color="red">Thất bại</Tag>;
  }
  return <Tag>{outcome || '—'}</Tag>;
}

function formatDateTime(iso: string): string {
  try {
    const d = parseUtc(iso);
    return d.toLocaleString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return iso;
  }
}

const ProfilePage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const { data: profile } = useQuery({
    queryKey: ['my-profile'],
    queryFn: apiFetchMyProfile,
    staleTime: 60000,
  });

  const {
    data,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['my-activity'],
    queryFn: () => apiFetchMyActivity(1, 50),
    staleTime: 30000,
  });

  const fullName = profile?.fullName ?? user?.displayName ?? '—';

  const columns: ColumnsType<MyActivityEventDto> = [
    {
      title: 'Thời gian',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (v: string) => formatDateTime(v),
    },
    {
      title: 'Hoạt động',
      dataIndex: 'eventCode',
      key: 'eventCode',
      render: (v: string) => eventLabel(v),
    },
    {
      title: 'Đối tượng',
      key: 'entity',
      render: (_: unknown, r: MyActivityEventDto) =>
        r.entityLabel && r.entityLabel.trim() !== ''
          ? r.entityLabel
          : entityText(r.entityType, r.entityId),
    },
    {
      title: 'Kết quả',
      dataIndex: 'outcome',
      key: 'outcome',
      width: 120,
      render: (v: string) => outcomeTag(v),
    },
  ];

  return (
    <div style={{ maxWidth: 960, margin: '0 auto' }}>
      <Title level={3} style={{ marginBottom: 24 }}>
        <UserOutlined style={{ marginRight: 8 }} />
        Trang cá nhân
      </Title>

      <Card style={{ marginBottom: 24 }}>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
            flexWrap: 'wrap',
            gap: 16,
          }}
        >
          <Descriptions column={1} style={{ flex: 1, minWidth: 260 }}>
            <Descriptions.Item label="Họ tên">
              <Text strong>{fullName}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Tài khoản">
              {profile?.username ?? user?.username ?? '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Mã nhân viên">
              {profile?.employeeCode ?? '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Công ty">{profile?.companyName ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Phòng ban">{profile?.departmentName ?? '—'}</Descriptions.Item>
          </Descriptions>

          <Space>
            <Button
              icon={<KeyOutlined />}
              onClick={() => navigate('/change-password')}
              data-testid="profile-change-password"
            >
              Đổi mật khẩu
            </Button>
          </Space>
        </div>
      </Card>

      <Card
        title={
          <span>
            <HistoryOutlined style={{ marginRight: 8 }} />
            Lịch sử thao tác gần đây
          </span>
        }
      >
        {isError ? (
          <Alert
            type="error"
            showIcon
            message="Không tải được lịch sử thao tác."
            description="Vui lòng thử lại sau."
          />
        ) : (
          <Table<MyActivityEventDto>
            rowKey="id"
            size="small"
            loading={isLoading}
            columns={columns}
            dataSource={data?.items ?? []}
            pagination={{ pageSize: 15, showSizeChanger: false }}
            locale={{ emptyText: 'Chưa có hoạt động nào được ghi nhận.' }}
          />
        )}
      </Card>
    </div>
  );
};

export default ProfilePage;
