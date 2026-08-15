-- V0037__permission_scope_is_a_matrix_cell.sql
--
-- CHUYỂN MÔ HÌNH PHẠM VI: từ "thuộc tính cứng của mã quyền" sang "ô trong ma trận phân quyền".
--
-- VÌ SAO. Trước đây cột Permissions.data_scope quyết định việc một mã quyền có được đánh giá kèm
-- công ty hay không. Chuỗi nhân quả:
--
--     data_scope = 'GLOBAL'  ->  endpoint buộc phải khai PermissionScope.Global
--                            ->  companyId truyền xuống LUÔN là NULL
--                            ->  mọi lần cấp scope_type = 'COMPANY' đều không khớp
--                            ->  ô ma trận của quản trị viên trở nên VÔ NGHĨA
--
-- Hệ quả đo được: cấp một quyền cho ai đó ở phạm vi "chỉ công ty A" thì hệ LƯU THÀNH CÔNG, không
-- cảnh báo, nhưng bản cấp KHÔNG BAO GIỜ có hiệu lực. Chỉ có hai nấc — mất sạch quyền, hoặc xem
-- được mọi công ty. Không có nấc "chỉ công ty mình", tức trạng thái mặc định của hệ đang ngược
-- với yêu cầu nghiệp vụ.
--
-- Phần mã nguồn đã được sửa ở bước trước (PermissionEvaluator bỏ hai chốt cứng data_scope; phạm
-- vi nay lấy từ scope_type của TỪNG LẦN CẤP). Migration này lo phần DỮ LIỆU.
--
-- ==========================================================================================
-- CHỐT AN TOÀN QUAN TRỌNG NHẤT
-- ==========================================================================================
-- Việc chuyển grant GLOBAL -> COMPANY chỉ áp cho 18 mã DỮ LIỆU NGHIỆP VỤ liệt kê tường minh ở
-- dưới. TUYỆT ĐỐI không áp cho nhóm SECURITY_* và ORGANIZATION_*: nếu chuyển cả nhóm quản trị
-- thì tài khoản admin mất quyền quản trị và KHÔNG CÒN AI đăng nhập được để sửa lại.
--
-- Danh sách 18 mã được khoá cứng trong bảng tạm #business_codes, không dùng LIKE hay suy đoán.
--
-- Đã đối chiếu dữ liệu trước khi viết: 5 công ty, tài khoản admin thuộc CẢ 5, nên việc chuyển
-- sang phạm vi công ty không làm admin mất khả năng nhìn dữ liệu nào. Không có người dùng nào
-- vừa có grant vừa không thuộc công ty nào. Vai trò và nhóm quản trị không mang mã nghiệp vụ
-- nào nên không phải xử lý (tránh được bài toán "vai trò trộn mã nghiệp vụ với mã quản trị").
--
-- CÁCH LÀM THEO LỐI COPY-BASED: bản cấp GLOBAL cũ KHÔNG bị xoá, chỉ bị đóng lại (REVOKED) và
-- bản mới được tạo bên cạnh, để dấu vết kiểm toán còn nguyên.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- ==========================================================================================
-- PHẦN 0 — Danh sách 18 mã dữ liệu nghiệp vụ (khoá cứng)
-- ==========================================================================================

CREATE TABLE #business_codes (permission_code varchar(100) NOT NULL PRIMARY KEY);

INSERT INTO #business_codes (permission_code) VALUES
    -- Khách hàng (9)
    ('CUSTOMER_VIEW_BASIC'),
    ('CUSTOMER_VIEW_SENSITIVE'),
    ('CUSTOMER_CREATE_FINAL'),
    ('CUSTOMER_MASTER_UPDATE'),
    ('CUSTOMER_CHANGE_REQUEST_CREATE'),
    ('CUSTOMER_MERGE_EXECUTE'),
    ('CUSTOMER_MERGE_REQUEST_CREATE'),
    ('CUSTOMER_MERGE_REQUEST_VIEW'),
    ('CUSTOMER_MERGE_REQUEST_ADMIN_VIEW'),
    -- Mộ (5)
    ('GRAVE_VIEW'),
    ('GRAVE_CREATE'),
    ('GRAVE_UPDATE'),
    ('GRAVE_TRANSFER_OWNERSHIP'),
    ('GRAVE_ATTACHMENT_MANAGE'),
    -- Gói chăm sóc của khách (2)
    ('CUSTOMER_CARE_PACKAGE_VIEW'),
    ('CUSTOMER_CARE_PACKAGE_MANAGE'),
    -- Thẻ phân loại (1)
    ('TAG_MANAGE'),
    -- Hồ sơ quy trình (1). Lưu ý: WORKFLOW_VIEW_ALL_COMPANIES CỐ Ý toàn cục, không nằm ở đây.
    ('WORKFLOW_VIEW');
GO

-- Chốt chặn: nếu vì lý do gì đó danh sách trên lọt mã quản trị thì dừng hẳn, không chạy tiếp.
IF EXISTS (
    SELECT 1 FROM #business_codes
    WHERE permission_code LIKE 'SECURITY[_]%' OR permission_code LIKE 'ORGANIZATION[_]%')
BEGIN
    THROW 51037, 'V0037: danh sách mã nghiệp vụ lọt mã quản trị hệ thống — dừng để tránh khoá chết tài khoản admin.', 1;
END;
GO

-- ==========================================================================================
-- PHẦN 1 — Đổi nhãn data_scope của 18 mã: GLOBAL -> COMPANY
-- ==========================================================================================
-- data_scope nay chỉ còn là NHÃN PHÂN LOẠI (dữ liệu nghiệp vụ theo công ty / quản trị toàn hệ
-- thống) để giao diện gom nhóm và giải thích cho người quản trị. Nó KHÔNG còn tham gia quyết
-- định phân quyền nữa. Vẫn đổi cho đúng bản chất, vì nhãn sai thì người đọc ma trận hiểu sai.

UPDATE p
SET p.data_scope = 'COMPANY',
    p.updated_at = SYSUTCDATETIME()
FROM dbo.Permissions p
INNER JOIN #business_codes b ON b.permission_code = p.permission_code
WHERE p.data_scope = 'GLOBAL';
GO

-- ==========================================================================================
-- PHẦN 2 — Chuyển bản cấp CÁ NHÂN từ GLOBAL sang COMPANY
-- ==========================================================================================
-- Mỗi bản cấp GLOBAL của 18 mã trên được thay bằng một bản cấp COMPANY cho TỪNG công ty người đó
-- đang được phân công.
--
-- Người dùng KHÔNG thuộc công ty nào thì BỎ QUA hoàn toàn — giữ nguyên bản cấp GLOBAL của họ.
-- Nếu đóng bản cũ mà không tạo được bản mới thì họ mất quyền một cách âm thầm, đó là kiểu hỏng
-- tệ nhất: không ai biết cho tới khi có người phàn nàn.

-- 2a. Các bản cấp sẽ chuyển (chỉ những người có ít nhất một công ty).
SELECT
    uip.id                AS old_id,
    uip.user_id,
    uip.permission_code,
    uip.grant_type,
    uip.effective_from,
    uip.created_by_user_id
INTO #convertible
FROM dbo.User_Individual_Permissions uip
INNER JOIN #business_codes b ON b.permission_code = uip.permission_code
WHERE uip.scope_type = 'GLOBAL'
  AND uip.assignment_status = 'ACTIVE'
  AND uip.effective_to IS NULL
  AND EXISTS (
      SELECT 1 FROM dbo.User_Company_Assignments a
      WHERE a.user_id = uip.user_id
        AND a.assignment_status = 'ACTIVE'
        AND a.effective_from <= SYSUTCDATETIME()
        AND (a.effective_to IS NULL OR a.effective_to > SYSUTCDATETIME()));
GO

-- 2b. Tạo bản cấp mới theo từng công ty. Giữ nguyên effective_from của bản gốc để không có
--     khoảng trống quyền. Bỏ qua nếu đã tồn tại bản COMPANY tương ứng (chạy lại được nhiều lần).
INSERT INTO dbo.User_Individual_Permissions
    (user_id, permission_code, scope_type, company_id, grant_type,
     assignment_status, effective_from, effective_to, reason, created_at, created_by_user_id)
SELECT DISTINCT
    c.user_id,
    c.permission_code,
    'COMPANY',
    a.company_id,
    c.grant_type,
    'ACTIVE',
    c.effective_from,
    NULL,
    N'V0037: chuyển từ phạm vi toàn cục sang phạm vi công ty theo mô hình phân quyền mới.',
    SYSUTCDATETIME(),
    c.created_by_user_id
FROM #convertible c
INNER JOIN dbo.User_Company_Assignments a
    ON a.user_id = c.user_id
   AND a.assignment_status = 'ACTIVE'
   AND a.effective_from <= SYSUTCDATETIME()
   AND (a.effective_to IS NULL OR a.effective_to > SYSUTCDATETIME())
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.User_Individual_Permissions x
    WHERE x.user_id = c.user_id
      AND x.permission_code = c.permission_code
      AND x.scope_type = 'COMPANY'
      AND x.company_id = a.company_id
      AND x.grant_type = c.grant_type
      AND x.assignment_status = 'ACTIVE'
      AND x.effective_to IS NULL);
GO

-- 2c. Đóng bản cấp GLOBAL cũ. Chỉ đóng những bản đã tạo được ít nhất một bản COMPANY thay thế.
UPDATE uip
SET uip.assignment_status = 'REVOKED',
    uip.effective_to      = SYSUTCDATETIME(),
    uip.reason            = N'V0037: thay bằng các bản cấp theo công ty (mô hình phạm vi mới).',
    uip.updated_at        = SYSUTCDATETIME()
FROM dbo.User_Individual_Permissions uip
INNER JOIN #convertible c ON c.old_id = uip.id
WHERE uip.assignment_status = 'ACTIVE'
  AND EXISTS (
      SELECT 1 FROM dbo.User_Individual_Permissions x
      WHERE x.user_id = c.user_id
        AND x.permission_code = c.permission_code
        AND x.scope_type = 'COMPANY'
        AND x.grant_type = c.grant_type
        AND x.assignment_status = 'ACTIVE'
        AND x.effective_to IS NULL);
GO

-- ==========================================================================================
-- PHẦN 3 — Bổ sung mã quyền XEM còn thiếu (tách quyền NHÌN khỏi quyền LÀM)
-- ==========================================================================================
-- Hiện muốn XEM danh sách phiếu thu thì phải có quyền TẠO phiếu thu (PaymentTransactionController
-- gác endpoint liệt kê bằng PAYMENT_CREATE_DRAFT), và muốn xem báo cáo đối soát thì phải có quyền
-- CHUẨN BỊ đối soát. Kế toán trưởng "chỉ xem, không tạo" là bất khả thi với ma trận hiện tại.
--
-- Chỉ thêm những mã sẽ được NỐI VÀO CODE ngay ở bước sau. Không thêm mã để đó — danh mục hiện
-- đã có 11-15 mã nằm chết không nơi nào dùng, thêm nữa chỉ làm ma trận nói dối thêm.

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'PAYMENT_VIEW')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('PAYMENT_VIEW', 'PAYMENT', 'VIEW', 'COMPANY', 0, 0, 0, 1,
         N'Xem phiếu thu và danh sách phiếu thu. Tách khỏi quyền tạo phiếu để cấp được vai trò chỉ xem.');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'RECONCILIATION_VIEW')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('RECONCILIATION_VIEW', 'RECONCILIATION', 'VIEW', 'COMPANY', 0, 0, 0, 1,
         N'Xem báo cáo và phiên đối soát. Tách khỏi quyền chuẩn bị đối soát để cấp được vai trò chỉ xem.');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'GRAVE_OCCUPANT_MANAGE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('GRAVE_OCCUPANT_MANAGE', 'GRAVE', 'OCCUPANT_MANAGE', 'COMPANY', 0, 0, 0, 1,
         N'Thêm/sửa người an táng trong phần mộ. Tách khỏi GRAVE_UPDATE vốn đang gánh 6 hành động khác nhau.');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'GRAVE_EMERGENCY_CONTACT_MANAGE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('GRAVE_EMERGENCY_CONTACT_MANAGE', 'GRAVE', 'EMERGENCY_CONTACT_MANAGE', 'COMPANY', 0, 0, 0, 1,
         N'Thêm/sửa/xoá người liên hệ khẩn cấp của phần mộ. Tách khỏi GRAVE_UPDATE.');
GO

-- ==========================================================================================
-- PHẦN 4 — Ai đang có quyền GỘP thì được cấp luôn mã TÁCH tương ứng
-- ==========================================================================================
-- Không làm bước này thì việc tách quyền sẽ LẤY MẤT quyền của người đang dùng được. Tách quyền
-- là để cấp được HẸP HƠN cho người mới, không phải để thu hẹp người đang có.

CREATE TABLE #derived (source_code varchar(100) NOT NULL, new_code varchar(100) NOT NULL);

INSERT INTO #derived (source_code, new_code) VALUES
    ('PAYMENT_CREATE_DRAFT',    'PAYMENT_VIEW'),
    ('PAYMENT_CONFIRM',         'PAYMENT_VIEW'),
    ('RECONCILIATION_PREPARE',  'RECONCILIATION_VIEW'),
    ('RECONCILIATION_CONFIRM',  'RECONCILIATION_VIEW'),
    ('GRAVE_UPDATE',            'GRAVE_OCCUPANT_MANAGE'),
    ('GRAVE_UPDATE',            'GRAVE_EMERGENCY_CONTACT_MANAGE');
GO

-- 4a. Bản cấp cá nhân
INSERT INTO dbo.User_Individual_Permissions
    (user_id, permission_code, scope_type, company_id, grant_type,
     assignment_status, effective_from, effective_to, reason, created_at, created_by_user_id)
SELECT DISTINCT
    uip.user_id,
    d.new_code,
    uip.scope_type,
    uip.company_id,
    uip.grant_type,
    'ACTIVE',
    uip.effective_from,
    NULL,
    N'V0037: cấp kèm khi tách quyền, để người đang có quyền gộp không bị mất khả năng đang dùng.',
    SYSUTCDATETIME(),
    uip.created_by_user_id
FROM dbo.User_Individual_Permissions uip
INNER JOIN #derived d ON d.source_code = uip.permission_code
WHERE uip.assignment_status = 'ACTIVE'
  AND uip.effective_to IS NULL
  AND uip.grant_type = 'ALLOW'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.User_Individual_Permissions x
      WHERE x.user_id = uip.user_id
        AND x.permission_code = d.new_code
        AND x.scope_type = uip.scope_type
        AND ((x.company_id = uip.company_id) OR (x.company_id IS NULL AND uip.company_id IS NULL))
        AND x.grant_type = 'ALLOW'
        AND x.assignment_status = 'ACTIVE'
        AND x.effective_to IS NULL);
GO

-- 4b. Quyền chuẩn của phòng ban
INSERT INTO dbo.Department_Permissions (department_id, permission_code, created_at, created_by_user_id)
SELECT DISTINCT dp.department_id, d.new_code, SYSUTCDATETIME(), dp.created_by_user_id
FROM dbo.Department_Permissions dp
INNER JOIN #derived d ON d.source_code = dp.permission_code
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Department_Permissions x
    WHERE x.department_id = dp.department_id AND x.permission_code = d.new_code);
GO

-- 4c. Vai trò và nhóm quản trị
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT DISTINCT rp.role_id, d.new_code, SYSUTCDATETIME(), rp.created_by_user_id
FROM dbo.Role_Permissions rp
INNER JOIN #derived d ON d.source_code = rp.permission_code
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Role_Permissions x
    WHERE x.role_id = rp.role_id AND x.permission_code = d.new_code);
GO

INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code, created_at, created_by_user_id)
SELECT DISTINCT gp.admin_group_id, d.new_code, SYSUTCDATETIME(), gp.created_by_user_id
FROM dbo.Admin_Group_Permissions gp
INNER JOIN #derived d ON d.source_code = gp.permission_code
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Admin_Group_Permissions x
    WHERE x.admin_group_id = gp.admin_group_id AND x.permission_code = d.new_code);
GO

-- ==========================================================================================
-- PHẦN 5 — Làm mới bộ đệm quyền
-- ==========================================================================================
-- Không tăng policy_version thì bộ đệm 5 phút của PermissionEvaluator vẫn trả quyền theo mô hình
-- cũ, và người dùng sẽ thấy quyền "lúc có lúc không" ngay sau khi chạy migration.

UPDATE dbo.Authorization_Policy_State
SET policy_version = policy_version + 1,
    updated_at = SYSUTCDATETIME()
WHERE id = 1;
GO

-- ==========================================================================================
-- PHẦN 6 — Báo cáo kết quả để đối chiếu bằng mắt
-- ==========================================================================================

SELECT 'Mã nghiệp vụ nay là COMPANY' AS hang_muc, COUNT(*) AS so_luong
FROM dbo.Permissions p INNER JOIN #business_codes b ON b.permission_code = p.permission_code
WHERE p.data_scope = 'COMPANY'
UNION ALL
SELECT 'Bản cấp GLOBAL đã đóng', COUNT(*) FROM dbo.User_Individual_Permissions
WHERE assignment_status = 'REVOKED' AND reason LIKE N'V0037:%'
UNION ALL
SELECT 'Bản cấp COMPANY mới tạo', COUNT(*) FROM dbo.User_Individual_Permissions
WHERE assignment_status = 'ACTIVE' AND reason LIKE N'V0037: chuyển từ phạm vi toàn cục%'
UNION ALL
SELECT 'Bản cấp mã tách mới tạo', COUNT(*) FROM dbo.User_Individual_Permissions
WHERE assignment_status = 'ACTIVE' AND reason LIKE N'V0037: cấp kèm khi tách quyền%'
UNION ALL
SELECT N'CẢNH BÁO - grant GLOBAL còn sót do người dùng không thuộc công ty nào', COUNT(*)
FROM dbo.User_Individual_Permissions uip
INNER JOIN #business_codes b ON b.permission_code = uip.permission_code
WHERE uip.scope_type = 'GLOBAL' AND uip.assignment_status = 'ACTIVE' AND uip.effective_to IS NULL;
GO

DROP TABLE #business_codes;
DROP TABLE #convertible;
DROP TABLE #derived;
GO
