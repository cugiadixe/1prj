import React, { useEffect, useMemo, useState } from 'react';
import { Select, Spin } from 'antd';
import { useQuery } from '@tanstack/react-query';

export interface RemoteSelectOption {
  value: number | string;
  label: string;
}

interface RemoteSelectProps {
  value?: number | string;
  onChange?: (value: number | string | undefined) => void;
  placeholder?: string;
  disabled?: boolean;
  allowClear?: boolean;
  /** Base cho queryKey của react-query; từ khoá tìm kiếm được nối vào cuối. */
  queryKey: (string | number | undefined | null)[];
  /** Hàm gọi API trả về danh sách lựa chọn theo từ khoá tìm kiếm. */
  fetchOptions: (search: string) => Promise<RemoteSelectOption[]>;
  'data-testid'?: string;
}

/**
 * Ô tìm-và-chọn (search select) lấy dữ liệu từ server có gõ tìm kiếm + chống dội (debounce).
 * Dùng thay cho việc bắt người dùng gõ ID thô.
 * Giữ lại nhãn của mục đã chọn kể cả khi nó không còn trong kết quả tìm kiếm hiện tại.
 */
const RemoteSelect: React.FC<RemoteSelectProps> = ({
  value,
  onChange,
  placeholder,
  disabled,
  allowClear = true,
  queryKey,
  fetchOptions,
  ...rest
}) => {
  const [search, setSearch] = useState('');
  const [debounced, setDebounced] = useState('');
  const [selectedOption, setSelectedOption] = useState<RemoteSelectOption | null>(null);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  const { data, isFetching } = useQuery({
    queryKey: [...queryKey, debounced],
    queryFn: () => fetchOptions(debounced),
    enabled: !disabled,
  });

  // Nếu value bị xoá từ bên ngoài (vd: đổi khách hàng làm reset dịch vụ) thì bỏ nhãn đã lưu.
  useEffect(() => {
    if (value === undefined || value === null) {
      setSelectedOption(null);
    }
  }, [value]);

  const options = useMemo(() => {
    const base = data ?? [];
    if (selectedOption && !base.some((o) => o.value === selectedOption.value)) {
      return [selectedOption, ...base];
    }
    return base;
  }, [data, selectedOption]);

  return (
    <Select
      showSearch
      filterOption={false}
      value={value}
      placeholder={placeholder}
      disabled={disabled}
      allowClear={allowClear}
      onSearch={setSearch}
      onChange={(val, option) => {
        if (val === undefined || val === null) {
          setSelectedOption(null);
        } else {
          setSelectedOption((option as RemoteSelectOption) ?? null);
        }
        onChange?.(val);
      }}
      notFoundContent={isFetching ? <Spin size="small" /> : null}
      loading={isFetching}
      options={options}
      style={{ width: '100%' }}
      {...rest}
    />
  );
};

export default RemoteSelect;
