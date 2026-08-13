// Model quan hệ chủ mộ ↔ người mất, có GIỚI TÍNH và NGHỊCH ĐẢO.
//
// - "Chủ mộ là ... của người mất": người dùng chọn, options LỌC theo giới tính chủ mộ.
// - "Người mất là ... của chủ mộ": TỰ suy ra từ vai chủ mộ + giới tính người mất
//   (ô chỉ đọc). Anh/chị vs em đã được mã hoá trong chính nhãn (Anh trai = lớn),
//   nên nghịch đảo không cần ngày sinh.

export type Gender = 'MALE' | 'FEMALE' | 'ANY';

/** Một vai quan hệ cụ thể mà người dùng có thể chọn cho "Chủ mộ là ...". */
export interface RelationshipRole {
  label: string;      // nhãn hiển thị (cũng là giá trị lưu)
  gender: Gender;     // giới tính của người MANG vai này → dùng để lọc dropdown
  inverse: string;    // mã nhóm nghịch đảo, resolve nhãn theo giới tính người kia
}

/** Nhãn nghịch đảo theo giới tính của đối tượng bên kia. */
const INVERSE_LABEL: Record<string, Record<Gender, string>> = {
  PARENT: { MALE: 'Cha', FEMALE: 'Mẹ', ANY: 'Cha/Mẹ' },
  CHILD: { MALE: 'Con trai', FEMALE: 'Con gái', ANY: 'Con' },
  SPOUSE: { MALE: 'Chồng', FEMALE: 'Vợ', ANY: 'Vợ/Chồng' },
  SIB_OLDER: { MALE: 'Anh trai', FEMALE: 'Chị gái', ANY: 'Anh/Chị' },
  SIB_YOUNGER: { MALE: 'Em trai', FEMALE: 'Em gái', ANY: 'Em' },
  GP_PAT: { MALE: 'Ông nội', FEMALE: 'Bà nội', ANY: 'Ông/Bà nội' },
  GP_MAT: { MALE: 'Ông ngoại', FEMALE: 'Bà ngoại', ANY: 'Ông/Bà ngoại' },
  GC_PAT: { MALE: 'Cháu nội (trai)', FEMALE: 'Cháu nội (gái)', ANY: 'Cháu nội' },
  GC_MAT: { MALE: 'Cháu ngoại (trai)', FEMALE: 'Cháu ngoại (gái)', ANY: 'Cháu ngoại' },
  UNCLE_AUNT: { MALE: 'Bác/Chú/Cậu', FEMALE: 'Bác/Cô/Dì', ANY: 'Bác/Cô/Chú/Dì/Cậu' },
  NIECE_NEPHEW: { MALE: 'Cháu (trai)', FEMALE: 'Cháu (gái)', ANY: 'Cháu' },
  OTHER: { MALE: 'Người thân khác', FEMALE: 'Người thân khác', ANY: 'Người thân khác' },
};

/** Danh mục vai (cho dropdown "Chủ mộ là ..."). */
export const RELATIONSHIP_ROLES: RelationshipRole[] = [
  { label: 'Cha', gender: 'MALE', inverse: 'CHILD' },
  { label: 'Mẹ', gender: 'FEMALE', inverse: 'CHILD' },
  { label: 'Con trai', gender: 'MALE', inverse: 'PARENT' },
  { label: 'Con gái', gender: 'FEMALE', inverse: 'PARENT' },
  { label: 'Chồng', gender: 'MALE', inverse: 'SPOUSE' },
  { label: 'Vợ', gender: 'FEMALE', inverse: 'SPOUSE' },
  { label: 'Anh trai', gender: 'MALE', inverse: 'SIB_YOUNGER' },
  { label: 'Chị gái', gender: 'FEMALE', inverse: 'SIB_YOUNGER' },
  { label: 'Em trai', gender: 'MALE', inverse: 'SIB_OLDER' },
  { label: 'Em gái', gender: 'FEMALE', inverse: 'SIB_OLDER' },
  { label: 'Ông nội', gender: 'MALE', inverse: 'GC_PAT' },
  { label: 'Bà nội', gender: 'FEMALE', inverse: 'GC_PAT' },
  { label: 'Ông ngoại', gender: 'MALE', inverse: 'GC_MAT' },
  { label: 'Bà ngoại', gender: 'FEMALE', inverse: 'GC_MAT' },
  { label: 'Cháu nội (trai)', gender: 'MALE', inverse: 'GP_PAT' },
  { label: 'Cháu nội (gái)', gender: 'FEMALE', inverse: 'GP_PAT' },
  { label: 'Cháu ngoại (trai)', gender: 'MALE', inverse: 'GP_MAT' },
  { label: 'Cháu ngoại (gái)', gender: 'FEMALE', inverse: 'GP_MAT' },
  { label: 'Bác/Chú/Cậu', gender: 'MALE', inverse: 'NIECE_NEPHEW' },
  { label: 'Bác/Cô/Dì', gender: 'FEMALE', inverse: 'NIECE_NEPHEW' },
  { label: 'Cháu (trai)', gender: 'MALE', inverse: 'UNCLE_AUNT' },
  { label: 'Cháu (gái)', gender: 'FEMALE', inverse: 'UNCLE_AUNT' },
  { label: 'Người thân khác', gender: 'ANY', inverse: 'OTHER' },
];

const ROLE_BY_LABEL = new Map(RELATIONSHIP_ROLES.map((r) => [r.label, r]));

function gkey(g?: string | null): Gender {
  return g === 'MALE' ? 'MALE' : g === 'FEMALE' ? 'FEMALE' : 'ANY';
}

/**
 * Options cho dropdown "Chủ mộ là ...", lọc theo giới tính chủ mộ.
 * - ownerGender rỗng/không rõ ⇒ hiện tất cả (không chặn nhập).
 * - currentValue (giá trị cũ / text tự do) luôn được chèn để không mất dữ liệu.
 */
export function ownerRoleOptions(
  ownerGender?: string | null,
  currentValue?: string | null,
): { label: string; value: string }[] {
  const g = gkey(ownerGender);
  const roles = g === 'ANY'
    ? RELATIONSHIP_ROLES
    : RELATIONSHIP_ROLES.filter((r) => r.gender === g || r.gender === 'ANY');
  const opts = roles.map((r) => ({ label: r.label, value: r.label }));
  if (currentValue && !opts.some((o) => o.value === currentValue)) {
    opts.unshift({ label: currentValue, value: currentValue });
  }
  return opts;
}

/**
 * Suy nhãn "Người mất là ... của chủ mộ" từ vai chủ mộ đã chọn + giới tính người mất.
 * Trả về null nếu vai chủ mộ không thuộc danh mục (text tự do cũ) ⇒ không suy được.
 */
export function deriveInverseLabel(
  ownerRoleLabel?: string | null,
  deceasedGender?: string | null,
): string | null {
  if (!ownerRoleLabel) return null;
  const role = ROLE_BY_LABEL.get(ownerRoleLabel);
  if (!role) return null;
  return INVERSE_LABEL[role.inverse][gkey(deceasedGender)];
}
