# Lộ trình Phát triển Hệ thống Fall Detection dành cho AI (AI Development Roadmap)

Tài liệu này đóng vai trò là "Kim chỉ nam" (Prompt Roadmap) để hướng dẫn AI (Github Copilot/ChatGPT) từng bước lập trình và hoàn thiện dự án **Fall Detection System** theo kiến trúc Microservices & Cross-platform. 

Dựa trên tài liệu `FallDetectionSystem_Plan.md`, lộ trình được chia thành các Giai đoạn cụ thể cho Backend (.NET 8) và Ứng dụng .NET MAUI.

---

## Giai đoạn 1: Chuẩn bị Cấu trúc & Tích hợp Hạ tầng (Infrastructure Integration)
**Mục tiêu:** Tích hợp thành công module `DATN.Infrastructure` vào ASP.NET Core API Backend (`DATN`).

**Các bước cần xử lý:**
1. Thêm Reference của project `DATN.Infrastructure.csproj` (đã có thư viện BSON, JSON, MQTT) vào API backend.
2. Cấu hình Injection/Khởi tạo `Database` của Infrastructure vào Backend. Hệ cơ sở dữ liệu BSON dựa trên FileStorage hiện tại (`BsonData`) phải được liên kết tại `Program.cs`.

---

## Giai đoạn 2: Xây dựng Models & Data Access Layer (Backend - C#)
**Mục tiêu:** Định nghĩa các entities theo chuẩn C# record/class cho MongoDB BSON lưu trữ trên Backend.

**Các bước cần xử lý:**
1. Định nghĩa Data Models trong dự án `DATN`:
   - `User.cs`: ID, Username, Email, Phone, Danh sách `EmergencyContacts`, tuỳ chọn `AutoEmergencyEnabled`.
   - `Device.cs`: Thiết bị cảm biến bao gồm MAC/Device ID, tên thiết bị, trạng thái kết nối. Liên kết một/nhiều với `User`.
   - `EmergencyContact.cs`: Tên, Số điện thoại, Quan hệ, `IsPrimary` marker.
   - `FallEvent.cs`: Lưu trữ dữ kiện của quá trình nhào lộn/té ngã: Thời gian (`DetectedAt`), Tọa độ (Lat/Long), DeviceId, Trạng thái (Đang chờ xử lý, v.v).
2. Tích hợp thư viện BSON attributes.
 
---

## Giai đoạn 3: Phát triển Core Services & APIs (Backend - C#)
**Mục tiêu:** Xử lý MQTT luồng dữ liệu liên tục (Continuous sensor streaming) & API Web chuẩn.

**Các bước cần xử lý:**
1. **MQTT Background Service:**
   - Sử dụng `Client.cs` (từ `Infrastructure.Mqtt`).
   - Lắng nghe Topic `sensor/#` đính kèm Background Worker `IHostedService` trong ASP.NET.
   - Dữ liệu té ngã đẩy tới Topic `sensor/alert`, giải nén và nạp vào collection "FallEvents" của `Database` (`BsonData`).
2. **RESTful Web APIs:**
   - **Auth API:** Đăng nhập/Đăng ký user tạo mã Hash `SHA256`.
   - **Emergency Contacts API:** Thêm/Sửa/Xóa (`Manage Emergency Contacts`).
   - **Device API:** Đăng ký Device (Link thiết bị vào tài khoản User).
   - **Fall History API:** Load dữ liệu lịch sử từ bảng `FallEvent`.

---

## Giai đoạn 4: Thiết lập Nền tảng Mobile App (.NET MAUI - C#)
**Mục tiêu:** Dựng bộ khung UX/UI cho Client trên nền tảng Mobile/Tablet. Tuân thủ mô hình MVVM (`CommunityToolkit.Mvvm`).

**Các bước cần xử lý:**
1. Cấu hình MAUI Shell, Settings cho iOS/Android trong `DATN.App`.
2. Tạo cấu trúc thư mục tiêu chuẩn:
   - `Models/` (Sử dụng lại Data Transfer Objects từ lúc xây dựng API).
   - `Services/` (`IApiService`, chứa logic gọi HTTP Client truy cập tới REST API của Backend).
   - `ViewModels/` chứa file base view model quản lý `IsBusy`.
   - `Views/` tạo page XAML.

---

## Giai đoạn 5: Phát triển Tính năng UI/UX Mobile App
**Mục tiêu:** Hoàn thiện luồng Activity.
1. **Auth UI:** Login / Register liên kết `AuthController`.
2. **Dashboard:** Hiển thị thời gian thực Device Status bằng cách fetch Data (hoặc poll).
3. **Quản lý danh bạ & Settings (Activity 4.3 & 4.6):**
   - CRUD thao tác lên Backend, cập nhật cài đặt Emergency.
4. **Register Device Flow (Activity 4.9):**
   - Nhập mã PIN/QR, lưu trữ ID thiết bị.
5. **Fall History:**
   - Xem lịch sử các lần té ngã được fetch từ Backend. Map trích xuất kinh độ/vĩ độ (nếu có map component).

---

## Giai đoạn 6: Tính năng Nâng cao & Dispatch Cấp Cứu (Realtime / Notification)
**Mục tiêu:** Giải quyết Real-time & Hospital Dispatching (`Request Ambulance`).
1. **SignalR / Websocket Server:** Xây dựng Hub trên Backend để stream thông báo té ngã lập tức cho gia đình, thay vì chỉ MQTT nội bộ.
2. SignalR Client: Đính kèm cho ứng dụng MAUI để báo động khẩn cấp ngay trên màn hình.
3. Push Notification: Cấu hình FCM (Tuỳ chọn) cho ứng dụng di động nếu MAUI app đóng.

---

### Hướng dẫn cách AI phối hợp 🤖
1. Liên tục kiểm tra các file trong thư mục của dự án và đảm bảo đúng các Package Reference khi có Dependency mới.
2. Module CSDL sử dụng `BsonData` từ `DATN.Infrastructure`. Hãy tìm hiểu file `Collection.cs` và `Database.cs` nếu cần thao tác CRUD.
3. Chắc chắn sử dụng `Insert`, `Update`, `Delete` thay vì `Add` hay `Remove` nguyên bản trên C# Collections khi thao tác với `BsonData`.
