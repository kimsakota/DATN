# Kế hoạch phát triển Hệ thống Phát hiện Tép ngã (Fall Detection System)

Dựa trên biểu đồ Use Case "Fall Detection System", dưới đây là phân tích và kế hoạch phát triển cho phần Backend (F# / .NET) và Ứng dụng di động (MAUI).

## 1. Phân tích Yêu cầu (Từ Use Case Diagram)

### Các Tác nhân (Actors)
1. **User (Người dùng/Người cao tuổi/Bệnh nhân):** Sử dụng thiết bị và quản lý cấu hình cá nhân.
2. **Device (Thiết bị cảm biến):** Gửi dữ liệu cảm biến định kỳ hoặc khi có sự cố.
3. **Emergency Contacts (Người liên hệ khẩn cấp):** Nhận thông báo khi có sự cố té ngã xảy ra.
4. **Hospital (Bệnh viện/Trung tâm y tế):** Xử lý yêu cầu cấp cứu, điều phối xe cứu thương.
   
### Các Chức năng chính (Use Cases)
- **Tương tác của User:**
  - `Register Device`: Đăng ký và liên kết thiết bị với tài khoản.
  - `Manage Emergency Contacts`: Quản lý danh sách người liên hệ khẩn cấp (Thêm, Sửa, Xóa).
  - `View Fall History`: Xem lịch sử các lần té ngã.
  - `Configure Auto Emergency Call`: Cấu hình tự động gọi/gửi yêu cầu cấp cứu.

- **Tương tác của Device:**
  - `Send Sensor Data`: Thiết bị đẩy dữ liệu cảm biến (Gia tốc, con quay hồi chuyển, nhịp tim...) lên hệ thống.

- **Hệ thống xử lý cốt lõi (Core System):**
  - `Process Sensor Data`: Xử lý dữ liệu thô từ cảm biến.
  - `Detect Fall`: Thuật toán phát hiện sự cố té ngã. Bao gồm việc lấy định vị (`Determine Location`) và Xử lý dữ liệu.
  - `Create Fall Alert`: Tạo cảnh báo té ngã. Bao gồm thông báo cho người thân (`Notify Emergency Contact`).
  - `Request Ambulance` (Mở rộng từ Create Fall Alert): Tự động hoặc thủ công gửi yêu cầu xe cấp cứu.

- **Tương tác của Hospital:**
  - `Receive Ambulance Request`: Tiếp nhận yêu cầu cấp cứu.
  - `Dispatch Ambulance`: Điều phối xe cứu thương.
  - `Update Dispatch Status`: Cập nhật trạng thái điều phối (Đang đến, Đã xử lý...).
  - `View Dispatch History`: Xem lịch sử điều phối.

---

## 2. Kế hoạch phát triển Backend (C# Server)

Hệ thống được thiết kế theo kiến trúc Microservices/Service-Oriented cho phép mở rộng dễ dàng. Các Component chính theo Sequence Diagram:

### 2.1 Các Core Services (Theo Sequence Diagram)

1. **MQTT Broker:** Nhận luồng dữ liệu liên tục (`Continuous sensor streaming`) từ thiết bị IoT (ESP32 + MPU).
2. **Data Ingestion Service:** 
   - Lắng nghe (`deliverSubscribedSensorData`) từ MQTT Broker.
   - `validateAndNormalize`: Kiểm tra và chuẩn hóa dữ liệu.
   - `storeRawSensorData`: Lưu trữ dữ liệu thô vào Time-series Database.
   - `forwardSensorFrame`: Chuyển tiếp các khung dữ liệu đã được làm sạch tới dịch vụ phân tích.
3. **Fall Detect Service (Core logic):**
   - Lắng nghe luồng dữ liệu khung `forwardSensorFrame`.
   - `analyzeMotionPattern`: Thuật toán lõi để phân tích hành vi và nhận diện té ngã.
   - Khi phát hiện té ngã (`[Fall detected]`):
     - `saveFallEvent`: Lưu sự kiện té ngã vào Database.
     - Gọi `User Management Service`: Lấy danh bạ khẩn cấp và cấu hình cài đặt `getEmergencyContactsAndSettings`.
     - Tạo yêu cầu Cảnh báo (`createAlertRequest`).
     - Tương tác với `Emergency Service`: Kiểm tra `checkAutoEmergencySetting` để xử lý luồng `Auto emergency`.
   - Nếu không té ngã (`[No fall]`): `continueMonitoring` (Tiếp tục giám sát).
4. **User Management Service:** 
   - `queryContactsAndSettings`: Truy vấn Database lấy danh bạ người thân, cùng cài đặt hệ thống người dùng (bật/tắt tự gọi cấp cứu).
5. **Emergency Service (Luồng Auto Emergency Dispatch):**
   - Nếu `[Auto emergency enabled]`:
     - Gửi `requestAmbulance`.
     - `saveAmbulanceRequest` vào DB.
     - Gửi yêu cầu qua Hospital: `sendAmbulanceRequest`.
     - Lắng nghe phản hồi `acceptRequest/dispatchAmbulance` từ Hospital.
     - `updateDispatchStatus`: Cập nhật trạng thái điều xe vào DB.
     - Cập nhật kết quả `notifyDispatchResult` cho Notification Service.
6. **Notification Service:**
   - Xử lý Push Notifications cho Mobile App: `pushFallAlert`, `pushAmbulanceStatusUpdate`, `pushNotifyEmergencyContacts`.
   - Lưu trữ lịch sử thông báo `saveNotificationLog` vào DB.

### 2.2 Cấu trúc Database (Dự kiến)
- **Users**: Lưu thông tin người dùng (Bệnh nhân, Nhân viên y tế).
- **Devices**: Quản lý thiết bị đã đăng ký, trạng thái active.
- **EmergencyContacts**: Danh sách người liên hệ của mỗi User.
- **SensorData**: Cần một Time-series DB (InfluxDB) để `storeRawSensorData` tốc độ cao.
- **FallEvents**: Lưu lịch sử té ngã (Thời gian, Tọa độ, Trạng thái).
- **NotificationLogs**: Lưu trữ lịch sử thông báo.
- **AmbulanceRequests**: Lưu yêu cầu cấp cứu và trạng thái từ hệ thống Hospital.

### 2.3 Các phân hệ API (Routing cho các giao diện ngoài)
1. **Device Integration:** MQTT (Topic subscription).
2. **Push Notifications:** WebSockets / SignalR hoặc Firebase FCM tích hợp vào `Notification Service`.
3. **Hospital Portal API:** Endpoint giao tiếp cho các Request từ `Emergency Service`.

---

## 3. Kế hoạch phát triển App Mobile (.NET MAUI)

Ứng dụng MAUI có thể được chia làm 2 chế độ hiển thị (hoặc 2 app riêng biệt) tùy vào người dùng đăng nhập (User bình thường hoặc Bệnh viện). Thông thường, Module cho Hospital sẽ hoạt động tốt hơn trên Web (Dashboard), nhưng nếu đưa lên App thì sẽ có dạng như sau:

### 3.1 Dành cho User (Bệnh nhân / Người giám hộ)
- **Màn hình Login/Register.**
- **Màn hình Home (Dashboard):** Hiển thị trạng thái của thiết bị (Đã kết nối, Pin), trạng thái sức khỏe tóm tắt. Cảnh báo đỏ nếu vừa có té ngã.
- **Màn hình Thiết bị (Device):** Quét Bluetooth/QR Code để Register Device.
- **Màn hình Danh bạ khẩn cấp (Emergency Contacts):** Giao diện CRUD (Thêm mới từ danh bạ điện thoại).
- **Màn hình Lịch sử (Fall History):** List hiển thị thời gian, vị trí (có bản đồ nhỏ) các lần té ngã.
- **Màn hình Cài đặt (Settings):** Toggle button cho thiết lập "Auto Emergency Call", thời gian đếm ngược trước khi gọi, v.v.

### 3.2 Dành cho Hospital (Nhân viên Điều phối)
- **Màn hình Dashboard / Cảnh báo:** Xem danh sách các `Ambulance Requests` mới nhất (Real-time).
- **Màn hình Chi tiết Yêu cầu:** Hiển thị thông tin người bệnh, định vị bản đồ (GPS) để điều xe.
- **Màn hình Cập nhật trạng thái:** Các nút trạng thái (Dispatch, Arrived, Completed) để `Update Dispatch Status`.
- **Màn hình Lịch sử (Dispatch History):** Thống kê và lịch sử các ca đã xử lý.

---

## 4. Phân tích Luồng Activity (Dựa trên Activity Diagrams)

Dưới đây là chi tiết mô tả 3 luồng Activity quan trọng nhất đối với nghiệp vụ của hệ thống:

### 4.1 Activity: Detect Fall
- **Bước 1:** `Receive processed sensor frame/window` - Backend tiếp nhận một khung dữ liệu cảm biến đã được xử lý sơ bộ.
- **Bước 2:** `Extract motion features` - Trích xuất các đặc trưng vận động (ví dụ: gia tốc biến đổi đột ngột).
- **Bước 3:** `Analyze motion pattern` - Phân tích mẫu chuyển động dựa trên thuật toán/AI.
- **Decision:** Có phải là sự kiện té ngã không (`Possible fall?`)
  - **No:** Chuyển sang `Continue monitoring` và kết thúc luồng cho khung dữ liệu đó.
  - **Yes:** Gọi `Determine location` (Xác định vị trí GPS hiện tại).
  - Tiếp theo: `Mark event as fall detected` (Đánh dấu sự kiện té ngã trong DB).
  - Cuối cùng: `Send event to Create Fall Alert` để chuyển tiếp thông tin báo động sang Module Alert.

### 4.2 Activity: Create Fall Alert
- **Bước 1:** `Receive fall event` - Bắt nguồn từ luồng Detect Fall gửi qua.
- **Bước 2:** `Create alert data` - Tổng hợp thông tin báo động bao gồm: user, time, location.
- **Bước 3:** `Store fall event / alert record` - Lưu bản ghi cảnh báo vào cơ sở dữ liệu để phục vụ việc xem lại (History).
- **Bước 4:** `Read user settings` - Đọc cấu hình hiện tại của user.
- **Bước 5:** `Read emergency contacts` - Lấy danh sách những người liên hệ đã lưu.
- **Decision:** Setup `Auto emergency enabled?`
  - **Yes:** 
    - `Send alert to emergency contacts` (Gửi SMS/Push notify đến người thân).
    - `Request ambulance` (Gửi yêu cầu tới Bệnh viện qua Hospital API/Dashboard). Kết thúc.
  - **No:** 
    - `Send alert to emergency contacts only` (Chỉ báo cho người thân). Kết thúc.

### 4.3 Activity: Configure Auto Emergency Call
*(Mô tả thao tác trên ứng dụng MAUI App của Người dùng)*
- **Bước 1:** `Open auto emergency settings` - Người dùng vào màn hình cài đặt.
- **Bước 2:** `View current setting` - Trạng thái hiện tại được hiển thị (đang bật hay tắt).
- **Bước 3:** `Toggle auto emergency option` - Chuyển trạng thái bằng Switch/Toggle control.
- **Decision:** Có bật (`Enable auto emergency?`) không?
  - **Yes:** Gọi `Set option = Enabled` -> `Save configuration` (Gửi API PUT lên Server) -> Hiển thị "Auto emergency enabled".
  - **No:** Gọi `Set option = Disabled` -> `Save configuration` -> Hiển thị "Auto emergency disabled".
- **Kết thúc luồng.**

### 4.4 Activity: Process Sensor Data
*(Xử lý dữ liệu Ingestion tại Backend)*
- **Bước 1:** `Receive sensor data` - Nhận bản tin từ thiết bị gửi qua MQTT/HTTP.
- **Bước 2:** `Validate payload format` - Kiểm tra tính hợp lệ của gói tin (đủ thông số gia tốc, gyroscope, vv).
- **Decision:** Gói tin có hợp lệ không (`Valid payload?`)
  - **No:** 
    - `Reject invalid data` (Loại bỏ data).
    - `Log error` (Ghi log lỗi) -> Kết thúc.
  - **Yes:** 
    - `Normalize / preprocess data` (Chuẩn hóa/Tiền xử lý).
    - `Store raw sensor data` (Lưu lịch sử thô vào Time-series DB).
    - `Build frame/window` (Gộp gói data thành một frame lớn đủ để phân tích).
    - `Forward processed data to fall detection` (Chuyển frame data sang service Detect Fall ở mục 4.1). Kết thúc.

### 4.5 Activity: Notify Emergency Contact
*(Chức năng bắn thông báo cho người thân)*
- **Bước 1:** `Receive alert request` - Nhận request cảnh báo khẩn cấp từ Service.
- **Bước 2:** `Prepare notification content` - Chuẩn bị nội dung thông báo kèm vị trí, thời gian thực.
- **Bước 3:** `Send push/SMS/call to emergency contacts` - Phát đi thông báo qua hệ thống (FCM Firebase/Twilio).
- **Decision:** Gửi thành công chưa (`Delivery success?`)
  - **Yes:** 
    - `Save notification log` (Ghi vào DB).
    - `Show delivery status` (Hiển thị trạng thái hoàn thành). Kết thúc.
  - **No:** 
    - `Retry delivery` (Thử lại).
    - **Decision:** Thử lại thành công không (`Retry success?`)
      - **Yes:** `Save notification log`. Kết thúc.
      - **No:** `Save failed delivery log` (Ghi nhận thất bại). Kết thúc.

### 4.6 Activity: Manage Emergency Contacts
*(Quy trình trên MAUI App khi thêm/sửa/xóa người liên hệ)*
- **Bước 1:** `Open emergency contacts screen` - Mở màn hình danh bạ.
- **Decision:** Chọn thao tác làm gì (`Choose action`)
  - **Nhánh Add:**
    - `Enter contact info` (Nhập Tên, SĐT, Quan hệ).
    - `Validate phone number / relationship` (Kiểm tra valid SĐT).
    - *Decision Valid?* Nếu Yes -> `Save contact` -> `Show success message`. Nếu No -> `Show input error`.
  - **Nhánh Edit:**
    - `Select existing contact` (Chọn một liên hệ).
    - `Update contact info` (Sửa thông tin).
    - `Validate updated info` (Kiểm tra valid).
    - *Decision Valid?* Nếu Yes -> `Save changes` -> `Show success message`. Nếu No -> `Show input error`.
  - **Nhánh Delete:**
    - `Select existing contact`.
    - `Confirm deletion` (Hiển thị popup hỏi "Bạn có chắc chắn muốn xoá?").
    - *Decision Confirmed?* Nếu Yes -> `Delete contact` -> `Show success message`. Nếu No -> `Cancel deletion`.
- *Luồng luôn kết thúc sau khi hoàn thành 1 nhánh hành động.*

### 4.7 Activity: Send Sensor Data
*(Quá trình thiết bị IoT gửi dữ liệu lên hệ thống)*
- **Bước 1:** `Collect accelerometer data` - Thiết bị (vd. ESP32) thu thập dữ liệu gia tốc kế.
- **Bước 2:** `Package sensor frame` - Đóng gói dữ liệu thành khung (frame) JSON hoặc binary.
- **Bước 3:** `Send data to MQTT broker` - Gửi dữ liệu qua kết nối MQTT.
- **Decision:** Gửi thành công chưa (`Transmission success?`)
  - **Yes:** `Wait for next sampling cycle` (Chờ chu kỳ lấy mẫu tiếp theo). Kết thúc.
  - **No:** 
    - `Retry sending` (Thử gửi lại).
    - **Decision:** Thử lại thành công không (`Retry success?`)
      - **Yes:** `Wait for next sampling cycle`. Kết thúc.
      - **No:** `Log transmission failure` (Ghi nhận lỗi truyền tải). Kết thúc.

### 4.8 Activity: Request Ambulance
*(Quá trình yêu cầu xe cứu thương)*
- **Bước 1:** `Receive ambulance request trigger` - Hệ thống nhận tín hiệu yêu cầu cấp cứu (từ người dùng hoặc tự động).
- **Bước 2:** `Create ambulance request` - Tạo bản ghi yêu cầu với thông tin vị trí và người bệnh.
- **Bước 3:** `Send request to hospital` - Chuyển tiếp yêu cầu tới hệ thống Bệnh viện.
- **Decision:** Bệnh viện có tiếp nhận không (`Hospital accepts request?`)
  - **Yes:** 
    - `Hospital dispatches ambulance` (Bệnh viện điều động xe).
    - `Update dispatch status` (Cập nhật trạng thái điều phối).
    - `Notify user/family via app` (Thông báo cho người dùng/người thân qua app). Kết thúc.
  - **No:** 
    - `Mark request as rejected / pending` (Đánh dấu từ chối hoặc chờ xử lý).
    - `Notify user/family of failure or delay` (Thông báo sự cố hoặc chậm trễ). Kết thúc.

### 4.9 Activity: Register Device
*(Quy trình đăng ký thiết bị mới)*
- **Bước 1:** `Open mobile app` - Mở ứng dụng di động.
- **Bước 2:** `Select "Register Device"` - Chọn chức năng đăng ký thiết bị.
- **Bước 3:** `Enter device information` - Nhập thông tin thiết bị (ID, mã pin, v.v.).
- **Bước 4:** `Submit registration request` - Gửi yêu cầu đăng ký lên server.
- **Decision:** ID thiết bị có hợp lệ không (`Device ID valid?`)
  - **No:** `Show validation error` (Hiển thị lỗi xác thực). Kết thúc.
  - **Yes:** 
    - **Decision:** Thiết bị đã được đăng ký chưa (`Device already registered?`)
      - **Yes:** `Show "Device already exists"` (Báo lỗi thiết bị đã tồn tại). Kết thúc.
      - **No:** 
        - `Create device record` (Tạo bản ghi thiết bị trong DB).
        - `Link device to user account` (Liên kết thiết bị với tài khoản người dùng).
        - `Show registration success` (Hiển thị thông báo thành công). Kết thúc.

### 4.10 Activity: View Fall History
*(Quy trình người dùng xem lịch sử té ngã)*
- **Bước 1:** `Open fall history screen` - Người dùng mở màn hình lịch sử té ngã trên ứng dụng.
- **Bước 2:** `System loads fall records` - Hệ thống truy vấn cơ sở dữ liệu để lấy danh sách các sự kiện té ngã.
- **Decision:** Có bản ghi nào không? (`Records found?`)
  - **No:** `Show "No fall history"` (Hiển thị thông báo không có lịch sử). Kết thúc.
  - **Yes:** 
    - `Display fall history list` (Hiển thị danh sách lịch sử té ngã).
    - `Select one event` (Người dùng chọn một sự kiện cụ thể).
    - `Display event details` (Hiển thị chi tiết sự kiện: thời gian, địa điểm, trạng thái xử lý). Kết thúc.

### 4.11 Activity: View Dispatch History
*(Quy trình bệnh viện xem lịch sử điều động xe cứu thương)*
- **Bước 1:** `Open dispatch history screen` - Nhân viên bệnh viện mở màn hình lịch sử điều phối trên dashboard.
- **Bước 2:** `System loads dispatch records` - Hệ thống truy vấn cơ sở dữ liệu lấy danh sách các ca điều phối.
- **Decision:** Có bản ghi nào không? (`Records found?`)
  - **No:** `Show "No dispatch history"` (Hiển thị thông báo không có lịch sử điều phối). Kết thúc.
  - **Yes:** 
    - `Display dispatch history list` (Hiển thị danh sách lịch sử điều phối).
    - `Select one request` (Nhân viên chọn một yêu cầu cụ thể).
    - `Display dispatch details/status` (Hiển thị chi tiết yêu cầu, trạng thái xe, thông tin bệnh nhân). Kết thúc.

---

## 5. Công nghệ đề xuất (Tech Stack)
- **Backend:** 
  - Ngôn ngữ: C# (.NET 8/9), ASP.NET Core Web API.
  - Database: PostgreSQL (cho dữ liệu tĩnh), InfluxDB/Redis (nếu cần lưu dữ liệu sensor rate cao).
  - Real-time: SignalR để gửi cảnh báo tức thì cho MAUI App hoặc Web.
- **Mobile (Cross-platform):**
  - Framework: .NET MAUI (iOS, Android, Windows).
  - MVVM pattern (CommunityToolkit.Mvvm).
  - Map integration (chỉ đường cho hospital, xem vị trí user ngã).
- **Phần cứng (Hardware - Device):** Mạch ESP32/Arduino hoặc thiết bị WearOS đeo tay, gửi data qua HTTP/MQTT cho Backend.

## 5. Các bước triển khai tiếp theo
1. Thiết lập Database schema và kết nối tại C# Server.
2. Xây dựng các API cơ bản (Auth, Tương tác thiết bị).
3. Khởi tạo Project MAUI, dựng UI mockup cho các luồng nghiệp vụ trên.
4. Tích hợp Backend và App (Gọi REST API).
5. Xây dựng Simulator cho Device (script tạo dữ liệu giả lập gửi lên API) để test thuật toán `Detect Fall`.
6. Hoàn thiện tính năng Notification và Real-time dispatch cho hệ thống bệnh viện.
