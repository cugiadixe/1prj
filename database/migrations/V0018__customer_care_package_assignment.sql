-- V0018__customer_care_package_assignment.sql
-- Quản lý gói chăm sóc khách hàng:
--   - Thêm cot_count (số cốt) vào dbo.Graves        → phục vụ quy tắc khớp cốt
--   - Thêm is_care_package vào dbo.Service_Types    → đánh dấu loại dịch vụ là "gói chăm sóc"
--   - dbo.Customer_Care_Packages                     → gói chăm sóc đã gán cho khách, rồi gán vào mộ
--   - Seed 2 permission + cấp cho admin
-- Ghi chú: dùng GO tách batch (deferred name resolution). Migrator tự bọc toàn bộ trong 1 transaction.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 1. Graves: số cốt (capacity)
-- ══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Graves') AND name = 'cot_count')
    ALTER TABLE dbo.Graves
        ADD cot_count int NOT NULL CONSTRAINT DF_Graves_cot_count DEFAULT 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Graves_cot_count')
    ALTER TABLE dbo.Graves
        ADD CONSTRAINT CK_Graves_cot_count CHECK (cot_count > 0);
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 2. Service_Types: cờ đánh dấu gói chăm sóc
-- ══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Service_Types') AND name = 'is_care_package')
    ALTER TABLE dbo.Service_Types
        ADD is_care_package bit NOT NULL CONSTRAINT DF_Service_Types_is_care_package DEFAULT 0;
GO

-- Bật cờ cho các gói chăm sóc (loại 'In thẻ mộ' và 'Tổ chức lễ giỗ')
UPDATE dbo.Service_Types
SET is_care_package = 1
WHERE code IN ('CHAM_SOC_NAM', 'CHAM_SOC_QUY', 'VE_SINH_CO_BAN', 'TRONG_HOA', 'BAO_TRI', 'LUU_TRU');
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 3. Customer_Care_Packages: gói của khách + gán vào mộ
-- ══════════════════════════════════════════════════════════════════════════

IF OBJECT_ID('dbo.Customer_Care_Packages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customer_Care_Packages
    (
        id                      bigint          IDENTITY(1,1)   NOT NULL,
        customer_id             bigint                          NOT NULL,   -- khách mua / chủ mộ
        service_type_id         bigint                          NOT NULL,   -- gói (nguồn giá + thời hạn)
        grave_id                bigint                          NULL,       -- mộ được gán (NULL đến khi gán)
        cot_count               int                             NOT NULL,   -- số cốt của gói
        unit_price              decimal(18,2)                   NOT NULL,   -- đơn giá chốt lúc tạo (theo cốt)
        total_price             decimal(18,2)                   NOT NULL,   -- = unit_price * cot_count
        start_date              date                            NOT NULL,
        end_date                date                            NULL,       -- NULL nếu gói không kỳ hạn
        status                  varchar(20)                     NOT NULL,   -- vòng đời gói
        notes                   nvarchar(2000)                  NULL,
        created_at              datetime2(3)                    NOT NULL,
        created_by_user_id      bigint                          NULL,
        updated_at              datetime2(3)                    NULL,
        updated_by_user_id      bigint                          NULL,
        row_version             rowversion                      NOT NULL,

        CONSTRAINT PK_Customer_Care_Packages PRIMARY KEY (id),
        CONSTRAINT FK_CCP_customer_id     FOREIGN KEY (customer_id)     REFERENCES dbo.Customers (id),
        CONSTRAINT FK_CCP_service_type_id FOREIGN KEY (service_type_id) REFERENCES dbo.Service_Types (id),
        CONSTRAINT FK_CCP_grave_id        FOREIGN KEY (grave_id)        REFERENCES dbo.Graves (id),
        CONSTRAINT FK_CCP_created_by      FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
        CONSTRAINT FK_CCP_updated_by      FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id),
        CONSTRAINT CK_CCP_cot_count CHECK (cot_count > 0),
        CONSTRAINT CK_CCP_status CHECK (status IN ('PENDING_GRAVE', 'ACTIVE', 'EXPIRED', 'CANCELLED'))
    );

    CREATE NONCLUSTERED INDEX IX_CCP_customer_id ON dbo.Customer_Care_Packages (customer_id);
    CREATE NONCLUSTERED INDEX IX_CCP_grave_id ON dbo.Customer_Care_Packages (grave_id) WHERE grave_id IS NOT NULL;
    CREATE NONCLUSTERED INDEX IX_CCP_service_type_id ON dbo.Customer_Care_Packages (service_type_id);
END
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 4. Seed permission + cấp admin
-- ══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CUSTOMER_CARE_PACKAGE_VIEW')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CUSTOMER_CARE_PACKAGE_VIEW', 'CARE_PACKAGE', 'VIEW', 'GLOBAL', 0, 0, 0, 1, N'Xem gói chăm sóc của khách hàng.');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CUSTOMER_CARE_PACKAGE_MANAGE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CUSTOMER_CARE_PACKAGE_MANAGE', 'CARE_PACKAGE', 'MANAGE', 'GLOBAL', 0, 0, 0, 1, N'Gán gói chăm sóc cho khách và gán vào mộ.');
GO

DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    SELECT @adminId, p.permission_code, 'GLOBAL', 'ALLOW', SYSUTCDATETIME(), @adminId
    FROM dbo.Permissions p
    WHERE p.permission_code IN ('CUSTOMER_CARE_PACKAGE_VIEW', 'CUSTOMER_CARE_PACKAGE_MANAGE')
      AND NOT EXISTS (
          SELECT 1 FROM dbo.User_Individual_Permissions uip
          WHERE uip.user_id = @adminId AND uip.permission_code = p.permission_code
      );
GO
