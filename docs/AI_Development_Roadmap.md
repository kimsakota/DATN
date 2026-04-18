# Lộ trình Phát triển Hệ thống Fall Detection dành cho AI (AI Development Roadmap)

Tài liệu này đóng vai trò là "Kim chỉ nam" (Prompt Roadmap) để hướng dẫn AI (Github Copilot/ChatGPT) từng bước lập trình và hoàn thiện dự án **Fall Detection System** từ A-Z. 

Dựa trên cấu trúc workspace hiện tại (Backend C#, Mobile App .NET MAUI, và bộ công cụ C# `Infrastructure` đã có), lộ trình được chia thành các Giai đoạn (Phases) với các câu lệnh (prompts) mẫu để bạn có thể copy/paste yêu cầu AI thực hiện.

---

## Giai đoạn 1: Chuẩn bị Cấu trúc & Tích hợp Hạ tầng (Infrastructure Integration)
*Mục tiêu: Đảm bảo bộ công cụ C# (`Infrastructure`) có thể được sử dụng liền mạch bên trong project C# (`DATN.Backend`).*
  
**Các bước thực hiện:**
1. **Tách/Cấu hình Project Class Library (Nếu cần):** AI cần hướng dẫn tạo một project `DATN.Infrastructure.csproj` (Class Library) chứa thư mục `Infrastructure` hiện tại, sau đó reference nó vào `DATN.Backend.csproj`.
2. **Khởi tạo Database Connection:** Cấu hình sử dụng các class `Database.cs`, `Collection.cs` để kết nối vào Database thực tế (MongoDB/LiteDB tương thích BSON).
3. **Cấu hình Dependency Injection (DI):** Đăng ký các service tiện ích (Json, MD5, Mqtt) vào `Program.cs`.

👉 **Prompt cho AI:** 
> "Hãy giúp tôi tái cấu trúc thư mục `Infrastructure` bằng cách đưa nó vào `DATN.Infrastructure.csproj` và liên kết nó vào `DATN.Backend.csproj`. Sau đó cấu hình Dependency Injection trong `Program.cs` cho các class Database và MQTT."

---

## Giai đoạn 2: Xây dựng Models & Data Access Layer (Backend - C#)
*Mục tiêu: Định nghĩa các thực thể dữ liệu (Entities) dựa trên `FallDetectionSystem_Plan.md`.*

**Các bước thực hiện:**
1. Tạo thư mục `Domain` hoặc `Models` trong `DATN.Backend`.
2. Định nghĩa các C# Models (Classes) cho: `User`, `Device`, `EmergencyContact`, `FallEvent`, `SensorData`, `AmbulanceRequest`.
3. Định nghĩa các Repository pattern hoặc module thao tác với `Infrastructure.Data.Collection`.

👉 **Prompt cho AI:** 
> "Dựa trên tài liệu `FallDetectionSystem_Plan.md`, hãy định nghĩa các C# Models cho User, Device, EmergencyContact, và FallEvent. Cấu hình các BSON Map dùng `BsonDataMap.cs` trong Infrastructure để chuẩn bị lưu xuống DB."

---

## Giai đoạn 3: Phát triển Core Services & APIs (Backend - C#)
*Mục tiêu: Xử lý logic nghiệp vụ và giao tiếp thiết bị.*

**Các bước thực hiện:**
1. **MQTT Background Service:** Viết một `IHostedService` (`BackgroundService`) trong C# sử dụng `Infrastructure.Mqtt` để lắng nghe dữ liệu từ thiết bị (Topic: `sensor/data`, `sensor/alert`).
2. **Fall Detection Service (Thuật toán lõi):** Viết module nhận Data Frame, filter nhiễu và phân tích.
3. **Restful APIs:** Xây dựng các endpoint cơ bản dùng ASP.NET Core (Controllers hoặc Minimal APIs / Giraffe):
   - Auth APIs (Login/Register).
   - CRUD Emergency Contacts.
   - Quản lý Device (Link, Query trạng thái).
   - Truy xuất Lịch sử té ngã.

👉 **Prompt cho AI:** 
> "Hãy viết một MQTT Background Service bằng C# (.NET 8) sử dụng module `Infrastructure.Mqtt.Client`. Lắng nghe topic 'sensor/#' và lưu trữ dữ liệu payload thô vào Database thông qua `Infrastructure.Data.Database`."

---

## Giai đoạn 4: Thiết lập Nền tảng Mobile App (.NET MAUI - C#)
*Mục tiêu: Dựng bộ khung kiến trúc cho App Mobile (`DATN.App`).*

**Các bước thực hiện:**
1. Cài đặt các thư viện cần thiết: `CommunityToolkit.Mvvm`, thư viện REST API Http client (Refit hoặc HttpClient cơ bản).
2. Xây dựng cấu trúc thư mục tiêu chuẩn: `Views`, `ViewModels`, `Models` (Share chung cấu trúc hoặc duplicate từ Backend), `Services` (gọi API).
3. Xử lý Base ViewModel, Routing System (Shell).
4. Viết các Services để kết nối với Backend API.

👉 **Prompt cho AI:** 
> "Trong project `DATN.App` (.NET 9 MAUI), hãy cài đặt CommunityToolkit.Mvvm và thiết lập cấu trúc thư mục Views, ViewModels, Services. Tạo BaseViewModel và cấu hình AppShell rỗng chuẩn bị cho các màn hình tính năng."

---

## Giai đoạn 5: Phát triển Tính năng UI/UX Mobile App
*Mục tiêu: Code giao diện và ghép nối với API.*

**Các tính năng cần làm theo thứ tự ưu tiên:**
1. **Auth UI:** Login / Register.
2. **Main Dashboard (User):** Hiển thị trạng thái kết nối thiết bị, Pin, nút bấm SOS giả lập.
3. **Quản lý danh bạ & Cấu hình:** UI CRUD Emergency Contacts; Trang cài đặt bật/tắt Auto Emergency Call (Toggle).
4. **Register Device Flow:** UI nhập mã thiết bị hoặc Scan QR (Activity 4.9).
5. **Fall History:** UI danh sách lịch sử, Map chỉ định vị trí.

👉 **Prompt cho AI:** 
> "Dựa trên Activity 4.6 (Manage Emergency Contacts), hãy code cho tôi giao diện (XAML) và ViewModels trên ứng dụng MAUI để thực hiện Thêm, Sửa, Xóa số điện thoại người liên hệ."

---

## Giai đoạn 6: Tính năng Nâng cao & Thông báo (Push Notifications / Hospital Flow)
*Mục tiêu: Hoàn thiện tính năng cảnh báo Real-time và quy trình cấp cứu.*

**Các bước thực hiện:**
1. **Push Notifications:** Tích hợp Firebase Cloud Messaging (FCM) hoặc OneSignal cho MAUI App để nhận cảnh báo té ngã ngay cả khi tắt App.
2. **SignalR/WebSockets:** Cấu hình WebSockets báo động Real-time về Web Dashboard của bệnh viện.
3. **Hospital Views:** Trang hiển thị Request xe cứu thương, quản lý trạng thái Dispatch (Activity 4.8 và 4.11).

👉 **Prompt cho AI:** 
> "Hãy tích hợp SignalR Client vào .NET MAUI App để lắng nghe sự kiện 'FallDetected' đẩy từ server về theo thời gian thực. Hiển thị một Cảnh báo (Alert/Popup) đỏ rực trên màn hình khi nhận được tín hiệu này."

---

## Hướng dẫn cho AI Code Assistant 🤖

Mỗi khi người dùng đưa ra một prompt, hãy:
1. Luôn ưu tiên dùng các tiện ích đã có sẵn trong dự án (như `Infrastructure`).
2. Code giao diện MAUI tuân thủ chặt chẽ pattern **MVVM**. Tách bạch UI (XAML) và Logic.
3. Code Backend C# đảm bảo clean architecture và DI đúng chuẩn ASP.NET Core. 
4. Mỗi khi báo lỗi, hướng dẫn người dùng gửi Log Output rõ ràng để dò lỗi và sửa chữa nhắm đúng mục tiêu.
