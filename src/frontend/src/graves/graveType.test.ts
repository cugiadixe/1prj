import { describe, it, expect } from 'vitest';
import { graveTypeForCotCount, GRAVE_TYPE_FILTER } from './types';

describe('graveTypeForCotCount', () => {
  it('1 cốt → Mộ đơn (SINGLE)', () => {
    expect(graveTypeForCotCount(1)).toBe('SINGLE');
  });
  it('2 cốt → Mộ đôi (DOUBLE)', () => {
    expect(graveTypeForCotCount(2)).toBe('DOUBLE');
  });
  it('≥3 cốt → Mộ gia tộc (FAMILY)', () => {
    expect(graveTypeForCotCount(3)).toBe('FAMILY');
    expect(graveTypeForCotCount(5)).toBe('FAMILY');
  });
  it('giá trị bất thường (≤0 / NaN) coi như 1 cốt', () => {
    expect(graveTypeForCotCount(0)).toBe('SINGLE');
    expect(graveTypeForCotCount(-2)).toBe('SINGLE');
    expect(graveTypeForCotCount(Number.NaN)).toBe('SINGLE');
  });
  it('bộ lọc chỉ gồm 3 loại theo số cốt', () => {
    expect(Object.keys(GRAVE_TYPE_FILTER)).toEqual(['SINGLE', 'DOUBLE', 'FAMILY']);
  });
});
