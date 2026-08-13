-- ============================================================
-- PTKD-ERP Production Seed Data
-- 1 tập đoàn + 5 công ty con, ~175 users, 1M khách hàng, dịch vụ
-- Chạy sau seed-sample-data.sql và bootstrap admin.
-- Idempotent: mỗi block có IF NOT EXISTS guard.
-- ============================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- ============================================================
-- PHẦN 1: TẬP ĐOÀN + CÔNG TY CON
-- ============================================================

-- 1a. Tạo công ty mẹ (tập đoàn)
IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE company_code = 'INDEVCO-GROUP')
BEGIN
    INSERT INTO dbo.Companies (company_code, name, tax_code, is_active, created_at)
    VALUES ('INDEVCO-GROUP', N'Tập đoàn INDEVCO', '0100000001', 1, GETUTCDATE());
    PRINT 'Created INDEVCO-GROUP';
END
GO

-- 1b. Gán 3 công ty hiện có làm con của tập đoàn
DECLARE @groupId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-GROUP');

IF @groupId IS NOT NULL
BEGIN
    UPDATE dbo.Companies SET parent_company_id = @groupId, updated_at = GETUTCDATE()
    WHERE company_code IN ('INDEVCO-HN', 'INDEVCO-HCM', 'INDEVCO-DN')
      AND parent_company_id IS NULL;

    IF @@ROWCOUNT > 0 PRINT 'Updated parent for existing subsidiaries';
END
GO

-- 1c. Tạo 2 công ty mới
DECLARE @groupId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-GROUP');

IF @groupId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE company_code = 'INDEVCO-HP')
BEGIN
    INSERT INTO dbo.Companies (company_code, parent_company_id, name, tax_code, is_active, created_at)
    VALUES
        ('INDEVCO-HP', @groupId, N'INDEVCO Hải Phòng', '0200555666', 1, GETUTCDATE()),
        ('INDEVCO-CT', @groupId, N'INDEVCO Cần Thơ', '0900777888', 1, GETUTCDATE());
    PRINT 'Created INDEVCO-HP, INDEVCO-CT';
END
GO

-- ============================================================
-- PHẦN 2: PHÒNG BAN
-- ============================================================
DECLARE @dnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-DN');
DECLARE @hpId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HP');
DECLARE @ctId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-CT');
DECLARE @hnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HN');
DECLARE @hcmId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HCM');
DECLARE @groupId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-GROUP');

-- Đà Nẵng (hiện chưa có phòng ban trong migration seed)
IF @dnId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE department_code = 'DN-SALES2')
BEGIN
    -- Check if DN departments from API seed exist, if not create standard set
    IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE company_id = @dnId AND department_code LIKE 'DN-%')
    BEGIN
        INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
        VALUES
            ('DN-SALES2', @dnId, N'Phòng Kinh doanh ĐN', 1, GETUTCDATE()),
            ('DN-CS2', @dnId, N'Phòng Chăm sóc KH ĐN', 1, GETUTCDATE()),
            ('DN-FINANCE2', @dnId, N'Phòng Tài chính ĐN', 1, GETUTCDATE()),
            ('DN-IT2', @dnId, N'Phòng CNTT ĐN', 1, GETUTCDATE());
    END
    ELSE
    BEGIN
        -- DN departments already exist from API seed, just ensure IT exists
        IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE company_id = @dnId AND department_code = 'DN-IT')
            INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
            VALUES ('DN-IT', @dnId, N'Phòng CNTT ĐN', 1, GETUTCDATE());
    END
END

-- Hải Phòng
IF @hpId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE department_code = 'HP-SALES')
BEGIN
    INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
    VALUES
        ('HP-SALES', @hpId, N'Phòng Kinh doanh HP', 1, GETUTCDATE()),
        ('HP-CS', @hpId, N'Phòng Chăm sóc KH HP', 1, GETUTCDATE()),
        ('HP-FINANCE', @hpId, N'Phòng Tài chính HP', 1, GETUTCDATE()),
        ('HP-IT', @hpId, N'Phòng CNTT HP', 1, GETUTCDATE());
    PRINT 'Created HP departments';
END

-- Cần Thơ
IF @ctId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE department_code = 'CT-SALES')
BEGIN
    INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
    VALUES
        ('CT-SALES', @ctId, N'Phòng Kinh doanh CT', 1, GETUTCDATE()),
        ('CT-CS', @ctId, N'Phòng Chăm sóc KH CT', 1, GETUTCDATE()),
        ('CT-FINANCE', @ctId, N'Phòng Tài chính CT', 1, GETUTCDATE()),
        ('CT-IT', @ctId, N'Phòng CNTT CT', 1, GETUTCDATE());
    PRINT 'Created CT departments';
END

-- Bổ sung phòng IT cho HN, HCM nếu chưa có
IF @hnId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE department_code = 'HN-IT2')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE company_id = @hnId AND department_code = 'HN-IT')
        INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
        VALUES ('HN-IT2', @hnId, N'Phòng CNTT HN', 1, GETUTCDATE());
END

IF @hcmId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE department_code = 'HCM-FINANCE2')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE company_id = @hcmId AND department_code LIKE 'HCM-FINANCE%')
        INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
        VALUES ('HCM-FINANCE2', @hcmId, N'Phòng Tài chính HCM', 1, GETUTCDATE());
    IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE company_id = @hcmId AND department_code LIKE 'HCM-IT%')
        INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
        VALUES ('HCM-IT2', @hcmId, N'Phòng CNTT HCM', 1, GETUTCDATE());
END

-- Tập đoàn (GROUP) — phòng ban tập đoàn
IF @groupId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE department_code = 'GRP-BOD')
BEGIN
    INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
    VALUES
        ('GRP-BOD', @groupId, N'Ban Giám đốc', 1, GETUTCDATE()),
        ('GRP-FINANCE', @groupId, N'Phòng Tài chính Tập đoàn', 1, GETUTCDATE()),
        ('GRP-HR', @groupId, N'Phòng Nhân sự Tập đoàn', 1, GETUTCDATE()),
        ('GRP-IT', @groupId, N'Phòng CNTT Tập đoàn', 1, GETUTCDATE()),
        ('GRP-LEGAL', @groupId, N'Phòng Pháp chế', 1, GETUTCDATE()),
        ('GRP-AUDIT', @groupId, N'Phòng Kiểm toán nội bộ', 1, GETUTCDATE());
    PRINT 'Created GROUP departments';
END
GO

-- ============================================================
-- PHẦN 3: USERS (~175 nhân viên)
-- ============================================================
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE employee_code = 'NV009')
BEGIN
    -- Tập đoàn (50 NV): NV009-NV058
    INSERT INTO dbo.Users (employee_code, full_name, email, employment_status, account_status, created_at, created_by_user_id)
    VALUES
    -- Ban Giám đốc (5)
    ('NV009', N'Trương Quốc Bảo', 'bao.tq@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV010', N'Ngô Thanh Tâm', 'tam.nt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV011', N'Đinh Văn Khoa', 'khoa.dv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV012', N'Lý Thị Phương', 'phuong.lt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV013', N'Hồ Minh Trí', 'tri.hm@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    -- Tài chính TĐ (10)
    ('NV014', N'Vương Thị Hạnh', 'hanh.vt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV015', N'Cao Đức Long', 'long.cd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV016', N'Tạ Thị Ngọc', 'ngoc.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV017', N'Dương Văn Thành', 'thanh.dv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV018', N'Châu Thị Liên', 'lien.ct@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV019', N'Mạc Hữu Đạt', 'dat.mh@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV020', N'Kiều Thị Thảo', 'thao.kt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV021', N'Lương Quang Vinh', 'vinh.lq@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV022', N'Trịnh Thị Xuân', 'xuan.tt2@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV023', N'Phan Anh Tuấn', 'tuan.pa@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    -- Nhân sự TĐ (10)
    ('NV024', N'Đoàn Thị Yến', 'yen.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV025', N'Nghiêm Xuân Phúc', 'phuc.nx@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV026', N'Quách Thị Hồng', 'hong.qt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV027', N'Tô Đình Nam', 'nam.td@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV028', N'La Thị Kim', 'kim.lt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV029', N'Thái Bá Cường', 'cuong.tb@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV030', N'Mai Thị Thanh', 'thanh.mt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV031', N'Từ Văn Hải', 'hai.tv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV032', N'Khuất Thị Lan', 'lan.kt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV033', N'Uông Đức Minh', 'minh.ud@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    -- CNTT TĐ (15)
    ('NV034', N'Phùng Văn Sơn', 'son.pv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV035', N'Đặng Quốc Huy', 'huy.dq@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV036', N'Nguyễn Thị Diệu', 'dieu.nt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV037', N'Lê Bá Phong', 'phong.lb@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV038', N'Trần Thị Oanh', 'oanh.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV039', N'Hoàng Anh Dũng', 'dung.ha@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV040', N'Vũ Thị Hiền', 'hien.vt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV041', N'Bùi Đức Trung', 'trung.bd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV042', N'Đỗ Thị Vân', 'van.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV043', N'Phạm Hồng Quân', 'quan.ph@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV044', N'Ngô Thị Trang', 'trang.nt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV045', N'Lý Văn Đức', 'duc.lv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV046', N'Hà Thị Nhung', 'nhung.ht@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV047', N'Đinh Quốc Thắng', 'thang.dq@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV048', N'Trương Thị Ánh', 'anh.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    -- Pháp chế (5)
    ('NV049', N'Cao Thị Tuyết', 'tuyet.ct@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV050', N'Tạ Đình Lộc', 'loc.td@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV051', N'Dương Thị Hương', 'huong.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV052', N'Châu Văn Bình', 'binh.cv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV053', N'Mạc Thị Ngân', 'ngan.mt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    -- Kiểm toán (5)
    ('NV054', N'Kiều Đức Anh', 'anh.kd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV055', N'Lương Thị Hà', 'ha.lt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV056', N'Trịnh Văn Quý', 'quy.tv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV057', N'Phan Thị Cúc', 'cuc.pt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV058', N'Đoàn Minh Khôi', 'khoi.dm@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId);

    -- Hà Nội thêm NV (25): NV059-NV083
    INSERT INTO dbo.Users (employee_code, full_name, email, employment_status, account_status, created_at, created_by_user_id)
    VALUES
    ('NV059', N'Nguyễn Hữu Nghĩa', 'nghia.nh@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV060', N'Trần Thị Phượng', 'phuong.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV061', N'Lê Văn Tùng', 'tung.lv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV062', N'Phạm Thị Nga', 'nga.pt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV063', N'Hoàng Đình Lợi', 'loi.hd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV064', N'Vũ Thị Hằng', 'hang.vt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV065', N'Bùi Trung Kiên', 'kien.bt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV066', N'Đỗ Thị Linh', 'linh.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV067', N'Ngô Quang Hợp', 'hop.nq@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV068', N'Lý Thị Sen', 'sen.lt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV069', N'Hồ Đức Thịnh', 'thinh.hd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV070', N'Đinh Thị Huệ', 'hue.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV071', N'Trương Văn Giang', 'giang.tv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV072', N'Cao Thị Cẩm', 'cam.ct@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV073', N'Tạ Hồng Phát', 'phat.th@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV074', N'Dương Thị Mỹ', 'my.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV075', N'Châu Đức Trọng', 'trong.cd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV076', N'Mạc Thị Đào', 'dao.mt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV077', N'Kiều Văn Lực', 'luc.kv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV078', N'Lương Thị Xuân', 'xuan.lt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV079', N'Trịnh Đức Tài', 'tai.td@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV080', N'Phan Thị Bích', 'bich.pt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV081', N'Đoàn Văn Sáng', 'sang.dv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV082', N'Nghiêm Thị Châu', 'chau.nt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV083', N'Quách Hữu Đăng', 'dang.qh@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId);

    -- HCM thêm NV (25): NV084-NV108
    INSERT INTO dbo.Users (employee_code, full_name, email, employment_status, account_status, created_at, created_by_user_id)
    VALUES
    ('NV084', N'Tô Thị Hiền', 'hien.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV085', N'La Đức Hoàng', 'hoang.ld@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV086', N'Thái Thị Loan', 'loan.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV087', N'Mai Văn Phú', 'phu.mv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV088', N'Từ Thị Yên', 'yen.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV089', N'Khuất Đức Hòa', 'hoa.kd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV090', N'Uông Thị Duyên', 'duyen.ut@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV091', N'Phùng Hồng Sơn', 'son.ph@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV092', N'Đặng Thị Thoa', 'thoa.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV093', N'Nguyễn Bá Toàn', 'toan.nb@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV094', N'Trần Thị Dung', 'dung.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV095', N'Lê Đình Phước', 'phuoc.ld@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV096', N'Phạm Thị Quyên', 'quyen.pt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV097', N'Hoàng Văn Tiến', 'tien.hv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV098', N'Vũ Thị Nhi', 'nhi.vt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV099', N'Bùi Quốc Bình', 'binh.bq@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV100', N'Đỗ Thị Ngà', 'nga.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV101', N'Ngô Đức Mạnh', 'manh.nd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV102', N'Lý Thị Thúy', 'thuy.lt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV103', N'Hồ Văn Chiến', 'chien.hv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV104', N'Đinh Thị Nguyệt', 'nguyet.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV105', N'Trương Quang Hiếu', 'hieu.tq@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV106', N'Cao Thị Hoa', 'hoa.ct@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV107', N'Tạ Minh Đức', 'duc.tm@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV108', N'Dương Thị Phúc', 'phuc.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId);

    -- Đà Nẵng (20): NV109-NV128
    INSERT INTO dbo.Users (employee_code, full_name, email, employment_status, account_status, created_at, created_by_user_id)
    VALUES
    ('NV109', N'Châu Văn Định', 'dinh.cv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV110', N'Mạc Thị Hảo', 'hao.mt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV111', N'Kiều Đức Phong', 'phong.kd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV112', N'Lương Thị Bảo', 'bao.lt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV113', N'Trịnh Văn Tân', 'tan.tv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV114', N'Phan Thị Thu', 'thu.pt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV115', N'Đoàn Hữu Thắng', 'thang.dh@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV116', N'Nghiêm Thị Phương', 'phuong.nt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV117', N'Quách Đình Lâm', 'lam.qd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV118', N'Tô Thị Uyên', 'uyen.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV119', N'La Văn Hùng', 'hung.lv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV120', N'Thái Thị Diễm', 'diem.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV121', N'Mai Đức Tâm', 'tam.md@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV122', N'Từ Thị Nhàn', 'nhan.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV123', N'Khuất Hồng Quang', 'quang.kh@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV124', N'Uông Thị Mai', 'mai.ut@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV125', N'Phùng Đức Dương', 'duong.pd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV126', N'Đặng Thị Trâm', 'tram.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV127', N'Nguyễn Văn Khải', 'khai.nv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV128', N'Trần Thị Huyền', 'huyen.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId);

    -- Hải Phòng (25): NV129-NV153
    INSERT INTO dbo.Users (employee_code, full_name, email, employment_status, account_status, created_at, created_by_user_id)
    VALUES
    ('NV129', N'Lê Thị Phương Anh', 'phuonganh.lt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV130', N'Phạm Đức Cảnh', 'canh.pd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV131', N'Hoàng Thị Lệ', 'le.ht@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV132', N'Vũ Minh Tuệ', 'tue.vm@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV133', N'Bùi Thị Ái', 'ai.bt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV134', N'Đỗ Văn Hữu', 'huu.dv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV135', N'Ngô Thị Bích', 'bich.nt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV136', N'Lý Hồng Khánh', 'khanh.lh@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV137', N'Hồ Thị Quyên', 'quyen.ht@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV138', N'Đinh Đức Nghĩa', 'nghia.dd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV139', N'Trương Thị Hợp', 'hop.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV140', N'Cao Văn Tín', 'tin.cv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV141', N'Tạ Thị Phương', 'phuong.tat@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV142', N'Dương Đức Hậu', 'hau.dd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV143', N'Châu Thị Giang', 'giang.ct@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV144', N'Mạc Hồng Phúc', 'phuc.mh@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV145', N'Kiều Thị Nhạn', 'nhan.kt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV146', N'Lương Văn Vĩnh', 'vinh.lv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV147', N'Trịnh Thị Lam', 'lam.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV148', N'Phan Đức Thông', 'thong.pd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV149', N'Đoàn Thị Ánh', 'anh.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV150', N'Nghiêm Văn Đạo', 'dao.nv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV151', N'Quách Thị Hạnh', 'hanh.qt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV152', N'Tô Đức Sỹ', 'sy.td@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV153', N'La Thị Thủy', 'thuy.lat@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId);

    -- Cần Thơ (25): NV154-NV178
    INSERT INTO dbo.Users (employee_code, full_name, email, employment_status, account_status, created_at, created_by_user_id)
    VALUES
    ('NV154', N'Thái Văn Luân', 'luan.tv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV155', N'Mai Thị Nguyệt', 'nguyet.mt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV156', N'Từ Đức Hiệp', 'hiep.td@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV157', N'Khuất Thị Thương', 'thuong.kt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV158', N'Uông Văn Bằng', 'bang.uv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV159', N'Phùng Thị Liễu', 'lieu.pt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV160', N'Đặng Hữu Thái', 'thai.dh@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV161', N'Nguyễn Thị Hậu', 'hau.nt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV162', N'Trần Quốc Bảo', 'bao.tq2@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV163', N'Lê Thị Quynh', 'quynh.lt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV164', N'Phạm Đình Trí', 'tri.pd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV165', N'Hoàng Thị Mến', 'men.ht@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV166', N'Vũ Đức Thọ', 'tho.vd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV167', N'Bùi Thị Nở', 'no.bt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV168', N'Đỗ Quang Vinh', 'vinh.dq@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV169', N'Ngô Thị Phấn', 'phan.nt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV170', N'Lý Đức Nhân', 'nhan.ld@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV171', N'Hồ Thị Xuân', 'xuan.ht@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV172', N'Đinh Văn Đại', 'dai.dv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV173', N'Trương Thị Khánh', 'khanh.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV174', N'Cao Minh Châu', 'chau.cm@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV175', N'Tạ Thị Hoa', 'hoa.tat@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV176', N'Dương Văn Khiêm', 'khiem.dv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV177', N'Châu Thị Lan', 'lan.ct@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
    ('NV178', N'Mạc Đình Toàn', 'toan.md@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId);

    PRINT 'Created 170 users (NV009-NV178)';
END
GO

-- ============================================================
-- PHẦN 4: USER-COMPANY + USER-DEPARTMENT ASSIGNMENTS
-- ============================================================
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
DECLARE @groupId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-GROUP');
DECLARE @hnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HN');
DECLARE @hcmId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HCM');
DECLARE @dnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-DN');
DECLARE @hpId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HP');
DECLARE @ctId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-CT');

-- Also assign admin to GROUP and all companies
IF @groupId IS NOT NULL AND @adminId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments WHERE user_id = @adminId AND company_id = @groupId)
BEGIN
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    VALUES (@adminId, @groupId, 0, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId);
END

-- Assign admin to all new companies
IF NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments WHERE user_id = @adminId AND company_id = @hpId) AND @hpId IS NOT NULL
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    VALUES (@adminId, @hpId, 0, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId);
IF NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments WHERE user_id = @adminId AND company_id = @ctId) AND @ctId IS NOT NULL
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    VALUES (@adminId, @ctId, 0, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId);
IF NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments WHERE user_id = @adminId AND company_id = @dnId) AND @dnId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments WHERE user_id = @adminId AND company_id = @dnId)
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    VALUES (@adminId, @dnId, 0, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId);
IF NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments WHERE user_id = @adminId AND company_id = @hcmId) AND @hcmId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments WHERE user_id = @adminId AND company_id = @hcmId)
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    VALUES (@adminId, @hcmId, 0, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId);

-- Batch assign new users to companies using employee_code ranges
IF NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments uca
               INNER JOIN dbo.Users u ON u.id = uca.user_id
               WHERE u.employee_code = 'NV009')
BEGIN
    -- GROUP: NV009-NV058
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    SELECT u.id, @groupId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId
    FROM dbo.Users u
    WHERE u.employee_code >= 'NV009' AND u.employee_code <= 'NV058'
      AND @groupId IS NOT NULL;

    -- HN: NV059-NV083
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    SELECT u.id, @hnId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId
    FROM dbo.Users u
    WHERE u.employee_code >= 'NV059' AND u.employee_code <= 'NV083'
      AND @hnId IS NOT NULL;

    -- HCM: NV084-NV108
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    SELECT u.id, @hcmId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId
    FROM dbo.Users u
    WHERE u.employee_code >= 'NV084' AND u.employee_code <= 'NV108'
      AND @hcmId IS NOT NULL;

    -- DN: NV109-NV128
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    SELECT u.id, @dnId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId
    FROM dbo.Users u
    WHERE u.employee_code >= 'NV109' AND u.employee_code <= 'NV128'
      AND @dnId IS NOT NULL;

    -- HP: NV129-NV153
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    SELECT u.id, @hpId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId
    FROM dbo.Users u
    WHERE u.employee_code >= 'NV129' AND u.employee_code <= 'NV153'
      AND @hpId IS NOT NULL;

    -- CT: NV154-NV178
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    SELECT u.id, @ctId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId
    FROM dbo.Users u
    WHERE u.employee_code >= 'NV154' AND u.employee_code <= 'NV178'
      AND @ctId IS NOT NULL;

    PRINT 'Created User_Company_Assignments for new users';
END
GO

-- Department assignments for new users
-- Strategy: distribute users across departments in their company
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF NOT EXISTS (SELECT 1 FROM dbo.User_Department_Assignments uda
               INNER JOIN dbo.Users u ON u.id = uda.user_id
               WHERE u.employee_code = 'NV009')
BEGIN
    INSERT INTO dbo.User_Department_Assignments (user_id, department_id, user_company_assignment_id, company_id, is_primary_for_company, assignment_status, effective_from, created_at, created_by_user_id)
    SELECT
        u.id,
        d.id,
        uca.id,
        uca.company_id,
        1,
        'ACTIVE',
        GETUTCDATE(),
        GETUTCDATE(),
        @adminId
    FROM dbo.Users u
    INNER JOIN dbo.User_Company_Assignments uca ON uca.user_id = u.id AND uca.assignment_status = 'ACTIVE'
    CROSS APPLY (
        SELECT TOP 1 d2.id
        FROM dbo.Departments d2
        WHERE d2.company_id = uca.company_id AND d2.is_active = 1
        ORDER BY ABS(CHECKSUM(CAST(u.id AS VARCHAR) + CAST(d2.id AS VARCHAR))) % 97
    ) d
    WHERE u.employee_code >= 'NV009' AND u.employee_code <= 'NV178'
      AND NOT EXISTS (SELECT 1 FROM dbo.User_Department_Assignments uda2 WHERE uda2.user_id = u.id);

    PRINT 'Created User_Department_Assignments for new users';
END
GO

-- ============================================================
-- PHẦN 5: AUTH ACCOUNTS cho tất cả users mới
-- Dùng password hash của admin (Halong@12345), must_change_password=1
-- ============================================================
DECLARE @adminHash NVARCHAR(500) = (SELECT password_hash FROM dbo.User_Auth_Accounts WHERE provider_subject = 'admin@ptkd.local');

IF @adminHash IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM dbo.User_Auth_Accounts uaa
    INNER JOIN dbo.Users u ON u.id = uaa.user_id
    WHERE u.employee_code = 'NV009'
)
BEGIN
    INSERT INTO dbo.User_Auth_Accounts (user_id, provider_type, provider_subject, password_hash, auth_account_status, failed_attempt_count, must_change_password, security_stamp, created_at)
    SELECT
        u.id,
        'INTERNAL',
        u.email,
        @adminHash,
        'ACTIVE',
        0,
        1,
        UPPER(NEWID()),
        GETUTCDATE()
    FROM dbo.Users u
    WHERE u.employee_code >= 'NV009' AND u.employee_code <= 'NV178'
      AND u.email IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.User_Auth_Accounts uaa WHERE uaa.user_id = u.id);

    PRINT 'Created auth accounts for all new users';
END
GO

-- ============================================================
-- PHẦN 6: SERVICE TYPES mới
-- ============================================================
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF NOT EXISTS (SELECT 1 FROM dbo.Service_Types WHERE code = 'BAO_TRI')
BEGIN
    INSERT INTO dbo.Service_Types (code, name, description, standard_price, standard_price_currency, cycle_duration_months, is_active, created_at, created_by_user_id)
    VALUES
        ('BAO_TRI', N'Bảo trì công trình', N'Dịch vụ bảo trì, sửa chữa công trình mộ phần', 3500000.00, 'VND', 12, 1, GETUTCDATE(), @adminId),
        ('LUU_TRU', N'Lưu trữ tro cốt', N'Dịch vụ lưu trữ tro cốt trong nhà lưu giữ', 5000000.00, 'VND', 12, 1, GETUTCDATE(), @adminId),
        ('LE_GIO', N'Tổ chức lễ giỗ', N'Dịch vụ tổ chức lễ giỗ tại nghĩa trang', 1500000.00, 'VND', NULL, 1, GETUTCDATE(), @adminId);
    PRINT 'Created 3 new service types';
END
GO

-- ============================================================
-- PHẦN 7: 1 TRIỆU KHÁCH HÀNG (batch 10,000)
-- ============================================================

-- Vietnamese name components for random generation
IF OBJECT_ID('tempdb..#LastNames') IS NOT NULL DROP TABLE #LastNames;
IF OBJECT_ID('tempdb..#MiddleNames') IS NOT NULL DROP TABLE #MiddleNames;
IF OBJECT_ID('tempdb..#FirstNamesMale') IS NOT NULL DROP TABLE #FirstNamesMale;
IF OBJECT_ID('tempdb..#FirstNamesFemale') IS NOT NULL DROP TABLE #FirstNamesFemale;
IF OBJECT_ID('tempdb..#Streets') IS NOT NULL DROP TABLE #Streets;
IF OBJECT_ID('tempdb..#Districts') IS NOT NULL DROP TABLE #Districts;

CREATE TABLE #LastNames (id INT IDENTITY(1,1), name NVARCHAR(50));
INSERT INTO #LastNames (name) VALUES
(N'Nguyễn'),(N'Trần'),(N'Lê'),(N'Phạm'),(N'Hoàng'),(N'Vũ'),(N'Võ'),(N'Đặng'),
(N'Bùi'),(N'Đỗ'),(N'Hồ'),(N'Ngô'),(N'Dương'),(N'Lý'),(N'Đinh'),(N'Trương'),
(N'Cao'),(N'Tạ'),(N'Châu'),(N'Phan');

CREATE TABLE #MiddleNames (id INT IDENTITY(1,1), name NVARCHAR(50));
INSERT INTO #MiddleNames (name) VALUES
(N'Văn'),(N'Thị'),(N'Đức'),(N'Hồng'),(N'Minh'),(N'Quốc'),(N'Thanh'),(N'Hữu'),
(N'Đình'),(N'Xuân');

CREATE TABLE #FirstNamesMale (id INT IDENTITY(1,1), name NVARCHAR(50));
INSERT INTO #FirstNamesMale (name) VALUES
(N'An'),(N'Bình'),(N'Cường'),(N'Dũng'),(N'Đức'),(N'Giang'),(N'Hải'),(N'Hùng'),
(N'Khoa'),(N'Lâm'),(N'Long'),(N'Minh'),(N'Nam'),(N'Phong'),(N'Quân'),(N'Sơn'),
(N'Tâm'),(N'Thắng'),(N'Trung'),(N'Tuấn'),(N'Vinh'),(N'Huy'),(N'Đạt'),(N'Kiên'),
(N'Nghĩa'),(N'Phúc'),(N'Tài'),(N'Thành'),(N'Trọng'),(N'Vượng');

CREATE TABLE #FirstNamesFemale (id INT IDENTITY(1,1), name NVARCHAR(50));
INSERT INTO #FirstNamesFemale (name) VALUES
(N'Anh'),(N'Bích'),(N'Chi'),(N'Diễm'),(N'Hà'),(N'Hạnh'),(N'Hiền'),(N'Hoa'),
(N'Hương'),(N'Lan'),(N'Linh'),(N'Mai'),(N'Nga'),(N'Ngọc'),(N'Nhung'),(N'Oanh'),
(N'Phương'),(N'Quỳnh'),(N'Thảo'),(N'Thu'),(N'Thủy'),(N'Trang'),(N'Uyên'),(N'Vân'),
(N'Xuân'),(N'Yến'),(N'Duyên'),(N'Hằng'),(N'Loan'),(N'Tuyết');

CREATE TABLE #Streets (id INT IDENTITY(1,1), name NVARCHAR(100));
INSERT INTO #Streets (name) VALUES
(N'Lê Lợi'),(N'Nguyễn Huệ'),(N'Trần Hưng Đạo'),(N'Hai Bà Trưng'),(N'Lý Thường Kiệt'),
(N'Nguyễn Trãi'),(N'Điện Biên Phủ'),(N'Phan Chu Trinh'),(N'Bà Triệu'),(N'Láng Hạ'),
(N'Giải Phóng'),(N'Lê Duẩn'),(N'Nguyễn Văn Linh'),(N'Trường Chinh'),(N'Phạm Văn Đồng'),
(N'Hoàng Quốc Việt'),(N'Cầu Giấy'),(N'Kim Mã'),(N'Đội Cấn'),(N'Tây Sơn');

CREATE TABLE #Districts (id INT IDENTITY(1,1), name NVARCHAR(100), city NVARCHAR(50));
INSERT INTO #Districts (name, city) VALUES
(N'Hoàn Kiếm', N'Hà Nội'),(N'Ba Đình', N'Hà Nội'),(N'Đống Đa', N'Hà Nội'),
(N'Cầu Giấy', N'Hà Nội'),(N'Thanh Xuân', N'Hà Nội'),(N'Long Biên', N'Hà Nội'),
(N'Q.1', N'TP.HCM'),(N'Q.3', N'TP.HCM'),(N'Q.7', N'TP.HCM'),
(N'Bình Thạnh', N'TP.HCM'),(N'Thủ Đức', N'TP.HCM'),(N'Gò Vấp', N'TP.HCM'),
(N'Hải Châu', N'Đà Nẵng'),(N'Thanh Khê', N'Đà Nẵng'),(N'Sơn Trà', N'Đà Nẵng'),
(N'Ngô Quyền', N'Hải Phòng'),(N'Lê Chân', N'Hải Phòng'),(N'Hồng Bàng', N'Hải Phòng'),
(N'Ninh Kiều', N'Cần Thơ'),(N'Cái Răng', N'Cần Thơ');

DECLARE @lastNameCount INT = (SELECT COUNT(*) FROM #LastNames);
DECLARE @middleNameCount INT = (SELECT COUNT(*) FROM #MiddleNames);
DECLARE @firstNameMaleCount INT = (SELECT COUNT(*) FROM #FirstNamesMale);
DECLARE @firstNameFemaleCount INT = (SELECT COUNT(*) FROM #FirstNamesFemale);
DECLARE @streetCount INT = (SELECT COUNT(*) FROM #Streets);
DECLARE @districtCount INT = (SELECT COUNT(*) FROM #Districts);

-- Check existing customer count
DECLARE @existingCustomers INT = (SELECT COUNT(*) FROM dbo.Customers);
DECLARE @targetCustomers INT = 1000000;
DECLARE @toCreate INT = @targetCustomers - @existingCustomers + 10; -- +10 for existing ones

IF @existingCustomers < @targetCustomers
BEGIN
    DECLARE @batchSize INT = 10000;
    DECLARE @batchNum INT = 0;
    DECLARE @totalBatches INT = CEILING(CAST(@toCreate AS FLOAT) / @batchSize);
    DECLARE @startCode INT = @existingCustomers + 1;
    DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

    -- Get company IDs (exclude GROUP — customers belong to subsidiaries)
    IF OBJECT_ID('tempdb..#CompanyIds') IS NOT NULL DROP TABLE #CompanyIds;
    CREATE TABLE #CompanyIds (id BIGINT, idx INT IDENTITY(1,1));
    INSERT INTO #CompanyIds (id)
    SELECT id FROM dbo.Companies
    WHERE company_code IN ('INDEVCO-HN','INDEVCO-HCM','INDEVCO-DN','INDEVCO-HP','INDEVCO-CT')
    ORDER BY id;
    DECLARE @companyCount INT = (SELECT COUNT(*) FROM #CompanyIds);

    -- Get staff IDs per company for assignment
    IF OBJECT_ID('tempdb..#StaffByCompany') IS NOT NULL DROP TABLE #StaffByCompany;
    CREATE TABLE #StaffByCompany (company_id BIGINT, user_id BIGINT, idx INT IDENTITY(1,1));
    INSERT INTO #StaffByCompany (company_id, user_id)
    SELECT uca.company_id, u.id
    FROM dbo.Users u
    INNER JOIN dbo.User_Company_Assignments uca ON uca.user_id = u.id AND uca.assignment_status = 'ACTIVE'
    WHERE uca.company_id IN (SELECT id FROM #CompanyIds)
    ORDER BY uca.company_id, u.id;

    PRINT 'Starting customer seed: ' + CAST(@toCreate AS VARCHAR) + ' customers in ' + CAST(@totalBatches AS VARCHAR) + ' batches';

    WHILE @batchNum < @totalBatches
    BEGIN
        DECLARE @batchStart INT = @startCode + (@batchNum * @batchSize);
        DECLARE @batchEnd INT = @batchStart + @batchSize - 1;
        IF @batchEnd > @startCode + @toCreate - 1
            SET @batchEnd = @startCode + @toCreate - 1;

        BEGIN TRANSACTION;

        -- Generate profiles + customers + contexts in one batch
        ;WITH Numbers AS (
            SELECT @batchStart AS n
            UNION ALL
            SELECT n + 1 FROM Numbers WHERE n < @batchEnd
        )
        INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, is_active, created_at)
        SELECT
            ln.name + N' ' + mn.name + N' ' +
                CASE WHEN n % 2 = 0
                    THEN fm.name
                    ELSE ff.name
                END,
            RIGHT('0' + CAST(1 + (n % 63) AS VARCHAR), 2)
                + RIGHT('0' + CAST(1 + ((n / 63) % 99) AS VARCHAR), 2)
                + RIGHT('00000000' + CAST(n AS VARCHAR), 8),
            DATEADD(DAY, -(n % 23725) - 7305, GETUTCDATE()), -- DOB between 1961 and 2006
            'FULL',
            CASE WHEN n % 2 = 0 THEN 'MALE' ELSE 'FEMALE' END,
            '09' + RIGHT('00000000' + CAST(ABS(CHECKSUM(NEWID())) % 100000000 AS VARCHAR), 8),
            CAST((n % 200) + 1 AS NVARCHAR) + N' ' + st.name + N', ' + dt.name + N', ' + dt.city,
            1,
            GETUTCDATE()
        FROM Numbers
        INNER JOIN #LastNames ln ON ln.id = (n % @lastNameCount) + 1
        INNER JOIN #MiddleNames mn ON mn.id = (n / @lastNameCount % @middleNameCount) + 1
        INNER JOIN #FirstNamesMale fm ON fm.id = (n % @firstNameMaleCount) + 1
        INNER JOIN #FirstNamesFemale ff ON ff.id = (n % @firstNameFemaleCount) + 1
        INNER JOIN #Streets st ON st.id = (n % @streetCount) + 1
        INNER JOIN #Districts dt ON dt.id = (n % @districtCount) + 1
        OPTION (MAXRECURSION 10000);

        -- Create customer records for newly inserted profiles
        INSERT INTO dbo.Customers (customer_code, profile_id, customer_status, created_at)
        SELECT
            'KH-' + RIGHT('0000000' + CAST(ROW_NUMBER() OVER (ORDER BY p.id) + @batchStart - 1 AS VARCHAR), 7),
            p.id,
            CASE WHEN p.id % 20 = 0 THEN 'INACTIVE' ELSE 'ACTIVE' END,
            GETUTCDATE()
        FROM dbo.Profiles p
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Customers c WHERE c.profile_id = p.id)
          AND p.id > (SELECT ISNULL(MAX(profile_id), 0) FROM dbo.Customers)
        ORDER BY p.id;

        -- Create company contexts
        INSERT INTO dbo.Customer_Company_Contexts (customer_id, company_id, assigned_staff_id, relationship_status, first_interaction_at, created_at)
        SELECT
            c.id,
            ci.id,
            staff.user_id,
            CASE WHEN c.customer_status = 'ACTIVE' THEN 'ACTIVE' ELSE 'INACTIVE' END,
            DATEADD(DAY, -(c.id % 1095), GETUTCDATE()),
            GETUTCDATE()
        FROM dbo.Customers c
        CROSS APPLY (
            SELECT TOP 1 cid.id FROM #CompanyIds cid WHERE cid.idx = (c.id % @companyCount) + 1
        ) ci
        CROSS APPLY (
            SELECT TOP 1 sc.user_id FROM #StaffByCompany sc WHERE sc.company_id = ci.id ORDER BY ABS(CHECKSUM(CAST(c.id AS VARCHAR) + CAST(sc.idx AS VARCHAR))) % 97
        ) staff
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Customer_Company_Contexts ccc WHERE ccc.customer_id = c.id)
          AND c.customer_code >= 'KH-' + RIGHT('0000000' + CAST(@batchStart AS VARCHAR), 7);

        COMMIT TRANSACTION;

        SET @batchNum = @batchNum + 1;
        IF @batchNum % 10 = 0
            PRINT 'Completed batch ' + CAST(@batchNum AS VARCHAR) + '/' + CAST(@totalBatches AS VARCHAR);
    END

    DECLARE @finalCustomerCount INT = (SELECT COUNT(*) FROM dbo.Customers);
    PRINT 'Customer seed completed: ' + CAST(@finalCustomerCount AS VARCHAR) + ' total customers';
END
ELSE
    PRINT 'Customers already at target count (' + CAST(@existingCustomers AS VARCHAR) + ')';
GO

-- Cleanup temp tables
IF OBJECT_ID('tempdb..#LastNames') IS NOT NULL DROP TABLE #LastNames;
IF OBJECT_ID('tempdb..#MiddleNames') IS NOT NULL DROP TABLE #MiddleNames;
IF OBJECT_ID('tempdb..#FirstNamesMale') IS NOT NULL DROP TABLE #FirstNamesMale;
IF OBJECT_ID('tempdb..#FirstNamesFemale') IS NOT NULL DROP TABLE #FirstNamesFemale;
IF OBJECT_ID('tempdb..#Streets') IS NOT NULL DROP TABLE #Streets;
IF OBJECT_ID('tempdb..#Districts') IS NOT NULL DROP TABLE #Districts;
GO

-- ============================================================
-- PHẦN 8: SERVICES (~200k dịch vụ cho KH active)
-- ============================================================
DECLARE @existingServices INT = (SELECT COUNT(*) FROM dbo.Services);
DECLARE @targetServices INT = 200000;

IF @existingServices < @targetServices
BEGIN
    DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
    DECLARE @toCreateSvc INT = @targetServices - @existingServices;
    DECLARE @batchSize INT = 10000;
    DECLARE @batchNum INT = 0;
    DECLARE @totalBatches INT = CEILING(CAST(@toCreateSvc AS FLOAT) / @batchSize);

    -- Get service type IDs
    IF OBJECT_ID('tempdb..#StIds') IS NOT NULL DROP TABLE #StIds;
    CREATE TABLE #StIds (id BIGINT, price DECIMAL(18,2), cycle INT NULL, idx INT IDENTITY(1,1));
    INSERT INTO #StIds (id, price, cycle)
    SELECT id, standard_price, cycle_duration_months FROM dbo.Service_Types WHERE is_active = 1 ORDER BY id;
    DECLARE @stCount INT = (SELECT COUNT(*) FROM #StIds);

    -- Get active customers who don't have services yet
    IF OBJECT_ID('tempdb..#EligibleCustomers') IS NOT NULL DROP TABLE #EligibleCustomers;
    CREATE TABLE #EligibleCustomers (customer_id BIGINT, company_id BIGINT, idx INT IDENTITY(1,1));
    INSERT INTO #EligibleCustomers (customer_id, company_id)
    SELECT TOP (@toCreateSvc) c.id, ccc.company_id
    FROM dbo.Customers c
    INNER JOIN dbo.Customer_Company_Contexts ccc ON ccc.customer_id = c.id
    WHERE c.customer_status = 'ACTIVE'
      AND NOT EXISTS (SELECT 1 FROM dbo.Services s WHERE s.customer_id = c.id)
    ORDER BY c.id;

    DECLARE @eligibleCount INT = (SELECT COUNT(*) FROM #EligibleCustomers);
    SET @totalBatches = CEILING(CAST(@eligibleCount AS FLOAT) / @batchSize);

    PRINT 'Starting service seed: ' + CAST(@eligibleCount AS VARCHAR) + ' services in ' + CAST(@totalBatches AS VARCHAR) + ' batches';

    WHILE @batchNum < @totalBatches
    BEGIN
        DECLARE @bStart INT = (@batchNum * @batchSize) + 1;
        DECLARE @bEnd INT = @bStart + @batchSize - 1;

        BEGIN TRANSACTION;

        INSERT INTO dbo.Services (service_type_id, customer_id, company_id, status, applied_price, standard_price_snapshot, is_override_price, valid_from, valid_to, cycle_number, created_by_user_id, created_at)
        SELECT
            st.id,
            ec.customer_id,
            ec.company_id,
            CASE
                WHEN ec.idx % 10 < 7 THEN 'ACTIVE'
                WHEN ec.idx % 10 < 9 THEN 'EXPIRED'
                ELSE 'CANCELLED'
            END,
            CASE WHEN ec.idx % 7 = 0 THEN st.price * 0.9 ELSE st.price END,
            st.price,
            CASE WHEN ec.idx % 7 = 0 THEN 1 ELSE 0 END,
            DATEADD(MONTH, -(ec.idx % 24), GETUTCDATE()),
            CASE WHEN st.cycle IS NOT NULL
                THEN DATEADD(MONTH, st.cycle - (ec.idx % 24), GETUTCDATE())
                ELSE NULL
            END,
            1 + (ec.idx % 5),
            @adminId,
            GETUTCDATE()
        FROM #EligibleCustomers ec
        CROSS APPLY (
            SELECT TOP 1 s2.id, s2.price, s2.cycle FROM #StIds s2 WHERE s2.idx = (ec.idx % @stCount) + 1
        ) st
        WHERE ec.idx >= @bStart AND ec.idx <= @bEnd;

        COMMIT TRANSACTION;

        SET @batchNum = @batchNum + 1;
        IF @batchNum % 5 = 0
            PRINT 'Services batch ' + CAST(@batchNum AS VARCHAR) + '/' + CAST(@totalBatches AS VARCHAR);
    END

    DECLARE @finalServiceCount INT = (SELECT COUNT(*) FROM dbo.Services);
    PRINT 'Service seed completed: ' + CAST(@finalServiceCount AS VARCHAR) + ' total services';
END
GO

-- ============================================================
-- PHẦN 9: Cấp quyền toàn bộ cho admin trên tất cả công ty mới
-- ============================================================
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
BEGIN
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    SELECT @adminId, p.permission_code, 'GLOBAL', 'ALLOW', GETUTCDATE(), @adminId
    FROM dbo.Permissions p
    WHERE p.is_active = 1
      AND NOT EXISTS (
          SELECT 1 FROM dbo.User_Individual_Permissions uip
          WHERE uip.user_id = @adminId AND uip.permission_code = p.permission_code
      );
    PRINT 'Admin permissions synced';
END
GO

-- ============================================================
-- FINAL SUMMARY
-- ============================================================
SELECT 'Companies' AS [Table], COUNT(*) AS [Count] FROM dbo.Companies
UNION ALL SELECT 'Departments', COUNT(*) FROM dbo.Departments
UNION ALL SELECT 'Users', COUNT(*) FROM dbo.Users
UNION ALL SELECT 'User_Auth_Accounts', COUNT(*) FROM dbo.User_Auth_Accounts
UNION ALL SELECT 'User_Company_Assignments', COUNT(*) FROM dbo.User_Company_Assignments
UNION ALL SELECT 'User_Department_Assignments', COUNT(*) FROM dbo.User_Department_Assignments
UNION ALL SELECT 'Profiles', COUNT(*) FROM dbo.Profiles
UNION ALL SELECT 'Customers', COUNT(*) FROM dbo.Customers
UNION ALL SELECT 'Customer_Company_Contexts', COUNT(*) FROM dbo.Customer_Company_Contexts
UNION ALL SELECT 'Service_Types', COUNT(*) FROM dbo.Service_Types
UNION ALL SELECT 'Services', COUNT(*) FROM dbo.Services;
GO

PRINT '=== Production seed completed ===';
