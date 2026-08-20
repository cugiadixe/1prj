import React, { useState } from 'react';
import { Alert, Button, Card, Form, Input, Space, Table, Typography } from 'antd';
import { useMutation } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { findMergeDuplicates } from './customerMergeApi';
import { getMergeErrorMessage } from './customerMergeErrorMessages';
import type { CustomerListItem, DuplicateCheckResult } from './types';
import type { MergeDuplicateSearchParams } from './customerMergeTypes';

const { Title } = Typography;

const CustomerMergeDuplicateSearchPage: React.FC = () => {
  const [form] = Form.useForm();
  const [searchError, setSearchError] = useState<string | null>(null);
  const [result, setResult] = useState<DuplicateCheckResult | null>(null);

  const searchMutation = useMutation({
    mutationFn: (params: MergeDuplicateSearchParams) =>
      findMergeDuplicates(params),
    onSuccess: (data) => {
      setResult(data);
      setSearchError(null);
    },
    onError: (err) => {
      setSearchError(getMergeErrorMessage(err));
      setResult(null);
    },
  });

  const handleSearch = (values: Record<string, unknown>) => {
    const cccd = (values.cccd as string)?.trim() || undefined;
    const phone = (values.phone as string)?.trim() || undefined;

    if (!cccd && !phone) {
      setSearchError('Vui lòng nhập ít nhất CCCD hoặc Điện thoại để tìm kiếm.');
      return;
    }

    setSearchError(null);
    searchMutation.mutate({ cccd, phone });
  };

  const columns = [
    {
      title: 'Mã KH',
      dataIndex: 'id',
      key: 'id',
    },
    {
      title: 'Mã khách hàng',
      dataIndex: 'customerCode',
      key: 'customerCode',
    },
    {
      title: 'Họ tên',
      dataIndex: 'fullName',
      key: 'fullName',
    },
    {
      title: 'CCCD',
      dataIndex: 'cccd',
      key: 'cccd',
      render: (text: string | null) => text || '—',
    },
    {
      title: 'Điện thoại',
      dataIndex: 'phone',
      key: 'phone',
      render: (text: string | null) => text || '—',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'customerStatus',
      key: 'customerStatus',
    },
    {
      title: 'Thao tác',
      key: 'action',
      render: (_: unknown, record: CustomerListItem) => {
        // Nếu còn ĐÚNG một ứng viên khác thì tự chọn nó làm KH đích (giữ lại) để prefill trang tạo gộp.
        const others = (result?.matches ?? []).filter((m) => m.id !== record.id);
        const targetParam = others.length === 1 ? `&targetCustomerId=${others[0].id}` : '';
        return (
          <Link
            to={`/customers/merge/new?sourceCustomerId=${record.id}${targetParam}`}
            data-testid={`select-source-${record.id}`}
          >
            Chọn làm nguồn
          </Link>
        );
      },
    },
  ];

  return (
    <div data-testid="customer-merge-duplicate-search-page">
      <Space
        style={{
          marginBottom: 16,
          width: '100%',
          justifyContent: 'space-between',
        }}
      >
        <Title level={4} style={{ margin: 0 }}>
          Tìm khách hàng trùng lặp
        </Title>
        <Button>
          <Link to="/customers">Quay lại khách hàng</Link>
        </Button>
      </Space>

      <Card title="Tiêu chí tìm kiếm" style={{ marginBottom: 16 }}>
        <Form
          form={form}
          layout="inline"
          onFinish={handleSearch}
          data-testid="duplicate-search-form"
        >
          <Form.Item name="cccd" label="CCCD">
            <Input
              placeholder="Nhập CCCD"
              data-testid="input-search-cccd"
            />
          </Form.Item>
          <Form.Item name="phone" label="Điện thoại">
            <Input
              placeholder="Nhập số điện thoại"
              data-testid="input-search-phone"
            />
          </Form.Item>
          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              loading={searchMutation.isPending}
              data-testid="search-duplicates-button"
            >
              Tìm kiếm
            </Button>
          </Form.Item>
        </Form>
      </Card>

      {searchError && (
        <Alert
          type="error"
          message={searchError}
          closable
          onClose={() => setSearchError(null)}
          style={{ marginBottom: 16 }}
          data-testid="search-error"
        />
      )}

      {result && !result.hasDuplicates && (
        <Alert
          type="info"
          message="Không tìm thấy khách hàng trùng lặp."
          style={{ marginBottom: 16 }}
          data-testid="no-duplicates-message"
        />
      )}

      {result && result.hasDuplicates && (
        <Card title="Ứng viên trùng lặp">
          <Table
            columns={columns}
            dataSource={result.matches}
            rowKey="id"
            pagination={false}
            data-testid="duplicate-results-table"
          />
        </Card>
      )}
    </div>
  );
};

export default CustomerMergeDuplicateSearchPage;
