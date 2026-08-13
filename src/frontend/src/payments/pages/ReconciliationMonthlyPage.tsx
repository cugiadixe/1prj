import React, { useState } from 'react';
import { Alert, DatePicker, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { getMonthlyReport } from '../reconciliationApi';
import { getErrorMessage, isPermissionDenied } from '../errorMessages';
import dayjs from 'dayjs';

const { Title } = Typography;

const ReconciliationMonthlyPage: React.FC = () => {

  const [companyId] = useState<number>(1);
  const [date, setDate] = useState<dayjs.Dayjs>(dayjs());

  const year = date.year();
  const month = date.month() + 1;

  const { data, isLoading, error } = useQuery({
    queryKey: ['reconciliation-monthly', companyId, year, month],
    queryFn: () => getMonthlyReport(companyId, year, month),
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem báo cáo đối soát."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Ngày',
      dataIndex: 'date',
      key: 'date',
      render: (val: string) => new Date(val).toLocaleDateString('vi-VN')
    },
    {
      title: 'Tổng số tiền',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (val: number) => `${val.toLocaleString()} VND`
    },
    { title: 'Số giao dịch', dataIndex: 'transactionCount', key: 'transactionCount' },
    {
      title: 'Trạng thái',
      dataIndex: 'periodStatus',
      key: 'periodStatus',
      render: (val: string | undefined) => {
        const status = val || 'UNPREPARED';
        let color = 'default';
        if (status === 'CONFIRMED') color = 'green';
        if (status === 'PREPARED') color = 'blue';
        return <Tag color={color}>{status}</Tag>;
      }
    },
  ];

  return (
    <div data-testid="reconciliation-monthly-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Báo cáo đối soát hàng tháng</Title>
        <Space>
          <DatePicker
            picker="month"
            value={date}
            onChange={(d) => setDate(d || dayjs())}
            data-testid="month-picker"
          />
        </Space>
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="reconciliation-error"
        />
      )}

      {isLoading && <Spin data-testid="reconciliation-loading" />}

      {data && (
        <>
          <div style={{ marginBottom: 16 }}>
            <strong>Tổng số tiền tháng: </strong>
            {`${data.monthlyTotalAmount.toLocaleString()} VND`}
            <strong style={{ marginLeft: 16 }}>Số giao dịch tháng: </strong>
            {data.monthlyTransactionCount}
          </div>

          <Table
            dataSource={data.dailySummaries}
            columns={columns}
            rowKey="date"
            pagination={false}
            data-testid="reconciliation-monthly-table"
          />
        </>
      )}
    </div>
  );
};

export default ReconciliationMonthlyPage;
