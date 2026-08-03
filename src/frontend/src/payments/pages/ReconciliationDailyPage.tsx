import React, { useState } from 'react';
import { Alert, Button, DatePicker, Space, Spin, Table, Tag, Typography, message } from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usePermissions } from '../../auth/AuthProvider';
import { getDailyReport, prepareReconciliation, confirmReconciliation } from '../reconciliationApi';
import { getErrorMessage, isPermissionDenied, isConcurrencyError } from '../errorMessages';
import dayjs from 'dayjs';

const { Title } = Typography;

const ReconciliationDailyPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const queryClient = useQueryClient();
  const [companyId] = useState<number>(1);
  const [date, setDate] = useState<string>(dayjs().format('YYYY-MM-DD'));

  const { data, isLoading, error } = useQuery({
    queryKey: ['reconciliation-daily', companyId, date],
    queryFn: () => getDailyReport(companyId, date),
  });

  const prepareMutation = useMutation({
    mutationFn: (args: { periodId: number, rowVersion: string }) =>
      prepareReconciliation(args.periodId, { rowVersion: args.rowVersion }),
    onSuccess: () => {
      message.success('Reconciliation period prepared.');
      queryClient.invalidateQueries({ queryKey: ['reconciliation-daily', companyId, date] });
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        message.error('Data has changed since you started. Please refresh and try again.');
      } else {
        message.error(getErrorMessage(err));
      }
    }
  });

  const confirmMutation = useMutation({
    mutationFn: (args: { periodId: number, rowVersion: string }) =>
      confirmReconciliation(args.periodId, { rowVersion: args.rowVersion }),
    onSuccess: () => {
      message.success('Reconciliation period confirmed.');
      queryClient.invalidateQueries({ queryKey: ['reconciliation-daily', companyId, date] });
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        message.error('Data has changed since you started. Please refresh and try again.');
      } else {
        message.error(getErrorMessage(err));
      }
    }
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view reconciliation reports."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    { title: 'Bill Code', dataIndex: 'billCode', key: 'billCode' },
    { title: 'Payment Method', dataIndex: 'paymentMethod', key: 'paymentMethod' },
    {
      title: 'Total Amount',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (val: number) => `${val.toLocaleString()} VND`
    },
    { title: 'Status', dataIndex: 'status', key: 'status' },
  ];

  return (
    <div data-testid="reconciliation-daily-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Daily Reconciliation Report</Title>
        <Space>
          <DatePicker
            value={dayjs(date)}
            onChange={(d) => setDate(d ? d.format('YYYY-MM-DD') : dayjs().format('YYYY-MM-DD'))}
            data-testid="date-picker"
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
            <strong>Period Status: </strong>
            <Tag color={data.period?.status === 'CONFIRMED' ? 'green' : data.period?.status === 'PREPARED' ? 'blue' : 'default'}>
              {data.period?.status || 'UNPREPARED'}
            </Tag>
            <strong style={{ marginLeft: 16 }}>Total Amount: </strong>
            {`${data.totalAmount.toLocaleString()} VND`}
            <strong style={{ marginLeft: 16 }}>Transactions: </strong>
            {data.transactionCount}

            <Space style={{ marginLeft: 32 }}>
              {data.period && data.period.status === 'PREPARED' && hasPermission('RECONCILIATION_CONFIRM', 'GLOBAL') && (
                <Button
                  type="primary"
                  data-testid="confirm-recon-btn"
                  loading={confirmMutation.isPending}
                  onClick={() => confirmMutation.mutate({ periodId: data.period!.id, rowVersion: data.period!.rowVersion })}
                >
                  Confirm Reconciliation
                </Button>
              )}
              {data.period && data.period.status !== 'CONFIRMED' && data.period.status !== 'PREPARED' && hasPermission('RECONCILIATION_PREPARE', 'GLOBAL') && (
                <Button
                  type="primary"
                  data-testid="prepare-recon-btn"
                  loading={prepareMutation.isPending}
                  onClick={() => prepareMutation.mutate({ periodId: data.period!.id, rowVersion: data.period!.rowVersion })}
                >
                  Prepare Reconciliation
                </Button>
              )}
            </Space>
          </div>

          <Table
            dataSource={data.payments}
            columns={columns}
            rowKey="id"
            pagination={false}
            data-testid="reconciliation-payments-table"
          />
        </>
      )}
    </div>
  );
};

export default ReconciliationDailyPage;
