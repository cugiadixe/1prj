export type TagType = 'CUSTOMER' | 'GRAVE';

export interface Tag {
  id: number;
  tagType: TagType;
  name: string;
  color: string | null;
  isActive: boolean;
  usageCount?: number;
  rowVersion?: string;
}

export interface SetEntityTagsRequest {
  tagIds: number[];
  newTagNames: string[];
}

// Màu mặc định khi thẻ chưa có màu (an toàn cho antd Tag).
export const DEFAULT_TAG_COLOR = 'blue';
