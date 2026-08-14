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

/** Ngày + giờ theo giờ VN. Rỗng/không hợp lệ → '—'. */
export function formatUtcDateTime(iso: string | null | undefined): string {
  if (!iso) return '—';
  try {
    return parseUtc(iso).toLocaleString('vi-VN');
  } catch {
    return iso;
  }
}

/** Chỉ ngày theo giờ VN. Rỗng/không hợp lệ → '—'. */
export function formatUtcDate(iso: string | null | undefined): string {
  if (!iso) return '—';
  try {
    return parseUtc(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}
