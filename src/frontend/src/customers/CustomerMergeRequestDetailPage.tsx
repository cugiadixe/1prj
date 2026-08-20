import React from 'react';
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Popconfirm,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
  message,
} from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getMergeRequestById, submitMergeRequest } from './customerMergeApi';
import { getMergeErrorMessage } from './customerMergeErrorMessages';
const { Title } = Typography;

const STATUS_COLORS: Record<string, string> = {
  DRAFT: 'default',
  SUBMITTED: 'processing',
  APPROVED: 'blue',
  EXECUTED: 'green',
  REJECTED: 'red',
  WITHDRAWN: 'orange',
};

const CustomerMergeRequestDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();

  const {
    data: request,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['merge-request', id],
    queryFn: () => getMergeRequestById(id!),
    enabled: !!id,
  });

  const submitMutation = useMutation({
    mutationFn: () => submitMergeRequest(id!),
    onSuccess: (result) => {
      message.success(
        result.requestStatus === 'EXECUTED'
          ? 'Đã duyệt và gộp xong: dữ liệu KH nguồn đã dồn về KH đích.'
          : 'Đã gửi duyệt yêu cầu gộp. Chờ người có thẩm quyền phê duyệt.',
      );
      queryClient.invalidateQueries({ queryKey: ['merge-request', id] });
    },
    onError: (err) => {
      message.error(getMergeErrorMessage(err));
    },
  });

  if (isLoading) {
    return <Spin data-testid="loading-spinner" />;
  }

  if (error || !request) {
    return (
      <Alert
        type="error"
        message={getMergeErrorMessage(error)}
        data-testid="error-alert"
      />
    );
  }

  const candidateColumns = [
    {
      title: 'Mã KH ứng viên',
      dataIndex: 'candidateCustomerId',
      key: 'candidateCustomerId',
    },
    {
      title: 'Loại khớp',
      dataIndex: 'matchType',
      key: 'matchType',
    },
    {
      title: 'Độ tin cậy',
      dataIndex: 'matchConfidence',
      key: 'matchConfidence',
      render: (val: number | null) =>
        val !== null ? `${val}%` : '—',
    },
  ];

  return (
    <div data-testid="customer-merge-request-detail-page">
      <Space
        style={{
          marginBottom: 16,
          width: '100%',
          justifyContent: 'space-between',
        }}
      >
        <Title level={4} style={{ margin: 0 }}>
          Chi tiết yêu cầu gộp
        </Title>
        <Space>
          {request.requestStatus === 'DRAFT' && (
            <Popconfirm
              title="Gửi duyệt yêu cầu gộp"
              description="Yêu cầu sẽ vào luồng duyệt. Sau khi duyệt, dữ liệu KH nguồn sẽ dồn về KH đích và không thể hoàn tác."
              okText="Gửi duyệt"
              cancelText="Huỷ"
              onConfirm={() => submitMutation.mutate()}
            >
              <Button
                type="primary"
                loading={submitMutation.isPending}
                data-testid="submit-merge-btn"
              >
                Gửi duyệt
              </Button>
            </Popconfirm>
          )}
          {request.workflowInstanceId && (
            <Button>
              <Link
                to={`/workflow/instances/${request.workflowInstanceId}`}
              >
                Xem quy trình
              </Link>
            </Button>
          )}
          <Button>
            <Link to={`/customers/${request.sourceCustomerId}`}>
              Xem KH nguồn
            </Link>
          </Button>
          <Button type="primary">
            <Link to={`/customers/${request.targetCustomerId}`}>
              Xem KH đích
            </Link>
          </Button>
          <Button>
            <Link to="/customers/merge-requests">
              Quay lại yêu cầu gộp
            </Link>
          </Button>
        </Space>
      </Space>

      <Card title="Trạng thái yêu cầu" style={{ marginBottom: 16 }}>
        <Descriptions bordered column={2}>
          <Descriptions.Item label="Mã yêu cầu">
            {request.id}
          </Descriptions.Item>
          <Descriptions.Item label="Trạng thái">
            <Tag
              color={STATUS_COLORS[request.requestStatus] || 'default'}
              data-testid="status-tag"
            >
              {request.requestStatus}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Mã KH nguồn">
            {request.sourceCustomerId}
          </Descriptions.Item>
          <Descriptions.Item label="Mã KH đích">
            {request.targetCustomerId}
          </Descriptions.Item>
          <Descriptions.Item label="Người yêu cầu">
            {request.requesterId}
          </Descriptions.Item>
          <Descriptions.Item label="Ngày tạo">
            {new Date(request.createdAt).toLocaleDateString('vi-VN')}
          </Descriptions.Item>
          <Descriptions.Item label="Cập nhật lần cuối">
            {request.updatedAt
              ? new Date(request.updatedAt).toLocaleDateString('vi-VN')
              : 'N/A'}
          </Descriptions.Item>
          <Descriptions.Item label="Phiên quy trình">
            {request.workflowInstanceId || 'Chưa liên kết'}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {request.candidates && request.candidates.length > 0 && (
        <Card title="Ứng viên" style={{ marginBottom: 16 }}>
          <Table
            columns={candidateColumns}
            dataSource={request.candidates}
            rowKey="candidateCustomerId"
            pagination={false}
            data-testid="candidates-table"
          />
        </Card>
      )}
    </div>
  );
};

export default CustomerMergeRequestDetailPage;
