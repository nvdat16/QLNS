# 2. Architecture Constraints

## 2.1 Định nghĩa và cách lập tài liệu

**Ràng buộc kiến trúc (architecture constraint)** là bất kỳ yêu cầu hoặc điều kiện nào làm hạn chế sự tự do của kiến trúc sư phần mềm khi đưa ra quyết định về thiết kế, triển khai hoặc quy trình phát triển.

Đối với QLNS, mỗi ràng buộc phải trả lời được bốn câu hỏi:

1. Ràng buộc xuất phát từ đâu: nghiệp vụ, tổ chức, công nghệ, hệ thống khác hay quy định pháp lý?
2. Ràng buộc giới hạn quyết định nào của kiến trúc?
3. Hệ quả đối với thiết kế, triển khai, kiểm thử và vận hành là gì?
4. Ràng buộc là bắt buộc lâu dài, baseline có thể thay đổi hay mới chỉ là định hướng?

Các nguyên tắc áp dụng trong chương này:

- Xem xét cả ràng buộc của các hệ thống khác trong tổ chức, đặc biệt là cổng tuyển dụng, email/calendar, chữ ký điện tử, thiết bị chấm công, kế toán và ngân hàng.
- Ghi rõ hệ quả kiến trúc của từng ràng buộc thay vì chỉ nêu tên công nghệ hoặc quy định.
- Bao gồm ràng buộc tổ chức như quy trình phê duyệt nghiệp vụ, quản lý tài liệu và trách nhiệm xác nhận của Product Owner, HR và pháp chế.
- Bao gồm ràng buộc thiết kế và phát triển như stack hiện tại, giao thức API, database, container hóa và quy trình migration.
- Phân biệt rõ các nhóm kỹ thuật, dữ liệu, bảo mật–tuân thủ, tổ chức–quy trình và giao diện–tích hợp.
- Không biến một lựa chọn kỹ thuật có thể thay đổi thành ràng buộc vĩnh viễn; lựa chọn như vậy phải được đánh giá và ghi nhận bằng ADR.

## 2.2 Phân loại mức độ

| Mức độ | Ý nghĩa |
| --- | --- |
| **Bắt buộc** | Xuất phát từ nghiệp vụ, bảo mật, pháp lý hoặc ranh giới hệ thống; chỉ thay đổi khi yêu cầu gốc thay đổi. |
| **Baseline dự án** | Đang tồn tại trong repository và cần được bảo toàn trong ngắn hạn; có thể thay đổi thông qua ADR và kế hoạch migration. |
| **Dự kiến** | Cần cho kiến trúc mục tiêu nhưng chưa được hiện thực hoặc chưa được xác nhận đầy đủ. |

## 2.3 Ràng buộc kỹ thuật

| ID | Mức độ | Ràng buộc | Hệ quả kiến trúc |
| --- | --- | --- | --- |
| TC-01 | Baseline dự án | Frontend là Single Page Application sử dụng React 18 và Vite 5. | Các chức năng web phải hoạt động trong trình duyệt hiện đại; chưa yêu cầu ứng dụng mobile native. |
| TC-02 | Baseline dự án | Backend sử dụng Python 3.12, FastAPI, Pydantic v2 và SQLAlchemy 2.0. | API, validation và truy cập dữ liệu mới phải tương thích với stack hiện tại hoặc cần ADR trước khi thay thế. |
| TC-03 | Baseline dự án | Dữ liệu nghiệp vụ được lưu trong PostgreSQL 16. | Thiết kế sử dụng mô hình quan hệ, khóa ngoại và transaction; thay đổi schema phải có migration có thể kiểm soát. |
| TC-04 | Bắt buộc | Frontend không truy cập trực tiếp database. Mọi truy cập dữ liệu nghiệp vụ đi qua Backend API. | Quyền truy cập, validation và quy tắc nghiệp vụ phải được thực thi phía server. |
| TC-05 | Baseline dự án | Giao tiếp frontend–backend sử dụng REST, JSON và tiền tố đường dẫn `/api`. | Endpoint mới phải giữ hợp đồng API rõ ràng; thay đổi không tương thích cần versioning hoặc lộ trình chuyển đổi. |
| TC-06 | Baseline dự án | Môi trường phát triển và triển khai tham chiếu dùng Docker Compose với ba container: frontend, backend và PostgreSQL. | Dịch vụ phải cấu hình được bằng biến môi trường và không phụ thuộc vào trạng thái cục bộ ngoài container. |
| TC-07 | Bắt buộc | Thời gian và định danh của thao tác nhạy cảm phải do server xác lập. | Check-in/check-out, phê duyệt, chuyển trạng thái và audit không được tin cậy timestamp hoặc actor do client tự khai báo. |
| TC-08 | Bắt buộc | Các cập nhật liên quan nhiều bản ghi phải bảo toàn tính nguyên tử. | Chuyển ứng viên thành nhân viên, chuyển trạng thái tuyển dụng, duyệt nghỉ hoặc cập nhật hợp đồng phải chạy trong transaction phù hợp. |
| TC-09 | Dự kiến | Cổng tuyển dụng, email/calendar, chữ ký điện tử và thiết bị chấm công là hệ thống bên ngoài. | Tích hợp phải qua adapter/gateway, có timeout, xử lý lỗi, retry có kiểm soát và chống tạo yêu cầu trùng. |
| TC-10 | Dự kiến | Attendance, Leave, Payroll, Performance, Learning, RBAC, Notification và Audit chưa có đầy đủ schema/API trong baseline. | Không được triển khai logic sản xuất cho các phân hệ này chỉ bằng dữ liệu UI mẫu; phải mở rộng schema, API và kiểm thử trước. |

## 2.4 Ràng buộc dữ liệu

| ID | Mức độ | Ràng buộc | Hệ quả kiến trúc |
| --- | --- | --- | --- |
| DC-01 | Baseline dự án | Schema hiện tại có 14 bảng tập trung vào Core HR, hợp đồng, tuyển dụng và onboarding. | Mọi sơ đồ dùng bảng ngoài phạm vi này phải ghi rõ **Proposed** cho đến khi migration tương ứng tồn tại. |
| DC-02 | Bắt buộc | Mã nhân viên, mã vị trí/phòng ban và các định danh nghiệp vụ cần duy nhất trong phạm vi áp dụng. | Database phải có unique constraint phù hợp; API phải trả lỗi xung đột có ý nghĩa. |
| DC-03 | Bắt buộc | Quan hệ giữa ứng viên, đơn ứng tuyển, offer và hồ sơ nhân viên phải truy vết được. | Luồng onboarding không được tạo nhân viên trùng và phải giữ liên kết với dữ liệu tuyển dụng nguồn. |
| DC-04 | Bắt buộc | Trạng thái nghiệp vụ chỉ được thay đổi theo tập giá trị và transition hợp lệ. | Không cho phép client cập nhật trực tiếp trạng thái tùy ý; backend kiểm tra transition và thẩm quyền. |
| DC-05 | Bắt buộc | Dữ liệu cá nhân và dữ liệu nhạy cảm chỉ được thu thập, hiển thị và xuất theo mục đích, vai trò và phạm vi được phép. | Cần phân quyền cấp bản ghi/trường dữ liệu, che dữ liệu khi cần và kiểm soát các bản export. |
| DC-06 | Dự kiến | Chính sách lưu trữ, xóa, ẩn danh và sao lưu dữ liệu chưa được xác nhận đầy đủ. | Trước production, Product Owner, pháp chế và vận hành phải thống nhất retention schedule và quy trình thực thi. |

## 2.5 Ràng buộc bảo mật và tuân thủ

| ID | Mức độ | Ràng buộc | Hệ quả kiến trúc |
| --- | --- | --- | --- |
| SC-01 | Bắt buộc | Các API nghiệp vụ phải yêu cầu danh tính đã xác thực trong môi trường vận hành. | Backend cần cơ chế authentication tập trung; không dựa vào dữ liệu người dùng gửi trong request để xác định actor. |
| SC-02 | Bắt buộc | Quyền được kiểm soát theo vai trò và phạm vi dữ liệu. Baseline nghiệp vụ xác định sáu vai trò chính: Admin, HR Manager, Recruiter, Interviewer, HR Officer và Employee. | Kiểm tra quyền phải nằm tại backend; frontend chỉ hỗ trợ trải nghiệm và không phải hàng rào bảo mật. |
| SC-03 | Bắt buộc | Thao tác tạo, sửa, xóa, xuất, phê duyệt, từ chối và chuyển trạng thái quan trọng phải được audit. | Audit record cần actor, thời điểm, hành động, đối tượng và kết quả; dữ liệu audit không được sửa bởi người dùng nghiệp vụ thông thường. |
| SC-04 | Bắt buộc | Dữ liệu truyền qua mạng trong môi trường vận hành phải được bảo vệ bằng HTTPS/TLS. | HTTP chỉ phù hợp cho môi trường phát triển cô lập; thông tin xác thực và dữ liệu cá nhân không truyền qua kết nối không mã hóa. |
| SC-05 | Bắt buộc | Secret và thông tin kết nối không được hard-code hoặc commit dưới dạng thông tin production. | Mật khẩu, token và khóa tích hợp phải được cấp qua secret/environment management và có khả năng luân chuyển. |
| SC-06 | Bắt buộc | Hệ thống phải tuân thủ quy định lao động và bảo vệ dữ liệu áp dụng tại Việt Nam. | Quy tắc hợp đồng, bảo hiểm, thuế, quyền riêng tư và thời hạn lưu trữ cần được pháp chế xác nhận trước khi nghiệm thu production. |
| SC-07 | Dự kiến | Kết quả AI/OCR chỉ hỗ trợ quyết định tuyển dụng. | Phải cho phép con người kiểm tra/sửa kết quả; không tự động loại ứng viên chỉ dựa trên điểm AI mà thiếu cơ chế kiểm soát đã được phê duyệt. |

## 2.6 Ràng buộc tổ chức và quy trình

| ID | Mức độ | Ràng buộc | Hệ quả kiến trúc |
| --- | --- | --- | --- |
| OC-01 | Baseline dự án | Hệ thống được phát triển tăng dần từ repository hiện tại. | Thay đổi phải giữ khả năng chạy của các luồng Core HR và Recruitment hiện có hoặc kèm migration rõ ràng. |
| OC-02 | Bắt buộc | Chủ sở hữu nghiệp vụ phê duyệt quy trình và trạng thái; nhóm phát triển không tự suy diễn quy định nhân sự hoặc pháp lý. | Các transition, ma trận quyền và công thức nghiệp vụ phải có tiêu chí nghiệm thu từ HR/Product Owner. |
| OC-03 | Baseline dự án | Tài liệu kiến trúc được quản lý cùng source code. | C4, arc42, SRS và schema phải được cập nhật trong cùng thay đổi khi ranh giới hoặc hợp đồng liên quan thay đổi. |
| OC-04 | Baseline dự án | Sơ đồ kiến trúc sử dụng C4; code view dùng PlantUML/UML và tài liệu diễn giải dùng Markdown. | Tên container/component phải nhất quán giữa sơ đồ, source code và tài liệu arc42. |
| OC-05 | Bắt buộc | Nội dung hiện thực và nội dung đề xuất phải phân biệt rõ. | Chức năng chưa có code/schema/API phải mang nhãn **Planned** hoặc **Proposed** trong tài liệu và sơ đồ. |
| OC-06 | Dự kiến | SLA, quy mô người dùng, RPO/RTO và ngân sách hạ tầng chưa được chốt. | Không được cam kết con số hiệu năng hay khả dụng production trước khi các đầu vào này được phê duyệt. |

## 2.7 Quy ước giao diện và tích hợp

| ID | Mức độ | Ràng buộc | Hệ quả kiến trúc |
| --- | --- | --- | --- |
| IC-01 | Baseline dự án | Giao diện nghiệp vụ chính sử dụng tiếng Việt; thuật ngữ kỹ thuật có thể kèm tiếng Anh. | Nhãn, trạng thái, thông báo lỗi và tài liệu người dùng cần dùng thuật ngữ thống nhất. |
| IC-02 | Bắt buộc | Giao diện phải responsive cho desktop, laptop và tablet; chức năng thiết yếu không phụ thuộc vào hover. | Bảng dữ liệu và quy trình phê duyệt phải sử dụng được ở các kích thước màn hình mục tiêu. |
| IC-03 | Bắt buộc | API phải trả lỗi có cấu trúc và không làm lộ stack trace, secret hoặc dữ liệu nhạy cảm. | Frontend ánh xạ lỗi nghiệp vụ thành thông báo hữu ích; log kỹ thuật được giữ ở backend. |
| IC-04 | Dự kiến | Hợp đồng API với dịch vụ bên ngoài chưa được chốt. | Mỗi tích hợp cần tài liệu về authentication, timeout, rate limit, retry, idempotency và đối soát trước khi triển khai. |

