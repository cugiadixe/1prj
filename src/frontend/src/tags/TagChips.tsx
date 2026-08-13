import React from 'react';
import { Tag as AntTag } from 'antd';
import type { Tag } from './types';
import { DEFAULT_TAG_COLOR } from './types';

interface Props {
  tags: Tag[] | undefined;
  emptyText?: string;
  size?: 'small' | 'default';
}

/** Hiển thị danh sách thẻ dưới dạng chip màu (chỉ đọc). Tên thẻ có tiền tố #. */
const TagChips: React.FC<Props> = ({ tags, emptyText = '—', size = 'default' }) => {
  if (!tags || tags.length === 0) return <span style={{ color: '#999' }}>{emptyText}</span>;
  return (
    <span style={{ display: 'inline-flex', flexWrap: 'wrap', gap: 4 }}>
      {tags.map((t) => (
        <AntTag
          key={t.id}
          color={t.color ?? DEFAULT_TAG_COLOR}
          style={size === 'small' ? { marginInlineEnd: 0, fontSize: 12 } : { marginInlineEnd: 0 }}
        >
          #{t.name}
        </AntTag>
      ))}
    </span>
  );
};

export default TagChips;
