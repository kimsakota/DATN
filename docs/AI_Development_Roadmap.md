# L? tr?nh Phát tri?n H? th?ng Fall Detection dành cho AI (AI Development Roadmap)

Tài li?u này ðóng vai tr? là "Kim ch? nam" (Prompt Roadmap) ð? hý?ng d?n AI (Github Copilot/ChatGPT) t?ng bý?c l?p tr?nh và hoàn thi?n d? án **Fall Detection System** theo ki?n trúc Microservices & Cross-platform. 

D?a trên tài li?u `FallDetectionSystem_Plan.md`, l? tr?nh ðý?c chia thành các Giai ðo?n c? th? cho Backend (.NET 8) và ?ng d?ng .NET MAUI.

---

## Giai ðo?n 1: Chu?n b? C?u trúc & Tích h?p H? t?ng (Infrastructure Integration)
**M?c tiêu:** Tích h?p thành công module `DATN.Infrastructure` vào ASP.NET Core API Backend (`DATN`).

**Các bý?c c?n x? l?:**
1. Thêm Reference c?a project `DATN.Infrastructure.csproj` (ð? có thý vi?n BSON, JSON, MQTT) vào API backend.
2. C?u h?nh Injection/Kh?i t?o `Database` c?a Infrastructure vào Backend. H? cõ s? d? li?u BSON d?a trên FileStorage hi?n t?i (`BsonData`) ph?i ðý?c liên k?t t?i `Program.cs`.

---

## Giai ðo?n 2: Xây d?ng Models & Data Access Layer (Backend - C#)
**M?c tiêu:** Ð?nh ngh?a các entities theo chu?n C# record/class cho MongoDB BSON lýu tr? trên Backend.

**Các bý?c c?n x? l?:**
1. Ð?nh ngh?a Data Models trong d? án `DATN`:
   - `User.cs`: ID, Username, Email, Phone, Danh sách `EmergencyContacts`, tu? ch?n `AutoEmergencyEnabled`.
   - `Device.cs`: Thi?t b? c?m bi?n bao g?m MAC/Device ID, tên thi?t b?, tr?ng thái k?t n?i. Liên k?t m?t/nhi?u v?i `User`.
   - `EmergencyContact.cs`: Tên, S? ði?n tho?i, Quan h?, `IsPrimary` marker.
   - `FallEvent.cs`: Lýu tr? d? ki?n c?a quá tr?nh nhào l?n/té ng?: Th?i gian (`DetectedAt`), T?a ð? (Lat/Long), DeviceId, Tr?ng thái (Ðang ch? x? l?, v.v).
2. Tích h?p thý vi?n BSON attributes.

---

## Giai ðo?n 3: Phát tri?n Core Services & APIs (Backend - C#)
**M?c tiêu:** X? l? MQTT lu?ng d? li?u liên t?c (Continuous sensor streaming) & API Web chu?n.

**Các bý?c c?n x? l?:**
1. **MQTT Background Service:**
   - S? d?ng `Client.cs` (t? `Infrastructure.Mqtt`).
   - L?ng nghe Topic `sensor/#` ðính kèm Background Worker `IHostedService` trong ASP.NET.
   - D? li?u té ng? ð?y t?i Topic `sensor/alert`, gi?i nén và n?p vào collection "FallEvents" c?a `Database` (`BsonData`).
2. **RESTful Web APIs:**
   - **Auth API:** Ðãng nh?p/Ðãng k? user t?o m? Hash `SHA256`.
   - **Emergency Contacts API:** Thêm/S?a/Xóa (`Manage Emergency Contacts`).
   - **Device API:** Ðãng k? Device (Link thi?t b? vào tài kho?n User).
   - **Fall History API:** Load d? li?u l?ch s? t? b?ng `FallEvent`.

---

## Giai ðo?n 4: Thi?t l?p N?n t?ng Mobile App (.NET MAUI - C#)
**M?c tiêu:** D?ng b? khung UX/UI cho Client trên n?n t?ng Mobile/Tablet. Tuân th? mô h?nh MVVM (`CommunityToolkit.Mvvm`).

**Các bý?c c?n x? l?:**
1. C?u h?nh MAUI Shell, Settings cho iOS/Android trong `DATN.App`.
2. T?o c?u trúc thý m?c tiêu chu?n:
   - `Models/` (S? d?ng l?i Data Transfer Objects t? lúc xây d?ng API).
   - `Services/` (`IApiService`, ch?a logic g?i HTTP Client truy c?p t?i REST API c?a Backend).
   - `ViewModels/` ch?a file base view model qu?n l? `IsBusy`.
   - `Views/` t?o page XAML.

---

## Giai ðo?n 5: Phát tri?n Tính nãng UI/UX Mobile App
**M?c tiêu:** Hoàn thi?n lu?ng Activity.
1. **Auth UI:** Login / Register liên k?t `AuthController`.
2. **Dashboard:** Hi?n th? th?i gian th?c Device Status b?ng cách fetch Data (ho?c poll).
3. **Qu?n l? danh b? & Settings (Activity 4.3 & 4.6):**
   - CRUD thao tác lên Backend, c?p nh?t cài ð?t Emergency.
4. **Register Device Flow (Activity 4.9):**
   - Nh?p m? PIN/QR, lýu tr? ID thi?t b?.
5. **Fall History:**
   - Xem l?ch s? các l?n té ng? ðý?c fetch t? Backend. Map trích xu?t kinh ð?/v? ð? (n?u có map component).

---

## Giai ðo?n 6: Tính nãng Nâng cao & Dispatch C?p C?u (Realtime / Notification)
**M?c tiêu:** Gi?i quy?t Real-time & Hospital Dispatching (`Request Ambulance`).
1. **SignalR / Websocket Server:** Xây d?ng Hub trên Backend ð? stream thông báo té ng? l?p t?c cho gia ð?nh, thay v? ch? MQTT n?i b?.
2. SignalR Client: Ðính kèm cho ?ng d?ng MAUI ð? báo ð?ng kh?n c?p ngay trên màn h?nh.
3. Push Notification: C?u h?nh FCM (Tu? ch?n) cho ?ng d?ng di ð?ng n?u MAUI app ðóng.

---

### Hý?ng d?n cách AI ph?i h?p ??
1. Liên t?c ki?m tra các file trong thý m?c c?a d? án và ð?m b?o ðúng các Package Reference khi có Dependency m?i.
2. Module CSDL s? d?ng `BsonData` t? `DATN.Infrastructure`. H?y t?m hi?u file `Collection.cs` và `Database.cs` n?u c?n thao tác CRUD.
3. Ch?c ch?n s? d?ng `Insert`, `Update`, `Delete` thay v? `Add` hay `Remove` nguyên b?n trên C# Collections khi thao tác v?i `BsonData`.
