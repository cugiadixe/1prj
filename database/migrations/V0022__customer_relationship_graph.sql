-- V0022__customer_relationship_graph.sql
-- Đồ thị quan hệ gia đình người ↔ người (nền tảng cho suy diễn khi đổi chủ mộ):
--   - Relationship_Kinds     : catalog loại quan hệ + nhãn theo GIỚI TÍNH; có nội/ngoại và anh/chị/em
--   - Customer_Relationships : cạnh 2 chiều giữa 2 khách hàng (from → to = relation_kind)
--   - Kinship_Composition    : bảng tra suy diễn 2 bậc (PA3 Hybrid), CÓ giới tính người trung gian
--                              để phân biệt nội/ngoại; ca ngoài bảng → 'OTHER' + cần xác nhận.
--
-- Nguyên tắc nhãn:
--   * Giới tính (Profiles.gender) quyết nhãn nam/nữ  → nam không bao giờ hiện "cháu gái".
--   * Nội/Ngoại   suy từ GIỚI TÍNH NGƯỜI TRUNG GIAN (qua cha → nội, qua mẹ → ngoại).
--   * Anh/Chị/Em  suy từ TUỔI (ngày sinh): lưu SIBLING trung tính, hiển thị resolve Anh/Chị vs Em.
-- An toàn: chỉ thêm bảng mới, không đụng dữ liệu/backend hiện có.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

-- ── 1. Catalog loại quan hệ (nhãn theo giới tính) ───────────────────────────
CREATE TABLE dbo.Relationship_Kinds
(
    kind_code       varchar(24)     NOT NULL,   -- mã trung tính giới tính
    label_male      nvarchar(50)    NOT NULL,   -- nhãn khi đối tượng là Nam
    label_female    nvarchar(50)    NOT NULL,   -- nhãn khi đối tượng là Nữ
    label_neutral   nvarchar(50)    NOT NULL,   -- nhãn khi không rõ giới tính
    inverse_code    varchar(24)     NOT NULL,   -- loại quan hệ chiều ngược lại
    is_symmetric    bit             NOT NULL,   -- 1 nếu nghịch đảo trùng chính nó (SPOUSE/SIBLING)
    sort_order      int             NOT NULL,
    CONSTRAINT PK_Relationship_Kinds PRIMARY KEY (kind_code)
);
GO

INSERT INTO dbo.Relationship_Kinds (kind_code, label_male, label_female, label_neutral, inverse_code, is_symmetric, sort_order) VALUES
    ('PARENT',               N'Cha',            N'Mẹ',            N'Cha/Mẹ',        'CHILD',                0, 1),
    ('CHILD',                N'Con trai',       N'Con gái',       N'Con',           'PARENT',               0, 2),
    ('SPOUSE',               N'Chồng',          N'Vợ',            N'Vợ/Chồng',      'SPOUSE',               1, 3),
    -- Anh/chị/em: SIBLING trung tính (cho lưu trữ & suy diễn); OLDER/YOUNGER là nhãn hiển thị theo tuổi
    ('SIBLING',              N'Anh/Em trai',    N'Chị/Em gái',    N'Anh/Chị/Em',    'SIBLING',              1, 4),
    ('SIBLING_OLDER',        N'Anh',            N'Chị',           N'Anh/Chị',       'SIBLING_YOUNGER',      0, 5),
    ('SIBLING_YOUNGER',      N'Em trai',        N'Em gái',        N'Em',            'SIBLING_OLDER',        0, 6),
    -- Nội (qua cha) / Ngoại (qua mẹ)
    ('GRANDPARENT_PATERNAL', N'Ông nội',        N'Bà nội',        N'Ông/Bà nội',    'GRANDCHILD_PATERNAL',  0, 7),
    ('GRANDPARENT_MATERNAL', N'Ông ngoại',      N'Bà ngoại',      N'Ông/Bà ngoại',  'GRANDCHILD_MATERNAL',  0, 8),
    ('GRANDCHILD_PATERNAL',  N'Cháu nội (trai)',N'Cháu nội (gái)',N'Cháu nội',      'GRANDPARENT_PATERNAL', 0, 9),
    ('GRANDCHILD_MATERNAL',  N'Cháu ngoại (trai)',N'Cháu ngoại (gái)',N'Cháu ngoại','GRANDPARENT_MATERNAL', 0, 10),
    ('OTHER',                N'Người thân',     N'Người thân',    N'Người thân',    'OTHER',                1, 99);
GO

-- ── 2. Cạnh quan hệ giữa các khách hàng (lưu 2 chiều) ───────────────────────
CREATE TABLE dbo.Customer_Relationships
(
    id                  bigint          IDENTITY(1,1)   NOT NULL,
    from_customer_id    bigint                          NOT NULL,   -- góc nhìn
    to_customer_id      bigint                          NOT NULL,   -- đối tượng
    relation_kind       varchar(24)                     NOT NULL,   -- 'to' là <relation_kind> của 'from'
    is_derived          bit                             NOT NULL,   -- 1 = suy diễn tự động, 0 = khai báo trực tiếp
    needs_confirmation  bit                             NOT NULL,   -- 1 = suy diễn không chắc, cần người xác nhận
    note                nvarchar(500)                   NULL,
    created_at          datetime2(3)                    NOT NULL,
    created_by_user_id  bigint                          NULL,
    updated_at          datetime2(3)                    NULL,
    updated_by_user_id  bigint                          NULL,
    row_version         rowversion                      NOT NULL,

    CONSTRAINT PK_Customer_Relationships PRIMARY KEY (id),
    CONSTRAINT UQ_Customer_Relationships_pair UNIQUE (from_customer_id, to_customer_id),
    CONSTRAINT FK_CR_from_customer FOREIGN KEY (from_customer_id) REFERENCES dbo.Customers (id),
    CONSTRAINT FK_CR_to_customer   FOREIGN KEY (to_customer_id)   REFERENCES dbo.Customers (id),
    CONSTRAINT FK_CR_relation_kind FOREIGN KEY (relation_kind)    REFERENCES dbo.Relationship_Kinds (kind_code),
    CONSTRAINT FK_CR_created_by    FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT FK_CR_updated_by    FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT CK_CR_not_self      CHECK (from_customer_id <> to_customer_id)
);
GO

CREATE NONCLUSTERED INDEX IX_CR_from ON dbo.Customer_Relationships (from_customer_id) INCLUDE (relation_kind, to_customer_id);
CREATE NONCLUSTERED INDEX IX_CR_to   ON dbo.Customer_Relationships (to_customer_id)   INCLUDE (relation_kind, from_customer_id);
GO

-- ── 3. Bảng tra suy diễn 2 bậc (Hybrid), có giới tính người trung gian ──────
-- Ý nghĩa: owner→pivot = kind_a  và  pivot→target = kind_b, giới tính pivot = pivot_gender
--          ⇒ owner→target = result_kind.
-- pivot_gender = 'ANY' khi giới tính người trung gian không ảnh hưởng kết quả.
CREATE TABLE dbo.Kinship_Composition
(
    kind_a              varchar(24)     NOT NULL,
    kind_b              varchar(24)     NOT NULL,
    pivot_gender        varchar(10)     NOT NULL,   -- MALE / FEMALE / ANY
    result_kind         varchar(24)     NOT NULL,
    needs_confirmation  bit             NOT NULL,
    note                nvarchar(200)   NULL,
    CONSTRAINT PK_Kinship_Composition PRIMARY KEY (kind_a, kind_b, pivot_gender),
    CONSTRAINT FK_KC_a      FOREIGN KEY (kind_a)      REFERENCES dbo.Relationship_Kinds (kind_code),
    CONSTRAINT FK_KC_b      FOREIGN KEY (kind_b)      REFERENCES dbo.Relationship_Kinds (kind_code),
    CONSTRAINT FK_KC_result FOREIGN KEY (result_kind) REFERENCES dbo.Relationship_Kinds (kind_code),
    CONSTRAINT CK_KC_pivot_gender CHECK (pivot_gender IN ('MALE', 'FEMALE', 'ANY'))
);
GO

-- Chỉ nạp tổ hợp XÁC ĐỊNH ĐƯỢC. Tổ hợp ngoài bảng ⇒ 'OTHER' + needs_confirmation = 1 (xử lý ở GĐ2).
INSERT INTO dbo.Kinship_Composition (kind_a, kind_b, pivot_gender, result_kind, needs_confirmation, note) VALUES
    -- Ông/Bà: nội nếu qua CHA (pivot nam), ngoại nếu qua MẸ (pivot nữ)
    ('PARENT', 'PARENT', 'MALE',   'GRANDPARENT_PATERNAL', 0, N'Cha/Mẹ của CHA ⇒ Ông/Bà nội'),
    ('PARENT', 'PARENT', 'FEMALE', 'GRANDPARENT_MATERNAL', 0, N'Cha/Mẹ của MẸ ⇒ Ông/Bà ngoại'),
    -- Cháu: nội nếu qua CON TRAI (pivot nam), ngoại nếu qua CON GÁI (pivot nữ)
    ('CHILD',  'CHILD',  'MALE',   'GRANDCHILD_PATERNAL',  0, N'Con của CON TRAI ⇒ Cháu nội'),
    ('CHILD',  'CHILD',  'FEMALE', 'GRANDCHILD_MATERNAL',  0, N'Con của CON GÁI ⇒ Cháu ngoại'),
    -- Anh/chị/em: con của cha/mẹ (nhãn Anh/Chị vs Em resolve theo tuổi lúc hiển thị)
    ('PARENT', 'CHILD',  'ANY',    'SIBLING',              0, N'Con của cha/mẹ ⇒ Anh/Chị/Em (theo tuổi)'),
    ('SIBLING','PARENT', 'ANY',    'PARENT',               0, N'Cha/Mẹ của anh/chị/em ⇒ Cha/Mẹ'),
    -- Bán chắc chắn (đánh dấu cần xác nhận vì có thể là quan hệ kế / con riêng)
    ('PARENT', 'SPOUSE', 'ANY',    'PARENT',               1, N'Vợ/chồng của cha/mẹ ⇒ Cha/Mẹ (có thể kế → xác nhận)'),
    ('SPOUSE', 'CHILD',  'ANY',    'CHILD',                1, N'Con của vợ/chồng ⇒ Con (có thể con riêng → xác nhận)');
GO
