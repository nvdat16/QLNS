# ⚙️ Phân Hệ Backend API (FastAPI)

> Dịch vụ RESTful API trung tâm của Hệ thống Quản lý Nhân sự (**QLNS**), xây dựng trên nền tảng **FastAPI (Python 3.12)**, **SQLAlchemy 2.0 ORM**, **Pydantic v2** và kết nối cơ sở dữ liệu **PostgreSQL 16**.

---

## 📌 Mục Lục

- [1. Kiến Trúc & Công Nghệ](#1-kiến-trúc--công-nghệ)
- [2. Cấu Trúc Thư Mục Backend](#2-cấu-trúc-thư-mục-backend)
- [3. Danh Mục REST API Endpoints](#3-danh-mục-rest-api-endpoints)
  - [3.1. Phân hệ Hồ sơ & Vòng đời Nhân sự (Core HR)](#31-phân-hệ-hồ-sơ--vòng-đời-nhân-sự-core-hr)
  - [3.2. Phân hệ Tuyển dụng Thông minh (Recruitment ATS)](#32-phân-hệ-tuyển-dụng-thông-minh-recruitment-ats)
  - [3.3. Giám sát & Kiểm tra Trạng thái (System & Health)](#33-giám-sát--kiểm-tra-trạng-thái-system--health)
- [4. Tài Liệu API Tương Tác (Swagger / ReDoc)](#4-tài-liệu-api-tương-tác-swagger--redoc)
- [5. Hướng Dẫn Cài Đặt & Chạy Cục Bộ](#5-hướng-dẫn-cài-đặt--chạy-cục-bộ)
- [🔗 Quay lại README Tổng Quan](../README.md)

---

## 1. Kiến Trúc & Công Nghệ

- **Framework**: [FastAPI 0.110+](https://fastapi.tiangolo.com/) (Asynchronous, High-Performance Python Web Framework)
- **Runtime**: Python 3.12 & Uvicorn ASGI Web Server
- **ORM & Data Access**: [SQLAlchemy 2.0](https://www.sqlalchemy.org/) với Connection Pooling & Session Local
- **Validation & Serialization**: [Pydantic v2](https://docs.pydantic.dev/) (Data parsing & Schemas)
- **Database Driver**: `psycopg2-binary` kết nối PostgreSQL 16
- **CORS Middleware**: Cấu hình mở linh hoạt cho Frontend React (`localhost:5173`) và các trang HTML standalone (`file://`).

---

## 2. Cấu Trúc Thư Mục Backend

```text
backend/
├── app/
│   ├── config.py             # Cấu hình môi trường (DATABASE_URL, API prefix, version)
│   ├── database.py           # Thiết lập SQLAlchemy engine, SessionLocal, Base
│   ├── main.py               # Entrypoint FastAPI, CORS middleware, khai báo router
│   ├── models/               # SQLAlchemy Declarative Models
│   │   ├── __init__.py
│   │   ├── employee.py       # Entity: Employee, Department, Position, Contract, v.v.
│   │   └── recruitment.py    # Entity: JobPosting, Candidate, Application, Interview, Offer
│   ├── routers/              # Bộ định tuyến REST API (APIRouter)
│   │   ├── __init__.py
│   │   ├── employees.py      # Endpoints quản lý nhân viên, hợp đồng, phòng ban
│   │   └── recruitment.py    # Endpoints ATS Kanban, danh sách ứng viên, tin tuyển dụng
│   └── schemas/              # Pydantic Schemas (Input DTO & Response Serialization)
│       ├── employee.py
│       └── recruitment.py
├── Dockerfile                # Multi-stage Docker image (Python 3.12 slim)
└── requirements.txt          # Danh mục thư viện phụ thuộc
```

---

## 3. Danh Mục REST API Endpoints

### 3.1. Phân hệ Hồ sơ & Vòng đời Nhân sự (Core HR)

| Phương thức | Đường dẫn API | Tham số | Mô Tả Chức Năng |
| :---: | :--- | :--- | :--- |
| `GET` | `/api/employees` | `query`, `department`, `status` | Tìm kiếm và lấy danh sách nhân viên theo bộ lọc. |
| `POST` | `/api/employees` | Body: `EmployeeCreate` | Thêm mới hồ sơ nhân sự vào hệ thống. |
| `GET` | `/api/employees/{id}` | Path: `id` | Lấy chi tiết hồ sơ nhân sự (kèm hợp đồng, công việc tiếp nhận, lịch sử biến động). |
| `GET` | `/api/departments` | - | Lấy danh mục tất cả phòng ban và số lượng nhân sự. |
| `GET` | `/api/positions` | - | Lấy danh mục vị trí, chức danh và cấp bậc (Level). |
| `GET` | `/api/contracts` | `employee_id`, `status` | Lấy danh sách hợp đồng lao động theo nhân viên hoặc trạng thái. |
| `GET` | `/api/stats/headcount` | - | Thống kê số lượng nhân sự phân bổ theo phòng ban. |

### 3.2. Phân hệ Tuyển dụng Thông minh (Recruitment ATS)

| Phương thức | Đường dẫn API | Tham số | Mô Tả Chức Năng |
| :---: | :--- | :--- | :--- |
| `GET` | `/api/recruitment/pipeline` | - | Lấy toàn bộ dữ liệu bảng Kanban ATS chia theo 6 giai đoạn tuyển dụng. |
| `POST` | `/api/recruitment/pipeline/advance` | Body: `application_id`, `target_stage` | Chuyển giai đoạn ứng viên trong đường ống tuyển dụng (Drag & Drop / Nút bấm). |
| `GET` | `/api/recruitment/jobs` | `department_id`, `status` | Lấy danh sách tin tuyển dụng / vị trí đang mở. |
| `POST` | `/api/recruitment/jobs` | Body: `JobPostingCreate` | Tạo mới yêu cầu tuyển dụng (Job Requisition). |
| `GET` | `/api/recruitment/candidates` | `query`, `status` | Lấy danh sách tổng hợp ứng viên trong Talent Pool. |

### 3.3. Giám sát & Kiểm tra Trạng thái (System & Health)

| Phương thức | Đường dẫn API | Mô Tả Chức Năng |
| :---: | :--- | :--- |
| `GET` | `/api/health` | Trả về trạng thái hoạt động của Backend và kết nối cơ sở dữ liệu (`{"status": "healthy"}`). |
| `GET` | `/` | Endpoint gốc giới thiệu hệ thống và chỉ mục đường dẫn tài liệu. |

---

## 4. Tài Liệu API Tương Tác (Swagger / ReDoc)

Khi dịch vụ backend đang chạy, bạn có thể truy cập tài liệu tương tác trực quan:
- **Swagger UI**: [http://localhost:8000/docs](http://localhost:8000/docs) (Hỗ trợ thử nghiệm gọi API trực tiếp)
- **ReDoc**: [http://localhost:8000/redoc](http://localhost:8000/redoc) (Giao diện tài liệu kỹ thuật tra cứu tham số chi tiết)

---

## 5. Hướng Dẫn Cài Đặt & Chạy Cục Bộ

### 5.1. Chạy bằng Docker Compose (Khuyên dùng)
```bash
# Khởi chạy cả backend và database cùng lúc
docker compose up -d backend
```

### 5.2. Chạy trực tiếp bằng Python Virtual Environment
```bash
cd backend

# Khởi tạo môi trường ảo
python3 -m venv venv
source venv/bin/activate

# Cài đặt thư viện
pip install -r requirements.txt

# Thiết lập chuỗi kết nối Database (nếu chạy DB ở máy trạm)
export DATABASE_URL="postgresql://postgres:postgres@localhost:5432/qlns_db"

# Khởi chạy máy chủ Uvicorn ở chế độ Reload
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

---

[⬅️ Trở về Trang Chủ Tài Liệu](../README.md)
