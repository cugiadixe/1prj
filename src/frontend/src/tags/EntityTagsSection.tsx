import React, { useState } from 'react';
import { Button, Card, Select, Space, Tag as AntTag, message } from 'antd';
import { EditOutlined, TagsOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { listTags } from './tagsApi';
import type { SetEntityTagsRequest, Tag, TagType } from './types';
import { DEFAULT_TAG_COLOR } from './types';
import TagChips from './TagChips';

interface Props {
  tagType: TagType;
  tags: Tag[] | undefined;          // thẻ hiện tại trên đối tượng (lấy từ dữ liệu chi tiết)
  canManage: boolean;
  onSave: (req: SetEntityTagsRequest) => Promise<unknown>;
  onSaved: () => void;              // ví dụ: invalidate query chi tiết
  title?: string;
  testId?: string;
}

/** Card "Thẻ" dùng chung: hiển thị chip + sửa bằng Select mode="tags" (gõ để tạo/chọn thẻ). */
const EntityTagsSection: React.FC<Props> = ({
  tagType, tags, canManage, onSave, onSaved, title = 'Thẻ', testId,
}) => {
  const [editing, setEditing] = useState(false);
  const [selected, setSelected] = useState<string[]>([]);

  const { data: catalog } = useQuery({
    queryKey: ['tags', tagType],
    queryFn: () => listTags(tagType),
    enabled: editing,
  });

  const colorByName: Record<string, string> = {};
  (catalog ?? []).forEach((t) => { colorByName[t.name.toLowerCase()] = t.color ?? DEFAULT_TAG_COLOR; });
  (tags ?? []).forEach((t) => { colorByName[t.name.toLowerCase()] = t.color ?? DEFAULT_TAG_COLOR; });

  const saveMutation = useMutation({
    mutationFn: () => onSave({ tagIds: [], newTagNames: selected.map((s) => s.replace(/^#/, '').trim()).filter(Boolean) }),
    onSuccess: () => {
      message.success('Đã cập nhật thẻ');
      setEditing(false);
      onSaved();
    },
    onError: (err: unknown) => {
      const e = err as { response?: { data?: { detail?: string } } };
      message.error(e.response?.data?.detail ?? 'Lưu thẻ thất bại');
    },
  });

  const startEdit = () => {
    setSelected((tags ?? []).map((t) => t.name));
    setEditing(true);
  };

  return (
    <Card
      title={<Space><TagsOutlined />{title}</Space>}
      style={{ marginTop: 16 }}
      data-testid={testId}
      extra={canManage && !editing && (
        <Button size="small" icon={<EditOutlined />} onClick={startEdit} data-testid={testId ? `${testId}-edit` : undefined}>
          {tags && tags.length > 0 ? 'Sửa thẻ' : 'Thêm thẻ'}
        </Button>
      )}
    >
      {editing ? (
        <Space direction="vertical" style={{ width: '100%' }}>
          <Select
            mode="tags"
            style={{ width: '100%' }}
            placeholder="Gõ để tạo thẻ mới hoặc chọn thẻ có sẵn..."
            value={selected}
            onChange={(v: string[]) => setSelected(v)}
            options={(catalog ?? []).map((t) => ({ value: t.name, label: `#${t.name}` }))}
            tokenSeparators={[',']}
            tagRender={(props) => {
              const { label, value, closable, onClose } = props;
              const name = String(value).replace(/^#/, '').toLowerCase();
              return (
                <AntTag color={colorByName[name] ?? DEFAULT_TAG_COLOR} closable={closable} onClose={onClose}
                  style={{ marginInlineEnd: 4 }}>
                  {label}
                </AntTag>
              );
            }}
            data-testid={testId ? `${testId}-select` : undefined}
          />
          <Space>
            <Button type="primary" loading={saveMutation.isPending} onClick={() => saveMutation.mutate()}
              data-testid={testId ? `${testId}-save` : undefined}>
              Lưu
            </Button>
            <Button onClick={() => setEditing(false)}>Hủy</Button>
          </Space>
        </Space>
      ) : (
        <TagChips tags={tags} emptyText="Chưa có thẻ nào." />
      )}
    </Card>
  );
};

export default EntityTagsSection;
