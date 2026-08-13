import React, { useEffect } from 'react';
import { Form, Input, Select } from 'antd';
import type { FormInstance } from 'antd';
import type { NamePath } from 'antd/es/form/interface';
import { deriveInverseLabel, ownerRoleOptions } from './relationshipOptions';

interface Props {
  form: FormInstance;
  /** Đường dẫn field "Chủ mộ là ..." (vd: 'ownerRelationship' hoặc ['occupants', 3, 'ownerRelationship']). */
  ownerName: NamePath;
  /** Đường dẫn field "Người mất là ..." (ô mờ, tự suy). */
  deceasedName: NamePath;
  /** Đường dẫn field giới tính người mất (để suy nghịch đảo). */
  genderName: NamePath;
  /** Giới tính chủ mộ — lọc options "Chủ mộ là ...". Rỗng ⇒ không lọc. */
  ownerGender?: string | null;
  /** {...restField} khi dùng trong Form.List. */
  restField?: object;
}

/**
 * Cặp field quan hệ chủ mộ ↔ người mất:
 *  - "Chủ mộ là ...": dropdown lọc theo giới tính chủ mộ.
 *  - "Người mất là ...": ô mờ chỉ đọc, tự suy từ vai chủ mộ + giới tính người mất.
 */
const OccupantRelationshipFields: React.FC<Props> = ({
  form, ownerName, deceasedName, genderName, ownerGender, restField = {},
}) => {
  const ownerRel = Form.useWatch(ownerName, form) as string | undefined;
  const deceasedRel = Form.useWatch(deceasedName, form) as string | undefined;
  const occGender = Form.useWatch(genderName, form) as string | undefined;

  // Tự suy "Người mất là ..." khi vai chủ mộ / giới tính người mất đổi.
  useEffect(() => {
    const derived = deriveInverseLabel(ownerRel, occGender);
    if (derived !== null && derived !== deceasedRel) {
      form.setFieldValue(deceasedName, derived);
    }
    // ownerName/deceasedName là NamePath ổn định theo vị trí field; chỉ phụ thuộc giá trị.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ownerRel, occGender, deceasedRel]);

  const options = ownerRoleOptions(ownerGender, ownerRel);

  return (
    <>
      <Form.Item
        {...restField}
        name={ownerName}
        label="Chủ mộ là ... của người mất"
        tooltip="Chọn vai của chủ mộ với người mất. Danh sách lọc theo giới tính chủ mộ."
      >
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder="Chọn quan hệ, vd: Con trai"
          options={options}
        />
      </Form.Item>
      <Form.Item
        {...restField}
        name={deceasedName}
        label="Người mất là ... của chủ mộ"
        tooltip="Tự suy ra từ 'Chủ mộ là ...' và giới tính người mất — không sửa trực tiếp."
      >
        <Input disabled placeholder="Tự suy theo lựa chọn trên" />
      </Form.Item>
    </>
  );
};

export default OccupantRelationshipFields;
