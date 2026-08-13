-- V0017__grave_management_schema.sql
-- Module Quản lý mộ (Graves):
--   - dbo.Graves            : phần mộ (vị trí theo 12 khu A–L, loại, diện tích, trạng thái, chủ mộ, liên hệ khẩn cấp)
--   - dbo.Grave_Occupants   : người an táng trong mộ (1 mộ → N người: mộ đơn / đôi / gia tộc)
-- Seed 3 permission code (GRAVE_VIEW, GRAVE_CREATE, GRAVE_UPDATE) và cấp cho admin.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ══════════════════════════════════════════════════════════════════════════
-- 1. Graves — phần mộ
-- ══════════════════════════════════════════════════════════════════════════

CREATE TABLE dbo.Graves
(
    id                              bigint          IDENTITY(1,1)   NOT NULL,
    grave_code                      nvarchar(50)                    NOT NULL,   -- mã mộ, vd A-0001 (khớp chuỗi Cards.grave_id đang dùng)
    zone                            varchar(10)                     NOT NULL,   -- khu: A..L
    plot_number                     varchar(20)                     NOT NULL,   -- số mộ trong khu
    row_label                       varchar(20)                     NULL,       -- hàng (cho bản đồ)
    col_label                       varchar(20)                     NULL,       -- cột (cho bản đồ)
    grave_type                      varchar(20)                     NOT NULL,   -- loại mộ
    area_m2                         decimal(10,2)                   NULL,       -- diện tích (m²)
    status                          varchar(20)                     NOT NULL,   -- vòng đời mộ
    owner_customer_id               bigint                          NULL,       -- chủ mộ (1 KH → N mộ)
    emergency_contact_name          nvarchar(200)                   NULL,       -- người liên hệ khẩn cấp
    emergency_contact_phone         varchar(20)                     NULL,
    emergency_contact_relationship  nvarchar(100)                   NULL,       -- quan hệ với chủ mộ
    notes                           nvarchar(2000)                  NULL,
    created_at                      datetime2(3)                    NOT NULL,
    created_by_user_id              bigint                          NULL,
    updated_at                      datetime2(3)                    NULL,
    updated_by_user_id              bigint                          NULL,
    row_version                     rowversion                      NOT NULL,

    CONSTRAINT PK_Graves PRIMARY KEY (id),
    CONSTRAINT UQ_Graves_grave_code UNIQUE (grave_code),
    CONSTRAINT FK_Graves_owner_customer_id FOREIGN KEY (owner_customer_id) REFERENCES dbo.Customers (id),
    CONSTRAINT FK_Graves_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT FK_Graves_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT CK_Graves_zone CHECK (zone IN ('A','B','C','D','E','F','G','H','I','J','K','L')),
    CONSTRAINT CK_Graves_grave_type CHECK (grave_type IN ('SINGLE','DOUBLE','FAMILY','CREMATION','OTHER')),
    CONSTRAINT CK_Graves_status CHECK (status IN ('EMPTY','RESERVED','OCCUPIED','RELOCATED')),
    CONSTRAINT CK_Graves_area_positive CHECK (area_m2 IS NULL OR area_m2 > 0)
);

CREATE NONCLUSTERED INDEX IX_Graves_zone ON dbo.Graves (zone);
CREATE NONCLUSTERED INDEX IX_Graves_status ON dbo.Graves (status);
CREATE NONCLUSTERED INDEX IX_Graves_owner_customer_id ON dbo.Graves (owner_customer_id) WHERE owner_customer_id IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Graves_zone_plot ON dbo.Graves (zone, plot_number);

-- ══════════════════════════════════════════════════════════════════════════
-- 2. Grave_Occupants — người an táng
-- ══════════════════════════════════════════════════════════════════════════

CREATE TABLE dbo.Grave_Occupants
(
    id                      bigint          IDENTITY(1,1)   NOT NULL,
    grave_id                bigint                          NOT NULL,
    full_name               nvarchar(200)                   NOT NULL,   -- người mất
    gender                  varchar(10)                     NULL,
    dob                     date                            NULL,       -- ngày sinh
    death_date_solar        date                            NULL,       -- ngày mất (dương)
    death_date_lunar        varchar(20)                     NULL,       -- ngày mất (âm)
    burial_date             date                            NULL,       -- ngày an táng
    hometown                nvarchar(200)                   NULL,       -- nguyên quán
    notes                   nvarchar(2000)                  NULL,
    created_at              datetime2(3)                    NOT NULL,
    created_by_user_id      bigint                          NULL,
    updated_at              datetime2(3)                    NULL,
    updated_by_user_id      bigint                          NULL,
    row_version             rowversion                      NOT NULL,

    CONSTRAINT PK_Grave_Occupants PRIMARY KEY (id),
    CONSTRAINT FK_Grave_Occupants_grave_id FOREIGN KEY (grave_id) REFERENCES dbo.Graves (id),
    CONSTRAINT FK_Grave_Occupants_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT FK_Grave_Occupants_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT CK_Grave_Occupants_gender CHECK (gender IS NULL OR gender IN ('MALE','FEMALE','OTHER'))
);

CREATE NONCLUSTERED INDEX IX_Grave_Occupants_grave_id ON dbo.Grave_Occupants (grave_id);
CREATE NONCLUSTERED INDEX IX_Grave_Occupants_full_name ON dbo.Grave_Occupants (full_name);

-- ══════════════════════════════════════════════════════════════════════════
-- 3. Seed permission codes
-- ══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'GRAVE_VIEW')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('GRAVE_VIEW', 'GRAVE', 'VIEW', 'GLOBAL', 0, 0, 0, 1, N'Xem danh sách và chi tiết phần mộ.');

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'GRAVE_CREATE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('GRAVE_CREATE', 'GRAVE', 'CREATE', 'GLOBAL', 0, 0, 0, 1, N'Tạo mới phần mộ.');

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'GRAVE_UPDATE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('GRAVE_UPDATE', 'GRAVE', 'UPDATE', 'GLOBAL', 0, 0, 0, 1, N'Cập nhật phần mộ và người an táng (chủ mộ, trạng thái, liên hệ, an táng).');

-- ══════════════════════════════════════════════════════════════════════════
-- 4. Cấp quyền mộ cho admin (đồng bộ với seed-production-data PHẦN 9)
-- ══════════════════════════════════════════════════════════════════════════

DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    SELECT @adminId, p.permission_code, 'GLOBAL', 'ALLOW', SYSUTCDATETIME(), @adminId
    FROM dbo.Permissions p
    WHERE p.permission_code IN ('GRAVE_VIEW', 'GRAVE_CREATE', 'GRAVE_UPDATE')
      AND NOT EXISTS (
          SELECT 1 FROM dbo.User_Individual_Permissions uip
          WHERE uip.user_id = @adminId AND uip.permission_code = p.permission_code
      );

COMMIT TRANSACTION;
