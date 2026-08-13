-- PTKD-ERP Sample Data Seed
-- Dữ liệu mẫu cho demo/test. KHÔNG dùng cho production.
-- Chạy sau khi migration V0001-V0015 đã apply và bootstrap admin xong.

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- ============================================================
-- 1. Companies
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE company_code = 'INDEVCO-HN')
BEGIN
    INSERT INTO dbo.Companies (company_code, name, tax_code, is_active, created_at)
    VALUES
        ('INDEVCO-HN', N'INDEVCO Hà Nội', '0100123456', 1, GETUTCDATE()),
        ('INDEVCO-HCM', N'INDEVCO Hồ Chí Minh', '0300654321', 1, GETUTCDATE()),
        ('INDEVCO-DN', N'INDEVCO Đà Nẵng', '0400111222', 1, GETUTCDATE());
END
GO

-- ============================================================
-- 2. Departments
-- ============================================================
DECLARE @hnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HN');
DECLARE @hcmId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HCM');

IF @hnId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE department_code = 'HN-SALES')
BEGIN
    INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
    VALUES
        ('HN-SALES', @hnId, N'Phòng Kinh doanh HN', 1, GETUTCDATE()),
        ('HN-CS', @hnId, N'Phòng Chăm sóc KH HN', 1, GETUTCDATE()),
        ('HN-FINANCE', @hnId, N'Phòng Tài chính HN', 1, GETUTCDATE()),
        ('HN-IT', @hnId, N'Phòng CNTT HN', 1, GETUTCDATE());
END

IF @hcmId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE department_code = 'HCM-SALES')
BEGIN
    INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
    VALUES
        ('HCM-SALES', @hcmId, N'Phòng Kinh doanh HCM', 1, GETUTCDATE()),
        ('HCM-CS', @hcmId, N'Phòng Chăm sóc KH HCM', 1, GETUTCDATE());
END
GO

-- ============================================================
-- 3. Users (employees)
-- ============================================================
DECLARE @hnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HN');
DECLARE @hcmId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HCM');
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE employee_code = 'NV001')
BEGIN
    INSERT INTO dbo.Users (employee_code, full_name, email, employment_status, account_status, created_at, created_by_user_id)
    VALUES
        ('NV001', N'Nguyễn Văn An', 'an.nv@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
        ('NV002', N'Trần Thị Bình', 'binh.tt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
        ('NV003', N'Lê Hoàng Cường', 'cuong.lh@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
        ('NV004', N'Phạm Minh Dũng', 'dung.pm@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
        ('NV005', N'Hoàng Thị Hoa', 'hoa.ht@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
        ('NV006', N'Võ Đức Lâm', 'lam.vd@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
        ('NV007', N'Đặng Thị Mai', 'mai.dt@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId),
        ('NV008', N'Bùi Quang Tùng', 'tung.bq@indevco.vn', 'ACTIVE', 'ACTIVE', GETUTCDATE(), @adminId);
END
GO

-- ============================================================
-- 4. Assign admin to company
-- ============================================================
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
DECLARE @hnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HN');

IF @adminId IS NOT NULL AND @hnId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments WHERE user_id = @adminId AND company_id = @hnId)
BEGIN
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    VALUES (@adminId, @hnId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId);
END
GO

-- ============================================================
-- 5. Assign employees to companies + departments
-- ============================================================
DECLARE @hnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HN');
DECLARE @hcmId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HCM');
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

DECLARE @nv1 BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'NV001');
DECLARE @nv2 BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'NV002');
DECLARE @nv3 BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'NV003');
DECLARE @nv4 BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'NV004');
DECLARE @nv5 BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'NV005');
DECLARE @nv6 BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'NV006');

-- Assign to companies
IF @nv1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.User_Company_Assignments WHERE user_id = @nv1)
BEGIN
    INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
    VALUES
        (@nv1, @hnId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId),
        (@nv2, @hnId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId),
        (@nv3, @hnId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId),
        (@nv4, @hcmId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId),
        (@nv5, @hcmId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId),
        (@nv6, @hcmId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId);
END

-- Assign to departments
DECLARE @salesHN BIGINT = (SELECT id FROM dbo.Departments WHERE department_code = 'HN-SALES');
DECLARE @csHN BIGINT = (SELECT id FROM dbo.Departments WHERE department_code = 'HN-CS');
DECLARE @finHN BIGINT = (SELECT id FROM dbo.Departments WHERE department_code = 'HN-FINANCE');
DECLARE @salesHCM BIGINT = (SELECT id FROM dbo.Departments WHERE department_code = 'HCM-SALES');

IF @nv1 IS NOT NULL AND @salesHN IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.User_Department_Assignments WHERE user_id = @nv1)
BEGIN
    DECLARE @ucaHN1 BIGINT = (SELECT id FROM dbo.User_Company_Assignments WHERE user_id = @nv1 AND company_id = @hnId);
    DECLARE @ucaHN2 BIGINT = (SELECT id FROM dbo.User_Company_Assignments WHERE user_id = @nv2 AND company_id = @hnId);
    DECLARE @ucaHN3 BIGINT = (SELECT id FROM dbo.User_Company_Assignments WHERE user_id = @nv3 AND company_id = @hnId);
    DECLARE @ucaHCM4 BIGINT = (SELECT id FROM dbo.User_Company_Assignments WHERE user_id = @nv4 AND company_id = @hcmId);
    DECLARE @ucaHCM5 BIGINT = (SELECT id FROM dbo.User_Company_Assignments WHERE user_id = @nv5 AND company_id = @hcmId);

    INSERT INTO dbo.User_Department_Assignments (user_id, department_id, user_company_assignment_id, company_id, is_primary_for_company, assignment_status, effective_from, created_at, created_by_user_id)
    VALUES
        (@nv1, @salesHN, @ucaHN1, @hnId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId),
        (@nv2, @csHN, @ucaHN2, @hnId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId),
        (@nv3, @finHN, @ucaHN3, @hnId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId),
        (@nv4, @salesHCM, @ucaHCM4, @hcmId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId),
        (@nv5, @salesHCM, @ucaHCM5, @hcmId, 1, 'ACTIVE', GETUTCDATE(), GETUTCDATE(), @adminId);
END
GO

-- ============================================================
-- 6. Grant ALL permissions to admin user (individual grants)
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
END
GO

-- ============================================================
-- 7. Customers with Profiles
-- ============================================================
DECLARE @hnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HN');
DECLARE @hcmId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HCM');
DECLARE @nv1 BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'NV001');
DECLARE @nv4 BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'NV004');

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE customer_code = 'KH-0001')
BEGIN
    DECLARE @p1 BIGINT, @p2 BIGINT, @p3 BIGINT, @p4 BIGINT, @p5 BIGINT, @p6 BIGINT, @p7 BIGINT, @p8 BIGINT, @p9 BIGINT, @p10 BIGINT;

    INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, contact_address, is_active, created_at)
    VALUES (N'Nguyễn Thị Lan', '012345678901', '1965-03-15', 'FULL', 'FEMALE', '0901234567', N'123 Lê Lợi, Q.1, TP.HCM', N'123 Lê Lợi, Q.1, TP.HCM', 1, GETUTCDATE());
    SET @p1 = SCOPE_IDENTITY();

    INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, is_active, created_at)
    VALUES (N'Trần Văn Hùng', '012345678902', '1958-07-20', 'FULL', 'MALE', '0912345678', N'45 Nguyễn Huệ, Q.1, TP.HCM', 1, GETUTCDATE());
    SET @p2 = SCOPE_IDENTITY();

    INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, is_active, created_at)
    VALUES (N'Lê Thị Hồng', '012345678903', '1972-11-08', 'FULL', 'FEMALE', '0923456789', N'78 Trần Hưng Đạo, Hoàn Kiếm, HN', 1, GETUTCDATE());
    SET @p3 = SCOPE_IDENTITY();

    INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, is_active, created_at)
    VALUES (N'Phạm Đức Thắng', '012345678904', '1980-01-25', 'FULL', 'MALE', '0934567890', N'156 Hai Bà Trưng, Q.3, TP.HCM', 1, GETUTCDATE());
    SET @p4 = SCOPE_IDENTITY();

    INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, is_active, created_at)
    VALUES (N'Hoàng Văn Đức', '012345678905', '1955-05-10', 'FULL', 'MALE', '0945678901', N'23 Phan Chu Trinh, Hoàn Kiếm, HN', 1, GETUTCDATE());
    SET @p5 = SCOPE_IDENTITY();

    INSERT INTO dbo.Profiles (full_name, dob, dob_precision, gender, phone, permanent_address, is_active, created_at)
    VALUES (N'Vũ Thị Nga', '1968-09-01', 'YEAR_MONTH', 'FEMALE', '0956789012', N'89 Láng Hạ, Đống Đa, HN', 1, GETUTCDATE());
    SET @p6 = SCOPE_IDENTITY();

    INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, death_date_solar, is_active, created_at)
    VALUES (N'Ngô Quang Minh', '012345678907', '1940-02-14', 'FULL', 'MALE', '0967890123', N'12 Bà Triệu, Hoàn Kiếm, HN', '2024-06-15', 1, GETUTCDATE());
    SET @p7 = SCOPE_IDENTITY();

    INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, is_active, created_at)
    VALUES (N'Đỗ Thị Thanh', '012345678908', '1975-12-30', 'FULL', 'FEMALE', '0978901234', N'234 Nguyễn Trãi, Thanh Xuân, HN', 1, GETUTCDATE());
    SET @p8 = SCOPE_IDENTITY();

    INSERT INTO dbo.Profiles (full_name, dob, dob_precision, gender, phone, permanent_address, is_active, created_at)
    VALUES (N'Bùi Xuân Trường', '1962-01-01', 'YEAR', 'MALE', '0989012345', N'567 Điện Biên Phủ, Q.3, TP.HCM', 1, GETUTCDATE());
    SET @p9 = SCOPE_IDENTITY();

    INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, is_active, created_at)
    VALUES (N'Lý Thị Phượng', '012345678910', '1985-04-22', 'FULL', 'FEMALE', '0990123456', N'34 Pasteur, Q.1, TP.HCM', 1, GETUTCDATE());
    SET @p10 = SCOPE_IDENTITY();

    INSERT INTO dbo.Customers (customer_code, profile_id, customer_status, created_at)
    VALUES
        ('KH-0001', @p1, 'ACTIVE', GETUTCDATE()),
        ('KH-0002', @p2, 'ACTIVE', GETUTCDATE()),
        ('KH-0003', @p3, 'ACTIVE', GETUTCDATE()),
        ('KH-0004', @p4, 'ACTIVE', GETUTCDATE()),
        ('KH-0005', @p5, 'ACTIVE', GETUTCDATE()),
        ('KH-0006', @p6, 'ACTIVE', GETUTCDATE()),
        ('KH-0007', @p7, 'ACTIVE', GETUTCDATE()),
        ('KH-0008', @p8, 'ACTIVE', GETUTCDATE()),
        ('KH-0009', @p9, 'ACTIVE', GETUTCDATE()),
        ('KH-0010', @p10, 'ACTIVE', GETUTCDATE());

    -- Assign customers to companies
    DECLARE @c1 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0001');
    DECLARE @c2 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0002');
    DECLARE @c3 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0003');
    DECLARE @c4 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0004');
    DECLARE @c5 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0005');
    DECLARE @c6 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0006');
    DECLARE @c7 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0007');
    DECLARE @c8 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0008');
    DECLARE @c9 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0009');
    DECLARE @c10 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0010');

    INSERT INTO dbo.Customer_Company_Contexts (customer_id, company_id, assigned_staff_id, relationship_status, first_interaction_at, created_at)
    VALUES
        (@c1, @hcmId, @nv4, 'ACTIVE', DATEADD(MONTH, -18, GETUTCDATE()), GETUTCDATE()),
        (@c2, @hcmId, @nv4, 'ACTIVE', DATEADD(MONTH, -12, GETUTCDATE()), GETUTCDATE()),
        (@c3, @hnId, @nv1, 'ACTIVE', DATEADD(MONTH, -24, GETUTCDATE()), GETUTCDATE()),
        (@c4, @hcmId, @nv4, 'ACTIVE', DATEADD(MONTH, -6, GETUTCDATE()), GETUTCDATE()),
        (@c5, @hnId, @nv1, 'ACTIVE', DATEADD(MONTH, -36, GETUTCDATE()), GETUTCDATE()),
        (@c6, @hnId, @nv1, 'ACTIVE', DATEADD(MONTH, -15, GETUTCDATE()), GETUTCDATE()),
        (@c7, @hnId, @nv1, 'ACTIVE', DATEADD(MONTH, -48, GETUTCDATE()), GETUTCDATE()),
        (@c8, @hnId, @nv1, 'ACTIVE', DATEADD(MONTH, -9, GETUTCDATE()), GETUTCDATE()),
        (@c9, @hcmId, @nv4, 'ACTIVE', DATEADD(MONTH, -20, GETUTCDATE()), GETUTCDATE()),
        (@c10, @hcmId, @nv4, 'ACTIVE', DATEADD(MONTH, -3, GETUTCDATE()), GETUTCDATE());
END
GO

-- ============================================================
-- 8. Service Types
-- ============================================================
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF NOT EXISTS (SELECT 1 FROM dbo.Service_Types WHERE code = 'CHAM_SOC_NAM')
BEGIN
    INSERT INTO dbo.Service_Types (code, name, description, standard_price, standard_price_currency, cycle_duration_months, is_active, created_at, created_by_user_id)
    VALUES
        ('CHAM_SOC_NAM', N'Chăm sóc hàng năm', N'Gói chăm sóc mộ phần hàng năm bao gồm vệ sinh, trồng hoa, thắp hương', 2000000.00, 'VND', 12, 1, GETUTCDATE(), @adminId),
        ('CHAM_SOC_QUY', N'Chăm sóc hàng quý', N'Gói chăm sóc mộ phần hàng quý', 800000.00, 'VND', 3, 1, GETUTCDATE(), @adminId),
        ('VE_SINH_CO_BAN', N'Vệ sinh cơ bản', N'Dịch vụ vệ sinh mộ phần cơ bản một lần', 300000.00, 'VND', NULL, 1, GETUTCDATE(), @adminId),
        ('TRONG_HOA', N'Trồng hoa trang trí', N'Dịch vụ trồng và chăm sóc hoa tại mộ phần', 500000.00, 'VND', 6, 1, GETUTCDATE(), @adminId),
        ('IN_THE', N'In thẻ mộ', N'Phí in thẻ mộ (lần đầu miễn phí, in lại 50.000đ/thẻ)', 50000.00, 'VND', NULL, 1, GETUTCDATE(), @adminId);
END
GO

-- ============================================================
-- 9. Services (active subscriptions)
-- ============================================================
DECLARE @hnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HN');
DECLARE @hcmId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HCM');
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
DECLARE @csNam BIGINT = (SELECT id FROM dbo.Service_Types WHERE code = 'CHAM_SOC_NAM');
DECLARE @csQuy BIGINT = (SELECT id FROM dbo.Service_Types WHERE code = 'CHAM_SOC_QUY');

DECLARE @c3 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0003');
DECLARE @c5 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0005');
DECLARE @c7 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0007');
DECLARE @c1 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0001');

IF @csNam IS NOT NULL AND @c3 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Services WHERE customer_id = @c3)
BEGIN
    INSERT INTO dbo.Services (service_type_id, customer_id, company_id, status, applied_price, standard_price_snapshot, is_override_price, valid_from, valid_to, cycle_number, created_at, created_by_user_id)
    VALUES
        (@csNam, @c3, @hnId, 'ACTIVE', 2000000.00, 2000000.00, 0, DATEADD(MONTH, -6, GETUTCDATE()), DATEADD(MONTH, 6, GETUTCDATE()), 1, GETUTCDATE(), @adminId),
        (@csNam, @c5, @hnId, 'ACTIVE', 2000000.00, 2000000.00, 0, DATEADD(MONTH, -3, GETUTCDATE()), DATEADD(MONTH, 9, GETUTCDATE()), 2, GETUTCDATE(), @adminId),
        (@csQuy, @c7, @hnId, 'ACTIVE', 800000.00, 800000.00, 0, DATEADD(MONTH, -1, GETUTCDATE()), DATEADD(MONTH, 2, GETUTCDATE()), 4, GETUTCDATE(), @adminId),
        (@csNam, @c1, @hcmId, 'ACTIVE', 1800000.00, 2000000.00, 1, DATEADD(MONTH, -2, GETUTCDATE()), DATEADD(MONTH, 10, GETUTCDATE()), 1, GETUTCDATE(), @adminId);
END
GO

-- ============================================================
-- 10. Payment Transactions
-- ============================================================
DECLARE @hnId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HN');
DECLARE @hcmId BIGINT = (SELECT id FROM dbo.Companies WHERE company_code = 'INDEVCO-HCM');
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
DECLARE @c3 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0003');
DECLARE @c5 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0005');
DECLARE @c1 BIGINT = (SELECT id FROM dbo.Customers WHERE customer_code = 'KH-0001');

IF @c3 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Payment_Transactions WHERE bill_code = 'HD-HN-2026-0001')
BEGIN
    INSERT INTO dbo.Payment_Transactions (bill_code, company_id, customer_id, payment_method, payment_date, total_amount, currency_code, status, confirmed_at, confirmed_by_user_id, created_at, created_by_user_id)
    VALUES
        ('HD-HN-2026-0001', @hnId, @c3, 'CASH', DATEADD(MONTH, -6, GETUTCDATE()), 2000000.00, 'VND', 'CONFIRMED', DATEADD(MONTH, -6, GETUTCDATE()), @adminId, DATEADD(MONTH, -6, GETUTCDATE()), @adminId),
        ('HD-HN-2026-0002', @hnId, @c5, 'TRANSFER', DATEADD(MONTH, -3, GETUTCDATE()), 2000000.00, 'VND', 'CONFIRMED', DATEADD(MONTH, -3, GETUTCDATE()), @adminId, DATEADD(MONTH, -3, GETUTCDATE()), @adminId),
        ('HD-HN-2026-0003', @hnId, @c3, 'CASH', DATEADD(DAY, -10, GETUTCDATE()), 300000.00, 'VND', 'DRAFT', NULL, NULL, DATEADD(DAY, -10, GETUTCDATE()), @adminId),
        ('HD-HCM-2026-0001', @hcmId, @c1, 'TRANSFER', DATEADD(MONTH, -2, GETUTCDATE()), 1800000.00, 'VND', 'CONFIRMED', DATEADD(MONTH, -2, GETUTCDATE()), @adminId, DATEADD(MONTH, -2, GETUTCDATE()), @adminId);
END
GO

PRINT 'Sample data seed completed successfully.';
