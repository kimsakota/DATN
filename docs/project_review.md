# 🔍 Đánh giá Dự án Fall Detection System
> So sánh giữa Kế hoạch (`FallDetectionSystem_Plan.md`) và Triển khai thực tế
> **Cập nhật lần cuối:** 18/04/2026 — Phiên cuối

---

## 📁 Tổng quan Cấu trúc Solution

| Project | Vai trò | Trạng thái |
|---|---|---|
| `DATN` | ASP.NET Core Backend (Web API + SignalR + MQTT) | ✅ Đang hoạt động |
| `DATN.App` | .NET MAUI Mobile App (Dual-mode: User + Hospital) | ✅ Đang hoạt động |
| `DATN.Infrastructure` | Thư viện dùng chung (BsonData, MQTT, JSON) | ✅ Đang hoạt động |

---

## ✅ NHỮNG GÌ ĐÃ LÀM ĐƯỢC

### 🔐 Auth & Role (§1 Actors)

| Tính năng | Chi tiết |
|---|---|
| Đăng nhập | Login API → hash password → trả về `userId` + `role` |
| Đăng ký | Register tạo tài khoản với `Role = "User"` mặc định |
| Role-based routing | Login route tới `DashboardPage` (User) hoặc `HospitalDashboardPage` (Hospital) |
| **UI chọn role** | LoginPage có 2 tab 👤 Người Dùng / 🏥 Bệnh Viện, validate role khớp với server |
| Đăng xuất | Xóa `userId`, `userRole`, `autoEmergencyEnabled`, `countdownSeconds` khỏi Preferences |

### 📱 MAUI App — Module User (§3.1)

| Màn hình | Tính năng đã có |
|---|---|
| **LoginPage** | Role selector tabs (Người Dùng / Bệnh Viện), form đăng nhập, link đăng ký |
| **RegisterPage** | Form đầy đủ, banner ghi rõ đây là tài khoản User |
| **DashboardPage** | ✅ Trạng thái thiết bị (số kết nối + số lần té ngã), ✅ Sức khỏe (nhịp tim ❤️), ✅ Banner cảnh báo đỏ khi có té ngã, ✅ Danh sách lịch sử té ngã có thể click |
| **FallDetailPage** | Chi tiết sự kiện (thời gian, thiết bị, nguồn), nút "Mở Bản Đồ", nút "Đánh Dấu Báo Động Giả" |
| **ContactsPage** | ✅ Add contact (Tên, SĐT, Quan hệ, Ưu tiên), ✅ Edit (điền lại form), ✅ Delete (confirm popup), EmptyView hướng dẫn |
| **SettingsPage** | Toggle Auto Emergency Call, Slider countdown 10-120s, Đăng ký thiết bị thủ công, Đăng xuất |
| **Real-time Alert** | SignalR nhận fall alert → hiện banner đỏ ngay lập tức |
| **GPS upload** | Khi nhận fall alert → lấy GPS điện thoại → upload lên backend `PUT /FallEvent/{id}/location` |

### 🏥 MAUI App — Module Hospital (§3.2)

| Màn hình | Tính năng đã có |
|---|---|
| **AppShell dual-mode** | `UserTabs` (Dashboard/Contacts/Settings) vs `HospitalTabs` (Dispatch/Settings) |
| **HospitalDashboardPage** | Stats bar (Chờ / Đang / Hoàn thành), Danh sách Pending requests real-time, nút Điều Động + Từ Chối |
| **Cập nhật trạng thái** | Dispatched → nút "Đã Đến Nơi"; Arrived → nút "Hoàn Thành" (hiện theo đúng status) |
| **Lịch sử** | Toggle hiện/ẩn danh sách tất cả requests với status badge |
| **Real-time** | SignalR push khi có yêu cầu mới → tự động reload |
| **Status model** | `AmbulanceRequest.IsDispatched`, `IsArrived`, `IsPending` cho XAML binding |

### ⚙️ Backend Services (§2.1)

| Service | Tính năng đã có |
|---|---|
| **MQTT Broker** | Subscribe `sensor/#`, xử lý `sensor/data` và `sensor/alert` |
| **Data Ingestion** | Validate DeviceId → Store SensorData → Update LastConnectedAt → Forward to FallDetect |
| **Fall Detection** | Threshold-based: Accel spike + Gyro + Stillness window, `ProcessSensorFrame()` |
| **Notify User** | `PushFallAlertAsync()` → SignalR group `user_{userId}` ngay lập tức |
| **Notify Emergency Contacts** | `NotifyEmergencyContactsAsync()` → đọc contacts từ DB → log từng người → SignalR group + NotificationLog per contact |
| **Backend Countdown** | Đọc `CountdownSeconds` từ DB → `Task.Delay(seconds)` → sau đó tạo AmbulanceRequest |
| **Auto Dispatch** | Tạo AmbulanceRequest → `NotifyHospitalAsync()` → SignalR group "hospital" |
| **Hospital Actions** | Dispatch / Arrived / Completed / Rejected qua `PUT /AmbulanceRequest/{id}/status` |
| **GPS Update** | `PUT /FallEvent/{id}/location` — MAUI upload lat/lng sau khi nhận alert |
| **NotificationLogs** | Lưu log cho từng loại thông báo, per-contact logging |
| **User Settings** | `PUT /User/{id}/settings` lưu cả `AutoEmergencyEnabled` + `CountdownSeconds` |
| **SignalR Hub** | Groups: `user_{userId}`, `hospital`, `contacts_{userId}` |
| **AmbulanceRequest API** | Pending, All, Status update |
| **FallEvent API** | History by userId, Detail, False alarm, Location update |
| **EmergencyContact API** | CRUD đầy đủ |
| **Device API** | Register, Link to user, LastConnected update |
| **SensorData API** | Store + query latest by deviceId |

### 🗄️ Database Models

| Model | Trạng thái |
|---|---|
| `User` | ✅ (Id, Username, PasswordHash, FullName, Phone, Address, **Role**, AutoEmergencyEnabled, **CountdownSeconds**) |
| `Device` | ✅ (DeviceId, UserId, Name, IsActive, LastConnectedAt) |
| `EmergencyContact` | ✅ (UserId, Name, PhoneNumber, Relationship, IsPrimary) |
| `FallEvent` | ✅ (UserId, DeviceId, DetectedAt, Latitude, Longitude, Status, FalseAlarm, Source) |
| `SensorData` | ✅ (DeviceId, Timestamp, AccelX/Y/Z, GyroX/Y/Z, HeartRate) |
| `AmbulanceRequest` | ✅ (FallEventId, UserId, RequestTime, Location, Status, DispatchNotes) |
| `NotificationLogs` | ✅ (TargetId, Type, Message, SentAt, IsRead) |

---

## ❌ NHỮNG GÌ CHƯA LÀM ĐƯỢC

### 🔴 Critical (ảnh hưởng nghiệp vụ chính)

| # | Vấn đề | Lý do chưa làm |
|---|---|---|
| 1 | **SMS/Call thực tới Emergency Contacts** | Cần Twilio/VNPT SMS API key — stub đã sẵn code, chỉ cần thêm credentials |
| 2 | **FCM Push Notification** (app ở background/đóng) | Cần Google Firebase project + `google-services.json` — SignalR chỉ hoạt động khi app mở |
| 3 | **GPS từ thiết bị IoT** (ESP32) | ESP32 cần module GPS riêng hoặc dùng Phone GPS (đã có) — FallEvent lat/lng từ algorithm là null cho đến khi Phone upload |

### 🟠 Important (ảnh hưởng UX đáng kể)

| # | Vấn đề | Ghi chú |
|---|---|---|
| 4 | **Inline Map trong Fall History** (§3.1) | Plan yêu cầu "bản đồ nhỏ" — hiện chỉ có nút "Mở Bản Đồ" mở app ngoài |
| 5 | **GPS Map trong Hospital Request Detail** (§3.2) | Hospital cần xem vị trí bệnh nhân để điều xe — chưa có màn hình Detail riêng |
| 6 | **BLE/QR Scan đăng ký thiết bị** (§3.1) | Hiện nhập Device ID thủ công — cần `Plugin.BLE` hoặc camera QR |

### 🔵 Minor / P3

| # | Vấn đề | Ghi chú |
|---|---|---|
| 7 | Dispatch History màn hình riêng | Hiện toggle show/hide trên Dashboard — chưa có trang riêng với thống kê |
| 8 | Pin thiết bị chưa hiển thị | Device model không có Battery field, ESP32 chưa gửi |
| 9 | Import Emergency Contact từ danh bạ điện thoại | Plan §3.1 — chưa tích hợp `Contacts` permission |
| 10 | Normalize/validate sensor data đầy đủ | Chỉ kiểm tra DeviceId null, chưa validate range giá trị cảm biến |
| 11 | ML-based Fall Detection | Hiện threshold-based — nâng cấp lên AI/ML là P4 |
| 12 | Edit User Profile | Không có màn hình sửa thông tin cá nhân |

---

## 📊 Tỷ lệ Hoàn thành Thực tế

| Module | Trước phiên này | Sau phiên này | Ghi chú |
|---|---|---|---|
| Backend Core Services | ~80% | **~90%** | Thêm countdown, GPS endpoint, contact notify |
| MAUI User Module | ~78% | **~88%** | GPS upload, role selector UI, ContactsPage CRUD đầy đủ |
| MAUI Hospital Module | 0% | **~85%** | HospitalDashboardPage hoàn chỉnh, status buttons fixed |
| Auth & Role | ~50% | **~100%** | Role field, login route, UI selector |
| **Tổng hệ thống** | **~63%** | **~88%** | |

> [!IMPORTANT]
> **Gap lớn nhất còn lại:** SMS thực tới người thân + FCM khi app đóng. Đây là 2 tính năng cốt lõi của §4.5 nhưng cần external credentials (Twilio, Firebase). Mọi code pipeline đã sẵn sàng — chỉ cần cắm API key vào.

---

## 📋 Checklist theo Plan §3.1 & §3.2

### §3.1 — User Module
- [x] Login / Register
- [x] Dashboard: device status
- [x] Dashboard: sức khỏe (nhịp tim)
- [x] Dashboard: cảnh báo đỏ real-time
- [ ] Device screen: BLE/QR scan ← **chưa**
- [x] Emergency Contacts: Add
- [x] Emergency Contacts: Edit
- [x] Emergency Contacts: Delete
- [ ] Emergency Contacts: Import từ danh bạ ← **chưa**
- [x] Fall History: list
- [x] Fall History: detail
- [ ] Fall History: inline map ← **chưa**
- [x] Settings: Toggle Auto Emergency
- [x] Settings: Countdown slider 10-120s (sync với backend)

### §3.2 — Hospital Module
- [x] Dashboard: danh sách requests real-time (SignalR)
- [ ] Request Detail: màn hình riêng với thông tin bệnh nhân đầy đủ ← **chưa**
- [ ] Request Detail: GPS map để điều xe ← **chưa**
- [x] Status buttons: Dispatch / Arrived / Complete / Reject
- [x] Status buttons: hiện đúng theo trạng thái hiện tại
- [x] Dispatch History: danh sách tất cả (toggle)
- [ ] Dispatch History: màn hình riêng với thống kê ← **chưa**

---

## 🚀 Việc cần làm tiếp theo (ưu tiên)

| Ưu tiên | Việc cần làm | Ước tính |
|---|---|---|
| 🔴 P1 | Tích hợp FCM Firebase (app background/closed alert) | Cần `google-services.json` |
| 🔴 P1 | Tích hợp Twilio SMS (SMS thực tới Emergency Contacts) | Cần Twilio API key |
| 🟠 P2 | Hospital Request Detail Page (bệnh nhân info + GPS map) | ~2-3h |
| 🟠 P2 | Inline Map trong FallDetailPage | Cần `Microsoft.Maui.Maps` hoặc `Mapsui` |
| 🔵 P3 | BLE/QR scan thiết bị (`Plugin.BLE`) | ~1 ngày |
| 🔵 P3 | Dispatch History màn hình riêng | ~1h |
| 🔵 P3 | Battery field ESP32 + hiển thị trong app | Cần firmware update |

---

## 📝 Changelog — Toàn bộ thay đổi trong phiên 18/04/2026

### Backend (`DATN`)
1. ✅ `User.cs` — thêm `Role` ("User"/"Hospital") + `CountdownSeconds` (default 30)
2. ✅ `AuthController.cs` — Login trả về `role`, Register lưu `Role`
3. ✅ `UserController.cs` — `PUT /settings` lưu cả `AutoEmergencyEnabled` + `CountdownSeconds`
4. ✅ `FallEventController.cs` — thêm `PUT /{id}/location` cho MAUI upload GPS
5. ✅ `NotificationService.cs` — thêm `NotifyEmergencyContactsAsync()` đọc contacts từ DB, log, push per-contact
6. ✅ `MqttBackgroundService.cs` — gọi contact notify, thêm `GetCountdownSeconds()`, `await Task.Delay()` trước dispatch

### MAUI App (`DATN.App`)
7. ✅ `AppShell.xaml` — Dual-mode: `UserTabs` + `HospitalTabs`
8. ✅ `LoginPage.xaml` — Role selector tabs (👤 Người Dùng / 🏥 Bệnh Viện)
9. ✅ `LoginViewModel.cs` — `SelectedRole`, `SelectUserRoleCommand`, `SelectHospitalRoleCommand`, validate role vs server
10. ✅ `RegisterPage.xaml` — Banner role info, GoBack button
11. ✅ `RegisterViewModel.cs` — thêm `GoBackCommand`
12. ✅ `HospitalDashboardPage.xaml` — Stats, Pending list (Dispatch/Reject), History toggle, status-based buttons
13. ✅ `HospitalDashboardViewModel.cs` — LoadData, Dispatch, Arrived, Complete, Reject commands + SignalR
14. ✅ `ContactsPage.xaml` — Add/Edit/Delete CRUD, EmptyView, dynamic form title, Cancel Edit
15. ✅ `ApiService.cs` — `UpdateAutoEmergencyAsync(+countdown)`, `UpdateFallLocationAsync()`, Hospital API methods
16. ✅ `SettingsViewModel.cs` — gửi CountdownSeconds lên backend, xóa `userRole` khi logout
17. ✅ `DashboardViewModel.cs` — `UploadLocationForFallAsync()` sau khi nhận SignalR fall alert
18. ✅ `Models/Device.cs` — `AmbulanceRequest.IsDispatched`, `IsArrived`, `IsPending` computed properties
19. ✅ `Converters/RoleTabConverters.cs` — `BoolToSelectedColorConverter`, `BoolToTextColorConverter`
20. ✅ `App.xaml` — đăng ký 2 converters mới
21. ✅ `AndroidManifest.xml` — thêm `ACCESS_FINE_LOCATION` + `ACCESS_COARSE_LOCATION`
22. ✅ `MauiProgram.cs` — đăng ký DI cho `HospitalDashboardPage` + `HospitalDashboardViewModel`
23. ✅ `DATN.App.csproj` — thêm MauiXaml entries cho `FallDetailPage` + `HospitalDashboardPage`
