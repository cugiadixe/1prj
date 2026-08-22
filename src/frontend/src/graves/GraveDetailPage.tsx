import React, { useState } from 'react';
import {
  Alert, Button, Card, DatePicker, Descriptions, Drawer, Form, Input, Modal, Popconfirm, Select, Space, Spin, Table, Tag, Tooltip, Typography, Upload, message,
} from 'antd';
import { DeleteOutlined, EditOutlined, PhoneOutlined, PlusOutlined, SwapOutlined, UploadOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import dayjs, { type Dayjs } from 'dayjs';
import { usePermissions } from '../auth/AuthProvider';
import {
  addOccupant, getGraveById, updateOccupant, transferOwnership, getOwnershipHistory,
  addEmergencyContact, updateEmergencyContact, removeEmergencyContact, getOccupantCandidates, relocateOccupant,
  processOwnerDeath,
} from './gravesApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import {
  GENDERS,
  GRAVE_STATUS_COLORS,
  GRAVE_STATUSES,
  GRAVE_TYPES,
  TRANSFER_TYPES,
  TRANSFER_TYPE_COLORS,
  type GraveAttachment,
  type GraveEmergencyContact,
  type GraveOccupant,
  type OwnershipHistoryItem,
} from './types';
import { searchCustomers, getCustomerById } from '../customers/customersApi';
import type { CustomerListItem } from '../customers/types';
import OccupantRelationshipFields from './OccupantRelationshipFields';
import { listByGrave } from '../customerCarePackages/api';
import { CCP_STATUS_COLORS, CCP_STATUS_LABELS } from '../customerCarePackages/types';
import GraveAttachmentsSection from './GraveAttachmentsSection';
import { fetchAttachmentObjectUrl, listAttachments, uploadAttachment } from './attachmentsApi';
import EntityTagsSection from '../tags/EntityTagsSection';
import { setGraveTags } from '../tags/tagsApi';

const { Title } = Typography;

const fmtDate = (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');
const d = (v: Dayjs | null | undefined) => (v ? v.format('YYYY-MM-DD') : null);

const GraveDetailPage: React.FC = () => {
  const { graveId } = useParams<{ graveId: string }>();
  const id = Number(graveId);
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const canUpdate = hasPermission('GRAVE_UPDATE');

  const [form] = Form.useForm();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<GraveOccupant | null>(null);
  const [viewing, setViewing] = useState<GraveOccupant | null>(null);
  const [relocating, setRelocating] = useState<GraveOccupant | null>(null);
  const [relocForm] = Form.useForm();

  const { data, isLoading, error } = useQuery({
    queryKey: ['grave', id],
    queryFn: () => getGraveById(id),
    enabled: !Number.isNaN(id),
  });

  const { data: gravePackages } = useQuery({
    queryKey: ['grave-ccp', id],
    queryFn: () => listByGrave(id),
    enabled: !Number.isNaN(id) && hasPermission('CUSTOMER_CARE_PACKAGE_VIEW'),
  });

  const saveMutation = useMutation({
    mutationFn: (values: Record<string, unknown>) => {
      // SỬA cốt cũ: giữ luồng nhập tay (occupant có thể chưa nối khách). THÊM cốt: chọn khách đã mất.
      if (editing) {
        const payload = {
          fullName: values.fullName as string,
          gender: (values.gender as string) || null,
          dob: d(values.dob as Dayjs),
          deathDateSolar: d(values.deathDateSolar as Dayjs),
          deathDateLunar: (values.deathDateLunar as string) || null,
          burialDate: d(values.burialDate as Dayjs),
          hometown: (values.hometown as string) || null,
          ownerRelationship: (values.ownerRelationship as string) || null,
          deceasedRelationship: (values.deceasedRelationship as string) || null,
          notes: (values.notes as string) || null,
        };
        return updateOccupant(id, editing.id, { ...payload, targetVersion: editing.rowVersion });
      }
      return addOccupant(id, {
        deceasedCustomerId: values.deceasedCustomerId as number,
        burialDate: d(values.burialDate as Dayjs),
        notes: (values.notes as string) || null,
      });
    },
    onSuccess: () => {
      message.success(editing ? 'Đã cập nhật người an táng' : 'Đã thêm người an táng');
      setModalOpen(false);
      setEditing(null);
      form.resetFields();
      queryClient.invalidateQueries({ queryKey: ['grave', id] });
    },
    onError: (err) => message.error(getErrorMessage(err)),
  });

  // ─── Quan hệ chủ mộ ↔ người mất: lọc theo giới tính chủ mộ + tự suy nghịch đảo ───
  const canViewCustomer = hasPermission('CUSTOMER_VIEW_BASIC');
  const { data: ownerProfile } = useQuery({
    queryKey: ['grave-owner-profile', data?.ownerCustomerId],
    queryFn: () => getCustomerById(data!.ownerCustomerId!),
    enabled: modalOpen && !!data?.ownerCustomerId && canViewCustomer,
  });

  // Ứng viên đặt cốt: khách đã mất có quan hệ với chủ mộ + chưa nằm mộ (chỉ khi THÊM cốt).
  const { data: candidates } = useQuery({
    queryKey: ['occupant-candidates', id],
    queryFn: () => getOccupantCandidates(id),
    enabled: modalOpen && !editing,
  });

  const relocateMutation = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      relocateOccupant(id, relocating!.id, {
        relocatedAt: d(values.relocatedAt as Dayjs),
        note: (values.note as string) || null,
      }),
    onSuccess: () => {
      message.success('Đã bốc/cải táng');
      setRelocating(null);
      relocForm.resetFields();
      queryClient.invalidateQueries({ queryKey: ['grave', id] });
    },
    onError: (err) => message.error(getErrorMessage(err)),
  });

  const openAdd = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEdit = (o: GraveOccupant) => {
    setEditing(o);
    form.setFieldsValue({
      fullName: o.fullName,
      gender: o.gender,
      dob: o.dob ? dayjs(o.dob) : null,
      deathDateSolar: o.deathDateSolar ? dayjs(o.deathDateSolar) : null,
      deathDateLunar: o.deathDateLunar,
      burialDate: o.burialDate ? dayjs(o.burialDate) : null,
      hometown: o.hometown,
      ownerRelationship: o.ownerRelationship,
      deceasedRelationship: o.deceasedRelationship,
      notes: o.notes,
    });
    setModalOpen(true);
  };

  // ─── Liên hệ khẩn cấp động (là khách hàng) ───
  const [ecForm] = Form.useForm();
  const [ecModalOpen, setEcModalOpen] = useState(false);
  const [ecEditing, setEcEditing] = useState<GraveEmergencyContact | null>(null);
  const [ecOptions, setEcOptions] = useState<CustomerListItem[]>([]);
  const [ecSearching, setEcSearching] = useState(false);
  const [viewingContact, setViewingContact] = useState<GraveEmergencyContact | null>(null);

  const handleEcSearch = async (kw: string) => {
    if (!kw || kw.trim().length < 2) { setEcOptions([]); return; }
    setEcSearching(true);
    try {
      const res = await searchCustomers({ search: kw.trim(), customerStatus: 'ACTIVE', pageSize: 20 });
      setEcOptions(res.items);
    } finally {
      setEcSearching(false);
    }
  };

  const openEcAdd = () => {
    setEcEditing(null);
    setEcOptions([]);
    ecForm.resetFields();
    setEcModalOpen(true);
  };

  const openEcEdit = (c: GraveEmergencyContact) => {
    setEcEditing(c);
    setEcOptions(c.contactCustomerId
      ? [{
        id: c.contactCustomerId, customerCode: c.contactCode ?? '', fullName: c.contactName,
        cccd: null, phone: c.contactPhone, customerStatus: 'ACTIVE', createdAt: '',
      }]
      : []);
    ecForm.setFieldsValue({
      contactCustomerId: c.contactCustomerId ?? undefined,
      relationshipNote: c.relationshipNote,
    });
    setEcModalOpen(true);
  };

  const ecMutation = useMutation({
    mutationFn: (values: Record<string, unknown>) => {
      const payload = {
        contactCustomerId: Number(values.contactCustomerId),
        relationshipNote: (values.relationshipNote as string) || null,
      };
      return ecEditing
        ? updateEmergencyContact(id, ecEditing.id, { ...payload, targetVersion: ecEditing.rowVersion })
        : addEmergencyContact(id, payload);
    },
    onSuccess: () => {
      message.success(ecEditing ? 'Đã cập nhật liên hệ khẩn cấp' : 'Đã thêm liên hệ khẩn cấp');
      setEcModalOpen(false);
      setEcEditing(null);
      setEcOptions([]);
      ecForm.resetFields();
      queryClient.invalidateQueries({ queryKey: ['grave', id] });
    },
    onError: (err) => message.error(getErrorMessage(err)),
  });

  const removeEcMutation = useMutation({
    mutationFn: (contactId: number) => removeEmergencyContact(id, contactId),
    onSuccess: () => {
      message.success('Đã xóa liên hệ khẩn cấp');
      queryClient.invalidateQueries({ queryKey: ['grave', id] });
    },
    onError: (err) => message.error(getErrorMessage(err)),
  });

  // Chi tiết khách hàng LH khẩn cấp đang xem (drawer)
  const { data: viewingContactDetail, isFetching: viewingContactLoading } = useQuery({
    queryKey: ['customer', viewingContact?.contactCustomerId],
    queryFn: () => getCustomerById(viewingContact!.contactCustomerId!),
    enabled: !!viewingContact?.contactCustomerId && canViewCustomer,
  });

  // ─── Chuyển quyền sở hữu ───
  const canTransfer = hasPermission('GRAVE_TRANSFER_OWNERSHIP');
  const [transferForm] = Form.useForm();
  const [transferOpen, setTransferOpen] = useState(false);
  const [ownerDeathOpen, setOwnerDeathOpen] = useState(false);
  const [ownerDeathForm] = Form.useForm();
  const [transferFile, setTransferFile] = useState<File | null>(null);
  const [ownerOptions, setOwnerOptions] = useState<CustomerListItem[]>([]);
  const [ownerSearching, setOwnerSearching] = useState(false);

  const { data: history } = useQuery({
    queryKey: ['grave-ownership-history', id],
    queryFn: () => getOwnershipHistory(id),
    enabled: !Number.isNaN(id) && canTransfer,
  });

  const { data: attachmentsForHistory } = useQuery({
    queryKey: ['grave-attachments', id],
    queryFn: () => listAttachments(id),
    enabled: !Number.isNaN(id) && canTransfer,
  });
  const openTransferDoc = async (att: GraveAttachment) => {
    try {
      const url = await fetchAttachmentObjectUrl(id, att.id, false);
      window.open(url, '_blank', 'noopener');
    } catch (e) { message.error(getErrorMessage(e)); }
  };

  const handleOwnerSearch = async (kw: string) => {
    if (!kw || kw.trim().length < 2) { setOwnerOptions([]); return; }
    setOwnerSearching(true);
    try {
      const res = await searchCustomers({ search: kw.trim(), customerStatus: 'ACTIVE', pageSize: 20 });
      setOwnerOptions(res.items);
    } finally {
      setOwnerSearching(false);
    }
  };

  const transferMutation = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      transferOwnership(id, {
        newOwnerCustomerId: values.newOwnerCustomerId as number,
        transferType: values.transferType as string,
        reason: (values.reason as string) || null,
        targetVersion: data?.rowVersion ?? '',
      }),
    onSuccess: async (res, values) => {
      if (transferFile) {
        try {
          await uploadAttachment(id, transferFile, 'TRANSFER_DOC', (values.reason as string) || undefined, res.ownershipHistoryId);
        } catch (e) {
          message.warning('Đã chuyển quyền nhưng tải văn bản lỗi: ' + getErrorMessage(e));
        }
      }
      message.success(
        `Đã chuyển quyền. Tái suy diễn ${res.occupantsRederived} cốt`
        + (res.occupantsNeedingConfirmation > 0 ? `, ${res.occupantsNeedingConfirmation} cần xác nhận.` : '.'),
      );
      setTransferOpen(false);
      transferForm.resetFields();
      setTransferFile(null);
      setOwnerOptions([]);
      queryClient.invalidateQueries({ queryKey: ['grave', id] });
      queryClient.invalidateQueries({ queryKey: ['grave-ownership-history', id] });
      queryClient.invalidateQueries({ queryKey: ['grave-attachments', id] });
    },
    onError: (err) => message.error(getErrorMessage(err)),
  });

  const ownerDeathMutation = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      processOwnerDeath({
        deceasedCustomerId: data!.ownerCustomerId!,
        heirCustomerId: values.heirCustomerId as number,
        deathDateSolar: d(values.deathDateSolar as Dayjs),
        reason: (values.reason as string) || null,
      }),
    onSuccess: (res) => {
      message.success(
        `Đã xử lý chủ mộ qua đời — chuyển ${res.gravesTransferred}/${res.gravesOwned} mộ cho người thừa kế. `
        + 'Có thể đặt chủ cũ làm cốt qua "Thêm người an táng".',
      );
      setOwnerDeathOpen(false);
      ownerDeathForm.resetFields();
      queryClient.invalidateQueries({ queryKey: ['grave', id] });
      queryClient.invalidateQueries({ queryKey: ['grave-ownership-history', id] });
      queryClient.invalidateQueries({ queryKey: ['occupant-candidates', id] });
    },
    onError: (err) => message.error(getErrorMessage(err)),
  });

  if (isPermissionDenied(error)) return <Alert type="error" message="Bạn không có quyền xem phần mộ." />;
  if (isLoading) return <Spin />;
  if (error || !data) return <Alert type="error" message={getErrorMessage(error)} />;

  const emergencyContacts = data.emergencyContacts ?? [];
  // Chưa có chủ mộ thì chưa thao tác gán được: khóa thêm liên hệ / người an táng / tải tài liệu.
  const hasOwner = !!data.ownerCustomerId;
  const noOwnerHint = 'Cần có chủ mộ trước. Dùng "Chỉnh sửa" hoặc gán chủ từ trang khách hàng.';

  const occupantColumns = [
    {
      title: 'Họ và tên',
      dataIndex: 'fullName',
      key: 'fullName',
      render: (name: string, o: GraveOccupant) => (o.deceasedCustomerId ? (
        <Link to={`/customers/${o.deceasedCustomerId}`} onClick={(e) => e.stopPropagation()}>{name}</Link>
      ) : name),
    },
    { title: 'Giới tính', dataIndex: 'gender', key: 'gender', render: (g: string | null) => (g ? GENDERS[g] ?? g : '—') },
    { title: 'Ngày sinh', dataIndex: 'dob', key: 'dob', render: fmtDate },
    { title: 'Ngày mất (DL)', dataIndex: 'deathDateSolar', key: 'deathDateSolar', render: fmtDate },
    { title: 'Ngày mất (ÂL)', dataIndex: 'deathDateLunar', key: 'deathDateLunar', render: (v: string | null) => v ?? '—' },
    { title: 'Ngày an táng', dataIndex: 'burialDate', key: 'burialDate', render: fmtDate },
    { title: 'Nguyên quán', dataIndex: 'hometown', key: 'hometown', render: (v: string | null) => v ?? '—' },
    {
      title: 'Quan hệ với chủ mộ',
      dataIndex: 'ownerRelationship',
      key: 'ownerRelationship',
      render: (v: string | null) => (v ? <Tag color="purple">{v}</Tag> : '—'),
    },
    {
      title: 'Trạng thái',
      key: 'status',
      render: (_: unknown, o: GraveOccupant) =>
        o.status === 'RELOCATED' ? (
          <Tag color="default" title={o.relocationNote ?? undefined}>
            Đã bốc{o.relocatedAt ? ` (${fmtDate(o.relocatedAt)})` : ''}
          </Tag>
        ) : (
          <Tag color="green">Đang an táng</Tag>
        ),
    },
    ...(canUpdate ? [{
      title: '',
      key: 'actions',
      width: 120,
      render: (_: unknown, o: GraveOccupant) =>
        o.status === 'RELOCATED' ? null : (
          <Space>
            <Button type="text" icon={<EditOutlined />}
              onClick={(e) => { e.stopPropagation(); openEdit(o); }}
              data-testid={`edit-occupant-${o.id}`} />
            <Button type="text" icon={<SwapOutlined />} title="Bốc / Cải táng"
              onClick={(e) => { e.stopPropagation(); setRelocating(o); relocForm.resetFields(); }}
              data-testid={`relocate-occupant-${o.id}`} />
          </Space>
        ),
    }] : []),
  ];

  return (
    <div data-testid="grave-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Mộ {data.graveCode}{' '}
          <Tag color={GRAVE_STATUS_COLORS[data.status] ?? 'default'}>
            {GRAVE_STATUSES[data.status] ?? data.status}
          </Tag>
        </Title>
        <Space>
          <Button onClick={() => navigate('/graves')}>Quay lại</Button>
          {canUpdate && (
            <Button type="primary" data-testid="edit-grave-btn">
              <Link to={`/graves/${data.id}/edit`}>Chỉnh sửa</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Card title="Thông tin phần mộ" style={{ marginBottom: 16 }}>
        <Descriptions column={{ xs: 1, sm: 2, md: 3 }} bordered size="small">
          <Descriptions.Item label="Mã mộ">{data.graveCode}</Descriptions.Item>
          <Descriptions.Item label="Khu">Khu {data.zone}</Descriptions.Item>
          <Descriptions.Item label="Số mộ">{data.plotNumber}</Descriptions.Item>
          <Descriptions.Item label="Hàng">{data.rowLabel ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Cột">{data.colLabel ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Loại mộ">{GRAVE_TYPES[data.graveType] ?? data.graveType}</Descriptions.Item>
          <Descriptions.Item label="Diện tích">{data.areaM2 != null ? `${data.areaM2} m²` : '—'}</Descriptions.Item>
          <Descriptions.Item label="Số cốt">{data.cotCount}</Descriptions.Item>
          <Descriptions.Item label="Trạng thái">
            <Tag color={GRAVE_STATUS_COLORS[data.status] ?? 'default'}>
              {GRAVE_STATUSES[data.status] ?? data.status}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Ghi chú" span={3}>{data.notes ?? '—'}</Descriptions.Item>
        </Descriptions>
      </Card>

      <Card
        title="Chủ mộ & liên hệ khẩn cấp"
        style={{ marginBottom: 16 }}
        extra={canTransfer && data.ownerCustomerId && (
          <Space>
            <Button icon={<SwapOutlined />} onClick={() => { transferForm.resetFields(); setTransferOpen(true); }}
              data-testid="transfer-owner-btn">
              Chuyển quyền sở hữu
            </Button>
            <Button danger onClick={() => {
              // Gợi ý người thừa kế = liên hệ khẩn cấp ưu tiên nhất (priority nhỏ nhất) có liên kết KH.
              const top = [...(data.emergencyContacts ?? [])]
                .filter((c) => c.contactCustomerId)
                .sort((a, b) => a.priority - b.priority)[0];
              ownerDeathForm.resetFields();
              if (top?.contactCustomerId) ownerDeathForm.setFieldsValue({ heirCustomerId: top.contactCustomerId });
              setOwnerDeathOpen(true);
            }} data-testid="owner-death-btn">
              Chủ mộ qua đời
            </Button>
          </Space>
        )}
      >
        <Descriptions column={1} bordered size="small">
          <Descriptions.Item label="Chủ mộ">
            {data.ownerCustomerId ? (
              <Link to={`/customers/${data.ownerCustomerId}`}>
                {data.ownerName} {data.ownerCode ? `(${data.ownerCode})` : ''}
              </Link>
            ) : '—'}
          </Descriptions.Item>
        </Descriptions>

        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', margin: '16px 0 8px' }}>
          <Typography.Text strong>Liên hệ khẩn cấp ({emergencyContacts.length})</Typography.Text>
          {canUpdate && (
            <Tooltip title={hasOwner ? '' : noOwnerHint}>
              <Button size="small" type="primary" icon={<PlusOutlined />} onClick={openEcAdd} disabled={!hasOwner} data-testid="add-emergency-contact-btn">
                Thêm liên hệ
              </Button>
            </Tooltip>
          )}
        </div>
        {emergencyContacts.length > 0 ? (
          <Table
            dataSource={emergencyContacts}
            rowKey={(c: GraveEmergencyContact) => c.id}
            pagination={false}
            size="small"
            onRow={(c: GraveEmergencyContact) => ({
              onClick: () => c.contactCustomerId && setViewingContact(c),
              style: { cursor: c.contactCustomerId ? 'pointer' : 'default' },
            })}
            columns={[
              {
                title: 'Ưu tiên', dataIndex: 'priority', key: 'priority', width: 80,
                render: (p: number) => <Tag color={p === 1 ? 'red' : 'default'}>{p === 1 ? 'Gọi trước' : `Ưu tiên ${p}`}</Tag>,
              },
              {
                title: 'Người liên hệ', dataIndex: 'contactName', key: 'contactName',
                render: (name: string, c: GraveEmergencyContact) => (c.contactCustomerId ? (
                  <Link to={`/customers/${c.contactCustomerId}`} onClick={(e) => e.stopPropagation()}>
                    {name} {c.contactCode ? `(${c.contactCode})` : ''}
                  </Link>
                ) : name),
              },
              {
                title: 'SĐT', dataIndex: 'contactPhone', key: 'contactPhone',
                render: (v: string | null) => (v ? <span><PhoneOutlined /> {v}</span> : '—'),
              },
              { title: 'Quan hệ / ghi chú', dataIndex: 'relationshipNote', key: 'relationshipNote', render: (v: string | null) => v ?? '—' },
              ...(canUpdate ? [{
                title: '', key: 'actions', width: 90,
                render: (_: unknown, c: GraveEmergencyContact) => (
                  <Space size={0} onClick={(e) => e.stopPropagation()}>
                    <Button type="text" icon={<EditOutlined />} onClick={() => openEcEdit(c)}
                      data-testid={`edit-emergency-contact-${c.id}`} />
                    <Popconfirm title="Xóa liên hệ khẩn cấp này?" okText="Xóa" cancelText="Hủy"
                      onConfirm={() => removeEcMutation.mutate(c.id)}>
                      <Button type="text" danger icon={<DeleteOutlined />}
                        data-testid={`remove-emergency-contact-${c.id}`} />
                    </Popconfirm>
                  </Space>
                ),
              }] : []),
            ]}
          />
        ) : (
          <Alert type="info" message="Chưa có liên hệ khẩn cấp. Bấm “Thêm liên hệ” để bổ sung (chọn từ danh sách khách hàng)." />
        )}
      </Card>

      <Card
        title={`Người an táng (${data.occupants.length})`}
        data-testid="grave-occupants-card"
        extra={canUpdate && (
          <Tooltip title={hasOwner ? '' : noOwnerHint}>
            <Button type="primary" icon={<PlusOutlined />} onClick={openAdd} disabled={!hasOwner} data-testid="add-occupant-btn">
              Thêm người an táng
            </Button>
          </Tooltip>
        )}
      >
        {data.occupants.length > 0 ? (
          <>
            <Alert type="info" showIcon style={{ marginBottom: 8 }}
              message="Bấm vào một cốt để xem chi tiết đầy đủ (360°)." />
            <Table
              dataSource={data.occupants}
              columns={occupantColumns}
              rowKey={(o: GraveOccupant) => o.id}
              pagination={false}
              size="small"
              onRow={(o: GraveOccupant) => ({
                onClick: () => setViewing(o),
                style: { cursor: 'pointer' },
              })}
            />
          </>
        ) : (
          <Alert type="info" message="Chưa có người an táng trong mộ này." />
        )}
      </Card>

      <Card title="Gói chăm sóc áp dụng cho mộ" style={{ marginTop: 16 }} data-testid="grave-ccp-card">
        {gravePackages && gravePackages.length > 0 ? (
          <Table
            dataSource={gravePackages}
            rowKey="id"
            pagination={false}
            size="small"
            columns={[
              { title: 'Gói', dataIndex: 'serviceTypeName', key: 'serviceTypeName', render: (v: string | null) => v ?? '—' },
              { title: 'Khách hàng', dataIndex: 'customerName', key: 'customerName', render: (v: string | null) => v ?? '—' },
              { title: 'Số cốt', dataIndex: 'cotCount', key: 'cotCount' },
              {
                title: 'Kỳ hạn', key: 'period',
                render: (_: unknown, r: { startDate: string; endDate: string | null }) =>
                  `${new Date(r.startDate).toLocaleDateString('vi-VN')} → ${r.endDate ? new Date(r.endDate).toLocaleDateString('vi-VN') : 'Không kỳ hạn'}`,
              },
              {
                title: 'Trạng thái', dataIndex: 'status', key: 'status',
                render: (s: string) => <Tag color={CCP_STATUS_COLORS[s] ?? 'default'}>{CCP_STATUS_LABELS[s] ?? s}</Tag>,
              },
            ]}
          />
        ) : (
          <Alert type="info" message="Chưa có gói chăm sóc nào áp dụng cho mộ này." />
        )}
      </Card>

      <EntityTagsSection
        tagType="GRAVE"
        tags={data.tags}
        canManage={hasPermission('TAG_MANAGE')}
        onSave={(req) => setGraveTags(id, req)}
        onSaved={() => queryClient.invalidateQueries({ queryKey: ['grave', id] })}
        testId="grave-tags-section"
      />

      <GraveAttachmentsSection graveId={id} hasOwner={hasOwner} />

      {canTransfer && history && history.length > 0 && (
        <Card title="Lịch sử chuyển quyền sở hữu" style={{ marginTop: 16 }} data-testid="ownership-history-card">
          <Table
            dataSource={history}
            rowKey="id"
            pagination={false}
            size="small"
            columns={[
              { title: 'Thời điểm', dataIndex: 'transferredAt', key: 'transferredAt', render: (v: string) => new Date(v).toLocaleString('vi-VN') },
              { title: 'Chủ cũ', dataIndex: 'previousOwnerName', key: 'prev', render: (v: string | null) => v ?? '—' },
              { title: 'Chủ mới', dataIndex: 'newOwnerName', key: 'new', render: (v: string | null) => v ?? '—' },
              { title: 'Lý do', dataIndex: 'transferType', key: 'type', render: (t: string) => <Tag color={TRANSFER_TYPE_COLORS[t] ?? 'default'}>{TRANSFER_TYPES[t] ?? t}</Tag> },
              { title: 'Ghi chú', dataIndex: 'reason', key: 'reason', render: (v: string | null) => v ?? '—' },
              {
                title: 'Văn bản',
                key: 'doc',
                render: (_: unknown, r: OwnershipHistoryItem) => {
                  const doc = attachmentsForHistory?.find((a) => a.ownershipHistoryId === r.id);
                  return doc
                    ? <Button type="link" size="small" style={{ padding: 0 }} onClick={() => openTransferDoc(doc)}>Xem văn bản</Button>
                    : '—';
                },
              },
            ]}
          />
        </Card>
      )}

      <Modal
        title="Chủ mộ qua đời"
        open={ownerDeathOpen}
        onCancel={() => { setOwnerDeathOpen(false); ownerDeathForm.resetFields(); }}
        onOk={() => ownerDeathForm.submit()}
        confirmLoading={ownerDeathMutation.isPending}
        okText="Xác nhận"
        cancelText="Hủy"
        destroyOnHidden
      >
        {(() => {
          const heirOptions = [...(data.emergencyContacts ?? [])]
            .filter((c) => c.contactCustomerId)
            .sort((a, b) => a.priority - b.priority)
            .map((c) => ({
              label: `${c.contactName ?? `KH ${c.contactCustomerId}`} — ưu tiên ${c.priority}${c.relationshipNote ? ` (${c.relationshipNote})` : ''}`,
              value: c.contactCustomerId as number,
            }));
          return (
            <Form form={ownerDeathForm} layout="vertical" onFinish={(v) => ownerDeathMutation.mutate(v)}>
              <Alert
                type="warning"
                showIcon
                style={{ marginBottom: 12 }}
                message={`Chủ mộ: ${data.ownerName ?? `KH ${data.ownerCustomerId}`}`}
                description="Đánh dấu chủ mộ đã mất và chuyển quyền MỌI mộ của họ cho người thừa kế. Sau đó có thể đặt chủ cũ làm cốt qua 'Thêm người an táng'."
              />
              {heirOptions.length === 0 ? (
                <Alert
                  type="error"
                  showIcon
                  message="Chưa có liên hệ khẩn cấp để làm người thừa kế. Hãy thêm liên hệ khẩn cấp (là khách hàng) trước."
                />
              ) : (
                <Form.Item
                  name="heirCustomerId"
                  label="Người thừa kế (từ liên hệ khẩn cấp)"
                  rules={[{ required: true, message: 'Chọn người thừa kế' }]}
                >
                  <Select options={heirOptions} data-testid="heir-select" />
                </Form.Item>
              )}
              <Form.Item name="deathDateSolar" label="Ngày mất (Dương lịch)">
                <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
              </Form.Item>
              <Form.Item name="reason" label="Lý do / ghi chú">
                <Input.TextArea rows={2} placeholder="VD: chủ mộ qua đời, chuyển cho con trưởng." />
              </Form.Item>
            </Form>
          );
        })()}
      </Modal>

      <Modal
        title="Chuyển quyền sở hữu phần mộ"
        open={transferOpen}
        onCancel={() => { setTransferOpen(false); setOwnerOptions([]); setTransferFile(null); }}
        onOk={() => transferForm.submit()}
        confirmLoading={transferMutation.isPending}
        okText="Chuyển quyền"
        cancelText="Hủy"
        destroyOnHidden
      >
        <Alert type="info" showIcon style={{ marginBottom: 12 }}
          message="Nhãn quan hệ của các cốt sẽ được TỰ ĐỘNG tính lại theo góc nhìn chủ mới." />
        <Descriptions column={1} size="small" style={{ marginBottom: 8 }}>
          <Descriptions.Item label="Chủ hiện tại">
            {data.ownerName ?? '—'} {data.ownerCode ? `(${data.ownerCode})` : ''}
          </Descriptions.Item>
        </Descriptions>
        <Form form={transferForm} layout="vertical" onFinish={(v) => transferMutation.mutate(v)}>
          <Form.Item name="newOwnerCustomerId" label="Chủ mới" rules={[{ required: true, message: 'Chọn chủ mới' }]}>
            <Select
              showSearch
              placeholder="Gõ tên khách hàng để tìm..."
              filterOption={false}
              onSearch={handleOwnerSearch}
              loading={ownerSearching}
              notFoundContent={ownerSearching ? <Spin size="small" /> : null}
              options={ownerOptions
                .filter((c) => c.id !== data.ownerCustomerId)
                .map((c) => ({ value: c.id, label: `${c.fullName} (${c.customerCode})${c.phone ? ' · ' + c.phone : ''}` }))}
              data-testid="new-owner-select"
            />
          </Form.Item>
          <Form.Item name="transferType" label="Lý do chuyển quyền" rules={[{ required: true, message: 'Chọn lý do' }]}>
            <Select
              placeholder="Chọn lý do"
              options={Object.entries(TRANSFER_TYPES)
                .filter(([value]) => value !== 'DEATH')
                .map(([value, label]) => ({ value, label }))}
              data-testid="transfer-type-select"
            />
          </Form.Item>
          <Form.Item name="reason" label="Ghi chú / lý do chi tiết" rules={[{ required: true, message: 'Vui lòng nêu lý do (bắt buộc)' }]}>
            <Input.TextArea rows={2} placeholder="VD: Chủ mộ chuyển công tác vào TP HCM, giao lại cho em ruột." />
          </Form.Item>
          <Form.Item label="Văn bản đã ký (scan PDF/ảnh) — tùy chọn"
            tooltip="Người chuyển quyền ký văn bản cứng, scan ra PDF/ảnh và đính kèm. File sẽ lưu vào hồ sơ mộ, gắn với lần chuyển quyền này.">
            <Upload
              maxCount={1}
              accept=".jpg,.jpeg,.png,.webp,.pdf"
              onRemove={() => setTransferFile(null)}
              beforeUpload={(file) => {
                if (!['image/jpeg', 'image/png', 'image/webp', 'application/pdf'].includes(file.type)) {
                  message.error('Chỉ nhận PDF hoặc ảnh.'); return Upload.LIST_IGNORE;
                }
                if (file.size > 10 * 1024 * 1024) { message.error('File ≤ 10MB.'); return Upload.LIST_IGNORE; }
                setTransferFile(file as unknown as File);
                return false; // không tự upload, chờ chuyển quyền xong mới gắn
              }}
            >
              <Button icon={<UploadOutlined />}>Chọn văn bản</Button>
            </Upload>
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={editing ? 'Sửa người an táng' : 'Thêm người an táng'}
        open={modalOpen}
        onCancel={() => { setModalOpen(false); setEditing(null); }}
        onOk={() => form.submit()}
        confirmLoading={saveMutation.isPending}
        okText="Lưu"
        cancelText="Hủy"
        destroyOnHidden
      >
        <Form form={form} layout="vertical" onFinish={(v) => saveMutation.mutate(v)}>
          {editing ? (
            <>
              <Form.Item name="fullName" label="Họ tên" rules={[{ required: true, message: 'Họ tên là bắt buộc' }]}>
                <Input />
              </Form.Item>
              <Form.Item name="gender" label="Giới tính">
                <Select allowClear options={Object.entries(GENDERS).map(([value, label]) => ({ label, value }))} />
              </Form.Item>
              <Form.Item name="dob" label="Ngày sinh"><DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" /></Form.Item>
              <Form.Item name="deathDateSolar" label="Ngày mất (Dương lịch)"><DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" /></Form.Item>
              <Form.Item name="deathDateLunar" label="Ngày mất (Âm lịch)" rules={[{ max: 20 }]}>
                <Input placeholder="VD: 15/7 Giáp Thìn" />
              </Form.Item>
              <Form.Item name="burialDate" label="Ngày an táng"><DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" /></Form.Item>
              <Form.Item name="hometown" label="Nguyên quán"><Input /></Form.Item>
              <OccupantRelationshipFields
                form={form}
                ownerName="ownerRelationship"
                deceasedName="deceasedRelationship"
                genderName="gender"
                ownerGender={ownerProfile?.profile.gender}
              />
              <Form.Item name="notes" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
            </>
          ) : (
            <>
              {candidates && candidates.length === 0 && (
                <Alert
                  type="warning"
                  showIcon
                  style={{ marginBottom: 12 }}
                  message="Chưa có ứng viên"
                  description="Người an táng phải là khách hàng ĐÃ MẤT có quan hệ gia đình với chủ mộ và chưa nằm mộ nào. Hãy đánh dấu khách đã mất + khai quan hệ với chủ mộ trước."
                />
              )}
              <Form.Item
                name="deceasedCustomerId"
                label="Người an táng (khách đã mất, có quan hệ với chủ mộ)"
                rules={[{ required: true, message: 'Chọn người an táng' }]}
              >
                <Select
                  showSearch
                  optionFilterProp="label"
                  placeholder="Chọn người đã mất..."
                  data-testid="occupant-candidate-select"
                  options={(candidates ?? []).map((c) => ({
                    label: `${c.fullName} (${c.customerCode}) — ${c.relationLabel}`,
                    value: c.customerId,
                  }))}
                />
              </Form.Item>
              <Form.Item name="burialDate" label="Ngày an táng"><DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" /></Form.Item>
              <Form.Item name="notes" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
            </>
          )}
        </Form>
      </Modal>

      <Modal
        title="Bốc / Cải táng"
        open={!!relocating}
        onCancel={() => { setRelocating(null); relocForm.resetFields(); }}
        onOk={() => relocForm.submit()}
        confirmLoading={relocateMutation.isPending}
        okText="Xác nhận bốc"
        cancelText="Hủy"
        destroyOnHidden
      >
        {relocating && (
          <Form form={relocForm} layout="vertical" onFinish={(v) => relocateMutation.mutate(v)}>
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message={`Bốc/cải táng: ${relocating.fullName}`}
              description="Suất này sẽ chuyển 'đã bốc', giải phóng người (được đặt sang mộ khác) và chỗ trong mộ."
            />
            <Form.Item name="relocatedAt" label="Ngày bốc/cải táng">
              <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
            </Form.Item>
            <Form.Item name="note" label="Lý do / ghi chú">
              <Input.TextArea rows={2} placeholder="VD: cải táng về quê, dồn mộ..." />
            </Form.Item>
          </Form>
        )}
      </Modal>

      <Modal
        title={ecEditing ? 'Sửa liên hệ khẩn cấp' : 'Thêm liên hệ khẩn cấp'}
        open={ecModalOpen}
        onCancel={() => { setEcModalOpen(false); setEcEditing(null); setEcOptions([]); }}
        onOk={() => ecForm.submit()}
        confirmLoading={ecMutation.isPending}
        okText="Lưu"
        cancelText="Hủy"
        destroyOnHidden
      >
        <Alert type="info" showIcon style={{ marginBottom: 12 }}
          message="Người liên hệ khẩn cấp phải là khách hàng — chọn trong danh sách. SĐT tự lấy theo hồ sơ khách hàng." />
        <Form form={ecForm} layout="vertical" onFinish={(v) => ecMutation.mutate(v)}>
          <Form.Item name="contactCustomerId" label="Khách hàng liên hệ" rules={[{ required: true, message: 'Chọn khách hàng' }]}>
            <Select
              showSearch
              placeholder="Gõ tên / mã khách hàng để tìm..."
              filterOption={false}
              onSearch={handleEcSearch}
              loading={ecSearching}
              notFoundContent={ecSearching ? <Spin size="small" /> : null}
              options={ecOptions.map((c) => ({
                value: c.id,
                label: `${c.fullName} (${c.customerCode})${c.phone ? ' · ' + c.phone : ''}`,
              }))}
              data-testid="emergency-contact-select"
            />
          </Form.Item>
          <Form.Item name="relationshipNote" label="Quan hệ với chủ mộ / ghi chú" rules={[{ max: 100 }]}>
            <Input placeholder="VD: Con trai · gọi trước tiên" />
          </Form.Item>
        </Form>
      </Modal>

      <Drawer
        title={viewingContact ? `Liên hệ khẩn cấp: ${viewingContact.contactName}` : 'Liên hệ khẩn cấp'}
        open={viewingContact != null}
        onClose={() => setViewingContact(null)}
        width={480}
        extra={viewingContact?.contactCustomerId && (
          <Button type="primary">
            <Link to={`/customers/${viewingContact.contactCustomerId}`}>Mở hồ sơ khách hàng →</Link>
          </Button>
        )}
      >
        {viewingContact && (
          viewingContactLoading ? <Spin /> : (
            <Descriptions column={1} bordered size="small">
              <Descriptions.Item label="Họ và tên">
                {viewingContactDetail?.profile.fullName ?? viewingContact.contactName}
              </Descriptions.Item>
              <Descriptions.Item label="Mã khách hàng">{viewingContact.contactCode ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Số điện thoại">{viewingContactDetail?.profile.phone ?? viewingContact.contactPhone ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="CCCD">{viewingContactDetail?.profile.cccd ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Giới tính">
                {viewingContactDetail?.profile.gender ? GENDERS[viewingContactDetail.profile.gender] ?? viewingContactDetail.profile.gender : '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Địa chỉ thường trú">{viewingContactDetail?.profile.permanentAddress ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Ưu tiên gọi">{viewingContact.priority === 1 ? 'Gọi trước tiên' : `Ưu tiên ${viewingContact.priority}`}</Descriptions.Item>
              <Descriptions.Item label="Quan hệ / ghi chú">{viewingContact.relationshipNote ?? '—'}</Descriptions.Item>
            </Descriptions>
          )
        )}
      </Drawer>

      <Drawer
        title={viewing ? `Chi tiết cốt: ${viewing.fullName}` : 'Chi tiết cốt'}
        open={viewing != null}
        onClose={() => setViewing(null)}
        width={480}
        extra={canUpdate && viewing && (
          <Button type="primary" icon={<EditOutlined />}
            onClick={() => { const o = viewing; setViewing(null); openEdit(o); }}>
            Sửa
          </Button>
        )}
      >
        {viewing && (
          <Descriptions column={1} bordered size="small">
            <Descriptions.Item label="Họ và tên">{viewing.fullName}</Descriptions.Item>
            <Descriptions.Item label="Hồ sơ khách hàng">
              {viewing.deceasedCustomerId ? (
                <Link to={`/customers/${viewing.deceasedCustomerId}`}>Xem khách hàng (đã mất) →</Link>
              ) : (
                <span style={{ color: '#888' }}>Chưa liên kết</span>
              )}
            </Descriptions.Item>
            <Descriptions.Item label="Giới tính">{viewing.gender ? GENDERS[viewing.gender] ?? viewing.gender : '—'}</Descriptions.Item>
            <Descriptions.Item label="Ngày sinh">{fmtDate(viewing.dob)}</Descriptions.Item>
            <Descriptions.Item label="Ngày mất (Dương lịch)">{fmtDate(viewing.deathDateSolar)}</Descriptions.Item>
            <Descriptions.Item label="Ngày mất (Âm lịch)">{viewing.deathDateLunar ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Ngày an táng">{fmtDate(viewing.burialDate)}</Descriptions.Item>
            <Descriptions.Item label="Nguyên quán">{viewing.hometown ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Chủ mộ là">
              {viewing.ownerRelationship ? <Tag color="purple">{viewing.ownerRelationship}</Tag> : '—'}
              {data.ownerName ? <span style={{ color: '#888' }}> của người mất</span> : null}
            </Descriptions.Item>
            <Descriptions.Item label="Người mất là">
              {viewing.deceasedRelationship ? <Tag color="magenta">{viewing.deceasedRelationship}</Tag> : '—'}
              {data.ownerName ? <span style={{ color: '#888' }}> của chủ mộ {data.ownerName}</span> : null}
            </Descriptions.Item>
            <Descriptions.Item label="Ghi chú">{viewing.notes ?? '—'}</Descriptions.Item>
          </Descriptions>
        )}
      </Drawer>
    </div>
  );
};

export default GraveDetailPage;
