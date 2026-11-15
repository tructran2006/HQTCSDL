# CineSeat - Hệ Thống Đặt Vé Xem Phim Trực Tuyến

> **Đề tài 06 - Môn Hệ Quản Trị Cơ Sở Dữ Liệu - HUFLIT**  
> **Công nghệ:** C# ASP.NET Core MVC (.NET 8) & Oracle Database 23ai Free (Docker)  
> **Giao diện:** Modern Cinema UI/UX (Bootstrap 5.3 + Custom CSS + Google Fonts + Chart.js)  
> **Driver kết nối:** `Oracle.ManagedDataAccess.Core` (Official ODP.NET Core)

---

## 1. Giới thiệu Dự án

**CineSeat** là hệ thống web đặt vé xem phim trực tuyến hiện đại, giải quyết triệt để bài toán giữ chỗ thời gian thực, tính giá động theo khung giờ/cuối tuần/khuyến mãi, và kiểm soát tranh chấp đồng thời (concurrency control / anti-double selling) dựa trên kiến trúc phân tầng kết hợp sức mạnh xử lý giao dịch mức CSDL của **Oracle Database 23ai Free**.

### Điểm nhấn Kiến trúc & Dữ liệu thật từ Oracle 23ai:
1. **100% Dữ liệu Thật (Zero Mock Data):** Mọi thông tin hiển thị trên cả Cổng Khách Hàng (Customer Portal) và Cổng Quản Trị (Admin Portal) đều được truy vấn trực tiếp từ schema `CINESEAT` (`MOVIES`, `CINEMAS`, `ROOMS`, `SEATS`, `SHOWTIMES`, `SHOWTIME_SEATS`, `PROMOTIONS`, `BOOKINGS`, `BOOKING_SEATS`, `PAYMENTS`, `BOOKING_AUDIT`, `V_SHOWTIME_CATALOG`, `V_BOOKING_REPORT`).
2. **Hình ảnh Poster Sắc Nét:** Đầy đủ 13 file poster chuẩn tỷ lệ $2:3$ trong `wwwroot/images/` khớp với cột `poster_file` trong CSDL, không bao giờ bị lỗi vỡ ảnh hay 404.
3. **Tuân thủ 100% Logic PL/SQL Cốt lõi:** Nghiệp vụ giữ ghế (`PR_HOLD`), tính giá (`FN_TOTAL_PRICE`), thanh toán (`PR_CONFIRM`), hủy đơn (`PR_CANCEL`) và quét thu hồi ghế quá hạn 5 phút (`PR_EXPIRE_HOLDS`) được thực thi bên trong Package PL/SQL `PKG_BOOKING`.
4. **Không Thao tác DML Trực tiếp từ Web:** Ứng dụng ASP.NET Core MVC **tuyệt đối không** viết các câu lệnh `INSERT`/`UPDATE` trực tiếp vào bảng `BOOKINGS` hoặc `SHOWTIME_SEATS`.
5. **Quản lý Giao dịch Tường minh (Explicit Transactions):** Quản lý `OracleTransaction` chặt chẽ (`Commit()` khi thành công, `Rollback()` khi có ngoại lệ).
6. **Bắt Mã Lỗi Oracle Chuyên Sâu (ORA-20001 -> ORA-20014):** Bắt chính xác các mã lỗi nghiệp vụ của Oracle và chuyển đổi thành thông báo thân thiện cho người dùng trên giao diện web.
7. **Kiểm soát Concurrency Tuyệt đối:** Khóa dòng `SELECT ... FOR UPDATE WAIT 5` trên bảng `SHOWTIMES` và kiểm tra trạng thái nguyên tử `UPDATE ... WHERE status = 'AVAILABLE'`, đảm bảo chống hoàn toàn tình trạng bán trùng ghế.

---

## 2. Cấu trúc Cơ sở Dữ liệu (Chuẩn 3NF)

Hệ thống bao gồm **12 bảng**, **2 views**, **1 package**, **1 procedure**, **2 triggers**:

| Tên Bảng / Object | Mô Tả Chức Năng |
| :--- | :--- |
| `APP_USERS` | Danh sách tài khoản người dùng demo (13 người dùng) |
| `CINEMAS` | Cụm rạp chiếu phim (CineSeat Nguyễn Huệ, CineSeat Thủ Đức) |
| `ROOMS` | Phòng chiếu thuộc rạp (Phòng A1, Phòng A2, Phòng B1) |
| `SEATS` | 120 ghế vật lý (5 hàng A-E, 8 ghế/hàng, D-E là ghế VIP) |
| `MOVIES` | Danh mục 13 bộ phim bom tấn kèm thời lượng, độ tuổi (`P`, `K`, `T13`, `T16`, `T18`), poster |
| `SHOWTIMES` | Suất chiếu phim kèm giá cơ bản (`base_price`), giờ bắt đầu, kết thúc |
| `SHOWTIME_SEATS` | Trạng thái ghế theo từng suất chiếu (`AVAILABLE`, `HELD`, `SOLD`) |
| `PROMOTIONS` | Danh mục mã khuyến mãi (`SALE10`, `STUDENT`, `WEEKEND20`, `VIPMEMBER`, `CINEFAN5`) |
| `BOOKINGS` | Đơn đặt vé với trạng thái (`HELD`, `PAID`, `CANCELLED`, `EXPIRED`) |
| `BOOKING_SEATS` | Chi tiết phân bổ ghế và đơn giá trong đơn đặt vé |
| `PAYMENTS` | Giao dịch thanh toán mô phỏng (`DEMO_QR`, `DEMO_CARD`, `DEMO_CASH`) |
| `BOOKING_AUDIT` | Bảng nhật ký kiểm toán trạng thái đơn hàng (Trigger tự động ghi) |
| `V_SHOWTIME_CATALOG` | View danh mục suất chiếu kèm số ghế trống thời gian thực |
| `V_BOOKING_REPORT` | View tổng hợp chi tiết đơn hàng, khách hàng, số ghế và tổng tiền |
| `PKG_BOOKING` | Package PL/SQL chứa toàn bộ hàm/thủ tục tính giá, giữ ghế, thanh toán, hủy, thu hồi |
| `PR_EXPIRE_HOLDS` | Thủ tục độc lập giải phóng các ghế và đơn giữ chỗ quá hạn 5 phút |
| `TRG_BOOKING_AUDIT` | Trigger ghi nhật ký tự động vào `BOOKING_AUDIT` mỗi khi trạng thái đơn đổi |
| `TRG_SEAT_STATE_GUARD` | Trigger bảo vệ toàn vẹn trạng thái ghế (chặn bán nhảy cóc, chặn đổi chủ) |

---

## 3. Cấu trúc Thư mục Dự án

```
CineSeat/
├── .env.example                     # Mẫu biến môi trường Docker & Oracle
├── .gitignore                       # Cấu hình bỏ qua file rác .NET / Docker
├── CineSeat.slnx                    # Solution file .NET 8
├── docker-compose.yml               # Cấu hình container Oracle 23ai Free
├── README.md                        # Tài liệu hướng dẫn dự án chi tiết
├── run-tests.ps1                    # Script chạy toàn bộ 6 test cases xUnit
├── test-endpoints.ps1               # Script kiểm thử tự động 13 HTTP/API endpoints
├── generate-posters.ps1             # Script tạo 13 poster phim sắc nét tỷ lệ 2:3
│
├── database/                        # Cơ sở dữ liệu Oracle PL/SQL
│   ├── 00_init_user.sql             # Tạo user CINESEAT và cấp quyền trong FREEPDB1
│   ├── 00_master_init.sql           # Master script nạp toàn bộ DDL, DML, Logic
│   ├── 01_schema.sql                # 12 bảng chuẩn 3NF, khóa ngoại, 2 views
│   ├── 02_logic.sql                 # PKG_BOOKING, PR_EXPIRE_HOLDS, 2 Triggers
│   ├── 03_seed.sql                  # Dữ liệu mẫu (13 phim, 13 users, 3 rạp, 20 suất chiếu)
│   ├── 04_verify.sql                # Kiểm tra trạng thái VALID của toàn bộ objects
│   ├── init-oracle.sh               # Shell script tự động chạy khi khởi tạo container
│   └── setup-db.ps1                 # PowerShell script nạp CSDL tự động
│
├── src/
│   └── CineSeat.Web/                # Ứng dụng C# ASP.NET Core MVC (.NET 8)
│       ├── Controllers/
│       │   ├── AdminController.cs   # Dashboard, quản lý phim, suất chiếu, đơn đặt vé
│       │   ├── BookingsController.cs# Sơ đồ ghế, tính giá, giữ ghế, checkout, lịch sử
│       │   ├── MoviesController.cs  # Trang chủ, chi tiết phim, lịch chiếu rạp, switch user
│       │   └── Api/
│       │       └── BookingApiController.cs # RESTful API cho kiểm thử & automation
│       ├── Data/
│       │   └── IDbConnectionFactory.cs # Quản lý kết nối OracleConnection
│       ├── Exceptions/
│       │   └── CineSeatException.cs # Ánh xạ ORA-20001 -> ORA-20014 sang tiếng Việt
│       ├── Models/
│       │   ├── Entities.cs          # Models ánh xạ bảng và views CSDL
│       │   └── ViewModels.cs        # ViewModels hiển thị và nhận form
│       ├── Services/
│       │   ├── ICineSeatService.cs  # Interface nghiệp vụ
│       │   ├── CineSeatService.cs   # Service gọi PKG_BOOKING với Transaction tường minh
│       │   └── UserContext.cs       # Quản lý tài khoản demo qua Session & Cookie
│       ├── Views/                   # Giao diện Razor UI hiện đại, responsive
│       │   ├── Shared/
│       │   │   ├── _Layout.cshtml   # Layout Cổng Khách Hàng (Customer Portal)
│       │   │   └── _AdminLayout.cshtml # Layout Cổng Quản Trị (Admin Portal với Sidebar)
│       │   ├── Movies/
│       │   │   ├── Index.cshtml     # Hero Banner, tìm kiếm, lọc, lưới thẻ phim 2:3
│       │   │   ├── Details.cshtml   # Chi tiết phim, lịch chiếu nhóm theo ngày & rạp
│       │   │   └── CinemaShowtimes.cshtml # Lịch chiếu theo từng cụm rạp
│       │   ├── Bookings/
│       │   │   ├── SelectSeats.cshtml # Màn hình cong 3D, sơ đồ 5x8 ghế, sticky card
│       │   │   ├── Checkout.cshtml  # Đếm ngược 5 phút, E-ticket preview, 3 phương thức
│       │   │   ├── History.cshtml   # Lịch sử đơn đặt vé theo người dùng đang chọn
│       │   │   └── Detail.cshtml    # Vé điện tử E-Ticket voucher kèm QR code
│       │   └── Admin/
│       │       ├── Dashboard.cshtml # 4 KPI, biểu đồ Chart.js, nhật ký BOOKING_AUDIT
│       │       ├── Movies.cshtml    # Quản lý danh mục phim, thêm phim, bật/tắt chiếu
│       │       ├── Showtimes.cshtml # Tạo suất chiếu mới (tự động sinh 40 ghế AVAILABLE)
│       │       └── Bookings.cshtml  # Xem tất cả đơn vé, lọc trạng thái, quét quá hạn
│       └── wwwroot/
│           ├── css/
│           │   └── cineseat.css     # CSS Dark-mode, Cinema Red, VIP Gold, 3D Screen
│           └── images/              # Thư mục chứa 13 ảnh poster phim tỷ lệ 2:3
│
└── tests/
    └── CineSeat.Tests/              # Bộ kiểm thử tự động xUnit (.NET 8)
        └── CineSeatBusinessTests.cs # Kiểm thử toàn bộ 6 kịch bản nghiệp vụ (TC01 - TC06)
```

---

## 4. Hướng dẫn Khởi chạy Hệ thống

### Yêu cầu Tiên quyết:
- **.NET SDK 8.0** trở lên (`dotnet --version`)
- **Docker Desktop** (hỗ trợ Linux containers trên Windows)
- **PowerShell 7+** hoặc Windows PowerShell

---

### Bước 1: Khởi động Oracle Database 23ai trên Docker

```bash
docker compose up -d
```
Kiểm tra trạng thái container cho đến khi đạt `healthy`:
```bash
docker ps --filter "name=oracle-23ai"
```

---

### Bước 2: Khởi tạo Schema & Nạp Dữ liệu

```powershell
powershell -ExecutionPolicy Bypass -File .\database\setup-db.ps1
```
Script sẽ tự động:
1. Tạo user `CINESEAT` / mật khẩu `CineSeat123` trong Pluggable Database `FREEPDB1`.
2. Tạo 12 bảng 3NF, Sequences, Indexes và 2 Views.
3. Biên dịch Package `PKG_BOOKING`, Procedure `PR_EXPIRE_HOLDS`, 2 Triggers.
4. Nạp dữ liệu mẫu đầy đủ (13 phim, 13 người dùng, 3 phòng, 120 ghế chuẩn, 20 suất chiếu, 800 trạng thái ghế, 5 mã khuyến mãi).
5. Xác minh toàn bộ đối tượng trong `USER_OBJECTS` đạt trạng thái **`VALID`** (0 invalid objects).

---

### Bước 3: Chạy Ứng dụng Web ASP.NET Core MVC

```bash
dotnet run --project src/CineSeat.Web --urls "http://localhost:5000"
```
Mở trình duyệt và truy cập:
- **Cổng Khách Hàng (Customer Portal):** [http://localhost:5000](http://localhost:5000)
- **Lịch chiếu theo rạp:** [http://localhost:5000/Movies/CinemaShowtimes](http://localhost:5000/Movies/CinemaShowtimes)
- **Vé của tôi:** [http://localhost:5000/Bookings/History](http://localhost:5000/Bookings/History)
- **Cổng Quản Trị (Admin Portal):** [http://localhost:5000/Admin/Dashboard](http://localhost:5000/Admin/Dashboard)
- **Quản lý Phim:** [http://localhost:5000/Admin/Movies](http://localhost:5000/Admin/Movies)
- **Quản lý Suất chiếu:** [http://localhost:5000/Admin/Showtimes](http://localhost:5000/Admin/Showtimes)
- **Quản lý Đơn vé:** [http://localhost:5000/Admin/Bookings](http://localhost:5000/Admin/Bookings)

---

## 5. Kết quả Kiểm thử Nghiệp vụ Tự động (TC01 -> TC06)

Thực thi bộ kiểm thử tự động xUnit:
```powershell
powershell -ExecutionPolicy Bypass -File .\run-tests.ps1
```

```text
Test run for D:\Study\HQTCSDL\Demo\tests\CineSeat.Tests\bin\Debug\net8.0\CineSeat.Tests.dll (.NETCoreApp,Version=v8.0)
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6

  Passed TC01_CalculateTicketPrice_BusinessRules_Succeeds
  Passed TC02_InvalidPromoCode_CatchesORA20002_ReturnsFriendlyError
  Passed TC03_HoldSeats_ThenCancel_RevertsSeatToAvailable
  Passed TC04_ConcurrentHoldSameSeat_OneSucceeds_OneFailsWithORA20005
  Passed TC05_HoldSeats_ThenConfirmPayment_PaidSoldAndAuditLogged
  Passed TC06_ExpiredHolds_AutoReleasedBy_PRExpireHolds
```

Kiểm tra toàn bộ 13 endpoints:
```powershell
powershell -ExecutionPolicy Bypass -File .\test-endpoints.ps1
```

---

## 6. Phân hệ Giao diện & Chức năng Nổi bật

### A. Cổng Khách Hàng (Customer Portal)
1. **Thanh điều hướng (Navbar):** Logo CineSeat nổi bật, menu điều hướng rõ ràng, nút chuyển sang Admin Portal và **Dropdown chuyển đổi 13 tài khoản khách hàng demo** (lưu vào Session & Cookie) hiển thị tên và email để kiểm thử tức thì.
2. **Trang Chủ (`Movies/Index`):**
   - **Hero Banner:** Giới thiệu phim nổi bật từ CSDL với poster, thể loại, thời lượng, nhãn độ tuổi và nút "Đặt vé ngay".
   - **Bộ lọc & Tìm kiếm:** Lọc động theo Thể loại, Nhãn độ tuổi (`P`, `K`, `T13`, `T16`, `T18`) hoặc tìm theo tên phim.
   - **Lưới Thẻ Phim:** Poster tỷ lệ chuẩn 2:3, hover effect mượt mà, số suất chiếu sắp tới.
3. **Chi tiết phim & Lịch chiếu (`Movies/Details/{id}`):**
   - Thông tin phim đầy đủ từ `MOVIES`.
   - Suất chiếu từ `V_SHOWTIME_CATALOG` nhóm theo **Ngày chiếu** và **Cụm rạp** (`CineSeat Nguyễn Huệ`, `CineSeat Thủ Đức`).
   - Thẻ suất chiếu hiển thị giờ, phòng, đơn giá, thanh tiến độ ghế trống (`available_seats` / 40 ghế).
4. **Sơ đồ ghế & Giữ chỗ (`Bookings/SelectSeats/{showtimeId}`):**
   - Tự động gọi `PR_EXPIRE_HOLDS` trước khi load sơ đồ ghế.
   - **Màn hình cong 3D (Cinema Screen)** ở phía trên.
   - **Sơ đồ 5 hàng A-E x 8 cột 1-8:** Phân biệt rõ Standard (xanh rêu), VIP Hàng D-E (viền vàng gold), Đang chọn (đỏ neon), Đang giữ HELD (vàng cam), Đã bán SOLD (xám có dấu X).
   - **Sticky Summary Card:** Danh sách ghế đang chọn, ô nhập mã khuyến mãi kèm gợi ý bấm tự điền, tính giá thời gian thực qua Fetch gọi `PKG_BOOKING.FN_TOTAL_PRICE`, nút "Tiến hành giữ ghế" gọi `PKG_BOOKING.PR_HOLD`.
   - **Xử lý ORA-20005:** Khi có người khác nhanh tay giữ trước, hiển thị Toast cảnh báo và tự động refresh sơ đồ ghế.
5. **Thanh toán mô phỏng (`Bookings/Checkout/{bookingId}`):**
   - **Đồng hồ đếm ngược 5 phút:** Đếm ngược từ `expires_at` đến `00:00`. Khi hết giờ tự động khóa nút thanh toán.
   - **Digital Ticket Preview:** Mã đơn, tên khách, phim, rạp, ghế, mã QR mô phỏng.
   - 3 phương thức demo: `DEMO_QR`, `DEMO_CARD`, `DEMO_CASH`. Nút "Xác nhận thanh toán" (`PR_CONFIRM`) và "Hủy giữ ghế" (`PR_CANCEL`).
6. **Lịch sử đặt vé (`Bookings/History`) & Vé điện tử (`Bookings/Detail/{id}`):**
   - Xem toàn bộ vé của tài khoản đang chọn từ `V_BOOKING_REPORT`.
   - Badge trạng thái: `PAID`, `HELD`, `CANCELLED`, `EXPIRED`.
   - Trang vé điện tử E-Ticket voucher kèm mã QR, thông tin xuất vé và nút In vé / Lưu PDF.

---

### B. Cổng Quản Trị (Admin Portal - `/Admin`)
1. **Giao diện Sidebar Chuyên Nghiệp:** Menu điều hướng Dashboard, Quản lý Phim, Quản lý Suất chiếu, Quản lý Đơn vé, chỉ báo trạng thái kết nối CSDL Oracle 23ai.
2. **Dashboard Quản Trị (`Admin/Dashboard`):**
   - **4 thẻ KPI:** Tổng doanh thu vé `PAID`, Số vé `SOLD`, Số đơn đang giữ `HELD`, Số phim & suất chiếu đang hoạt động.
   - **Biểu đồ Chart.js:** Bar Chart doanh thu theo từng phim và Doughnut Chart tỷ lệ trạng thái đơn hàng.
   - **Bảng Nhật ký kiểm toán BOOKING_AUDIT:** 20 bản ghi mới nhất do trigger `TRG_BOOKING_AUDIT` tự động ghi nhận mỗi khi trạng thái đơn thay đổi.
3. **Quản lý Danh Mục Phim (`Admin/Movies`):**
   - Xem danh sách toàn bộ phim, thêm phim mới, bật/tắt trạng thái hoạt động (`is_active`).
4. **Quản lý Suất Chiếu (`Admin/Showtimes`):**
   - Xem danh sách suất chiếu, tạo suất chiếu mới.
   - **Tự động sinh 40 ghế:** Khi tạo suất chiếu mới, hệ thống tự động sinh 40 dòng ghế `AVAILABLE` trong bảng `SHOWTIME_SEATS` theo phòng chiếu.
5. **Quản lý Đơn Đặt Vé (`Admin/Bookings`):**
   - Xem toàn bộ đơn vé của tất cả khách hàng, lọc theo trạng thái (`ALL`, `PAID`, `HELD`, `CANCELLED`, `EXPIRED`).
   - Nút **"Chạy quét thu hồi ghế quá hạn (`PR_EXPIRE_HOLDS`)"** cho phép kích hoạt trực tiếp từ giao diện để giảng viên thấy số đơn `HELD` quá hạn chuyển sang `EXPIRED` và ghế được giải phóng ngay lập tức.

---

## 7. Quy trình GitFlow & Nhánh Dự Án

- `main`: Nhánh sản phẩm chính thức, ổn định tuyệt đối.
- `develop`: Nhánh tích hợp toàn bộ tính năng và kiểm thử.
- `feature/modern-ui-and-admin-portal`: Nâng cấp giao diện Modern Cinema UI/UX, phân hệ Admin, poster 2:3 và dữ liệu thật 100% từ Oracle 23ai.
