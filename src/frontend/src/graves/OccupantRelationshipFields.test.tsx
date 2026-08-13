import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { Form, Input } from 'antd';
import { describe, it, expect } from 'vitest';
import OccupantRelationshipFields from './OccupantRelationshipFields';

const Harness: React.FC<{ ownerGender?: string; owner?: string; gender?: string }> = ({
  ownerGender, owner = 'Cha', gender = 'MALE',
}) => {
  const [form] = Form.useForm();
  return (
    <Form form={form} initialValues={{ gender, ownerRelationship: owner }}>
      <Form.Item name="gender"><Input /></Form.Item>
      <OccupantRelationshipFields
        form={form}
        ownerName="ownerRelationship"
        deceasedName="deceasedRelationship"
        genderName="gender"
        ownerGender={ownerGender}
      />
    </Form>
  );
};

describe('OccupantRelationshipFields', () => {
  const deceasedInput = () =>
    screen.getByPlaceholderText('Tự suy theo lựa chọn trên') as HTMLInputElement;

  it('ô "Người mất là" bị disable (chỉ đọc)', async () => {
    render(<Harness />);
    await waitFor(() => expect(deceasedInput()).toBeDisabled());
  });

  it('tự suy: Chủ mộ = Cha, người mất nam → Con trai', async () => {
    render(<Harness owner="Cha" gender="MALE" />);
    await waitFor(() => expect(deceasedInput().value).toBe('Con trai'));
  });

  it('tự suy: Chủ mộ = Anh trai, người mất nữ → Em gái', async () => {
    render(<Harness owner="Anh trai" gender="FEMALE" />);
    await waitFor(() => expect(deceasedInput().value).toBe('Em gái'));
  });

  it('tự suy: Chủ mộ = Em gái, người mất nam → Anh trai', async () => {
    render(<Harness owner="Em gái" gender="MALE" />);
    await waitFor(() => expect(deceasedInput().value).toBe('Anh trai'));
  });
});
