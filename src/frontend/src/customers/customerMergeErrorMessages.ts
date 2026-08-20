// Ánh xạ lỗi gộp khách hàng sang thông báo tiếng Việt, thân thiện, KHÔNG lộ chi tiết kỹ thuật.
// Khoá là thông điệp lỗi thô từ backend (giữ nguyên tiếng Anh để khớp), giá trị là câu hiển thị.
export const MERGE_ERROR_MESSAGES: Record<string, string> = {
  'Source and target customer cannot be the same.':
    'Khách hàng nguồn và đích không được trùng nhau.',
  'Cannot merge a customer that is already merged.':
    'Khách hàng này đã được gộp trước đó, không thể gộp lại.',
  'Target customer must be active.':
    'Khách hàng đích (giữ lại) phải đang hoạt động.',
  'Cannot automatically merge overlapping company contexts. Manual resolution required.':
    'Hai khách hàng có quan hệ công ty chồng lấn. Cần xử lý thủ công trước khi gộp.',
  'One or both customers not found.':
    'Không tìm thấy một hoặc cả hai khách hàng.',
  'Only DRAFT merge requests can be submitted for approval.':
    'Chỉ yêu cầu gộp ở trạng thái Nháp mới có thể gửi duyệt.',
  'Failed to load saved request':
    'Không lưu được yêu cầu gộp. Vui lòng thử lại.',
};

export const MERGE_GENERIC_ERROR =
  'Có lỗi xảy ra. Vui lòng thử lại.';
export const MERGE_PERMISSION_DENIED =
  'Bạn không có quyền thực hiện thao tác này.';
export const MERGE_NOT_FOUND = 'Không tìm thấy yêu cầu gộp.';
export const MERGE_CONCURRENCY_ERROR =
  'Dữ liệu đã thay đổi kể từ khi bạn mở. Vui lòng tải lại và thử lại.';

export function getMergeErrorMessage(error: unknown): string {
  try {
    const err = error as {
      response?: {
        status?: number;
        data?: {
          title?: string;
          detail?: string;
        };
      };
    };

    const status = err?.response?.status;

    // Ưu tiên map theo NỘI DUNG lỗi nghiệp vụ (kể cả khi status là 400/403/409) để câu chữ rõ nghĩa
    // hơn thông báo chung theo mã. Ví dụ "đã gộp rồi" trả về từ backend dưới dạng 400.
    const detail = err?.response?.data?.detail;
    if (detail && typeof detail === 'string') {
      const mapped = MERGE_ERROR_MESSAGES[detail];
      if (mapped) return mapped;
      if (detail.startsWith('Concurrency conflict')) return MERGE_CONCURRENCY_ERROR;
    }

    if (status === 403) return MERGE_PERMISSION_DENIED;
    if (status === 404) return MERGE_NOT_FOUND;
    if (status === 409) return MERGE_CONCURRENCY_ERROR;
  } catch {
    // ignore
  }
  return MERGE_GENERIC_ERROR;
}

export function isMergePermissionDenied(error: unknown): boolean {
  try {
    const err = error as { response?: { status?: number } };
    return err?.response?.status === 403;
  } catch {
    return false;
  }
}
