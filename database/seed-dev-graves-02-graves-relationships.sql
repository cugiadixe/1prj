-- ============================================================================
-- SEED DEV — BƯỚC B: Phần mộ + Cốt + Quan hệ + Liên hệ khẩn cấp (PTKD_DEV)
-- 50.000 mộ (khu A–L), mỗi mộ random 1–6 cốt (cốt = khách hàng DECEASED).
-- Quan hệ 2 chiều chủ↔cốt (relation_kind + nhãn owner/deceased theo giới tính).
-- Liên hệ khẩn cấp: 2 người/mộ (ưu tiên 1,2), link khách hàng đang sống.
-- Chạy SAU Bước A. CHỈ trên PTKD_DEV. Một batch (test được trong transaction).
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @N_GRAVES INT = 50000;    -- << đổi nhỏ để test
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
DECLARE @now DATETIME2(3) = SYSUTCDATETIME();

-- ── Pool khách hàng đang sống (chủ mộ + liên hệ khẩn cấp) ───────────────────
CREATE TABLE #living (j INT IDENTITY(1,1), customer_id BIGINT, gender VARCHAR(10), full_name NVARCHAR(200), phone VARCHAR(20));
INSERT #living (customer_id, gender, full_name, phone)
SELECT c.id, p.gender, p.full_name, p.phone
FROM dbo.Customers c JOIN dbo.Profiles p ON p.id = c.profile_id
WHERE c.customer_status = 'ACTIVE'
ORDER BY c.customer_code;
DECLARE @nLiving INT = @@ROWCOUNT;

-- ── Pool khách hàng đã mất (= cốt) ──────────────────────────────────────────
CREATE TABLE #deceased (j INT IDENTITY(1,1), customer_id BIGINT, gender VARCHAR(10),
                        full_name NVARCHAR(200), dob DATE, death_solar DATE, hometown NVARCHAR(200));
INSERT #deceased (customer_id, gender, full_name, dob, death_solar, hometown)
SELECT c.id, p.gender, p.full_name, p.dob, p.death_date_solar, p.hometown
FROM dbo.Customers c JOIN dbo.Profiles p ON p.id = c.profile_id
WHERE c.customer_status = 'DECEASED'
ORDER BY c.customer_code;
DECLARE @nDeceased INT = @@ROWCOUNT;

-- ── Numbers 0..5 (tối đa 6 cốt) ─────────────────────────────────────────────
CREATE TABLE #num (k INT);
INSERT #num VALUES (0),(1),(2),(3),(4),(5);

-- ── 1. GRAVES: cot_count (1–6, thiên về ít) + chủ mộ + prefix-sum slot ───────
CREATE TABLE #grave (g INT, cot_count INT, owner_j INT, owner_id BIGINT, owner_gender VARCHAR(10),
                     zone VARCHAR(2), grave_code NVARCHAR(50), grave_type VARCHAR(20),
                     slot_start INT, grave_id BIGINT);

;WITH nums AS (
    SELECT TOP (@N_GRAVES) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS g
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT #grave (g, cot_count, owner_j, zone, grave_code, grave_type)
SELECT g,
    -- phân bố 1..6 trung bình ~2.4 (đủ nhỏ để tổng < số cốt)
    CASE WHEN r < 30 THEN 1 WHEN r < 60 THEN 2 WHEN r < 80 THEN 3
         WHEN r < 92 THEN 4 WHEN r < 97 THEN 5 ELSE 6 END,
    1 + (CHECKSUM(NEWID()) & 2147483647) % @nLiving,
    CHAR(65 + (g % 12)),
    CHAR(65 + (g % 12)) + '-' + RIGHT('000000' + CAST(g AS VARCHAR(7)), 6),
    CASE WHEN g % 20 = 0 THEN 'FAMILY' WHEN g % 7 = 0 THEN 'DOUBLE' ELSE 'SINGLE' END
FROM nums
CROSS APPLY (SELECT (CHECKSUM(NEWID()) & 2147483647) % 100 AS r) rr;

-- prefix-sum để cấp phát cốt theo thứ tự
;WITH ord AS (
    SELECT g, cot_count, SUM(cot_count) OVER (ORDER BY g ROWS UNBOUNDED PRECEDING) - cot_count AS ss
    FROM #grave
)
UPDATE gr SET slot_start = ord.ss FROM #grave gr JOIN ord ON ord.g = gr.g;

-- gán chủ mộ (id + giới tính)
UPDATE gr SET owner_id = l.customer_id, owner_gender = l.gender
FROM #grave gr JOIN #living l ON l.j = gr.owner_j;

-- Insert Graves (cot_count = số cốt; status OCCUPIED)
INSERT INTO dbo.Graves (grave_code, zone, plot_number, grave_type, status, owner_customer_id, cot_count, area_m2, created_at, created_by_user_id)
SELECT grave_code, zone, CAST(g AS VARCHAR(20)), grave_type, 'OCCUPIED', owner_id, cot_count,
       CAST(4 + (g % 8) AS DECIMAL(10,2)), @now, @adminId
FROM #grave;
UPDATE gr SET grave_id = gv.id FROM #grave gr JOIN dbo.Graves gv ON gv.grave_code = gr.grave_code;

-- ── 2. CỐT: cấp phát khách hàng DECEASED theo slot, chọn loại quan hệ ────────
CREATE TABLE #occ (
    grave_id BIGINT, owner_id BIGINT, owner_gender VARCHAR(10),
    deceased_id BIGINT, occ_gender VARCHAR(10), occ_name NVARCHAR(200),
    occ_dob DATE, occ_death DATE, occ_hometown NVARCHAR(200),
    kind VARCHAR(24), inv_kind VARCHAR(24), owner_rel NVARCHAR(100), dec_rel NVARCHAR(100)
);

INSERT #occ (grave_id, owner_id, owner_gender, deceased_id, occ_gender, occ_name, occ_dob, occ_death, occ_hometown, kind)
SELECT gr.grave_id, gr.owner_id, gr.owner_gender,
       d.customer_id, d.gender, d.full_name, d.dob, d.death_solar, d.hometown,
       CASE WHEN rr.r < 40 THEN 'PARENT'
            WHEN rr.r < 55 THEN 'GRANDPARENT_PATERNAL'
            WHEN rr.r < 67 THEN 'GRANDPARENT_MATERNAL'
            WHEN rr.r < 82 THEN 'SPOUSE'
            WHEN rr.r < 92 THEN 'SIBLING'
            ELSE 'CHILD' END
FROM #grave gr
JOIN #num n ON n.k < gr.cot_count
JOIN #deceased d ON d.j = gr.slot_start + n.k + 1          -- +1 vì IDENTITY bắt đầu từ 1
CROSS APPLY (SELECT (CHECKSUM(NEWID()) & 2147483647) % 100 AS r) rr
WHERE gr.slot_start + n.k < @nDeceased;

-- vợ/chồng phải khác giới: cùng giới thì đổi sang anh/chị/em
UPDATE #occ SET kind = 'SIBLING' WHERE kind = 'SPOUSE' AND occ_gender = owner_gender;

-- nghịch đảo + nhãn theo giới tính (owner_rel theo giới tính CỐT, dec_rel theo giới tính CHỦ)
-- owner_rel = "chủ mộ LÀ GÌ của người mất": quan hệ NGHỊCH ĐẢO (rki), theo giới tính CHỦ
-- dec_rel   = "người mất LÀ GÌ của chủ mộ": quan hệ THUẬN (rko = kind), theo giới tính CỐT
UPDATE o SET
    inv_kind  = rko.inverse_code,
    owner_rel = CASE o.owner_gender WHEN 'MALE' THEN rki.label_male WHEN 'FEMALE' THEN rki.label_female ELSE rki.label_neutral END,
    dec_rel   = CASE o.occ_gender   WHEN 'MALE' THEN rko.label_male WHEN 'FEMALE' THEN rko.label_female ELSE rko.label_neutral END
FROM #occ o
JOIN dbo.Relationship_Kinds rko ON rko.kind_code = o.kind
JOIN dbo.Relationship_Kinds rki ON rki.kind_code = rko.inverse_code;

-- Insert Grave_Occupants (link cốt = customer, snapshot tên + nhãn quan hệ)
INSERT INTO dbo.Grave_Occupants
    (grave_id, full_name, gender, dob, death_date_solar, burial_date, hometown, deceased_customer_id, owner_relationship, deceased_relationship, created_at, created_by_user_id)
SELECT grave_id, occ_name, occ_gender, occ_dob, occ_death, DATEADD(DAY, 3, occ_death), occ_hometown,
       deceased_id, owner_rel, dec_rel, @now, @adminId
FROM #occ;

-- ── 3. QUAN HỆ 2 CHIỀU chủ ↔ cốt (Customer_Relationships) ───────────────────
INSERT INTO dbo.Customer_Relationships (from_customer_id, to_customer_id, relation_kind, is_derived, needs_confirmation, created_at, created_by_user_id)
SELECT owner_id, deceased_id, kind, 0, 0, @now, @adminId FROM #occ
UNION ALL
SELECT deceased_id, owner_id, inv_kind, 0, 0, @now, @adminId FROM #occ;

-- ── 4. LIÊN HỆ KHẨN CẤP: 2 người/mộ (link khách hàng đang sống) ─────────────
-- Vật chất hoá chỉ số ngẫu nhiên vào bảng tạm (tránh NEWID trong điều kiện JOIN)
CREATE TABLE #ec (grave_id BIGINT, priority INT, lj INT);
INSERT #ec (grave_id, priority, lj)
SELECT gr.grave_id, pr.priority, 1 + (CHECKSUM(NEWID()) & 2147483647) % @nLiving
FROM #grave gr CROSS JOIN (VALUES (1),(2)) pr(priority);

INSERT INTO dbo.Grave_Emergency_Contacts (grave_id, priority, contact_customer_id, relationship_note, is_active, created_at, created_by_user_id)
SELECT ec.grave_id, ec.priority, l.customer_id,
       CASE ec.priority WHEN 1 THEN N'Người thân (ưu tiên gọi trước)' ELSE N'Người thân (dự phòng)' END,
       1, @now, @adminId
FROM #ec ec JOIN #living l ON l.j = ec.lj;

-- ── KẾT QUẢ ─────────────────────────────────────────────────────────────────
PRINT '=== SEED BUOC B XONG ===';
SELECT 'Graves' AS bang, COUNT(*) AS n FROM #grave
UNION ALL SELECT 'Grave_Occupants (cot)', COUNT(*) FROM #occ
UNION ALL SELECT 'Customer_Relationships', COUNT(*) FROM #occ  -- x2 thuc te
UNION ALL SELECT 'So cot TB/mo (x100)', 100*(SELECT COUNT(*) FROM #occ)/@N_GRAVES;
