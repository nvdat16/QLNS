# 🌐 Phân Hệ Frontend React SPA (Vite + React 18)

> Ứng dụng Single Page Application (SPA) xây dựng trên nền tảng **React 18** và **Vite 5**, kết nối đồng bộ trực tiếp với **FastAPI Backend** để quản trị nguồn nhân lực và tuyển dụng theo thời gian thực.

---

## 📌 Mục Lục

- [1. Kiến Trúc & Công Nghệ](#1-kiến-trúc--công-nghệ)
- [2. Cấu Trúc Thư Mục](#2-cấu-trúc-thư-mục)
- [3. Các Tính Năng Đã Tích Hợp](#3-các-tính-năng-đã-tích-hợp)
- [4. Hướng Dẫn Cài Đặt & Khởi Chạy](#4-hướng-dẫn-cài-đặt--khởi-chạy)
- [🔗 Quay lại README Tổng Quan](../README.md)

---

## 1. Kiến Trúc & Công Nghệ

- **Framework**: [React 18](https://react.dev/) (Hooks: `useState`, `useEffect`, `useCallback`, `useMemo`)
- **Build Tool**: [Vite 5](https://vitejs.dev/) (Tốc độ khởi động máy chủ tức thì, Hot Module Replacement - HMR)
- **Styling**: Modern CSS Design System (`styles.css`) với hệ thống Design Tokens (màu sắc HSL, hiệu ứng đổ bóng, bo góc, thanh cuộn tùy biến)
- **Networking**: Trình duyệt Fetch API kết nối RESTful API Backend (`http://localhost:8000/api`)

---

## 2. Cấu Trúc Thư Mục

```text
frontend/
├── src/
│   ├── App.jsx               # Thành phần gốc React điều hướng phân hệ, gọi API và hiển thị giao diện
│   ├── main.jsx              # React DOM entrypoint
│   └── styles.css            # Bộ quy chuẩn CSS responsive, layout grid và animations
├── index.html                # HTML entrypoint cho Vite
├── vite.config.js            # Cấu hình Vite dev server và React plugin
├── Dockerfile                # Multi-stage Docker build với Node.js 20 Alpine
└── package.json              # Khai báo dependencies và npm scripts
```

---

## 3. Các Tính Năng Đã Tích Hợp

1. **Điều Hướng Module Trực Quan**: Chuyển đổi linh hoạt giữa các phân hệ:
   - *Hồ sơ Nhân sự (Employee Master Data)*
   - *Đường ống Tuyển dụng (ATS Kanban)*
   - *Tin Tuyển dụng (Job Requisitions)*
   - *Thống kê Tổng quan (Headcount Metrics)*
2. **Đồng Bộ Dữ Liệu Thực Tế**: Tự động kết nối và lấy dữ liệu trực tiếp từ các endpoints của FastAPI (`/api/employees`, `/api/recruitment/pipeline`, `/api/stats/headcount`).
3. **Cơ Chế Dự Phòng (Fallback Resilience)**: Tự động chuyển sang dữ liệu mẫu nội bộ nếu máy chủ backend chưa sẵn sàng, đảm bảo trải nghiệm giao diện người dùng không bị gián đoạn.
4. **Giao Diện Thích Ứng (Responsive)**: Hỗ trợ linh hoạt trên màn hình Desktop, Laptop và Tablet.

---

## 4. Hướng Dẫn Cài Đặt & Khởi Chạy

### 4.1. Chạy với Docker Compose
```bash
docker compose up -d frontend
```
Sau đó truy cập: [http://localhost:5173](http://localhost:5173)

### 4.2. Chạy Cục Bộ với Node.js (v18 trở lên)
```bash
# Di chuyển vào thư mục frontend
cd frontend

# Cài đặt các gói phụ thuộc
npm install

# Khởi chạy Vite Dev Server
npm run dev
```
Trình duyệt sẽ tự động mở hoặc bạn truy cập [http://localhost:5173](http://localhost:5173).

### 4.3. Đóng Gói Bản Build Production
```bash
npm run build
# Các tệp tối ưu sẽ được xuất ra thư mục dist/
```

---

[⬅️ Trở về Trang Chủ Tài Liệu](../README.md)
