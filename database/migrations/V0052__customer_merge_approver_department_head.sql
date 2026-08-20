-- V0052__customer_merge_approver_department_head.sql
--
-- Đổi NGƯỜI DUYỆT của quy trình gộp khách hàng (CUSTOMER_MERGE_DEFAULT, seed ở V0051) từ
-- PLACEHOLDER 'SPECIFIC_USER = admin' sang 'APPROVAL_AUTHORITY cấp 1' = TRƯỞNG PHÒNG của người tạo.
--
-- Ý đồ (anh Bách 2026-08-20): nhân viên tạo yêu cầu gộp thì TRƯỞNG PHÒNG của họ duyệt. Còn admin
-- full quyền (có CUSTOMER_MERGE_EXECUTE toàn cục) thì TẦNG SERVICE cho tự duyệt + thực thi ngay,
-- KHÔNG đi qua workflow này (nên rule ở đây chỉ áp cho người không-full-quyền).
--
-- APPROVAL_AUTHORITY tra Approval_Authorities theo phòng chính của người tạo + cấp = value ('1').
-- ⚠️ Cần seed Approval_Authorities (process_code NULL hoặc = CUSTOMER_MERGE_DUPLICATE) cho từng
--    phòng qua UI thì nhân viên phòng đó mới có người duyệt; chưa có thì submit báo WF_NO_ASSIGNEE.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

UPDATE r
   SET r.approver_source_type  = 'APPROVAL_AUTHORITY',
       r.approver_source_value = '1'
FROM dbo.Workflow_Step_Approver_Rules r
JOIN dbo.Workflow_Steps s              ON s.id = r.workflow_step_id
JOIN dbo.Workflow_Definition_Versions v ON v.id = s.workflow_version_id
JOIN dbo.Workflow_Definitions d         ON d.id = v.workflow_definition_id
WHERE d.definition_code = 'CUSTOMER_MERGE_DEFAULT'
  AND r.approver_source_type = 'SPECIFIC_USER';
GO
