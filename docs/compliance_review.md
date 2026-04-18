# 📋 Đánh giá Tuân thủ Yêu cầu — FallDetectionSystem_Plan.md
> So sánh **trực tiếp** từng mục trong Plan với Implementation hiện tại
> Ngày: 18/04/2026

---

## 1. USE CASES (§1 — Actors & Use Cases)

### Actor: User (Bệnh nhân / Người giám hộ)

| Use Case | Plan yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|---|
| Register Device | Đăng ký thiết bị với tài khoản | SettingsPage — nhập Device ID thủ công | ⚠️ **Đúng nhưng thiếu** BLE/QR scan |
| Manage Emergency Contacts | CRUD danh bạ khẩn cấp | ContactsPage — Add + Edit + Delete + List | ✅ |
| View Fall History | Xem lịch sử té ngã | DashboardPage + FallDetailPage | ✅ |
| Configure Auto Emergency Call | Toggle + thời gian đếm ngược | SettingsPage — Switch + Slider 10-120s | ✅ |

### Actor: Device (Thiết bị IoT)

| Use Case | Plan yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|---|
| Send Sensor Data | Gửi Accel, Gyro, HeartRate qua MQTT | MqttBackgroundService nhận topic `sensor/data` và `sensor/alert` | ✅ |

### Actor: Hospital (Nhân viên Điều phối)

| Use Case | Plan yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|---|
| Receive Ambulance Request | Tiếp nhận yêu cầu mới (real-time) | HospitalDashboardPage — SignalR push, reload danh sách | ✅ |
| Dispatch Ambulance | Nút điều động xe | Button "Dieu Dong" → `UpdateRequestStatusAsync("Dispatched")` | ✅ |
| Update Dispatch Status | Các nút trạng thái | Dispatched → Arrived → Completed / Rejected | ✅ |
| View Dispatch History | Lịch sử điều phối | Toggle show/hide AllRequests trên cùng trang | ⚠️ **Không có màn hình riêng** |

---

## 2. BACKEND CORE SERVICES (§2.1)

### Service 1: MQTT Broker

| Yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|
| Nhận `Continuous sensor streaming` từ ESP32 | Subscribe `sensor/#` tại `broker.emqx.io:1883` | ✅ |
| Phân biệt `sensor/data` vs `sensor/alert` | `MqttBackgroundService.MqttClient_DataReceived()` | ✅ |

### Service 2: Data Ingestion Service

| Yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|
| `validateAndNormalize` | Kiểm tra DeviceId null → reject | ⚠️ **Chưa normalize** giá trị cảm biến |
| `storeRawSensorData` | Insert vào collection `SensorData` | ✅ |
| `forwardSensorFrame` | Gọi `FallDetectionService.ProcessSensorFrame()` | ✅ |
| Update device last connected | `UpdateDeviceLastConnected()` | ✅ |

### Service 3: Fall Detection Service (Core Logic)

| Yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|
| `analyzeMotionPattern` | Threshold-based: Accel spike, Gyro, Stillness | ✅ |
| `saveFallEvent` | `SaveFallEvent(result, userId)` | ✅ |
| `getEmergencyContactsAndSettings` | `CheckAutoEmergencySetting()` → đọc AutoEmergencyEnabled | ⚠️ **Không lấy danh bạ khẩn cấp** để notify |
| `createAlertRequest` | `PushFallAlertAsync()` qua SignalR | ⚠️ **Chỉ push SignalR** — không SMS/Call thực |
| `Determine location` | **Luôn null** — không lấy GPS từ đâu | ❌ **THIẾU** |
| `continueMonitoring` | Tiếp tục buffer frames sau No-fall | ✅ |

### Service 4: User Management Service

| Yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|
| `queryContactsAndSettings` | Chỉ query `AutoEmergencyEnabled`, **không query EmergencyContacts** | ❌ **Thiếu query contacts** |

> [!CAUTION]
> **Bug nghiêm trọng về nghiệp vụ:** Khi phát hiện té ngã, hệ thống **chỉ push SignalR** tới `contacts_{userId}` group — nhưng **Emergency Contacts là số điện thoại**, họ không phải user của app. Không có SMS hoặc FCM gửi cho họ. Danh sách `EmergencyContacts` không bao giờ được đọc trong luồng fall detection. Yêu cầu §4.5 chưa được thực thi thực sự.

### Service 5: Emergency Service (Auto Dispatch)

| Yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|
| `requestAmbulance` | Tạo `AmbulanceRequest` với Status="Pending" | ✅ |
| `saveAmbulanceRequest` | Insert vào collection `AmbulanceRequests` | ✅ |
| `sendAmbulanceRequest` tới Hospital | `NotifyHospitalAsync()` → SignalR group "hospital" | ✅ |
| `acceptRequest/dispatchAmbulance` (Hospital phản hồi) | Hospital app gọi `UpdateRequestStatusAsync()` | ✅ |
| `updateDispatchStatus` | Update document trong DB | ✅ |
| `notifyDispatchResult` cho User | `PushAmbulanceStatusAsync()` | ✅ |
| **Thời gian đếm ngược trước dispatch** | Lưu ở client Preferences, **backend dispatch ngay lập tức** | ❌ **Backend không delay** |

### Service 6: Notification Service

| Yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|
| `pushFallAlert` | SignalR → `user_{userId}` group | ✅ |
| `pushAmbulanceStatusUpdate` | SignalR → `user_{userId}` group | ✅ |
| `pushNotifyEmergencyContacts` | SignalR → `contacts_{userId}` group | ❌ **Contacts không join app** — group luôn rỗng |
| `saveNotificationLog` | Insert vào `NotificationLogs` | ✅ |
| Firebase FCM (§2.3) | **Không có** | ❌ **Chưa tích hợp** |

---

## 3. MAUI APP (§3.1 — User Module)

| Màn hình | Plan yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|---|
| **Login/Register** | ✅ | LoginPage + RegisterPage + UraniumUI | ✅ |
| **Dashboard** — Device status | Kết nối + Pin thiết bị | Số thiết bị active — **không có Pin** | ⚠️ |
| **Dashboard** — Sức khỏe tóm tắt | Nhịp tim, v.v. | ❤️ HeartRate từ SensorData API | ✅ |
| **Dashboard** — Cảnh báo đỏ | Cảnh báo khi có té ngã | Recent Alert Banner + SignalR real-time | ✅ |
| **Màn hình Thiết bị** | Quét BLE/QR để Register | Nhập ID thủ công trong Settings | ⚠️ **Không đủ** |
| **Emergency Contacts** | CRUD từ danh bạ điện thoại | Add/Edit/Delete manual — **không đọc danh bạ điện thoại** | ⚠️ |
| **Fall History** — List | Thời gian + vị trí (có bản đồ nhỏ) | List đầy đủ + FallDetailPage | ✅ |
| **Fall History** — Bản đồ nhỏ | Inline map trong list | **Không có inline map** — chỉ có nút "Mở Bản Đồ" | ❌ |
| **Settings** — Toggle Auto Emergency | ✅ | Switch + API call | ✅ |
| **Settings** — Countdown trước khi gọi | Thời gian đếm ngược | Slider 10-120s, lưu Preferences | ⚠️ **Chỉ UI** — chưa có countdown popup thực sự |
| **Logout** | Không đề cập trong Plan | Đã có | ✅ (bonus) |

---

## 4. MAUI APP (§3.2 — Hospital Module)

| Màn hình | Plan yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|---|
| **Dashboard/Cảnh báo** | Danh sách Ambulance Requests mới nhất (real-time) | HospitalDashboardPage — Pending list + SignalR | ✅ |
| **Chi tiết Yêu cầu** | Thông tin người bệnh + **GPS bản đồ** để điều xe | Inline trên Dashboard — **không có GPS map** | ❌ |
| **Cập nhật trạng thái** | Dispatch / Arrived / Completed | 4 buttons trong HospitalDashboardPage | ✅ |
| **Lịch sử (Dispatch History)** | Màn hình riêng thống kê các ca | Toggle show/hide trên Dashboard — **không trang riêng** | ⚠️ |
| **Role-based login** | 2 chế độ theo user type | AppShell dual TabBar + Login routing | ✅ |

---

## 5. ACTIVITY DIAGRAMS (§4) — Kiểm tra các Luồng Nghiệp vụ

| Activity | Plan | Hiện trạng | Đánh giá |
|---|---|---|---|
| **4.1 Detect Fall** | Receive frame → Analyze → Determine Location → Mark event → Send to Alert | ✅ có đủ bước — **thiếu Determine Location** | ⚠️ |
| **4.2 Create Fall Alert** | Read contacts → Read settings → Auto emergency → Send SMS/Push contacts | Read settings ✅ — **Không đọc contacts, không gửi SMS** | ❌ |
| **4.3 Configure Auto Emergency** | Toggle → Save API | Có | ✅ |
| **4.4 Process Sensor Data** | Validate → Normalize → Store → Build frame → Forward | Validate ✅, Store ✅, Forward ✅ — **Normalize chưa đầy đủ** | ⚠️ |
| **4.5 Notify Emergency Contact** | Prepare content → Send Push/SMS/Call → Retry → Save log | **Chỉ SignalR push tới group rỗng** — không SMS thực | ❌ |
| **4.6 Manage Emergency Contacts** | Add/Edit/Delete với validation | Đầy đủ | ✅ |
| **4.7 Send Sensor Data** | ESP32 → MQTT → Retry | Device phía firmware — ngoài scope backend | ✅ (backend ready) |
| **4.8 Request Ambulance** | Create → Send to Hospital → Accept → Update → Notify user | Đầy đủ | ✅ |
| **4.9 Register Device** | Check valid → Check duplicate → Link to user | Có validation + duplicate check | ✅ |
| **4.10 View Fall History** | Load list → Select → Show detail | DashboardPage + FallDetailPage | ✅ |
| **4.11 View Dispatch History** | Màn hình riêng → Select → Show detail | Toggle trên Dashboard — không có trang riêng | ⚠️ |

---

## 6. TECH STACK (§5)

| Yêu cầu | Hiện trạng | Đánh giá |
|---|---|---|
| ASP.NET Core Web API | ✅ | ✅ |
| SignalR (Real-time) | ✅ Hub + Client | ✅ |
| Firebase FCM (Push khi app đóng) | **Không có** | ❌ |
| .NET MAUI (Cross-platform) | ✅ | ✅ |
| MVVM — CommunityToolkit.Mvvm | ✅ | ✅ |
| Map integration | **Chỉ có LaunchAsync mở app ngoài** | ❌ |
| BsonData (NoSQL) | ✅ — thay cho PostgreSQL/InfluxDB | ✅ |
| MQTT (IoT) | ✅ | ✅ |

---

## 7. Tổng kết — Tỷ lệ Tuân thủ Thực tế

### ✅ Hoàn chỉnh (không thiếu gì đáng kể)
- Auth (Login/Register/Role)
- Device Registration (API, manual input)
- Emergency Contacts CRUD
- View Fall History + Detail
- Auto Emergency Toggle + Settings API
- MQTT pipeline (receive → ingest → detect → dispatch)
- Fall Detection Algorithm
- Auto Ambulance Dispatch workflow
- Hospital Dispatch actions (Dispatch/Arrived/Complete/Reject)
- SignalR Hub + real-time dashboard

### ⚠️ Triển khai nhưng không đủ (Partial)
- Countdown timer — UI có, backend không delay thực
- Device registration — không BLE/QR scan
- Emergency Contact — không import từ danh bạ điện thoại
- Dispatch History — không có màn hình riêng
- Hospital Request Detail — không có GPS map thực

### ❌ Chưa triển khai (Thiếu theo Plan)
1. **Notify Emergency Contact thực sự** (§4.5) — không SMS/Call tới số điện thoại người thân
2. **Determine Location** (§4.1) — GPS của FallEvent từ algorithm luôn null
3. **Inline Map** trong Fall History (§3.1)
4. **Hospital Request Detail với GPS Map** (§3.2)
5. **BLE/QR Device Registration** (§3.1)
6. **Firebase FCM** — không nhận alert khi app đóng (§2.3)
7. **Backend countdown delay** trước auto-dispatch

---

## 8. Mức độ hoàn thành thực tế

| Module | Hoàn thành |
|---|---|
| Backend Core Services | **~80%** |
| MAUI User Module | **~78%** |
| MAUI Hospital Module | **~75%** |
| **Tổng hệ thống** | **~78%** |

> [!IMPORTANT]
> Gap lớn nhất và quan trọng nhất về **nghiệp vụ**: Plan §4.5 yêu cầu gửi SMS/Push/Call tới **Emergency Contacts** (người thân) khi phát hiện té ngã. Hiện tại hệ thống chỉ push SignalR tới app — nghĩa là người thân **không bao giờ nhận được thông báo** trừ khi họ cũng mở app và join đúng group. Đây là gap quan trọng nhất cần xử lý.

> [!TIP]
> Giải pháp đề xuất cho gap lớn nhất: Tích hợp **Twilio** (SMS) hoặc **Firebase FCM** để gửi thông báo thực cho số điện thoại trong `EmergencyContacts`. Khi fall detected, backend đọc danh sách contacts của user và gửi SMS/notification.
