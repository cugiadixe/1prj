import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/setupTests.ts'],
    globals: true,
    // Mặc định 5000ms quá chặt cho bộ antd + jsdom này: khi nhiều file chạy song song trên
    // máy tải nặng, các test hoàn toàn bình thường vẫn timeout NGẪU NHIÊN, xoay vòng qua
    // từng file khác nhau mỗi lượt. Fail nhấp nháy kiểu đó làm bộ test mất tin cậy còn hơn
    // là chạy chậm. Đây KHÔNG phải để né test chậm thật — không test nào ở đây cần tới 20s.
    testTimeout: 20000,
    hookTimeout: 20000
  }
})
