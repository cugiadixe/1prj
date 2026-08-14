import React from 'react';
import { Select, Spin } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { getApproverSourceOptions } from './workflowApi';

interface Props {
  /** Loại nguồn đang chọn ở ô phía trên. Chưa chọn thì ô này bị khoá. */
  sourceType?: string;
  /** antd Form tự truyền value/onChange khi component nằm trong Form.Item. */
  value?: string;
  onChange?: (value: string) => void;
}

/**
 * Ô chọn "giá trị nguồn" của luật người duyệt.
 *
 * Trước đây đây là ô nhập tự do: admin phải nhớ và gõ đúng SỐ ID người dùng / phòng ban,
 * hoặc mã vai trò / nhóm quản trị. Gõ sai thì không có lỗi nào báo lúc cấu hình — mãi tới khi
 * nhân viên gửi duyệt mới phát hiện "không tìm được người duyệt" và hồ sơ bị chặn.
 * Nay danh sách được nạp theo đúng loại nguồn để admin chọn.
 */
const ApproverSourceValueInput: React.FC<Props> = ({ sourceType, value, onChange }) => {
  const { data: options, isLoading } = useQuery({
    queryKey: ['approver-source-options', sourceType],
    queryFn: () => getApproverSourceOptions(sourceType!),
    enabled: !!sourceType,
    staleTime: 60000,
  });

  if (!sourceType) {
    return <Select placeholder="Chọn loại nguồn ở trên trước" disabled data-testid="approver-source-value" />;
  }

  return (
    <Select
      showSearch
      allowClear
      value={value}
      onChange={(v) => onChange?.(v)}
      loading={isLoading}
      notFoundContent={isLoading ? <Spin size="small" /> : 'Không có lựa chọn nào'}
      placeholder="Chọn giá trị"
      data-testid="approver-source-value"
      optionFilterProp="label"
      options={(options ?? []).map((o) => ({
        value: o.value,
        label: o.hint ? `${o.label} — ${o.hint}` : o.label,
      }))}
    />
  );
};

export default ApproverSourceValueInput;
