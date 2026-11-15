# CineSeat - Hệ Thống Đặt Vé Xem Phim Trực Tuyến

> **Đề tài 06 - Môn Hệ Quản Trị Cơ Sở Dữ Liệu - HUFLIT**  
> **Công nghệ:** C# ASP.NET Core MVC (.NET 8) & Oracle Database 23ai Free (Docker)  
> **Driver kết nối:** `Oracle.ManagedDataAccess.Core` (Official ODP.NET Core)

---

## 1. Giới thiệu Dự án

**CineSeat** là hệ thống web đặt vé xem phim trực tuyến hiện đại, giải quyết triệt để bài toán giữ chỗ thời gian thực, tính giá động theo khung giờ/cuối tuần/khuyến mãi, và kiểm soát tranh chấp đồng thời (concurrency control / anti-double selling) dựa trên kiến trúc phân tầng kết hợp sức mạnh xử lý giao dịch mức CSDL của **Oracle Database 23ai Free**.

### Điểm nhấn Kiến trúc & Tuân thủ Thiết kế CSDL:
1. **Tuân thủ 100% Logic CSDL:** Toàn bộ nghiệp vụ tính giá (`FN_TOTAL_PRICE`), giữ ghế (`PR_HOLD`), thanh toán (`PR_CONFIRM`), hủy đơn (`PR_CANCEL`) và quét thu hồi ghế quá hạn 5 phút (`PR_EXPIRE_HOLDS`) được thực thi bên trong Package PL/SQL `PKG_BOOKING`.
2. **Không thao tác DML trực tiếp từ Web:** Ứng dụng ASP.NET Core MVC **tuyệt đối không** viết các câu lệnh `INSERT`/`UPDATE` trực tiếp vào bảng `BOOKINGS` hoặc `SHOWTIME_SEATS`.
3. **Quản lý Giao dịch tường minh (Explicit Transactions):** Vì Package PL/SQL không commit tự động, tầng C# Service quản lý `OracleTransaction` chặt chẽ (`Commit()` khi thành công, `Rollback()` khi có ngoại lệ).
4. **Bắt mã lỗi Oracle chuyên sâu (ORA-20001 -> ORA-20014):** Bắt chính xác các mã lỗi nghiệp vụ của Oracle và chuyển đổi thành thông báo thân thiện cho người dùng trên giao diện web (không crash, không lộ lỗi kỹ thuật).
5. **Kiểm soát Concurrency tối đa:** Sử dụng khóa dòng `SELECT ... FOR UPDATE WAIT 5` trên bảng `SHOWTIMES` và kiểm tra trạng thái nguyên tử `UPDATE ... WHERE status = 'AVAILABLE'`, đảm bảo chống hoàn toàn tình trạng bán trùng ghế khi nhiều người cùng đặt 1 ghế cùng lúc.

---

## 2. Cấu trúc Cơ sở Dữ liệu (Chuẩn 3NF)

Hệ thống bao gồm **12 bảng**, **2 views**, **1 package**, **1 procedure**, **2 triggers**:

| Tên Bảng / Object | Mô Tả Chức Năng |
| :--- | :--- |
| `APP_USERS` | Danh sách tài khoản người dùng demo (13 người dùng) |
| `CINEMAS` | Danh sách rạp chiếu phim (CineSeat Quận 1, Quận 7, Tân Bình) |
| `ROOMS` | Phòng chiếu thuộc rạp (Screen 1 Standard, Screen 2 VIP Atmos, Screen 3 IMAX) |
| `SEATS` | Danh mục ghế vật lý theo hàng (A-E) và số (1-8), phân loại `STANDARD`/`VIP` |
| `MOVIES` | Danh mục 13 bộ phim bom tấn kèm thời lượng, độ tuổi (`P`, `K`, `T13`, `T16`, `T18`) |
| `SHOWTIMES` | Suất chiếu phim kèm giá cơ bản (`base_price`), giờ bắt đầu, kết thúc |
| `SHOWTIME_SEATS` | Trạng thái ghế theo từng suất chiếu (`AVAILABLE`, `HELD`, `SOLD`) |
| `PROMOTIONS` | Danh mục mã khuyến mãi (`SALE10`, `STUDENT`, `WEEKEND20`, `VIPMEMBER`, `CINEFAN5`) |
| `BOOKINGS` | Đơn đặt vé với trạng thái (`HELD`, `PAID`, `CANCELLED`, `EXPIRED`) |
| `BOOKING_SEATS` | Chi tiết phân bổ ghế và đơn giá trong đơn đặt vé |
| `PAYMENTS` | Giao dịch thanh toán mô phỏng (`DEMO_QR`, `DEMO_CARD`, `DEMO_CASH`) |
| `BOOKING_AUDIT` | Bảng nhật ký vết kiểm toán trạng thái đơn hàng (Trigger tự động ghi) |
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
├── test-endpoints.ps1               # Script kiểm thử tự động 8 HTTP/API endpoints
│
├── database/                        # Cơ sở dữ liệu Oracle PL/SQL
│   ├── 00_init_user.sql             # Tạo user CINESEAT và cấp quyền trong FREEPDB1
│   ├── 00_master_init.sql           # Master script nạp toàn bộ DDL, DML, Logic
│   ├── 01_schema.sql                # 12 bảng chuẩn 3NF, khóa ngoại, 2 views
│   ├── 02_logic.sql                 # PKG_BOOKING, PR_EXPIRE_HOLDS, 2 Triggers
│   ├── 03_seed.sql                  # Dữ liệu mẫu (phim, rạp, ghế, suất, khuyến mãi)
│   ├── 04_verify.sql                # Kiểm tra trạng thái VALID của toàn bộ objects
│   ├── init-oracle.sh               # Shell script tự động chạy khi khởi tạo container
│   └── setup-db.ps1                 # PowerShell script nạp CSDL tự động
│
├── src/
│   └── CineSeat.Web/                # Ứng dụng C# ASP.NET Core MVC (.NET 8)
│       ├── Controllers/
│       │   ├── AdminController.cs   # Dashboard doanh thu, audit log, trigger expire
│       │   ├── BookingsController.cs# Sơ đồ ghế, tính giá, giữ ghế, checkout, lịch sử
│       │   ├── MoviesController.cs  # Danh sách phim, lọc phim, lịch chiếu, switch user
│       │   └── Api/
│       │       └── BookingApiController.cs # RESTful API cho tích hợp & automation
│       ├── Data/
│       │   └── IDbConnectionFactory.cs # Quản lý kết nối OracleConnection chuẩn
│       ├── Exceptions/
│       │   └── CineSeatException.cs # Ánh xạ ORA-20001 -> ORA-20014 sang tiếng Việt
│       ├── Models/
│       │   ├── Entities.cs          # Models ánh xạ bảng và views CSDL
│       │   └── ViewModels.cs        # ViewModels hiển thị và nhận form
│       ├── Services/
│       │   ├── ICineSeatService.cs  # Interface nghiệp vụ
│       │   ├── CineSeatService.cs   # Service thực thi gọi PKG_BOOKING với Transaction
│       │   └── UserContext.cs       # Quản lý tài khoản demo qua Cookie
│       ├── Views/                   # Giao diện Razor UI hiện đại, responsive
│       │   ├── Admin/Dashboard.cshtml
│       │   ├── Bookings/SelectSeats.cshtml
│       │   ├── Bookings/Checkout.cshtml
│       │   ├── Bookings/History.cshtml
│       │   ├── Bookings/Detail.cshtml
│       │   ├── Movies/Index.cshtml
│       │   └── Movies/Showtimes.cshtml
│       └── appsettings.json         # Chuỗi kết nối OracleDb
│
└── tests/
    └── CineSeat.Tests/              # Bộ kiểm thử tự động xUnit (.NET 8)
        └── CineSeatBusinessTests.cs # Kiểm thử 6 kịch bản nghiệp vụ (TC01 - TC06)
```

---

## 4. Hướng dẫn Khởi chạy Hệ thống

### Yêu cầu Tiên quyết:
- **.NET SDK 8.0** trở lên (`dotnet --version`)
- **Docker Desktop** (hỗ trợ Linux containers trên Windows)
- **PowerShell 7+** hoặc Windows PowerShell

---

### Bước 1: Khởi động Oracle Database 23ai trên Docker

Chạy lệnh Docker Compose để kéo image và khởi chạy container:
```bash
docker compose up -d
```
Kiểm tra trạng thái container cho đến khi đạt `healthy`:
```bash
docker ps --filter "name=oracle-23ai"
```
*(Lần đầu khởi động, Oracle mất khoảng 1-2 phút để mở PDB `FREEPDB1` và sẵn sàng nhận kết nối).*

---

### Bước 2: Khởi tạo Schema & Nạp Dữ liệu Ban Đầu

Thực thi script khởi tạo tự động bằng PowerShell:
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

### Bước 3: Build & Chạy Ứng dụng Web ASP.NET Core MVC

Chạy ứng dụng bằng lệnh:
```bash
dotnet run --project src/CineSeat.Web --urls "http://localhost:5000"
```
Mở trình duyệt và truy cập:
- **Trang chủ đặt vé:** [http://localhost:5000](http://localhost:5000)
- **Admin Dashboard & Audit:** [http://localhost:5000/Admin/Dashboard](http://localhost:5000/Admin/Dashboard)
- **Lịch sử đặt vé:** [http://localhost:5000/Bookings/History](http://localhost:5000/Bookings/History)

---

## 5. Kết quả Kiểm thử Nghiệp vụ Tự động (TC01 -> TC06)

Hệ thống được tích hợp bộ kiểm thử tự động xUnit hoàn chỉnh tại `tests/CineSeat.Tests/CineSeatBusinessTests.cs`. Để thực thi toàn bộ kịch bản kiểm thử, chạy:

```powershell
powershell -ExecutionPolicy Bypass -File .\run-tests.ps1
```
hoặc:
```bash
dotnet test CineSeat.slnx --logger "console;verbosity=normal"
```

### Bảng Chi Tiết Kết Quả Kiểm Thử (100% Passed):

| Mã TC | Tên Kịch Bản Nghiệp Vụ | Mô Tả & Điều Kiện Kiểm Tra | Kết Quả Thực Tế | Trạng Thái |
| :---: | :--- | :--- | :--- | :---: |
| **TC01** | **Tính giá vé linh hoạt** | Suất chiếu thường: giá gốc.<br>Suất tối ($\ge$ 18h): phụ thu +20%.<br>Cuối tuần (T7, CN): phụ thu +10%.<br>Mã `SALE10`: giảm 10% tổng tiền, làm tròn đến hàng nghìn đồng. | `CalculateTotalPriceAsync` trả về giá chính xác tuyệt đối theo công thức CSDL cho cả 1 ghế và nhiều ghế. | **PASSED** |
| **TC02** | **Bắt lỗi mã khuyến mãi sai** | Nhập mã không tồn tại hoặc hết hạn (`MA_KHONG_TON_TAI_9999`) khi tính giá và khi giữ ghế. | Bắt chính xác mã lỗi `ORA-20002`, ném `CineSeatException` chứa thông báo *"Mã khuyến mãi không hợp lệ hoặc đã hết hạn sử dụng"* mà không crash ứng dụng. | **PASSED** |
| **TC03** | **Giữ ghế & Hủy đơn giữ** | Khách chọn ghế trống -> gọi `PR_HOLD` -> ghế chuyển `HELD`. Sau đó khách bấm Hủy -> gọi `PR_CANCEL`. | Đơn chuyển sang `CANCELLED`, ghế trong `SHOWTIME_SEATS` lập tức quay về trạng thái `AVAILABLE` (booking_id xóa về NULL). | **PASSED** |
| **TC04** | **Concurrency - Chống bán trùng ghế** | 2 luồng song song chạy đồng thời (`Task.WhenAll`), cùng tranh chấp giữ 1 ghế duy nhất cho 1 suất chiếu. | Cơ chế `SELECT ... FOR UPDATE WAIT 5` và `UPDATE ... WHERE status = 'AVAILABLE'` bảo đảm **chính xác 1 request thành công** (`HELD`), request còn lại bị chặn bởi `ORA-20005` (*"Ghế đã có người khác nhanh tay giữ hoặc mua trước đó"*). | **PASSED** |
| **TC05** | **Thanh toán & Ghi nhận Audit** | Giữ ghế -> Xác nhận thanh toán bằng phương thức `DEMO_QR` -> gọi `PR_CONFIRM`. | Đơn chuyển sang `PAID`, ghế chuyển sang `SOLD`, bảng `PAYMENTS` ghi nhận giao dịch, trigger `TRG_BOOKING_AUDIT` tự động ghi nhận vết thay đổi `NULL -> HELD` và `HELD -> PAID`. | **PASSED** |
| **TC06** | **Tự động thu hồi ghế quá hạn** | Tạo đơn `HELD`, cập nhật thời gian `expires_at` và `held_until` về quá khứ (> 5 phút), sau đó gọi `PR_EXPIRE_HOLDS`. | Đơn tự động chuyển `EXPIRED`, ghế trong `SHOWTIME_SEATS` tự động trả về `AVAILABLE` để khách khác có thể đặt. | **PASSED** |

---

## 6. Hướng dẫn Trải nghiệm Các Màn hình Chức năng

### 1. Trang chủ (`/` hoặc `/Movies/Index`)
- Xem danh sách các bộ phim đang chiếu với poster, thể loại, thời lượng, phân loại độ tuổi (`P`, `K`, `T13`, `T16`, `T18`).
- Thanh tìm kiếm theo tên phim và bộ lọc nhanh theo thể loại.
- Thanh chuyển đổi tài khoản người dùng demo (13 khách hàng mẫu từ bảng `APP_USERS`), mặc định là **Nguyễn Minh Anh** (`user_id = 1`).

### 2. Danh sách Suất chiếu (`/Movies/Showtimes/{movieId}`)
- Hiển thị các suất chiếu của bộ phim được nhóm theo từng rạp chiếu và phòng chiếu (Standard, VIP Atmos, IMAX).
- Huy hiệu thời gian thực thể hiện số ghế trống còn lại (truy vấn trực tiếp từ View `V_SHOWTIME_CATALOG`).

### 3. Sơ đồ Ghế & Giữ chỗ (`/Bookings/SelectSeats/{showtimeId}`)
- Tự động gọi `PR_EXPIRE_HOLDS` trước khi tải danh sách ghế để dọn sạch các đơn quá hạn.
- Màn hình cong mô phỏng rạp chiếu với lưới ghế chuẩn $5 \times 8 = 40$ ghế (hàng A-E, cột 1-8).
- Trực quan hóa màu sắc:
  - 🟩 **Xanh lá (AVAILABLE):** Ghế trống, có thể bấm chọn (tối đa 8 ghế/lần).
  - 🟨 **Vàng (HELD):** Ghế đang được giữ tạm thời.
  - ⬛/🟥 **Xám/Đỏ (SOLD):** Ghế đã bán, bị vô hiệu hóa chọn.
  - Phân biệt rõ loại ghế **STANDARD** và **VIP**.
- Tạm tính tiền động theo thời gian thực (gọi AJAX `FN_TOTAL_PRICE` khi bấm chọn/bỏ chọn ghế).
- Hỗ trợ nhập mã khuyến mãi có sẵn gợi ý (`SALE10`, `STUDENT`, `WEEKEND20`, `VIPMEMBER`, `CINEFAN5`).
- Nút **"Tiến Hành Giữ Ghế"** gọi `PKG_BOOKING.PR_HOLD` truyền mảng JSON `[1, 2]`.

### 4. Thanh toán Mô phỏng (`/Bookings/Checkout/{bookingId}`)
- Đồng hồ đếm ngược trực quan chính xác 5 phút dựa trên `expires_at` của CSDL. Khi hết giờ, hệ thống tự động khóa và thông báo quá hạn.
- Chọn 1 trong 3 phương thức thanh toán demo:
  - `DEMO_QR` (Mã QR thanh toán nhanh)
  - `DEMO_CARD` (Thẻ tín dụng / ATM)
  - `DEMO_CASH` (Tiền mặt tại quầy)
- Nút **"Xác Nhận Thanh Toán"** gọi `PKG_BOOKING.PR_CONFIRM` hoàn tất giao dịch.
- Nút **"Hủy Đơn Giữ Chỗ"** gọi `PKG_BOOKING.PR_CANCEL` giải phóng ghế ngay lập tức.

### 5. Chi tiết Vé & Lịch sử Đặt vé (`/Bookings/Detail/{id}` & `/Bookings/History`)
- Giao diện E-Ticket voucher hiện đại kèm mã QR code mô phỏng, chi tiết ghế đã mua, rạp chiếu, phòng, thời gian và tổng tiền.
- Lịch sử đặt vé hiển thị toàn bộ các đơn hàng đã đặt của khách hàng đang chọn (truy vấn từ View `V_BOOKING_REPORT`).

### 6. Admin Dashboard Thống kê & Kiểm toán (`/Admin/Dashboard`)
- 5 thẻ thống kê tổng quan:
  - Tổng doanh thu vé đã thanh toán (`PAID`).
  - Số lượng đơn thành công (`PAID`).
  - Số đơn đang giữ chỗ (`HELD`).
  - Số đơn đã hủy (`CANCELLED`).
  - Số đơn hết hạn (`EXPIRED`).
- Bảng nhật ký kiểm toán **BOOKING_AUDIT**: Truy vết lịch sử toàn bộ các lần đổi trạng thái do trigger `TRG_BOOKING_AUDIT` tự động ghi nhận (thời gian, old_status, new_status, mô tả).
- Nút tiện ích quản trị **"Kích hoạt quét đơn quá hạn"**: Cho phép admin chủ động kích hoạt thủ tục `PR_EXPIRE_HOLDS`.

---

## 7. Quy trình GitFlow Đã Triển khai

Dự án áp dụng quy trình Git chuẩn:
- `main`: Nhánh sản phẩm chính thức, ổn định tuyệt đối.
- `develop`: Nhánh tích hợp toàn bộ tính năng và kiểm thử.
- `feature/docker-oracle-setup`: Hạ tầng Docker Compose và kịch bản khởi tạo CSDL Oracle 23ai.
- `feature/mvc-core-and-booking`: Toàn bộ mã nguồn ASP.NET Core MVC .NET 8, kết nối ODP.NET, services, controllers, views.
- `feature/admin-dashboard-and-tests`: Dashboard quản trị, bộ kiểm thử tự động xUnit TC01 -> TC06, script chạy tự động và tài liệu hoàn chỉnh.

---

## 8. Tác Giả & Bản Quyền

- **Đề tài:** Hệ Thống Đặt Vé Xem Phim Trực Tuyến CineSeat (Đề tài 06)
- **Môn học:** Hệ Quản Trị Cơ Sở Dữ Liệu
- **Trường:** Đại học Ngoại ngữ - Tin học TP.HCM (HUFLIT)
