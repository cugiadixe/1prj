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
      setSearchError('Please enter at least CCCD or Phone to search.');
      return;
    }

    setSearchError(null);
    searchMutation.mutate({ cccd, phone });
  };

  const columns = [
    {
      title: 'Customer ID',
      dataIndex: 'id',
      key: 'id',
    },
    {
      title: 'Customer Code',
      dataIndex: 'customerCode',
      key: 'customerCode',
    },
    {
      title: 'Full Name',
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
      title: 'Phone',
      dataIndex: 'phone',
      key: 'phone',
      render: (text: string | null) => text || '—',
    },
    {
      title: 'Status',
      dataIndex: 'customerStatus',
      key: 'customerStatus',
    },
    {
      title: 'Action',
      key: 'action',
      render: (_: unknown, record: CustomerListItem) => (
        <Link
          to={`/customers/merge/new?sourceCustomerId=${record.id}`}
          data-testid={`select-source-${record.id}`}
        >
          Select as Source
        </Link>
      ),
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
          Find Duplicate Customers
        </Title>
        <Button>
          <Link to="/customers">Back to Customers</Link>
        </Button>
      </Space>

      <Card title="Search Criteria" style={{ marginBottom: 16 }}>
        <Form
          form={form}
          layout="inline"
          onFinish={handleSearch}
          data-testid="duplicate-search-form"
        >
          <Form.Item name="cccd" label="CCCD">
            <Input
              placeholder="Enter CCCD"
              data-testid="input-search-cccd"
            />
          </Form.Item>
          <Form.Item name="phone" label="Phone">
            <Input
              placeholder="Enter phone number"
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
              Search
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
          message="No duplicate customers found."
          style={{ marginBottom: 16 }}
          data-testid="no-duplicates-message"
        />
      )}

      {result && result.hasDuplicates && (
        <Card title="Duplicate Candidates">
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
