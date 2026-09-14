# 1. Introduction and Goals

## 1.1 Tổng quan yêu cầu

### Giới thiệu hệ thống

**QLNS** là hệ thống quản trị nguồn nhân lực (Human Resource Management System — HRMS) kết hợp quản lý tuyển dụng (Applicant Tracking System — ATS). Hệ thống cung cấp một nguồn dữ liệu tập trung cho quá trình từ yêu cầu tuyển dụng, quản lý ứng viên, phỏng vấn và đề nghị nhận việc đến hồ sơ nhân viên, cơ cấu tổ chức, hợp đồng, chấm công và nghỉ phép.

Hệ thống hướng đến các doanh nghiệp cần chuẩn hóa nghiệp vụ nhân sự, giảm thao tác thủ công và tạo khả năng truy vết đối với dữ liệu cũng như quyết định nghiệp vụ. Các nhóm người dùng chính gồm nhân viên, quản lý trực tiếp, nhân sự, tuyển dụng và quản trị hệ thống.

### Bối cảnh và vấn đề cần giải quyết

Quy trình nhân sự phân tán qua bảng tính, email và nhiều công cụ riêng lẻ thường dẫn đến dữ liệu trùng lặp, nhập liệu nhiều lần, khó theo dõi trách nhiệm phê duyệt và thiếu số liệu nhất quán cho quản lý. Đặc biệt, việc chuyển một ứng viên trúng tuyển thành nhân viên, theo dõi hiệu lực hợp đồng, ghi nhận công và xử lý nghỉ phép cần dùng chung dữ liệu nhưng thường bị chia cắt giữa các bộ phận.

QLNS giải quyết vấn đề này bằng cách:

- Tập trung dữ liệu ứng viên, nhân viên, tổ chức và hợp đồng trên một mô hình dữ liệu thống nhất.
- Chuẩn hóa trạng thái và các bước phê duyệt của quy trình tuyển dụng và nhân sự.
- Hạn chế nhập lại dữ liệu khi chuyển từ ứng viên trúng tuyển sang hồ sơ nhân viên.
- Cung cấp giao diện tự phục vụ phù hợp với quyền hạn của từng vai trò.
- Tạo nền tảng cho báo cáo nhân sự, nhật ký kiểm toán và tích hợp với các dịch vụ bên ngoài.

### Mục tiêu và phạm vi

#### Mục tiêu nghiệp vụ

| ID | Mục tiêu | Kết quả mong đợi |
| --- | --- | --- |
| BG-01 | Số hóa vòng đời tuyển dụng và nhân sự | Dữ liệu được quản lý xuyên suốt từ yêu cầu tuyển dụng đến khi nhân viên nghỉ việc. |
| BG-02 | Rút ngắn và minh bạch hóa tuyển dụng | Recruiter và Hiring Manager theo dõi được ứng viên, lịch phỏng vấn, scorecard, offer và người chịu trách nhiệm tại từng bước. |
| BG-03 | Xây dựng nguồn dữ liệu nhân sự thống nhất | Hồ sơ, phòng ban, vị trí, hợp đồng và lịch sử công tác sử dụng chung một nguồn dữ liệu có kiểm soát. |
| BG-04 | Chuẩn hóa chấm công và nghỉ phép | Ca làm việc, sự kiện vào/ra, số dư phép, đơn nghỉ và phê duyệt tuân theo quy tắc xác định. |
| BG-05 | Hỗ trợ quyết định quản trị | Cung cấp dữ liệu đáng tin cậy cho dashboard, báo cáo quân số và hiệu quả tuyển dụng. |
| BG-06 | Giảm rủi ro vận hành và tuân thủ | Kiểm soát quyền truy cập, trạng thái nghiệp vụ, thời hạn hợp đồng và lịch sử thay đổi. |

#### Phạm vi chức năng mục tiêu

Kiến trúc mục tiêu của QLNS bao gồm tám nhóm năng lực:

1. Tuyển dụng: yêu cầu tuyển dụng, đăng tin, tiếp nhận CV, pipeline ứng viên, phỏng vấn, scorecard và offer.
2. Core HR: hồ sơ nhân viên, phòng ban, vị trí, cơ cấu tổ chức, hợp đồng và vòng đời nhân sự.
3. Chấm công và nghỉ phép: ca làm việc, phân ca, check-in/check-out, bảng công, đơn nghỉ và phê duyệt.
4. Lương thưởng và phúc lợi.
5. Quản lý hiệu suất.
6. Đào tạo và phát triển.
7. Báo cáo và phân tích.
8. Quản trị hệ thống: tài khoản, RBAC, cấu hình và audit trail.

#### Phạm vi triển khai hiện tại

Tại thời điểm lập tài liệu, repository mới hiện thực một phần kiến trúc mục tiêu:

| Trạng thái | Phạm vi |
| --- | --- |
| Đã có mô hình dữ liệu và API nền tảng | Hồ sơ nhân viên, phòng ban, vị trí, hợp đồng, tuyển dụng và pipeline ứng viên. |
| Đã có giao diện tích hợp API một phần | Danh sách nhân viên và pipeline tuyển dụng. |
| Đã có prototype hoặc thiết kế đề xuất | Tổ chức, hợp đồng, lịch phỏng vấn, onboarding, chấm công, ca làm việc và nghỉ phép. Một số màn hình vẫn dùng dữ liệu mẫu và thao tác thông báo. |
| Chưa phải chức năng vận hành hoàn chỉnh | Authentication/RBAC, audit log, notification, tích hợp nền tảng tuyển dụng, email/calendar, chữ ký điện tử, thiết bị chấm công, payroll, performance và learning. |

Các sơ đồ hoặc đặc tả có nhãn **Proposed** hoặc **Planned** thể hiện kiến trúc đích, không phải bằng chứng rằng chức năng đã được triển khai.

#### Ngoài phạm vi

- QLNS không thay thế hệ thống kế toán, ngân hàng, cổng tuyển dụng, email/calendar, chữ ký điện tử hoặc thiết bị chấm công; các hệ thống này được xem là dịch vụ bên ngoài.
- Phiên bản hiện tại không đặt mục tiêu cung cấp ứng dụng di động native.
- Kết quả AI/OCR hoặc điểm phù hợp ứng viên chỉ là thông tin hỗ trợ; hệ thống không tự động đưa ra quyết định tuyển dụng hoặc quyết định ảnh hưởng đến người lao động.
- Yêu cầu pháp lý chi tiết phải được xác nhận bởi bộ phận pháp chế trước khi đưa hệ thống vào vận hành thực tế.

## 1.2 Mục tiêu chất lượng

Các mục tiêu dưới đây được sắp xếp theo mức ưu tiên đối với kiến trúc mục tiêu.

| Ưu tiên | ID | Mục tiêu chất lượng | Kịch bản kiểm chứng |
| ---: | --- | --- | --- |
| 1 | QG-01 | Bảo mật và bảo vệ dữ liệu cá nhân | Khi người dùng truy cập hồ sơ, lương, hợp đồng hoặc thao tác phê duyệt ngoài phạm vi quyền, hệ thống phải từ chối ở backend, không chỉ ẩn trên giao diện, và ghi nhận sự kiện bảo mật phù hợp. |
| 2 | QG-02 | Toàn vẹn và khả năng truy vết dữ liệu | Mọi thao tác tạo, sửa, chuyển trạng thái, duyệt hoặc từ chối phải lưu người thực hiện, thời điểm và thay đổi; giao dịch lỗi không được để lại trạng thái nghiệp vụ dở dang. |
| 3 | QG-03 | Chính xác của quy trình nghiệp vụ | Các chuyển trạng thái không hợp lệ, phê duyệt thiếu thẩm quyền, phân ca trùng hoặc đơn nghỉ vượt điều kiện phải bị chặn bằng quy tắc phía server và trả về lỗi có thể xử lý. |
| 4 | QG-04 | Khả dụng và hiệu quả sử dụng | Người dùng hoàn thành các tác vụ thường xuyên như tìm hồ sơ, chuyển vòng ứng viên, chấm công và duyệt phép trên giao diện responsive mà không phải nhập lại dữ liệu đã có. |
| 5 | QG-05 | Dễ bảo trì và mở rộng | Một phân hệ mới hoặc adapter tích hợp bên ngoài có thể được bổ sung qua ranh giới API/service rõ ràng mà không phải sửa logic không liên quan; quy tắc nghiệp vụ quan trọng có thể kiểm thử độc lập. |
| 6 | QG-06 | Khả năng phục hồi khi tích hợp lỗi | Khi email, lịch, chữ ký điện tử hoặc cổng tuyển dụng tạm thời không khả dụng, dữ liệu nghiệp vụ chính vẫn nhất quán; yêu cầu tích hợp có thể retry có kiểm soát mà không tạo bản ghi trùng. |

### Chỉ số kết quả cần theo dõi

- Tỷ lệ hoàn thiện hồ sơ nhân viên mục tiêu lớn hơn 95%.
- Tỷ lệ chấp thuận offer mục tiêu lớn hơn 85%.
- Thời gian tuyển dụng, số hồ sơ qua từng vòng, số hợp đồng sắp hết hạn và thời gian xử lý đơn nghỉ phải đo được từ dữ liệu hệ thống.
- Các ngưỡng hiệu năng, khả dụng và tải đồng thời cần được chốt sau khi xác định quy mô người dùng và hạ tầng vận hành; không coi dữ liệu prototype là số liệu SLA.

### Tiêu chí thành công ở cấp kiến trúc

Phần kiến trúc được xem là đáp ứng mục tiêu khi:

- Mỗi năng lực nghiệp vụ có ranh giới trách nhiệm rõ giữa frontend, backend, database và hệ thống bên ngoài.
- Quyền truy cập và quy tắc chuyển trạng thái được thực thi tại backend.
- Dữ liệu tuyển dụng có thể bàn giao sang Core HR mà không tạo hồ sơ nhân viên trùng lặp.
- Các thao tác nhạy cảm và phê duyệt quan trọng có lịch sử kiểm toán.
- Những phần chưa triển khai được ghi nhãn rõ là planned/proposed trong tài liệu và sơ đồ.
- Các quyết định kiến trúc tiếp theo có thể liên kết về ít nhất một mục tiêu nghiệp vụ hoặc mục tiêu chất lượng trong chương này.

## 1.3 Các bên liên quan

| Bên liên quan | Mối quan tâm và kỳ vọng chính |
| --- | --- |
| Ban lãnh đạo / Nhà tài trợ | Hiệu quả đầu tư, số liệu nhân sự đáng tin cậy, giảm thời gian xử lý và kiểm soát rủi ro. |
| HR Director / HR Manager | Kiểm soát quy trình, phê duyệt đúng thẩm quyền, báo cáo tổng thể và khả năng truy vết quyết định. |
| Talent Acquisition / Recruiter | Quản lý requisition, ứng viên, pipeline, lịch phỏng vấn và offer trên một luồng thống nhất. |
| Hiring Manager / Interviewer | Tạo nhu cầu tuyển dụng, theo dõi tiến độ, tham gia phỏng vấn và gửi scorecard thuận tiện. |
| HR Officer / C&B / Records | Duy trì hồ sơ, hợp đồng, onboarding, chấm công và nghỉ phép chính xác, hạn chế nhập liệu lặp lại. |
| Line Manager | Theo dõi thành viên trong nhóm và xử lý các yêu cầu phê duyệt đúng hạn. |
| Employee | Xem và đề nghị cập nhật dữ liệu cá nhân, chấm công, xem lịch làm việc, số dư phép và trạng thái yêu cầu. |
| Candidate | Nộp hồ sơ, nhận lịch phỏng vấn và offer; dữ liệu cá nhân được sử dụng minh bạch và đúng mục đích. |
| System Administrator | Quản lý tài khoản, vai trò, quyền, cấu hình và nhật ký hệ thống an toàn. |
| Nhóm phát triển và kiểm thử | Yêu cầu rõ ràng, ranh giới module ổn định, môi trường tái lập được và tiêu chí nghiệm thu có thể kiểm chứng. |
| IT Operations / Security | Khả năng triển khai, giám sát, sao lưu, phục hồi, quản lý bí mật và ứng phó sự cố. |
| Pháp chế / Bảo vệ dữ liệu | Tuân thủ quy định lao động và bảo vệ dữ liệu cá nhân, chính sách lưu trữ và cung cấp bằng chứng kiểm toán. |
| Chủ sở hữu hệ thống tích hợp | Hợp đồng API rõ ràng, cơ chế xác thực, giới hạn gọi, retry, đối soát và trách nhiệm xử lý lỗi. |
