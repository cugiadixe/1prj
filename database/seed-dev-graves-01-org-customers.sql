-- ============================================================================
-- SEED DEV — BƯỚC A: Tổ chức + Khách hàng (PTKD_DEV)
-- 5 công ty con · phòng ban · ~100 nhân viên (trưởng phòng/nhân viên qua Role)
-- 300.000 khách hàng: 175K đang sống (ACTIVE) + 125K đã mất (DECEASED, = cốt cho Bước B)
-- Tên KHÁC NHAU (bijection 4 âm tiết) · CCCD 12 số / CMND 10 số đúng quy tắc.
-- CHỈ chạy trên PTKD_DEV. Một batch (không GO) để test trong transaction rồi rollback.
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @N_LIVING   INT = 175000;   -- << đổi nhỏ để test
DECLARE @N_DECEASED INT = 125000;   -- << đổi nhỏ để test
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
DECLARE @now DATETIME2(3) = SYSUTCDATETIME();
DECLARE @LM INT = @N_LIVING/2, @LF INT = @N_LIVING - @N_LIVING/2;
DECLARE @DM INT = @N_DECEASED/2, @DF INT = @N_DECEASED - @N_DECEASED/2;
DECLARE @N_TOTAL INT = @N_LIVING + @N_DECEASED;

-- ── POOLS ───────────────────────────────────────────────────────────────────
CREATE TABLE #surname (idx INT, val NVARCHAR(20));
INSERT #surname SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 1))-1, v FROM (VALUES
 (N'Nguyễn'),(N'Trần'),(N'Lê'),(N'Phạm'),(N'Hoàng'),(N'Huỳnh'),(N'Phan'),(N'Vũ'),(N'Võ'),(N'Đặng'),
 (N'Bùi'),(N'Đỗ'),(N'Hồ'),(N'Ngô'),(N'Dương'),(N'Lý'),(N'Đào'),(N'Đoàn'),(N'Trịnh'),(N'Đinh'),
 (N'Lâm'),(N'Mai'),(N'Trương'),(N'Tô'),(N'Cao'),(N'Chu'),(N'Hà'),(N'Lương'),(N'Vương'),(N'Tạ')) t(v);

CREATE TABLE #middle (idx INT, val NVARCHAR(20));
INSERT #middle SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 1))-1, v FROM (VALUES
 (N'Minh'),(N'Đức'),(N'Hữu'),(N'Quang'),(N'Xuân'),(N'Thanh'),(N'Ngọc'),(N'Gia'),
 (N'Bảo'),(N'Kim'),(N'Hải'),(N'Hoài'),(N'Nhật'),(N'Khánh'),(N'Phương'),(N'Duy')) t(v);

CREATE TABLE #mg1 (idx INT, val NVARCHAR(20));
INSERT #mg1 SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 1))-1, v FROM (VALUES
 (N'An'),(N'Bá'),(N'Chí'),(N'Công'),(N'Duy'),(N'Đạt'),(N'Đức'),(N'Hoàng'),(N'Hùng'),(N'Khoa'),
 (N'Long'),(N'Mạnh'),(N'Nam'),(N'Phú'),(N'Quốc'),(N'Sơn'),(N'Tân'),(N'Thành'),(N'Trung'),(N'Tuấn')) t(v);
CREATE TABLE #mg2 (idx INT, val NVARCHAR(20));
INSERT #mg2 SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 1))-1, v FROM (VALUES
 (N'An'),(N'Bình'),(N'Cường'),(N'Dũng'),(N'Đạt'),(N'Hải'),(N'Hào'),(N'Hiếu'),(N'Huy'),(N'Khang'),
 (N'Kiên'),(N'Lâm'),(N'Lộc'),(N'Nghĩa'),(N'Phong'),(N'Quân'),(N'Thắng'),(N'Toàn'),(N'Tú'),(N'Vũ')) t(v);

CREATE TABLE #fg1 (idx INT, val NVARCHAR(20));
INSERT #fg1 SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 1))-1, v FROM (VALUES
 (N'Ánh'),(N'Bích'),(N'Diễm'),(N'Giang'),(N'Hà'),(N'Hoài'),(N'Hồng'),(N'Hương'),(N'Khánh'),(N'Lan'),
 (N'Linh'),(N'Mai'),(N'Ngọc'),(N'Nhung'),(N'Phương'),(N'Quỳnh'),(N'Thanh'),(N'Thảo'),(N'Thu'),(N'Trang')) t(v);
CREATE TABLE #fg2 (idx INT, val NVARCHAR(20));
INSERT #fg2 SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 1))-1, v FROM (VALUES
 (N'An'),(N'Anh'),(N'Chi'),(N'Dung'),(N'Hà'),(N'Hằng'),(N'Hoa'),(N'Huệ'),(N'Lan'),(N'Linh'),
 (N'Loan'),(N'My'),(N'Nga'),(N'Nhi'),(N'Oanh'),(N'Phượng'),(N'Thảo'),(N'Trâm'),(N'Vân'),(N'Yến')) t(v);

CREATE TABLE #prov (idx INT, code VARCHAR(3), name NVARCHAR(30));
INSERT #prov SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 1))-1, code, name FROM (VALUES
 ('001',N'Hà Nội'),('079',N'TP Hồ Chí Minh'),('048',N'Đà Nẵng'),('031',N'Hải Phòng'),('092',N'Cần Thơ'),
 ('036',N'Nam Định'),('038',N'Thanh Hóa'),('040',N'Nghệ An'),('056',N'Khánh Hòa'),('075',N'Đồng Nai'),
 ('077',N'Bà Rịa-Vũng Tàu'),('019',N'Thái Nguyên'),('024',N'Bắc Giang'),('033',N'Hưng Yên'),('046',N'Thừa Thiên Huế'),('060',N'Bình Thuận')) t(code,name);
DECLARE @nProv INT = (SELECT COUNT(*) FROM #prov);

-- ── 1. CÔNG TY (5 công ty con) ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE company_code = 'PTKD-HN')
    INSERT INTO dbo.Companies (company_code, name, tax_code, is_active, created_at) VALUES
     ('PTKD-HN', N'PTKD Hà Nội',       '0101000001', 1, @now),
     ('PTKD-HCM',N'PTKD Hồ Chí Minh',  '0301000002', 1, @now),
     ('PTKD-DN', N'PTKD Đà Nẵng',      '0401000003', 1, @now),
     ('PTKD-HP', N'PTKD Hải Phòng',    '0201000004', 1, @now),
     ('PTKD-CT', N'PTKD Cần Thơ',      '1801000005', 1, @now);

CREATE TABLE #company (idx INT IDENTITY(0,1), id BIGINT, code VARCHAR(20));
INSERT #company (id, code) SELECT id, company_code FROM dbo.Companies WHERE company_code LIKE 'PTKD-%' ORDER BY id;
DECLARE @nCompany INT = (SELECT COUNT(*) FROM #company);

-- ── 2. PHÒNG BAN (5 phòng / công ty = 25) ───────────────────────────────────
CREATE TABLE #depttype (idx INT, suffix VARCHAR(8), name NVARCHAR(50));
INSERT #depttype SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 1))-1, s, n FROM (VALUES
 ('KD',  N'Phòng Kinh doanh'),
 ('CSKH',N'Phòng Chăm sóc khách hàng'),
 ('TC',  N'Phòng Tài chính'),
 ('CNTT',N'Phòng Công nghệ thông tin'),
 ('VH',  N'Phòng Vận hành nghĩa trang')) t(s,n);

IF NOT EXISTS (SELECT 1 FROM dbo.Departments d JOIN dbo.Companies c ON c.id=d.company_id WHERE c.company_code='PTKD-HN')
    INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
    SELECT co.code + '-' + dt.suffix, co.id, dt.name + N' — ' + (SELECT name FROM dbo.Companies WHERE id=co.id), 1, @now
    FROM #company co CROSS JOIN #depttype dt;

CREATE TABLE #dept (rn INT IDENTITY(1,1), id BIGINT, company_id BIGINT);
INSERT #dept (id, company_id) SELECT d.id, d.company_id FROM dbo.Departments d JOIN #company c ON c.id=d.company_id ORDER BY d.id;
DECLARE @nDept INT = (SELECT COUNT(*) FROM #dept);

-- ── 3. ROLE trưởng phòng / nhân viên ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE role_code='TRUONG_PHONG')
    INSERT INTO dbo.Roles (role_code, name, description, scope_type, is_active, created_at, created_by_user_id) VALUES
     ('TRUONG_PHONG', N'Trưởng phòng', N'Người đứng đầu phòng ban', 'GLOBAL', 1, @now, @adminId),
     ('NHAN_VIEN',    N'Nhân viên',    N'Nhân viên phòng ban',      'GLOBAL', 1, @now, @adminId);
DECLARE @roleHead BIGINT = (SELECT id FROM dbo.Roles WHERE role_code='TRUONG_PHONG');
DECLARE @roleStaff BIGINT = (SELECT id FROM dbo.Roles WHERE role_code='NHAN_VIEN');

-- ── 4. NHÂN VIÊN: mỗi phòng 4 người (1 trưởng phòng + 3 nhân viên) ───────────
CREATE TABLE #emp (rn INT, dept_id BIGINT, company_id BIGINT, pos INT, is_head BIT,
                   si INT, mi INT, gi INT,
                   employee_code VARCHAR(20), full_name NVARCHAR(200), email VARCHAR(200), user_id BIGINT);
;WITH pos AS (SELECT p FROM (VALUES (0),(1),(2),(3)) t(p))
INSERT #emp (rn, dept_id, company_id, pos, is_head, si, mi, gi, employee_code, email)
SELECT ROW_NUMBER() OVER (ORDER BY d.rn, p.p) AS rn, d.id, d.company_id, p.p,
       CASE WHEN p.p=0 THEN 1 ELSE 0 END,
       (CHECKSUM(NEWID()) & 2147483647) % 30,
       (CHECKSUM(NEWID()) & 2147483647) % 16,
       (CHECKSUM(NEWID()) & 2147483647) % 20,
       'EMP' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY d.rn, p.p) AS VARCHAR(6)),4),
       ''
FROM #dept d CROSS JOIN pos p;
UPDATE e SET full_name = s.val + N' ' + m.val + N' ' + g.val
FROM #emp e JOIN #surname s ON s.idx=e.si JOIN #middle m ON m.idx=e.mi JOIN #mg1 g ON g.idx=e.gi;
UPDATE #emp SET email = LOWER('emp' + RIGHT('0000'+CAST(rn AS VARCHAR(6)),4)) + '@ptkd.local';

INSERT INTO dbo.Users (employee_code, full_name, email, employment_status, account_status, created_at, created_by_user_id)
SELECT employee_code, full_name, email, 'ACTIVE', 'ACTIVE', @now, @adminId FROM #emp;
UPDATE e SET user_id = u.id FROM #emp e JOIN dbo.Users u ON u.employee_code = e.employee_code;

INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at, created_by_user_id)
SELECT user_id, company_id, 1, 'ACTIVE', @now, @now, @adminId FROM #emp;

INSERT INTO dbo.User_Department_Assignments (user_id, department_id, user_company_assignment_id, company_id, is_primary_for_company, assignment_status, effective_from, created_at, created_by_user_id)
SELECT e.user_id, e.dept_id, uca.id, e.company_id, 1, 'ACTIVE', @now, @now, @adminId
FROM #emp e JOIN dbo.User_Company_Assignments uca ON uca.user_id = e.user_id;

INSERT INTO dbo.User_Role_Assignments (user_id, role_id, assignment_status, effective_from, created_at, created_by_user_id)
SELECT user_id, CASE WHEN is_head=1 THEN @roleHead ELSE @roleStaff END, 'ACTIVE', @now, @now, @adminId FROM #emp;

-- ── 5. KHÁCH HÀNG (300K) — Profiles → Customers → Company Contexts ───────────
CREATE TABLE #cust (
    i INT, is_deceased BIT, gender VARCHAR(10), gseq INT, scramble INT, prov_idx INT,
    birth_year INT, dob DATE, death_date DATE,
    full_name NVARCHAR(200), doc_number VARCHAR(12), phone VARCHAR(20),
    prov_name NVARCHAR(30), address NVARCHAR(200),
    profile_id BIGINT, customer_id BIGINT, company_id BIGINT
);

;WITH nums AS (
    SELECT TOP (@N_TOTAL) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT #cust (i, is_deceased, gender, gseq)
SELECT i,
    CASE WHEN i >= @N_LIVING THEN 1 ELSE 0 END,
    CASE WHEN i < @LM THEN 'MALE'
         WHEN i < @N_LIVING THEN 'FEMALE'
         WHEN i < @N_LIVING + @DM THEN 'MALE'
         ELSE 'FEMALE' END,
    CASE WHEN i < @LM THEN i
         WHEN i < @N_LIVING THEN i - @LM
         WHEN i < @N_LIVING + @DM THEN @LM + (i - @N_LIVING)
         ELSE @LF + (i - @N_LIVING - @DM) END
FROM nums;

-- năm sinh + chỉ số tỉnh + ngày sinh + ngày mất  (bitmask tránh tràn ABS(INT_MIN))
UPDATE #cust SET
    birth_year = CASE WHEN is_deceased = 1 THEN 1920 + (gseq % 55) ELSE 1955 + (gseq % 50) END,
    prov_idx   = (CHECKSUM(NEWID()) & 2147483647) % @nProv,
    -- Xáo trộn bijective để họ/tên rải đều (không cùng họ theo khối), vẫn duy nhất
    scramble   = (gseq * 7919) % 192000;
UPDATE #cust SET
    dob = DATEFROMPARTS(birth_year, 1 + (CHECKSUM(NEWID()) & 2147483647) % 12, 1 + (CHECKSUM(NEWID()) & 2147483647) % 28),
    death_date = CASE WHEN is_deceased = 1
        THEN DATEFROMPARTS(
                CASE WHEN birth_year + 55 + ((CHECKSUM(NEWID()) & 2147483647) % 35) > 2024 THEN 2024 ELSE birth_year + 55 + ((CHECKSUM(NEWID()) & 2147483647) % 35) END,
                1 + (CHECKSUM(NEWID()) & 2147483647) % 12, 1 + (CHECKSUM(NEWID()) & 2147483647) % 28)
        ELSE NULL END;

-- họ tên (bijection theo gseq) + giới tính
UPDATE c SET full_name =
       s.val + N' ' + m.val + N' ' + g1.val + N' ' + g2.val
FROM #cust c
JOIN #surname s ON s.idx = (c.scramble / 6400) % 30
JOIN #middle  m ON m.idx = (c.scramble / 400) % 16
JOIN #mg1 g1 ON c.gender='MALE'   AND g1.idx = (c.scramble / 20) % 20
JOIN #mg2 g2 ON c.gender='MALE'   AND g2.idx = c.scramble % 20;
UPDATE c SET full_name =
       s.val + N' ' + m.val + N' ' + g1.val + N' ' + g2.val
FROM #cust c
JOIN #surname s ON s.idx = (c.scramble / 6400) % 30
JOIN #middle  m ON m.idx = (c.scramble / 400) % 16
JOIN #fg1 g1 ON c.gender='FEMALE' AND g1.idx = (c.scramble / 20) % 20
JOIN #fg2 g2 ON c.gender='FEMALE' AND g2.idx = c.scramble % 20;

-- CCCD (12 số) cho người sống / CMND (10 số) cho người đã mất, + phone + địa chỉ
UPDATE c SET
    prov_name = p.name,
    doc_number = CASE WHEN c.is_deceased = 0
        THEN p.code
             + CAST((CASE WHEN c.gender='MALE' THEN 0 ELSE 1 END) + (CASE WHEN c.birth_year >= 2000 THEN 2 ELSE 0 END) AS VARCHAR(1))
             + RIGHT('00' + CAST(c.birth_year % 100 AS VARCHAR(2)), 2)
             + RIGHT('000000' + CAST(c.i AS VARCHAR(7)), 6)
        ELSE p.code + RIGHT('0000000' + CAST(c.i AS VARCHAR(7)), 7) END,
    phone = '09' + RIGHT('00000000' + CAST(c.i AS VARCHAR(9)), 8),
    address = N'Số ' + CAST(1 + (CHECKSUM(NEWID()) & 2147483647) % 300 AS NVARCHAR(5)) + N', ' + p.name
FROM #cust c
JOIN #prov p ON p.idx = c.prov_idx;

-- company context (chia đều 5 công ty)
UPDATE c SET company_id = co.id FROM #cust c JOIN #company co ON co.idx = c.i % @nCompany;

-- Insert Profiles
INSERT INTO dbo.Profiles (full_name, cccd, dob, dob_precision, gender, phone, permanent_address, contact_address, hometown, death_date_solar, is_active, created_at)
SELECT full_name, doc_number, dob, 'FULL', gender, phone, address, address, prov_name,
       CASE WHEN is_deceased = 1 THEN death_date ELSE NULL END, 1, @now
FROM #cust;
UPDATE c SET profile_id = p.id FROM #cust c JOIN dbo.Profiles p ON p.cccd = c.doc_number;

-- Insert Customers
INSERT INTO dbo.Customers (customer_code, profile_id, customer_status, created_at)
SELECT 'KH' + RIGHT('0000000' + CAST(i AS VARCHAR(8)), 7), profile_id,
       CASE WHEN is_deceased = 1 THEN 'DECEASED' ELSE 'ACTIVE' END, @now
FROM #cust;
UPDATE c SET customer_id = cu.id FROM #cust c JOIN dbo.Customers cu ON cu.profile_id = c.profile_id;

-- Insert Customer_Company_Contexts
INSERT INTO dbo.Customer_Company_Contexts (customer_id, company_id, relationship_status, created_at)
SELECT customer_id, company_id, 'ACTIVE', @now FROM #cust;

-- ── KẾT QUẢ ─────────────────────────────────────────────────────────────────
PRINT '=== SEED BUOC A XONG ===';
SELECT 'Companies' AS bang, COUNT(*) AS n FROM dbo.Companies WHERE company_code LIKE 'PTKD-%'
UNION ALL SELECT 'Departments', COUNT(*) FROM dbo.Departments d JOIN dbo.Companies c ON c.id=d.company_id WHERE c.company_code LIKE 'PTKD-%'
UNION ALL SELECT 'Employees (Users)', COUNT(*) FROM #emp
UNION ALL SELECT 'Customers total', COUNT(*) FROM #cust
UNION ALL SELECT '  - dang song', COUNT(*) FROM #cust WHERE is_deceased=0
UNION ALL SELECT '  - da mat', COUNT(*) FROM #cust WHERE is_deceased=1
UNION ALL SELECT 'Ten khac nhau (distinct)', COUNT(DISTINCT full_name) FROM #cust;
