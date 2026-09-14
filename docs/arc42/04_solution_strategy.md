# 4. Solution Strategy

Chương này tóm tắt các quyết định nền tảng định hình kiến trúc **QLNS**. Các quyết định dựa trên mục tiêu nghiệp vụ ở Chương 1, ràng buộc ở Chương 2 và ranh giới hệ thống ở Chương 3. Chi tiết cấu trúc sẽ được trình bày ở Chương 5; các cơ chế áp dụng xuyên suốt như security, audit và error handling sẽ được trình bày ở Chương 8.

## 4.1 Các quyết định nền tảng

| ID | Quyết định | Lý do |
| --- | --- | --- |
| SS-01 | Giữ kiến trúc ba tầng gồm React SPA, FastAPI Backend API và PostgreSQL. | Phù hợp baseline hiện tại, dễ triển khai và tạo ranh giới rõ giữa giao diện, nghiệp vụ và dữ liệu. |
| SS-02 | Tổ chức backend theo **modular monolith**, phân chia theo Recruitment, Core HR, Attendance, Leave và các capability dùng chung. | Các phân hệ còn cần transaction và dữ liệu liên kết chặt; microservice sẽ làm tăng chi phí vận hành khi tải và SLA chưa được xác định. |
| SS-03 | Tách frontend theo feature và đưa việc gọi HTTP vào API client thay vì tiếp tục tập trung trong `App.jsx`. | Giảm coupling, giúp từng nghiệp vụ được phát triển và kiểm thử độc lập hơn. |
| SS-04 | Backend là nơi duy nhất thực thi authorization, validation và state transition. | Client là môi trường không tin cậy; quy tắc phải không thể bị bỏ qua bằng cách gọi API trực tiếp. |
| SS-05 | PostgreSQL là system of record; dùng foreign key, constraint, transaction và schema migration có version. | Dữ liệu HR có quan hệ chặt, yêu cầu toàn vẹn và khả năng truy vết cao. |
| SS-06 | Giao tiếp frontend–backend qua REST/JSON dưới `/api`; request/response được định nghĩa bằng DTO và OpenAPI. | Duy trì tương thích với stack hiện tại và tạo hợp đồng rõ giữa frontend với backend. |
| SS-07 | Workflow tuyển dụng, hợp đồng, onboarding và nghỉ phép sử dụng transition tường minh. | Ngăn cập nhật trạng thái tùy ý, bảo đảm đúng thứ tự và đúng thẩm quyền phê duyệt. |
| SS-08 | Hệ thống ngoài được kết nối qua port/adapter; các tác vụ có thể retry phải hỗ trợ idempotency. | Cô lập phụ thuộc nhà cung cấp và hạn chế lỗi email, lịch, chữ ký hoặc thiết bị làm sai trạng thái nghiệp vụ chính. |
| SS-09 | Authentication, RBAC theo phạm vi dữ liệu và audit trail phải hoàn thành trước production. | QLNS xử lý dữ liệu cá nhân và các quyết định nhân sự nhạy cảm. |

## 4.2 Đáp ứng các mục tiêu chất lượng

| Quality goal | Kịch bản chính | Solution approach | Chi tiết |
| --- | --- | --- | --- |
| QG-01 — Bảo mật và privacy | Người dùng truy cập hồ sơ hoặc hành động ngoài quyền. | Identity tập trung, authorization tại backend, least privilege, TLS và quản lý secret. | Chương 8 — Security (sẽ bổ sung) |
| QG-02 — Toàn vẹn và truy vết | Một thao tác nhiều bước lỗi giữa chừng hoặc cần xác định người thay đổi. | Database transaction, constraint, audit event và correlation ID. | Chương 8 — Persistence và Audit (sẽ bổ sung) |
| QG-03 — Chính xác nghiệp vụ | Client yêu cầu transition hoặc phê duyệt không hợp lệ. | Application service và state-transition rules kiểm tra trạng thái, actor và điều kiện. | Chương 5 — Backend Building Blocks (sẽ bổ sung) |
| QG-04 — Khả dụng sử dụng | Người dùng thực hiện tác vụ thường xuyên trên nhiều kích thước màn hình. | React UI theo feature, responsive design, API error mapping và không nhập lại dữ liệu. | Chương 5 — Frontend Building Blocks (sẽ bổ sung) |
| QG-05 — Dễ bảo trì và mở rộng | Bổ sung module hoặc nhà cung cấp tích hợp mới. | Modular monolith, dependency direction và repository/integration ports. | Chương 5 và Chương 8 (sẽ bổ sung) |
| QG-06 — Phục hồi tích hợp | Dịch vụ ngoài timeout hoặc gửi callback lặp lại. | Timeout, retry/backoff, idempotency, delivery state và đối soát. | Chương 8 — Integration (sẽ bổ sung) |

## 4.3 Quyết định tổ chức và phát triển

| Quyết định | Lý do |
| --- | --- |
| Phát triển tăng dần từ baseline, ưu tiên hardening Core HR và Recruitment trước khi mở rộng module. | Giảm rủi ro và tránh xây chức năng mới trên nền authentication, migration và audit chưa hoàn chỉnh. |
| Product Owner/HR xác nhận workflow và ma trận quyền; pháp chế xác nhận các quy tắc tuân thủ. | Nhóm kỹ thuật không tự suy diễn quy định nhân sự hoặc pháp lý. |
| Quản lý C4, arc42, SRS, API và schema cùng source code. | Giúp tài liệu thay đổi đồng bộ với implementation. |
| Dùng automated tests ở cấp domain, API, database và end-to-end cho luồng quan trọng. | Cung cấp bằng chứng rằng các quy tắc và tích hợp vẫn đúng sau thay đổi. |
| Ủy quyền việc chuyển phát email, lịch, chữ ký điện tử và thiết bị cho nhà cung cấp chuyên biệt. | QLNS chỉ sở hữu orchestration và trạng thái nghiệp vụ, không tự xây lại các nền tảng chuyên dụng. |

## 4.4 Các quyết định chưa chốt

Các nội dung sau cần thêm dữ liệu và được ghi bằng ADR trước khi triển khai: Identity Provider, công nghệ migration, object storage, background worker/message broker, nền tảng production, SLA, RPO/RTO và retention policy.

