/**
 * Tiện ích hiển thị ngày giờ.
 *
 * Backend serialize DateTime (Kind=Unspecified, giá trị là UTC) KHÔNG kèm hậu tố 'Z'
 * hay offset, nên `new Date('2026-08-14T06:33:03')` bị JS hiểu là giờ LOCAL → lệch
 * đúng bằng chênh múi giờ (VN +7). Các hàm dưới tự chèn 'Z' khi chuỗi chưa có múi giờ
 * để luôn diễn giải là UTC, rồi mới đổi sang giờ máy người dùng.
 */

function hasTimezone(iso: string): boolean {
  return /[zZ]$|[+-]\d{2}:?\d{2}$/.test(iso);
}

/** Chuỗi ISO (UTC, có thể thiếu 'Z') → Date đúng mốc UTC. */
export function parseUtc(iso: string): Date {
  return new Date(hasTimezone(iso) ? iso : `${iso}Z`);
}

/**
 * Ngày + giờ theo giờ máy người dùng. Rỗng → '—'.
 *
 * Chuỗi không phân tích được thì trả lại NGUYÊN VĂN. Lưu ý: `toLocaleString` trên một Date
 * không hợp lệ KHÔNG ném lỗi mà trả về chuỗi 'Invalid Date', nên phải tự kiểm bằng
 * `isNaN(getTime())` — chỉ dựa vào try/catch là khối catch không bao giờ chạy và người dùng
 * sẽ thấy chữ 'Invalid Date' trên màn hình.
 */
export function formatUtcDateTime(iso: string | null | undefined): string {
  if (!iso) return '—';
  const d = parseUtc(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString('vi-VN');
}

/** Chỉ ngày. Rỗng → '—'; không phân tích được → trả lại nguyên văn (xem ghi chú ở trên). */
export function formatUtcDate(iso: string | null | undefined): string {
  if (!iso) return '—';
  const d = parseUtc(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString('vi-VN');
}
