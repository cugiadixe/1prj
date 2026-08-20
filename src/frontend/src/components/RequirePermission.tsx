import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Result, Button } from 'antd';
import { usePermissions } from '../auth/AuthProvider';

interface RequirePermissionProps {
  /** Cho vào nếu có BẤT KỲ quyền nào trong danh sách (OR). */
  anyOf: string[];
  children: React.ReactNode;
}

/**
 * Guard route theo QUYỀN (chạy sau ProtectedRoute nên đã chắc chắn đã đăng nhập).
 * Chặn truy cập trực tiếp bằng URL vào trang mà menu đã ẩn — tránh lộ trang khi gõ thẳng đường dẫn.
 * Không đủ quyền → hiện màn 403 (không điều hướng ngầm để người dùng hiểu chuyện gì xảy ra).
 */
const RequirePermission: React.FC<RequirePermissionProps> = ({ anyOf, children }) => {
  const { hasPermission } = usePermissions();
  const location = useLocation();
  const navigate = useNavigate();

  const allowed = anyOf.some((code) => hasPermission(code));
  if (!allowed) {
    return (
      <Result
        status="403"
        title="403"
        subTitle="Bạn không có quyền truy cập trang này."
        extra={
          <Button type="primary" onClick={() => navigate('/')}>
            Về trang chủ
          </Button>
        }
        data-testid={`forbidden-${location.pathname}`}
      />
    );
  }
  return <>{children}</>;
};

export default RequirePermission;
