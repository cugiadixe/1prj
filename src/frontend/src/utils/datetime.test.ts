import { describe, it, expect } from 'vitest';
import { parseUtc, formatUtcDateTime, formatUtcDate } from './datetime';

/**
 * Bộ test TRỰC TIẾP cho tiện ích ngày giờ.
 *
 * Vì sao cần: các test ở tầng component đang so kết quả render với chính hàm này, nên chúng
 * KHÔNG thể phát hiện lỗi nằm bên trong hàm — cả hai vế cùng sai thì vẫn bằng nhau. Mà đây
 * đúng là chỗ đã từng gây lỗi lệch 7 tiếng (backend trả UTC nhưng thiếu hậu tố 'Z').
 *
 * Các test dưới đây so với MỐC UTC TUYỆT ĐỐI nên không phụ thuộc múi giờ máy chạy test.
 */
describe('parseUtc', () => {
  it('hiểu chuỗi THIẾU hậu tố Z là giờ UTC (đây là bẫy đã gây lệch 7 tiếng)', () => {
    const d = parseUtc('2026-08-14T06:33:03');
    expect(d.toISOString()).toBe('2026-08-14T06:33:03.000Z');
  });

  it('giữ nguyên khi chuỗi đã có Z', () => {
    expect(parseUtc('2026-08-14T06:33:03Z').toISOString()).toBe('2026-08-14T06:33:03.000Z');
  });

  it('không chèn thêm Z khi chuỗi đã có offset múi giờ', () => {
    // 06:33+07:00 = 23:33Z hôm trước. Nếu hàm chèn nhầm 'Z' thì kết quả sẽ sai hẳn.
    expect(parseUtc('2026-08-14T06:33:03+07:00').toISOString()).toBe('2026-08-13T23:33:03.000Z');
  });

  it('xử lý cả offset dạng không có dấu hai chấm', () => {
    expect(parseUtc('2026-08-14T06:33:03+0700').toISOString()).toBe('2026-08-13T23:33:03.000Z');
  });

  it('giữ mili giây', () => {
    expect(parseUtc('2026-08-14T06:33:03.120').toISOString()).toBe('2026-08-14T06:33:03.120Z');
  });
});

describe('formatUtcDateTime', () => {
  it('trả dấu gạch khi rỗng', () => {
    expect(formatUtcDateTime(null)).toBe('—');
    expect(formatUtcDateTime(undefined)).toBe('—');
    expect(formatUtcDateTime('')).toBe('—');
  });

  it('đổi cùng một mốc UTC ra cùng một giờ địa phương, bất kể có Z hay không', () => {
    // Không hard-code chuỗi giờ (phụ thuộc máy), nhưng khẳng định hai dạng phải TRÙNG nhau —
    // chính chỗ này vỡ khi hàm quên coi chuỗi thiếu Z là UTC.
    expect(formatUtcDateTime('2026-08-14T06:33:03')).toBe(formatUtcDateTime('2026-08-14T06:33:03Z'));
  });

  it('hai mốc cách nhau 1 giờ phải cho kết quả khác nhau', () => {
    expect(formatUtcDateTime('2026-08-14T06:00:00Z')).not.toBe(formatUtcDateTime('2026-08-14T07:00:00Z'));
  });

  it('trả lại nguyên chuỗi khi không phân tích được, thay vì ném lỗi', () => {
    expect(formatUtcDateTime('khong-phai-ngay')).toBe('khong-phai-ngay');
  });
});

describe('formatUtcDate', () => {
  it('trả dấu gạch khi rỗng', () => {
    expect(formatUtcDate(null)).toBe('—');
  });

  it('cùng mốc UTC thì có/không Z đều cho cùng kết quả', () => {
    expect(formatUtcDate('2026-08-14T06:33:03')).toBe(formatUtcDate('2026-08-14T06:33:03Z'));
  });
});
