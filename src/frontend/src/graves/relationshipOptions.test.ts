import { describe, it, expect } from 'vitest';
import { ownerRoleOptions, deriveInverseLabel, RELATIONSHIP_ROLES } from './relationshipOptions';

describe('ownerRoleOptions', () => {
  it('filters by owner gender = MALE (chỉ vai nam)', () => {
    const values = ownerRoleOptions('MALE').map((o) => o.value);
    expect(values).toContain('Cha');
    expect(values).toContain('Anh trai');
    expect(values).toContain('Em trai');
    expect(values).toContain('Con trai');
    expect(values).not.toContain('Mẹ');
    expect(values).not.toContain('Chị gái');
    expect(values).not.toContain('Con gái');
  });

  it('filters by owner gender = FEMALE (chỉ vai nữ)', () => {
    const values = ownerRoleOptions('FEMALE').map((o) => o.value);
    expect(values).toContain('Mẹ');
    expect(values).toContain('Chị gái');
    expect(values).not.toContain('Cha');
    expect(values).not.toContain('Anh trai');
  });

  it('unknown gender shows all roles', () => {
    const values = ownerRoleOptions(null).map((o) => o.value);
    expect(values).toContain('Cha');
    expect(values).toContain('Mẹ');
    expect(values.length).toBe(RELATIONSHIP_ROLES.length);
  });

  it('never offers combined sibling labels', () => {
    const values = ownerRoleOptions(null).map((o) => o.value);
    expect(values).not.toContain('Chị/Em gái');
    expect(values).not.toContain('Anh/Em trai');
  });

  it('injects legacy value not in the filtered catalog', () => {
    const opts = ownerRoleOptions('MALE', 'Chị/Em gái');
    expect(opts[0]).toEqual({ label: 'Chị/Em gái', value: 'Chị/Em gái' });
  });
});

describe('deriveInverseLabel', () => {
  it('owner Cha → deceased Con trai/Con gái theo giới tính người mất', () => {
    expect(deriveInverseLabel('Cha', 'MALE')).toBe('Con trai');
    expect(deriveInverseLabel('Cha', 'FEMALE')).toBe('Con gái');
    expect(deriveInverseLabel('Cha', null)).toBe('Con');
  });

  it('owner Anh trai (lớn) → deceased là em: Em trai/Em gái', () => {
    expect(deriveInverseLabel('Anh trai', 'MALE')).toBe('Em trai');
    expect(deriveInverseLabel('Anh trai', 'FEMALE')).toBe('Em gái');
  });

  it('owner Em gái (nhỏ) → deceased là anh/chị: Anh trai/Chị gái', () => {
    expect(deriveInverseLabel('Em gái', 'MALE')).toBe('Anh trai');
    expect(deriveInverseLabel('Em gái', 'FEMALE')).toBe('Chị gái');
  });

  it('owner Chồng → deceased Vợ (nghịch đảo vợ/chồng)', () => {
    expect(deriveInverseLabel('Vợ', 'MALE')).toBe('Chồng');
    expect(deriveInverseLabel('Chồng', 'FEMALE')).toBe('Vợ');
  });

  it('owner Ông nội → deceased Cháu nội (trai/gái)', () => {
    expect(deriveInverseLabel('Ông nội', 'MALE')).toBe('Cháu nội (trai)');
    expect(deriveInverseLabel('Bà nội', 'FEMALE')).toBe('Cháu nội (gái)');
  });

  it('returns null for empty or legacy free-text roles', () => {
    expect(deriveInverseLabel(null, 'MALE')).toBeNull();
    expect(deriveInverseLabel('Chị/Em gái', 'MALE')).toBeNull();
  });
});
