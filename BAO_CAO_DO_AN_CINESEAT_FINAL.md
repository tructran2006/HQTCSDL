TRƯỜNG ĐẠI HỌC NGOẠI NGỮ – TIN HỌC TP. HỒ CHÍ MINH
KHOA CÔNG NGHỆ THÔNG TIN

BÁO CÁO ĐỒ ÁN CUỐI KỲ
HỌC PHẦN: HỆ QUẢN TRỊ CƠ SỞ DỮ LIỆU

ĐỀ TÀI 06:
HỆ THỐNG ĐẶT VÉ XEM PHIM TRỰC TUYẾN
ỨNG DỤNG ORACLE DATABASE 23ai VÀ PL/SQL

GIẢNG VIÊN HƯỚNG DẪN: ThS. LÊ THỊ MINH NGUYỆN
SINH VIÊN / NHÓM THỰC HIỆN:
1. HUỲNH THỊ PHƯƠNG CHI  - MSSV: 24DH113043
2. NGUYỄN TRẦN DUY TÂN    - MSSV: 24DH141765
3. TRẦN CÔNG CHÍNH TRỰC  - MSSV: 24DH112970

TP. HỒ CHÍ MINH, THÁNG 11 NĂM 2026

================================================================================
[TRANG 1]
================================================================================

TÓM TẮT ĐỀ TÀI

CineSeat là hệ thống đặt vé xem phim trực tuyến hiện đại được xây dựng nhằm giải quyết toàn diện bài toán quản lý trạng thái ghế theo suất chiếu theo thời gian thực, tính giá động theo ca chiếu/khuyến mãi, và kiểm soát xung đột đồng thời (anti-double selling) khi nhiều người dùng cùng thao tác đặt vé. Trọng tâm của đồ án là ứng dụng chuyên sâu công nghệ cơ sở dữ liệu Oracle Database 23ai Free chạy trên nền tảng Docker container hóa, kết hợp ngôn ngữ thủ tục PL/SQL để đảm bảo dữ liệu ghế, đơn hàng và doanh thu luôn đạt tính toàn vẹn và nhất quán tuyệt đối (ACID).

Toàn bộ hệ sinh thái của CineSeat bao gồm 12 bảng quan hệ chuẩn 3NF, 2 view nghiệp vụ, 4 chỉ mục tối ưu, 1 package cốt lõi PKG_BOOKING, 2 triggers giám sát và 1 procedure thu hồi ghế quá hạn. Đặc biệt, ứng dụng minh họa đã được xây dựng hoàn chỉnh và chạy thực tế bằng C# ASP.NET Core MVC (.NET 8) kết nối trực tiếp tới Oracle Database 23ai qua thư viện chính thức Oracle.ManagedDataAccess.Core (ODP.NET). 

Hệ thống xử lý nghiệp vụ giữ 1–8 ghế trong thời hạn 5 phút, thanh toán mô phỏng (DEMO_QR, DEMO_CARD, DEMO_CASH), và thu hồi ghế quá hạn tự động. Bộ kiểm thử tự động xUnit (CineSeat.Tests) gồm 10 kịch bản nghiệp vụ (TC01 đến TC10) đã được thực thi và đạt kết quả PASS 100%, bao gồm kiểm chứng chống bán trùng ghế thông qua cơ chế khóa dòng SELECT ... FOR UPDATE WAIT 5 kết hợp SAVEPOINT. Báo cáo này trình bày chi tiết từ kiến trúc CSDL, thiết kế UML, mã nguồn PL/SQL, kết nối ứng dụng web, đến kết quả kiểm thử và phân tích kế hoạch thực thi truy vấn (Execution Plan).

================================================================================
[TRANG 2]
================================================================================

MỤC LỤC

TÓM TẮT ĐỀ TÀI ................................................................ Trang 1
MỤC LỤC ...................................................................... Trang 2
DANH MỤC TỪ VIẾT TẮT ......................................................... Trang 3

CHƯƠNG 1. MỞ ĐẦU .............................................................. Trang 4
  1.1. Đặt vấn đề ............................................................. Trang 4
  1.2. Lý do chọn đề tài ...................................................... Trang 4
  1.3. Mục tiêu đề tài ........................................................ Trang 4
       1.3.1. Mục tiêu tổng quát .............................................. Trang 4
       1.3.2. Mục tiêu cụ thể ................................................. Trang 4
  1.4. Đối tượng và phạm vi nghiên cứu ........................................ Trang 5
       1.4.1. Đối tượng nghiên cứu ............................................ Trang 5
       1.4.2. Phạm vi nghiên cứu .............................................. Trang 5
  1.5. Ý nghĩa khoa học và thực tiễn .......................................... Trang 5
  1.6. Phương pháp nghiên cứu ................................................. Trang 5

CHƯƠNG 2. TỔNG QUAN ........................................................... Trang 6
  2.1. Các mô hình đặt vé liên quan ........................................... Trang 6
  2.2. Cơ sở lý thuyết và đối chiếu mã nguồn PL/SQL ........................... Trang 6
       2.2.1. Mô hình quan hệ và phân tách ghế vật lý / ghế theo suất ......... Trang 6
       2.2.2. Chuẩn hóa 3NF và phân bổ giá vé ................................. Trang 7
       2.2.3. Function tính giá vé và quy tắc khuyến mãi ...................... Trang 7
       2.2.4. Procedure giữ ghế và kỹ thuật JSON_TABLE ........................ Trang 8
       2.2.5. Giao dịch ACID và cơ chế khóa đồng thời ......................... Trang 8
       2.2.6. Thanh toán mô phỏng và bảo toàn đơn hàng ........................ Trang 9
       2.2.7. Cursor thu hồi ghế quá hạn và xử lý ngoại lệ .................... Trang 9
       2.2.8. Package, Trigger và phạm vi bảo vệ dữ liệu ...................... Trang 10
       2.2.9. View nghiệp vụ và đánh chỉ mục tối ưu ........................... Trang 10
  2.3. Công nghệ và môi trường triển khai ..................................... Trang 11

CHƯƠNG 3. PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG ...................................... Trang 12
  3.1. Phân tích yêu cầu hệ thống ............................................. Trang 12
       3.1.1. Xác định các tác nhân (Actors) .................................. Trang 12
       3.1.2. Yêu cầu chức năng ............................................... Trang 12
       3.1.3. Yêu cầu phi chức năng ........................................... Trang 12
  3.2. Mô hình hóa hệ thống bằng UML .......................................... Trang 13
       3.2.1. Biểu đồ Use Case tổng quát ...................................... Trang 13
       3.2.2. Đặc tả các Use Case chính ....................................... Trang 13
       3.2.3. Biểu đồ tuần tự giữ ghế (Sequence Diagram - PR_HOLD) ............ Trang 14
       3.2.4. Biểu đồ tuần tự thanh toán (Sequence Diagram - PR_CONFIRM) ...... Trang 14
       3.2.5. Biểu đồ hoạt động đặt vé (Activity Diagram) ..................... Trang 14
  3.3. Thiết kế cơ sở dữ liệu ................................................. Trang 15
       3.3.1. Mô hình ERD vật lý 12 bảng ...................................... Trang 15
       3.3.2. Từ điển dữ liệu và ràng buộc toàn vẹn ........................... Trang 16
       3.3.3. Vòng đời trạng thái và các bất biến nghiệp vụ ................... Trang 18
  3.4. Thiết kế Ứng dụng Web C# ASP.NET Core MVC .............................. Trang 19
       3.4.1. Kiến trúc phân tầng và Service tích hợp Oracle .................. Trang 19
       3.4.2. Quản lý giao dịch tường minh và ánh xạ mã lỗi ORA ............... Trang 19
       3.4.3. Các màn hình chức năng Cổng Khách hàng và Cổng Quản trị ......... Trang 20
  3.5. Tổ chức cài đặt và nạp dữ liệu Oracle 23ai trên Docker ................. Trang 21

CHƯƠNG 4. KIỂM THỬ VÀ TỐI ƯU HÓA .............................................. Trang 22
  4.1. Kịch bản kiểm thử tự động xUnit (TC01 đến TC10) ........................ Trang 22
       4.1.1. Kết quả kiểm thử tự động 10/10 Test Cases ....................... Trang 22
       4.1.2. Kiểm chứng các ca biên và ngoại lệ .............................. Trang 22
  4.2. Kiểm chứng kỹ thuật chống bán trùng ghế (Concurrency Test) ............. Trang 23
  4.3. Phân tích Execution Plan trước và sau tối ưu ........................... Trang 24
       4.3.1. Truy vấn Q1: Tra cứu suất chiếu theo phim và thời gian .......... Trang 24
       4.3.2. Truy vấn Q2: Lịch sử đơn hàng sắp xếp theo thời gian ............ Trang 25
       4.3.3. Bảng tổng hợp chi phí Cost và đánh giá chỉ mục .................. Trang 25
  4.4. Đánh giá chất lượng vận hành hệ thống .................................. Trang 26

CHƯƠNG 5. KẾT LUẬN ............................................................ Trang 27
  5.1. Kết quả đạt được ....................................................... Trang 27
  5.2. Hạn chế của đề tài ..................................................... Trang 27
  5.3. Hướng phát triển trong tương lai ....................................... Trang 27
  5.4. Bảng phân công và tỷ lệ đóng góp của thành viên ........................ Trang 27

TÀI LIỆU THAM KHẢO ............................................................ Trang 28

================================================================================
[TRANG 3]
================================================================================

DANH MỤC TỪ VIẾT TẮT

| Thuật ngữ | Tiếng Anh / Tên đầy đủ | Giải thích ngữ cảnh trong đồ án |
| :--- | :--- | :--- |
| **PK** | Primary Key | Khóa chính xác định tính duy nhất của bản ghi trong bảng. |
| **FK** | Foreign Key | Khóa ngoại thiết lập ràng buộc toàn vẹn tham chiếu giữa các bảng. |
| **3NF** | Third Normal Form | Dạng chuẩn 3 trong thiết kế cơ sở dữ liệu quan hệ. |
| **PL/SQL** | Procedural Language for SQL | Ngôn ngữ thủ tục tích hợp chặt chẽ với SQL của Oracle Database. |
| **ODP.NET** | Oracle Data Provider for .NET | Trình điều khiển kết nối chính thức tốc độ cao từ C# .NET tới Oracle. |
| **MVC** | Model - View - Controller | Mô hình kiến trúc phân tách tầng dữ liệu, giao diện và điều hướng. |
| **ACID** | Atomicity, Consistency, Isolation, Durability | 4 thuộc tính đảm bảo độ tin cậy của giao dịch cơ sở dữ liệu. |
| **TTL** | Time To Live | Thời hạn giữ chỗ tạm thời (được đặt là 5 phút trong CineSeat). |
| **HELD** | Seat / Booking Status: Held | Trạng thái ghế/đơn đặt chỗ đang được giữ tạm thời, chờ thanh toán. |
| **SOLD** | Seat Status: Sold | Trạng thái ghế đã thanh toán thành công, không thể đặt lại. |
| **AVAILABLE**| Seat Status: Available | Trạng thái ghế đang trống trong suất chiếu, sẵn sàng để khách đặt. |
| **PAID** | Booking Status: Paid | Trạng thái đơn đặt vé đã hoàn tất thanh toán mô phỏng thành công. |
| **EXPIRED** | Booking Status: Expired | Trạng thái đơn đặt vé bị hủy tự động do quá thời hạn 5 phút chưa trả tiền. |
| **CANCELLED**| Booking Status: Cancelled | Trạng thái đơn đặt vé do khách hàng chủ động hủy khi còn trong hạn giữ. |
| **UML** | Unified Modeling Language | Ngôn ngữ mô hình hóa thống nhất (Use Case, Sequence, Activity). |
| **ERD** | Entity Relationship Diagram | Lược đồ quan hệ thực thể mô tả cấu trúc dữ liệu. |

================================================================================
[TRANG 4]
================================================================================

CHƯƠNG 1. MỞ ĐẦU

1.1. Đặt vấn đề
Trong thời đại chuyển đổi số của ngành dịch vụ giải trí, các cụm rạp chiếu phim hiện đại đều chuyển dịch hoàn toàn từ bán vé quầy sang nền tảng trực tuyến. Khách hàng đòi hỏi khả năng tra cứu lịch chiếu tức thì, xem sơ đồ ghế trực quan theo thời gian thực, áp dụng mã giảm giá và hoàn tất đơn hàng mọi lúc mọi nơi.
Thách thức kỹ thuật lớn nhất của hệ thống đặt vé trực tuyến nằm ở tầng cơ sở dữ liệu: Khi nhiều người dùng cùng truy cập và bấm chọn cùng một ghế tại cùng một giây (Flash-Sale hoặc phim bom tấn), hệ thống phải ngăn chặn tuyệt đối tình trạng bán trùng ghế (Double Booking). Đồng thời, nếu người dùng đã giữ ghế nhưng không hoàn tất thanh toán, hệ thống phải tự động giải phóng ghế sau 5 phút để mở cơ hội cho khách hàng khác mà không làm sai lệch số liệu doanh thu.

1.2. Lý do chọn đề tài
Đề tài 06 "Hệ thống đặt vé xem phim trực tuyến" thuộc Nhóm 3 (PL/SQL: Procedure, Function, Cursor, Exception) trong đề cương môn học là sự lựa chọn lý tưởng vì:
1. Có cấu trúc thực thể kinh điển, rõ ràng (Phim, Rạp, Phòng, Ghế, Suất chiếu, Đơn đặt vé) giúp thực hành chuẩn hóa CSDL đến 3NF một cách mẫu mực.
2. Đòi hỏi giải quyết các bài toán giao dịch phức tạp: xử lý danh sách ghế động bằng `JSON_TABLE`, khóa dòng giao dịch bằng `FOR UPDATE WAIT`, tính giá vé đa điều kiện, và xử lý hủy tự động theo thời gian thực bằng `Explicit Cursor`. Các bài toán này chỉ có thể được giải quyết triệt để và an toàn tại tầng CSDL thông qua PL/SQL.

1.3. Mục tiêu đề tài
1.3.1. Mục tiêu tổng quát
Xây dựng trọn vẹn giải pháp hệ thống web đặt vé xem phim CineSeat, kết hợp sức mạnh giao dịch của Oracle Database 23ai Free với ứng dụng web C# ASP.NET Core MVC (.NET 8), đảm bảo tính toàn vẹn dữ liệu, chống bán trùng ghế và mang lại trải nghiệm người dùng hiện đại, sắc nét.

1.3.2. Mục tiêu cụ thể
- Thiết kế 12 bảng quan hệ chuẩn 3NF, phân tách rành mạch giữa ghế vật lý (`SEATS`) và trạng thái ghế theo từng suất (`SHOWTIME_SEATS`).
- Xây dựng Package `PKG_BOOKING` gồm Function tính giá `FN_TOTAL_PRICE`, các Procedure `PR_HOLD`, `PR_CONFIRM`, `PR_CANCEL` và `PR_EXPIRE_HOLDS`.
- Cài đặt 2 Trigger: `TRG_BOOKING_AUDIT` ghi log kiểm toán và `TRG_SEAT_STATE_GUARD` bảo vệ trạng thái ghế.
- Xây dựng ứng dụng web ASP.NET Core MVC hoàn chỉnh với Cổng Khách hàng (đặt vé, chọn ghế 3D, thanh toán đếm ngược, E-ticket QR) và Cổng Quản trị Admin (KPI Dashboard, CRUD Phim, Suất chiếu, Rạp/Phòng, Khuyến mãi).
- Triển khai toàn bộ hạ tầng trên Docker và kiểm thử tự động đạt 10/10 test cases xUnit PASS 100%.

================================================================================
[TRANG 5]
================================================================================

1.4. Đối tượng và phạm vi nghiên cứu
1.4.1. Đối tượng nghiên cứu
- Nghiệp vụ: Quy trình giữ chỗ tạm thời trong 5 phút, thanh toán vé xem phim, tính giá vé phụ thu ca tối / cuối tuần / mã giảm giá, kiểm soát xung đột đồng thời và quản trị rạp chiếu phim.
- Kỹ thuật: Oracle Database 23ai Free, PL/SQL Package/Procedure/Function/Trigger, Oracle Transaction & Concurrency Control, C# ASP.NET Core MVC (.NET 8), ODP.NET Core, Docker Containerization.

1.4.2. Phạm vi nghiên cứu
| Phạm vi | Nội dung triển khai thực tế của CineSeat |
| :--- | :--- |
| **Quy mô CSDL** | 13 phim bom tấn, 13 người dùng demo, 2 cụm rạp, 3 phòng chiếu, 120 ghế vật lý, 20 suất chiếu, 800 bản ghi trạng thái ghế theo suất, 5 mã khuyến mãi. |
| **Nghiệp vụ Web** | Tra cứu phim, xem lịch chiếu theo rạp, chọn 1–8 ghế theo sơ đồ 3D, giữ chỗ 5 phút, thanh toán mô phỏng (`DEMO_QR`, `DEMO_CARD`, `DEMO_CASH`), hủy giữ chỗ, xem vé điện tử QR, lịch sử đặt vé, và cổng Admin toàn diện. |
| **Giới hạn đề tài** | Thanh toán là mô phỏng trong môi trường đào tạo (không tích hợp cổng ngân hàng thực tế đòi hỏi giấy phép kinh doanh); chưa tích hợp SMS Gateway thật. |
| **Môi trường** | Oracle Database 23ai Free chạy trên Docker (PDB: `FREEPDB1`), Web chạy ASP.NET Core 8.0 Kestrel Server trên Docker (Port 5000), hệ điều hành Windows 11. |

1.5. Ý nghĩa khoa học và thực tiễn
- Ý nghĩa khoa học: Minh chứng rõ nét nguyên lý đưa logic tính toán và kiểm soát giao dịch về gần dữ liệu (Data-Centric Logic), tận dụng tối đa cơ chế khóa ACID của DBMS thay vì đẩy rủi ro xử lý đồng thời lên tầng Web Application.
- Ý nghĩa thực tiễn: Tạo ra sản phẩm ứng dụng hoàn chỉnh, có thể ứng dụng trực tiếp làm nền tảng cho các chuỗi rạp chiếu phim vừa và nhỏ, đồng thời làm tài liệu tham khảo chất lượng cao cho sinh viên ngành Hệ thống Thông tin.

1.6. Phương pháp nghiên cứu
- Phương pháp phân tích - thiết kế hệ thống thông tin theo hướng đối tượng (OOAD) sử dụng chuẩn UML.
- Phương pháp chuẩn hóa cơ sở dữ liệu quan hệ đạt chuẩn 3NF.
- Phương pháp kiểm thử phần mềm tự động (Automated Unit Testing) với xUnit và phân tích kế hoạch thực thi câu lệnh SQL qua `DBMS_XPLAN.DISPLAY`.

================================================================================
[TRANG 6]
================================================================================

CHƯƠNG 2. TỔNG QUAN

2.1. Các mô hình đặt vé liên quan
Trong các kiến trúc đặt vé phổ biến hiện nay, có 2 trường phái thiết kế chính:
1. Trường phái Application-Centric: Trạng thái ghế được lưu trữ tạm thời trên Redis/In-memory Cache và xử lý bằng Web Server. Phương pháp này tải nhanh nhưng đối mặt với nguy cơ lệch đồng bộ giữa Cache và Database khi có sự cố mạng hoặc sập server đột ngột.
2. Trường phái Database-Centric (CineSeat lựa chọn): Mọi trạng thái ghế và đơn hàng được bảo vệ bằng các ràng buộc quan hệ, bảng trạng thái riêng biệt theo suất chiếu, và thực thi trực tiếp bằng thủ tục lưu trữ (Stored Procedure) trong Oracle 23ai. Cách tiếp cận này đảm bảo dữ liệu không bao giờ bị corrupt ngay cả khi có hàng trăm request đồng thời.

2.2. Cơ sở lý thuyết và đối chiếu mã nguồn PL/SQL
2.2.1. Mô hình quan hệ và phân tách ghế vật lý / ghế theo suất
Bảng `SEATS` mô tả vị trí ghế vật lý tại phòng chiếu: gồm hàng ghế (`row_label`), số ghế (`seat_number`) và loại ghế (`seat_type`: STANDARD/VIP). Trong khi đó, bảng `SHOWTIME_SEATS` phản ánh khả năng bán của ghế tại một suất chiếu cụ thể:
```sql
CREATE TABLE showtime_seats (
  showtime_id   NUMBER NOT NULL REFERENCES showtimes(showtime_id),
  seat_id       NUMBER NOT NULL REFERENCES seats(seat_id),
  status        VARCHAR2(15) DEFAULT 'AVAILABLE' NOT NULL
                  CHECK(status IN ('AVAILABLE','HELD','SOLD')),
  booking_id    NUMBER REFERENCES bookings(booking_id),
  held_until    TIMESTAMP,
  CONSTRAINT pk_showtime_seats PRIMARY KEY(showtime_id, seat_id),
  CONSTRAINT chk_seat_holder CHECK (
    (status='AVAILABLE' AND booking_id IS NULL AND held_until IS NULL)
    OR (status='HELD' AND booking_id IS NOT NULL AND held_until IS NOT NULL)
    OR (status='SOLD' AND booking_id IS NOT NULL AND held_until IS NULL)
  )
);
```
Khóa chính ghép `(showtime_id, seat_id)` triệt tiêu hoàn toàn khả năng trùng lặp bản ghi trạng thái. Ràng buộc `chk_seat_holder` bắt buộc tính nhất quán nghiệp vụ: Ghế `AVAILABLE` thì không được có người giữ; ghế `HELD` phải gắn với đơn hàng và thời hạn giữ; ghế `SOLD` đã bán thành công thì không còn thời hạn giữ.

================================================================================
[TRANG 7]
================================================================================

2.2.2. Chuẩn hóa 3NF và phân bổ giá vé
Hệ thống đạt chuẩn 3NF thông qua việc phân rã các quan hệ:
- Bảng `BOOKINGS` lưu tổng tiền `total_amount` đã chốt của cả đơn hàng.
- Bảng `BOOKING_SEATS` lưu chi tiết từng ghế cùng đơn giá `unit_price` của ghế đó tại thời điểm đặt vé.
```sql
CREATE TABLE booking_seats (
  booking_id    NUMBER NOT NULL REFERENCES bookings(booking_id),
  seat_id       NUMBER NOT NULL REFERENCES seats(seat_id),
  unit_price    NUMBER(12,0) NOT NULL CHECK(unit_price >= 0),
  CONSTRAINT pk_booking_seats PRIMARY KEY(booking_id, seat_id)
);
```
Khi chia đều tổng tiền cho danh sách ghế, mã nguồn PL/SQL trong `PR_HOLD` tính phần dư và cộng vào ghế cuối cùng, đảm bảo bất biến: $\sum \text{unit\_price} = \text{total\_amount}$, loại bỏ sai số do làm tròn số học.

2.2.3. Function tính giá vé và quy tắc khuyến mãi
Hàm `FN_TOTAL_PRICE` trong Package `PKG_BOOKING` chịu trách nhiệm định giá vé dựa trên giá gốc của suất chiếu (`base_price`), hệ số ca tối (bắt đầu sau 18:00 tăng 20%), hệ số cuối tuần (thứ Bảy/Chủ Nhật tăng 10%), và phần trăm giảm giá của mã khuyến mãi hợp lệ:
```sql
FUNCTION fn_total_price(
  p_showtime_id NUMBER,
  p_seat_count  NUMBER,
  p_promo_code  VARCHAR2 DEFAULT NULL
) RETURN NUMBER IS
  v_base NUMBER; v_time TIMESTAMP; v_multiplier NUMBER := 1.0;
  v_discount NUMBER := 0;
BEGIN
  IF p_seat_count < 1 OR p_seat_count > 8 THEN
    RAISE_APPLICATION_ERROR(-20001, 'So luong ghe phai tu 1 den 8');
  END IF;
  SELECT base_price, start_at INTO v_base, v_time FROM showtimes
   WHERE showtime_id = p_showtime_id AND is_active = 1;
  IF TO_NUMBER(TO_CHAR(v_time, 'HH24')) >= 18 THEN v_multiplier := 1.20; END IF;
  IF TRIM(TO_CHAR(v_time, 'DY', 'NLS_DATE_LANGUAGE=ENGLISH')) IN ('SAT','SUN') THEN
    v_multiplier := v_multiplier * 1.10;
  END IF;
  IF TRIM(p_promo_code) IS NOT NULL THEN
    SELECT discount_pct INTO v_discount FROM promotions
     WHERE UPPER(promo_code) = UPPER(TRIM(p_promo_code))
       AND valid_until >= TRUNC(SYSDATE) AND is_active = 1;
  END IF;
  RETURN ROUND(v_base * v_multiplier * p_seat_count * (100 - v_discount) / 100, -3);
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    RAISE_APPLICATION_ERROR(-20002, 'Ma khuyen mai hoac suat chieu khong hop le');
END fn_total_price;
```

================================================================================
[TRANG 8]
================================================================================

2.2.4. Procedure giữ ghế và kỹ thuật JSON_TABLE
Thủ tục `PR_HOLD` nhận danh sách ghế dạng chuỗi JSON (ví dụ: `'[1, 2, 3]'`) từ ứng dụng web, sử dụng hàm `JSON_TABLE` của Oracle để bóc tách thành tập hợp dữ liệu quan hệ ngay trong bộ nhớ CSDL:
```sql
SELECT COUNT(*), COUNT(DISTINCT seat_id) INTO v_count, v_distinct
  FROM JSON_TABLE(p_seats_json, '$[*]' COLUMNS (seat_id NUMBER PATH '$'));
IF v_count < 1 OR v_count > 8 OR v_count <> v_distinct THEN
  RAISE_APPLICATION_ERROR(-20004, 'Danh sach ghe khong hop le hoac bi trung lap');
END IF;
```
Sau khi tạo bản ghi đơn hàng ở trạng thái `HELD` với thời hạn hết hạn sau 5 phút (`LOCALTIMESTAMP + NUMTODSINTERVAL(5, 'MINUTE')`), vòng lặp con trỏ duyệt từng ghế và cập nhật có điều kiện:
```sql
UPDATE showtime_seats 
   SET status = 'HELD', booking_id = p_booking_id, held_until = p_expires
 WHERE showtime_id = p_showtime_id AND seat_id = v_seat.seat_id
   AND status = 'AVAILABLE';
IF SQL%ROWCOUNT <> 1 THEN
  RAISE_APPLICATION_ERROR(-20005, 'Ghe da co nguoi giu hoac khong ton tai');
END IF;
```
Nếu bất kỳ ghế nào trong danh sách không thể cập nhật (do người khác đã giữ trước), ngoại lệ lập tức phát sinh và kích hoạt `ROLLBACK TO sp_hold`, hoàn tác toàn bộ đơn hàng và các ghế đã cập nhật dở dang.

2.2.5. Giao dịch ACID và cơ chế khóa đồng thời
Để chống bán trùng ghế khi có nhiều tiến trình chạy song song, `PR_HOLD` thiết lập ranh giới giao dịch nghiêm ngặt:
```sql
SAVEPOINT sp_hold;
-- Khoa suat chieu de sap xep cac request dong thoi theo thu tu
SELECT showtime_id INTO v_show_id FROM showtimes
 WHERE showtime_id = p_showtime_id AND is_active = 1 AND start_at > LOCALTIMESTAMP
 FOR UPDATE WAIT 5;
```
Lệnh `FOR UPDATE WAIT 5` khóa bản ghi suất chiếu tương ứng. Khi 2 request cùng gửi lệnh giữ ghế trong cùng 1 suất chiếu: Request 1 lấy được khóa và thực thi; Request 2 phải chờ tối đa 5 giây. Khi Request 1 hoàn tất và Commit, Request 2 mới được bước vào kiểm tra ghế. Lúc này trạng thái ghế đã chuyển sang `HELD`, khiến câu lệnh `UPDATE ... WHERE status = 'AVAILABLE'` trả về 0 dòng, kích hoạt lỗi ORA-20005 và rollback sạch sẽ.

================================================================================
[TRANG 9]
================================================================================

2.2.6. Thanh toán mô phỏng và bảo toàn đơn hàng
Thủ tục `PR_CONFIRM` tiếp nhận `p_booking_id` và phương thức thanh toán (`DEMO_QR`, `DEMO_CARD`, `DEMO_CASH`). Thủ tục thực hiện kiểm tra thời hạn và cập nhật chuyển đổi trạng thái nguyên tử:
```sql
SAVEPOINT sp_confirm;
-- Kiem tra don con trong han giu 5 phut
SELECT showtime_id, total_amount INTO v_show, v_total FROM bookings
 WHERE booking_id = p_booking_id AND status = 'HELD' 
   AND expires_at > LOCALTIMESTAMP FOR UPDATE;

-- Chuyen trang thai ghe sang SOLD
UPDATE showtime_seats SET status = 'SOLD', held_until = NULL
 WHERE showtime_id = v_show AND booking_id = p_booking_id AND status = 'HELD';
v_expected_count := SQL%ROWCOUNT;

SELECT COUNT(*) INTO v_lock FROM booking_seats WHERE booking_id = p_booking_id;
IF v_expected_count <> v_lock THEN
  RAISE_APPLICATION_ERROR(-20010, 'Ghe khong con du; huy giao dich');
END IF;

INSERT INTO payments(booking_id, amount, method, payment_status)
  VALUES(p_booking_id, v_total, p_method, 'PAID');
UPDATE bookings SET status = 'PAID', paid_at = LOCALTIMESTAMP
 WHERE booking_id = p_booking_id;
```
Khóa ngoại độc nhất `UNIQUE(booking_id)` trên bảng `PAYMENTS` bảo đảm mỗi đơn đặt vé chỉ có tối đa một giao dịch thanh toán thành công duy nhất.

2.2.7. Cursor thu hồi ghế quá hạn và xử lý ngoại lệ
Thủ tục `PR_EXPIRE_HOLDS` sử dụng Explicit Cursor kết hợp khối lặp `FOR s IN c_showtimes` để quét và giải phóng toàn bộ các ghế giữ chỗ quá hạn 5 phút mà chưa thanh toán:
```sql
CURSOR c_showtimes IS
  SELECT DISTINCT showtime_id FROM bookings
   WHERE status = 'HELD' AND expires_at <= LOCALTIMESTAMP
   ORDER BY showtime_id;
BEGIN
  FOR s IN c_showtimes LOOP
    SAVEPOINT sp_expire_one;
    BEGIN
      SELECT showtime_id INTO v_lock FROM showtimes
       WHERE showtime_id = s.showtime_id FOR UPDATE WAIT 2;
      UPDATE bookings SET status = 'EXPIRED'
       WHERE showtime_id = s.showtime_id AND status = 'HELD' 
         AND expires_at <= LOCALTIMESTAMP;
      UPDATE showtime_seats SET status = 'AVAILABLE', booking_id = NULL, held_until = NULL
       WHERE showtime_id = s.showtime_id AND status = 'HELD' 
         AND held_until <= LOCALTIMESTAMP;
    EXCEPTION WHEN OTHERS THEN
      ROLLBACK TO sp_expire_one;
    END;
  END LOOP;
END pr_expire_holds;
```

================================================================================
[TRANG 10]
================================================================================

2.2.8. Package, Trigger và phạm vi bảo vệ dữ liệu
Package `PKG_BOOKING` đóng gói toàn bộ logic nghiệp vụ cốt lõi, bảo đảm tính module hóa và che giấu cấu trúc dữ liệu nội bộ. Đi kèm với Package là 2 Trigger cấp dòng (Row-Level Triggers):
1. `TRG_BOOKING_AUDIT`: Tự động ghi chép lịch sử thay đổi trạng thái của đơn hàng vào bảng `BOOKING_AUDIT` (`old_status` -> `new_status`), hỗ trợ đối soát và kiểm toán giao dịch:
```sql
CREATE OR REPLACE TRIGGER trg_booking_audit
AFTER INSERT OR UPDATE OF status ON bookings
FOR EACH ROW
BEGIN
  IF INSERTING OR NVL(:OLD.status,'~') <> :NEW.status THEN
    INSERT INTO booking_audit(booking_id, old_status, new_status, description)
      VALUES(:NEW.booking_id, :OLD.status, :NEW.status, 'Thay doi trang thai booking');
  END IF;
END;
```
2. `TRG_SEAT_STATE_GUARD`: Hoạt động như một "bức tường lửa" ngăn chặn các hành vi cập nhật bất hợp pháp trực tiếp từ SQL: chặn bán trực tiếp ghế `AVAILABLE` thành `SOLD` mà không thông qua `HELD` (ORA-20013), chặn sửa trạng thái ghế đã bán `SOLD` (ORA-20012), và chặn đổi chủ sở hữu đơn hàng khi thanh toán (ORA-20014).

2.2.9. View nghiệp vụ và đánh chỉ mục tối ưu
1. `V_SHOWTIME_CATALOG`: Kết hợp bảng `SHOWTIMES`, `MOVIES`, `ROOMS`, `CINEMAS` và truy vấn con đếm số ghế trống `AVAILABLE` thời gian thực, phục vụ hiển thị lịch chiếu tức thì trên trang chủ mà không làm khóa bảng.
2. `V_BOOKING_REPORT`: Tổng hợp chi tiết thông tin đơn hàng, tên khách hàng, số lượng vé, tổng tiền và trạng thái, phục vụ báo cáo quản trị.
Hệ thống thiết lập 4 chỉ mục độc lập nhằm tối ưu hóa đường dẫn truy xuất dữ liệu:
- `ix_showtime_movie_date` trên `SHOWTIMES(movie_id, start_at)`: Tăng tốc tra cứu lịch chiếu.
- `ix_bookings_user_date` trên `BOOKINGS(user_id, created_at)`: Tăng tốc hiển thị lịch sử mua vé.
- `ix_bookings_status_expire` trên `BOOKINGS(status, expires_at)`: Tối ưu hóa tác vụ quét ghế quá hạn.
- `ix_showtime_seats_lookup` trên `SHOWTIME_SEATS(showtime_id, status)`: Tối ưu hóa việc lọc ghế trống.

================================================================================
[TRANG 11]
================================================================================

2.3. Công nghệ và môi trường triển khai

Bảng tổng hợp công nghệ sử dụng trong toàn bộ giải pháp CineSeat:

| Thành phần hệ thống | Công nghệ / Thư viện | Phiên bản | Vai trò & Lý do lựa chọn |
| :--- | :--- | :---: | :--- |
| **Cơ sở dữ liệu** | Oracle Database 23ai Free | 23.4+ | Quản lý bảng quan hệ 3NF, thực thi Package PL/SQL, khóa giao dịch ACID, chỉ mục tối ưu. |
| **Nền tảng Container**| Docker & Docker Compose | 24+ | Đóng gói môi trường CSDL và Web Server độc lập, cấu hình biến môi trường `AL32UTF8`. |
| **Ứng dụng Web** | C# ASP.NET Core MVC | .NET 8.0 | Xây dựng Cổng Khách hàng và Cổng Quản trị Admin, cấu hình Cookie Authentication, DI. |
| **Driver kết nối** | Oracle.ManagedDataAccess.Core | 23.7.0 | Thư viện ODP.NET Core chính thức từ Oracle, hỗ trợ kết nối thuần 100% Managed Code, Stored Procedure. |
| **Giao diện Front-end**| Razor Pages, Bootstrap, CSS3 | 5.3.3 | Thiết kế UI phong cách rạp chiếu phim hiện đại (Dark Mode, Cinema Red), sơ đồ ghế 3D, responsive. |
| **Biểu đồ thống kê** | Chart.js | 4.4 | Hiển thị trực quan KPI doanh thu, biểu đồ tỷ lệ đặt vé theo trạng thái trên Admin Dashboard. |
| **Kiểm thử tự động** | xUnit Test Framework | 2.8+ | Kiểm thử tự động hóa 10 kịch bản kiểm thử độc lập (TC01 đến TC10), tích hợp CI/CD. |
| **Quản lý mã nguồn** | Git & GitHub | 2.40+ | Quản lý phân nhánh Git Flow chuẩn (`main`, `develop`, `feature/*`), commit hoàn toàn bằng tiếng Việt. |

Kiến trúc kết nối tổng thể:
Trình duyệt Web (Client) <---> ASP.NET Core MVC (.NET 8 Kestrel) <---[ODP.NET Connection Pool]---> Oracle Database 23ai Free (Docker Container)

Ứng dụng web ASP.NET Core MVC tuyệt đối không phát sinh các lệnh SQL tùy tiện để thêm/sửa dữ liệu giao dịch, mà 100% nghiệp vụ đặt vé đều được đóng gói thông qua các lời gọi thực thi thủ tục trong Package `PKG_BOOKING`.

================================================================================
[TRANG 12]
================================================================================

CHƯƠNG 3. PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG

3.1. Phân tích yêu cầu hệ thống
3.1.1. Xác định các tác nhân (Actors)
1. Khách hàng (Customer): Người dùng cuối truy cập hệ thống để tra cứu danh mục phim bom tấn, xem lịch chiếu theo từng cụm rạp, chọn chỗ ngồi trên sơ đồ ghế 3D, áp dụng mã giảm giá, thực hiện giữ ghế trong 5 phút, thanh toán mô phỏng và xem lại vé điện tử E-Ticket cùng lịch sử đơn hàng.
2. Quản trị viên (Admin): Đăng nhập tài khoản quản trị (`admin@cineseat.vn`), theo dõi bảng điều khiển KPI doanh thu trực quan qua biểu đồ Chart.js, quản lý danh mục phim (thêm phim, cập nhật, upload poster thật), tạo suất chiếu mới (tự động sinh 40 ghế), quản lý rạp và phòng chiếu, quản lý mã khuyến mãi, và kích hoạt quét thu hồi ghế quá hạn tức thì.
3. Tác vụ hệ thống (System Engine): Tự động tính toán giá vé động bằng hàm `FN_TOTAL_PRICE`, kiểm tra xung đột đồng thời, ghi log kiểm toán vào `BOOKING_AUDIT` qua Trigger, và quét thu hồi các đơn hàng `HELD` quá 5 phút chưa thanh toán.

3.1.2. Yêu cầu chức năng
- Phân hệ Khách hàng: Tra cứu và lọc phim theo trạng thái đang chiếu; xem chi tiết phim và lịch chiếu nhóm theo cụm rạp; chọn từ 1 đến 8 ghế trên sơ đồ 5 hàng (A-E) 8 cột trực quan; tính tổng tiền tự động; đếm ngược 5 phút thanh toán; thanh toán qua DEMO_QR / DEMO_CARD / DEMO_CASH; tra cứu lịch sử đặt vé.
- Phân hệ Quản trị: Đăng nhập phân quyền Cookie; Dashboard tổng quan 4 chỉ số KPI (tổng doanh thu, vé đã bán, suất đang chiếu, đơn chờ xử lý); CRUD phim với tính năng tải ảnh poster trực tiếp từ máy; CRUD suất chiếu với cơ chế tự động ngăn chặn trùng giờ chiếu trong cùng một phòng; CRUD rạp và phòng; kích hoạt quét giải phóng ghế quá hạn bằng thủ tục `PR_EXPIRE_HOLDS`.

3.1.3. Yêu cầu phi chức năng
- Tính toàn vẹn và nhất quán: Đảm bảo không xảy ra hiện tượng bán trùng ghế; tổng tiền thanh toán luôn bằng tổng tiền các ghế chi tiết.
- Hiệu năng: Thời gian phản hồi API tra cứu dưới 200ms; hỗ trợ Connection Pooling thông qua ODP.NET Core.
- Bảo mật và An toàn: Sử dụng tham số hóa hoàn toàn (Bind Variables) chống lỗi SQL Injection; bọc giao dịch bằng `OracleTransaction` tường minh với cơ chế rollback an toàn khi xảy ra sự cố.

================================================================================
[TRANG 13]
================================================================================

3.2. Mô hình hóa hệ thống bằng UML
3.2.1. Biểu đồ Use Case tổng quát
Biểu đồ Use Case thể hiện rõ ranh giới chức năng giữa Khách hàng, Quản trị viên và Tác vụ hệ thống:
- Khách hàng: «Tra cứu phim», «Xem lịch chiếu», «Chọn ghế & Giữ chỗ» (include «Tính giá vé» và extend «Nhập mã khuyến mãi»), «Thanh toán vé», «Hủy giữ chỗ», «Xem vé điện tử QR».
- Quản trị viên: «Đăng nhập Admin», «Xem Dashboard KPI», «Quản lý phim», «Quản lý suất chiếu», «Quản lý rạp & phòng», «Quản lý khuyến mãi», «Quét giải phóng ghế quá hạn».
- Tác vụ hệ thống: «Thu hồi ghế hết hạn TTL 5 phút», «Ghi log kiểm toán trạng thái».

3.2.2. Đặc tả các Use Case chính
Đặc tả Use Case "Giữ ghế" (Hold Seats):
- Tác nhân chính: Khách hàng.
- Tiền điều kiện: Khách hàng đã chọn một suất chiếu hợp lệ; suất chiếu chưa bắt đầu và còn ít nhất 1 ghế trống.
- Dữ liệu đầu vào: `user_id`, `showtime_id`, danh sách `seat_id` (1 đến 8 ghế), `promo_code` (nếu có).
- Luồng sự kiện chính:
  1. Người dùng chọn các ghế trên giao diện sơ đồ 3D và bấm "Tiếp tục đặt vé".
  2. Web Server gửi yêu cầu xuống CSDL thực thi `PKG_BOOKING.PR_HOLD`.
  3. CSDL khóa dòng suất chiếu `FOR UPDATE WAIT 5`, dọn dẹp các ghế quá hạn trước đó.
  4. CSDL bóc tách mảng JSON bằng `JSON_TABLE`, kiểm tra tính hợp lệ và duy nhất của ghế.
  5. CSDL tính giá bằng `FN_TOTAL_PRICE`, tạo bản ghi `BOOKINGS` trạng thái `HELD` với thời hạn 5 phút.
  6. CSDL cập nhật các ghế tương ứng trong `SHOWTIME_SEATS` sang `HELD`.
  7. CSDL thêm chi tiết vào `BOOKING_SEATS`, Trigger `TRG_BOOKING_AUDIT` tự động ghi nhận log.
  8. Web Server nhận `booking_id`, `total_amount`, `expires_at` và hiển thị màn hình thanh toán đếm ngược.
- Ngoại lệ: Nếu có bất kỳ ghế nào đã bị người khác chọn trước, CSDL phát lỗi `ORA-20005`, thực thi `ROLLBACK TO sp_hold`, Web Server hiển thị thông báo "Ghế đã có người đặt trước! Vui lòng chọn ghế khác."

================================================================================
[TRANG 14]
================================================================================

3.2.3. Biểu đồ tuần tự giữ ghế (Sequence Diagram - PR_HOLD)
Luồng tương tác giữa Khách hàng, Giao diện Web, Service C#, và Package PL/SQL:
1. `Client` -> `BookingsController`: Gửi HTTP POST yêu cầu giữ ghế kèm danh sách ghế.
2. `BookingsController` -> `CineSeatService`: Gọi `HoldSeatsAsync(userId, showtimeId, seatIds, promoCode)`.
3. `CineSeatService` -> `OracleDatabase`: Mở `OracleConnection`, bắt đầu `OracleTransaction`, thiết lập `SAVEPOINT sp_hold`.
4. `CineSeatService` -> `PKG_BOOKING`: Thực thi `PR_HOLD(p_user_id, p_showtime_id, p_seats_json, p_promo_code, out booking_id, out total, out expires)`.
5. `OracleDatabase`: Khóa dòng `SHOWTIMES FOR UPDATE WAIT 5` -> Parse `JSON_TABLE` -> Kiểm tra ghế -> Update `SHOWTIME_SEATS` -> Insert `BOOKINGS` & `BOOKING_SEATS` -> Trigger ghi audit.
6. `PKG_BOOKING` -> `CineSeatService`: Trả về các tham số OUT (`booking_id`, `total`, `expires`).
7. `CineSeatService`: Thực hiện `transaction.Commit()`.
8. `BookingsController` -> `Client`: Chuyển hướng sang màn hình Checkout đếm ngược 5 phút.

3.2.4. Biểu đồ tuần tự thanh toán (Sequence Diagram - PR_CONFIRM)
1. `Client` -> `BookingsController`: Chọn phương thức thanh toán (DEMO_QR) và bấm "Xác nhận thanh toán".
2. `BookingsController` -> `CineSeatService`: Gọi `ConfirmBookingAsync(bookingId, paymentMethod)`.
3. `CineSeatService` -> `PKG_BOOKING`: Gọi `PR_CONFIRM(p_booking_id, p_method)`.
4. `OracleDatabase`: Kiểm tra đơn hàng còn `HELD` và trong hạn 5 phút -> Chuyển ghế sang `SOLD` -> Chèn bảng `PAYMENTS` -> Chuyển đơn hàng sang `PAID` -> Trigger ghi log kiểm toán `PAID`.
5. `CineSeatService`: Gọi `transaction.Commit()`.
6. `BookingsController` -> `Client`: Điều hướng đến trang Vé điện tử E-Ticket thành công.

3.2.5. Biểu đồ hoạt động đặt vé (Activity Diagram)
Bắt đầu -> Chọn phim & Suất chiếu -> Xem sơ đồ ghế -> Chọn 1-8 ghế -> Nhập mã giảm giá (tùy chọn) -> Bấm Đặt vé -> [Hệ thống kiểm tra ghế trống?] 
- Nếu Trùng ghế: Thông báo lỗi ORA-20005 -> Quay lại chọn ghế.
- Nếu Hợp lệ: Giữ chỗ 5 phút, đếm ngược -> [Khách chọn hành động?]
  + Thanh toán: Thực thi `PR_CONFIRM` -> Chuyển `PAID`, xuất vé QR -> Kết thúc.
  + Hủy vé: Thực thi `PR_CANCEL` -> Giải phóng ghế thành `AVAILABLE` -> Kết thúc.
  + Quá 5 phút: `PR_EXPIRE_HOLDS` quét chuyển `EXPIRED`, giải phóng ghế -> Kết thúc.

================================================================================
[TRANG 15]
================================================================================

3.3. Thiết kế cơ sở dữ liệu
3.3.1. Mô hình ERD vật lý 12 bảng
Lược đồ quan hệ thực thể của CineSeat bao gồm 12 bảng chuẩn 3NF:
1. `CINEMAS` (cinema_id, cinema_name, address)
2. `ROOMS` (room_id, cinema_id, room_name)
3. `SEATS` (seat_id, room_id, row_label, seat_number, seat_type)
4. `MOVIES` (movie_id, title, genre, duration_min, age_rating, synopsis, poster_file, is_active)
5. `SHOWTIMES` (showtime_id, movie_id, room_id, start_at, end_at, base_price, is_active)
6. `SHOWTIME_SEATS` (showtime_id, seat_id, status, booking_id, held_until)
7. `APP_USERS` (user_id, full_name, email, phone, created_at)
8. `PROMOTIONS` (promotion_id, promo_code, discount_pct, valid_until, is_active)
9. `BOOKINGS` (booking_id, user_id, showtime_id, promotion_id, status, total_amount, created_at, expires_at, paid_at)
10. `BOOKING_SEATS` (booking_id, seat_id, unit_price)
11. `PAYMENTS` (payment_id, booking_id, amount, method, payment_status, created_at)
12. `BOOKING_AUDIT` (audit_id, booking_id, old_status, new_status, changed_at, description)

Mối quan hệ chính:
- `CINEMAS` (1) ---- (N) `ROOMS` (1) ---- (N) `SEATS`
- `MOVIES` (1) ---- (N) `SHOWTIMES`
- `ROOMS` (1) ---- (N) `SHOWTIMES`
- `SHOWTIMES` (1) ---- (N) `SHOWTIME_SEATS` (N) ---- (1) `SEATS`
- `APP_USERS` (1) ---- (N) `BOOKINGS` (1) ---- (N) `BOOKING_SEATS` (N) ---- (1) `SEATS`
- `PROMOTIONS` (1) ---- (N) `BOOKINGS`
- `BOOKINGS` (1) ---- (1) `PAYMENTS`
- `BOOKINGS` (1) ---- (N) `BOOKING_AUDIT`

================================================================================
[TRANG 16]
================================================================================

3.3.2. Từ điển dữ liệu và ràng buộc toàn vẹn

1. Bảng `APP_USERS` (Người dùng)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `user_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã người dùng định danh duy nhất. |
| `full_name` | NVARCHAR2(120) | NOT NULL | Họ và tên khách hàng. |
| `email` | VARCHAR2(180) | NOT NULL, UNIQUE | Địa chỉ email đăng nhập, không trùng lặp. |
| `phone` | VARCHAR2(20) | NULL | Số điện thoại liên hệ. |
| `created_at` | TIMESTAMP | DEFAULT LOCALTIMESTAMP, NOT NULL | Thời điểm khởi tạo tài khoản. |

2. Bảng `MOVIES` (Phim)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `movie_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã phim định danh. |
| `title` | NVARCHAR2(160) | NOT NULL | Tựa đề phim chiếu rạp. |
| `genre` | NVARCHAR2(80) | NOT NULL | Thể loại phim (Hành động, Viễn tưởng,...). |
| `duration_min`| NUMBER(4) | NOT NULL, CHECK (duration_min BETWEEN 30 AND 300) | Thời lượng phim (phút). |
| `age_rating` | VARCHAR2(10) | NOT NULL, CHECK (age_rating IN ('P','K','T13','T16','T18')) | Phân loại độ tuổi khán giả. |
| `synopsis` | NVARCHAR2(900) | NULL | Tóm tắt nội dung kịch bản phim. |
| `poster_file` | VARCHAR2(100) | NULL | Tên file hình ảnh poster phim. |
| `is_active` | NUMBER(1) | DEFAULT 1, CHECK (is_active IN (0,1)) | Cờ trạng thái: 1=Đang chiếu, 0=Ngừng chiếu. |

3. Bảng `CINEMAS` (Cụm rạp)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `cinema_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã rạp chiếu phim. |
| `cinema_name` | NVARCHAR2(120) | NOT NULL | Tên cụm rạp (CineSeat Nguyễn Huệ,...). |
| `address` | NVARCHAR2(260) | NOT NULL | Địa chỉ chi tiết cụm rạp. |

4. Bảng `ROOMS` (Phòng chiếu)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `room_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã phòng chiếu. |
| `cinema_id` | NUMBER | FK -> CINEMAS(cinema_id), NOT NULL | Mã rạp sở hữu phòng chiếu. |
| `room_name` | NVARCHAR2(50) | NOT NULL | Tên phòng (Phòng A1, Phòng VIP 01,...). |
| *Ràng buộc* | UNIQUE(cinema_id, room_name) | | Đảm bảo không trùng tên phòng trong cùng 1 rạp. |

================================================================================
[TRANG 17]
================================================================================

5. Bảng `SEATS` (Ghế vật lý)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `seat_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã ghế vật lý duy nhất. |
| `room_id` | NUMBER | FK -> ROOMS(room_id), NOT NULL | Mã phòng chứa ghế. |
| `row_label` | VARCHAR2(2) | NOT NULL | Hàng ghế (A, B, C, D, E). |
| `seat_number` | NUMBER(3) | NOT NULL, CHECK (seat_number BETWEEN 1 AND 40) | Vị trí số ghế trong hàng. |
| `seat_type` | VARCHAR2(15) | NOT NULL, CHECK (seat_type IN ('STANDARD','VIP')) | Phân hạng ghế: Tiêu chuẩn hoặc VIP. |
| *Ràng buộc* | UNIQUE(room_id, row_label, seat_number) | | Không trùng vị trí ghế trong cùng 1 phòng. |

6. Bảng `SHOWTIMES` (Suất chiếu)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `showtime_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã suất chiếu duy nhất. |
| `movie_id` | NUMBER | FK -> MOVIES(movie_id), NOT NULL | Phim được chiếu. |
| `room_id` | NUMBER | FK -> ROOMS(room_id), NOT NULL | Phòng chiếu tổ chức suất chiếu. |
| `start_at` | TIMESTAMP | NOT NULL | Thời điểm bắt đầu chiếu. |
| `end_at` | TIMESTAMP | NOT NULL | Thời điểm kết thúc (end_at > start_at). |
| `base_price` | NUMBER(12,0) | NOT NULL, CHECK (base_price > 0) | Giá vé cơ bản cho suất chiếu (VNĐ). |
| `is_active` | NUMBER(1) | DEFAULT 1, CHECK (is_active IN (0,1)) | Trạng thái suất chiếu khả dụng. |
| *Ràng buộc* | UNIQUE(room_id, start_at) | | Chặn xếp 2 suất chiếu cùng lúc trong 1 phòng. |

7. Bảng `SHOWTIME_SEATS` (Trạng thái ghế theo suất chiếu)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `showtime_id` | NUMBER | PK ghép, FK -> SHOWTIMES(showtime_id) | Mã suất chiếu. |
| `seat_id` | NUMBER | PK ghép, FK -> SEATS(seat_id) | Mã ghế vật lý. |
| `status` | VARCHAR2(15) | NOT NULL, CHECK (status IN ('AVAILABLE','HELD','SOLD')) | Trạng thái hiện hành của ghế. |
| `booking_id` | NUMBER | FK -> BOOKINGS(booking_id), NULL | Đơn hàng đang giữ/đã mua ghế. |
| `held_until` | TIMESTAMP | NULL | Hạn chót giữ ghế tạm thời (5 phút). |

8. Bảng `PROMOTIONS` (Khuyến mãi)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `promotion_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã khuyến mãi định danh. |
| `promo_code` | VARCHAR2(30) | NOT NULL, UNIQUE | Mã nhập (SALE10, STUDENT,...). |
| `discount_pct` | NUMBER(5,2) | NOT NULL, CHECK (discount_pct BETWEEN 0 AND 50) | Tỷ lệ giảm giá (0% đến 50%). |
| `valid_until` | DATE | NOT NULL | Hạn chót sử dụng mã khuyến mãi. |
| `is_active` | NUMBER(1) | DEFAULT 1, CHECK (is_active IN (0,1)) | Trạng thái kích hoạt của mã. |

================================================================================
[TRANG 18]
================================================================================

9. Bảng `BOOKINGS` (Đơn đặt vé)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `booking_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã đơn đặt vé định danh. |
| `user_id` | NUMBER | FK -> APP_USERS(user_id), NOT NULL | Khách hàng thực hiện đặt vé. |
| `showtime_id` | NUMBER | FK -> SHOWTIMES(showtime_id), NOT NULL | Suất chiếu được đặt. |
| `promotion_id` | NUMBER | FK -> PROMOTIONS(promotion_id), NULL | Mã khuyến mãi áp dụng. |
| `status` | VARCHAR2(15) | NOT NULL, CHECK (status IN ('HELD','PAID','CANCELLED','EXPIRED')) | Trạng thái đơn đặt vé. |
| `total_amount` | NUMBER(12,0) | NOT NULL, CHECK (total_amount >= 0) | Tổng tiền thanh toán của đơn (VNĐ). |
| `created_at` | TIMESTAMP | DEFAULT LOCALTIMESTAMP, NOT NULL | Thời điểm khởi tạo đơn hàng. |
| `expires_at` | TIMESTAMP | NOT NULL | Thời điểm hết hạn giữ ghế (sau 5 phút). |
| `paid_at` | TIMESTAMP | NULL | Thời điểm hoàn tất thanh toán. |

10. Bảng `BOOKING_SEATS` (Chi tiết ghế của đơn hàng)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `booking_id` | NUMBER | PK ghép, FK -> BOOKINGS(booking_id) | Mã đơn đặt vé. |
| `seat_id` | NUMBER | PK ghép, FK -> SEATS(seat_id) | Mã ghế trong đơn hàng. |
| `unit_price` | NUMBER(12,0) | NOT NULL, CHECK (unit_price >= 0) | Đơn giá đã chốt của ghế (VNĐ). |

11. Bảng `PAYMENTS` (Thanh toán mô phỏng)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `payment_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã giao dịch thanh toán. |
| `booking_id` | NUMBER | FK -> BOOKINGS(booking_id), NOT NULL, UNIQUE | Đơn hàng thanh toán (1-1). |
| `amount` | NUMBER(12,0) | NOT NULL, CHECK (amount >= 0) | Số tiền giao dịch thanh toán. |
| `method` | VARCHAR2(20) | NOT NULL, CHECK (method IN ('DEMO_QR','DEMO_CARD','DEMO_CASH')) | Phương thức thanh toán mô phỏng. |
| `payment_status`| VARCHAR2(12) | DEFAULT 'PAID', NOT NULL | Trạng thái giao dịch. |
| `created_at` | TIMESTAMP | DEFAULT LOCALTIMESTAMP, NOT NULL | Thời điểm giao dịch phát sinh. |

12. Bảng `BOOKING_AUDIT` (Nhật ký kiểm toán)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| `audit_id` | NUMBER | PK, GENERATED BY DEFAULT AS IDENTITY | Mã nhật ký kiểm toán. |
| `booking_id` | NUMBER | NOT NULL | Mã đơn hàng được theo dõi. |
| `old_status` | VARCHAR2(15) | NULL | Trạng thái trước khi thay đổi. |
| `new_status` | VARCHAR2(15) | NOT NULL | Trạng thái mới được ghi nhận. |
| `changed_at` | TIMESTAMP | DEFAULT LOCALTIMESTAMP, NOT NULL | Thời điểm thay đổi trạng thái. |
| `description` | NVARCHAR2(240) | NULL | Mô tả nguyên nhân thay đổi. |

3.3.3. Vòng đời trạng thái và các bất biến nghiệp vụ
- Bất biến 1: Một ghế trong cùng một suất chiếu chỉ có tối đa một trạng thái duy nhất tại một thời điểm (`AVAILABLE`, `HELD`, hoặc `SOLD`).
- Bất biến 2: Ghế chuyển sang `SOLD` bắt buộc phải trải qua trạng thái `HELD` bởi chính đơn hàng đó (được bảo vệ tuyệt đối bởi Trigger `TRG_SEAT_STATE_GUARD`).
- Bất biến 3: Tổng tiền thanh toán trên bảng `PAYMENTS` luôn khớp chính xác với `BOOKINGS.total_amount` và bằng $\sum \text{BOOKING\_SEATS.unit\_price}$.

================================================================================
[TRANG 19]
================================================================================

3.4. Thiết kế Ứng dụng Web C# ASP.NET Core MVC

3.4.1. Kiến trúc phân tầng và Service tích hợp Oracle
Ứng dụng CineSeat.Web được tổ chức theo chuẩn kiến trúc phân tầng sạch (Clean Architecture):
- Controllers: `MoviesController` (Trang chủ, chi tiết phim, lịch chiếu rạp), `BookingsController` (Sơ đồ ghế, Checkout, Vé QR, Lịch sử), `AdminController` (Quản trị toàn diện).
- Services: `ICineSeatService` và `CineSeatService` chịu trách nhiệm mở kết nối `OracleConnection`, bọc giao dịch `OracleTransaction`, thực thi các Stored Procedure của Package `PKG_BOOKING` bằng thư viện ODP.NET Core.
- Helpers: `PosterHelper` thông minh tự động tìm kiếm, làm sạch ký tự đặc biệt và giải quyết ảnh poster thật từ thư mục `wwwroot/images/`.

Mã nguồn C# mẫu thực thi thủ tục `PKG_BOOKING.PR_HOLD` trong `CineSeatService.cs`:
```csharp
public async Task<HoldSeatsResult> HoldSeatsAsync(int userId, int showtimeId, List<int> seatIds, string? promoCode)
{
    using var conn = _dbFactory.CreateConnection();
    await conn.OpenAsync();
    using var tx = conn.BeginTransaction();
    try
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "PKG_BOOKING.PR_HOLD";
        cmd.CommandType = CommandType.StoredProcedure;
        
        string seatsJson = JsonSerializer.Serialize(seatIds);
        cmd.Parameters.Add("p_user_id", OracleDbType.Int32).Value = userId;
        cmd.Parameters.Add("p_showtime_id", OracleDbType.Int32).Value = showtimeId;
        cmd.Parameters.Add("p_seats_json", OracleDbType.Clob).Value = seatsJson;
        cmd.Parameters.Add("p_promo_code", OracleDbType.Varchar2).Value = (object?)promoCode ?? DBNull.Value;
        
        var pBookingId = cmd.Parameters.Add("p_booking_id", OracleDbType.Int32, ParameterDirection.Output);
        var pTotal = cmd.Parameters.Add("p_total", OracleDbType.Decimal, ParameterDirection.Output);
        var pExpires = cmd.Parameters.Add("p_expires", OracleDbType.TimeStamp, ParameterDirection.Output);
        
        await cmd.ExecuteNonQueryAsync();
        await tx.CommitAsync();
        
        return new HoldSeatsResult {
            BookingId = Convert.ToInt32(pBookingId.Value.ToString()),
            TotalAmount = Convert.ToDecimal(pTotal.Value.ToString()),
            ExpiresAt = DateTime.Parse(pExpires.Value.ToString())
        };
    }
    catch (OracleException ex)
    {
        await tx.RollbackAsync();
        throw new CineSeatException(ex.Number, ex.Message);
    }
}
```

3.4.2. Quản lý giao dịch tường minh và ánh xạ mã lỗi ORA
Lớp `CineSeatException` thực hiện ánh xạ toàn bộ mã lỗi từ tầng Oracle sang thông điệp tiếng Việt thân thiện với khách hàng:
- `ORA-20001`: "Số lượng ghế đặt phải từ 1 đến 8 ghế!"
- `ORA-20002`: "Mã khuyến mãi không tồn tại hoặc đã hết hạn sử dụng!"
- `ORA-20004`: "Danh sách ghế không hợp lệ hoặc bị chọn trùng lặp!"
- `ORA-20005`: "Ghế đã có khách hàng khác đặt trước! Vui lòng chọn ghế khác."
- `ORA-20007`: "Đơn đặt vé không ở trạng thái hợp lệ để thanh toán."
- `ORA-20008`: "Đã quá thời gian giữ chỗ 5 phút! Đơn đặt vé đã bị hủy tự động."

================================================================================
[TRANG 20]
================================================================================

3.4.3. Các màn hình chức năng Cổng Khách hàng và Cổng Quản trị

1. Cổng Khách hàng (Customer Portal):
- Trang chủ (`/` hoặc `/Movies`): Hero Banner phim nổi bật sắc nét tỷ lệ 2:3, thanh tìm kiếm phim theo tên và thể loại, lưới 13 thẻ phim với đầy đủ nhãn độ tuổi (`P`, `T13`, `T16`, `T18`), thời lượng và nút "Đặt vé ngay".
- Chi tiết Phim & Lịch chiếu theo Rạp (`/Movies/CinemaShowtimes`): Thẻ chọn rạp dạng card nằm ngang, dải chọn ngày xem trực quan, thẻ phim hiển thị các khung giờ chiếu kèm số ghế trống thời gian thực.
- Màn hình chọn ghế 3D (`/Bookings/SelectSeats`): Thiết kế mô phỏng màn hình cong chiếu rạp 3D, sơ đồ 40 ghế (5 hàng A-E, cột 1-8), phân màu trực quan: Xám (Ghế trống Standard), Vàng Gold (Ghế VIP), Đỏ đậm (Ghế đã bán), Cam (Ghế bạn đang chọn). Panel tóm tắt sticky card tính tiền thời gian thực và cho phép nhập mã giảm giá.
- Trang Checkout đếm ngược (`/Bookings/Checkout`): Đồng hồ đếm ngược 5 phút thời gian thực, bảng phân rã chi tiết tiền vé, lựa chọn 3 hình thức thanh toán mô phỏng (`DEMO_QR`, `DEMO_CARD`, `DEMO_CASH`), nút hủy đơn chủ động.
- Vé điện tử E-Ticket (`/Bookings/Detail`): Thiết kế dạng Voucher cao cấp, hiển thị mã đặt vé, mã QR Code động, phòng chiếu, danh sách ghế và mã kiểm toán.
- Lịch sử đặt vé (`/Bookings/History`): Danh sách các vé đã đặt phân loại theo trạng thái (`PAID`, `CANCELLED`, `EXPIRED`).

2. Cổng Quản trị (Admin Portal):
- Bảng điều khiển KPI (`/Admin/Dashboard`): 4 thẻ KPI nổi bật (Doanh thu thực tế, Vé đã bán, Suất chiếu hoạt động, Đơn chờ xử lý), biểu đồ cột Chart.js thể hiện doanh thu theo ngày, và bảng theo dõi 10 bản ghi kiểm toán gần nhất từ `BOOKING_AUDIT`.
- Quản lý phim (`/Admin/Movies`): Bảng danh mục 13 phim, chức năng thêm phim mới có xem trước ảnh (FileReader) và tải file ảnh trực tiếp lên server `wwwroot/images/`, sửa phim, bật/tắt ngừng chiếu.
- Quản lý suất chiếu (`/Admin/Showtimes`): Tạo suất chiếu mới với cơ chế tự động kiểm tra chống trùng phòng (`start_at < new_end AND end_at > new_start`), tự động sinh 40 bản ghi ghế `AVAILABLE` trong `SHOWTIME_SEATS`.
- Quản lý Rạp & Phòng (`/Admin/Cinemas`): Quản lý danh mục rạp, thêm phòng chiếu mới và tự động sinh 40 ghế vật lý (A-C Standard, D-E VIP) vào bảng `SEATS`.
- Quản lý Khuyến mãi (`/Admin/Promotions`): Thêm mã giảm giá từ 1% đến 50%, thiết lập hạn dùng, bật/tắt hoạt động.
- Quản lý Đơn vé & Quét quá hạn (`/Admin/Bookings`): Lọc đơn vé theo trạng thái, nút bấm kích hoạt thủ tục `PR_EXPIRE_HOLDS` giải phóng toàn bộ ghế giữ quá hạn 5 phút tức thì.

================================================================================
[TRANG 21]
================================================================================

3.5. Tổ chức cài đặt và nạp dữ liệu Oracle 23ai trên Docker

Hệ thống được tổ chức triển khai tự động hóa thông qua Docker Compose và các script SQL phân tầng:
1. `docker-compose.yml`:
   - Service `oracle-23ai`: Image `container-registry.oracle.com/database/free:latest`, cấu hình biến môi trường `ORACLE_PASSWORD=CineSeat123`, `NLS_LANG=AMERICAN_AMERICA.AL32UTF8`, mở port `1521:1521`, cấu hình healthcheck tự động.
   - Service `cineseat-web`: Multi-stage build Dockerfile (.NET 8 SDK -> ASP.NET Runtime), mở port `5000:8080`, phụ thuộc vào trạng thái `healthy` của container Oracle.
2. Thư mục `database/` chứa các script triển khai theo thứ tự chuẩn:
   - `00_init_user.sql`: Kết nối vào `FREEPDB1`, tạo User/Schema riêng `CINESEAT`, cấp đầy đủ quyền tài nguyên và hạn ngạch trên tablespace `USERS`.
   - `01_schema.sql`: Khởi tạo 12 bảng quan hệ chuẩn 3NF, ràng buộc khóa chính, khóa ngoại, ràng buộc CHECK và 2 Views nghiệp vụ.
   - `02_logic.sql`: Biên dịch Package `PKG_BOOKING` (Spec và Body), Stored Procedure độc lập `PR_EXPIRE_HOLDS`, và 2 Triggers `TRG_BOOKING_AUDIT`, `TRG_SEAT_STATE_GUARD`.
   - `03_seed.sql`: Nạp bộ dữ liệu mẫu chuẩn UTF-8 gồm 13 bộ phim bom tấn, 13 người dùng, 2 cụm rạp, 3 phòng chiếu, 120 ghế vật lý, 20 suất chiếu và đồng bộ 800 ghế trạng thái theo suất.
   - `04_verify.sql`: Truy vấn bảng `USER_OBJECTS` và `USER_ERRORS` bảo đảm toàn bộ 12 bảng, 2 view, 1 package, 1 procedure, 2 trigger đều đạt trạng thái `VALID`.

Script PowerShell `setup-db.ps1` thực thi toàn bộ quy trình trên chỉ với một câu lệnh duy nhất:
```powershell
powershell -ExecutionPolicy Bypass -File .\database\setup-db.ps1
```

================================================================================
[TRANG 22]
================================================================================

CHƯƠNG 4. KIỂM THỬ VÀ TỐI ƯU HÓA

4.1. Kịch bản kiểm thử tự động xUnit (TC01 đến TC10)
Dự án kiểm thử tự động `CineSeat.Tests` sử dụng nền tảng xUnit trên .NET 8, kết nối trực tiếp vào schema `CINESEAT` trên container Oracle 23ai để kiểm thử toàn diện mọi tình huống nghiệp vụ:

| Mã test | Tên kịch bản kiểm thử | Dữ liệu đầu vào & Thao tác | Kết quả kỳ vọng | Kết quả thực tế |
| :---: | :--- | :--- | :--- | :---: |
| **TC01** | Tính giá vé đa điều kiện | Suất tối cuối tuần, 2 ghế, mã SALE10. | Giá tính đúng hệ số $1.20 \times 1.10$, giảm 10%, làm tròn -3. | **PASS** |
| **TC02** | Bắt mã khuyến mãi sai | Mã ưu đãi không tồn tại `MA_SAI_999`. | CSDL phát sinh lỗi `ORA-20002`. | **PASS** |
| **TC03** | Giữ ghế và hủy đơn hợp lệ | Giữ 1 ghế `AVAILABLE`, sau đó gọi `PR_CANCEL`. | Đơn chuyển `CANCELLED`, ghế trả về `AVAILABLE`. | **PASS** |
| **TC04** | Chống tranh chấp giữ ghế | Giữ ghế đã có người giữ trong cùng suất. | CSDL phát sinh lỗi `ORA-20005`, rollback toàn bộ. | **PASS** |
| **TC05** | Thanh toán vé thành công | Giữ ghế và gọi `PR_CONFIRM` với `DEMO_QR`. | Đơn chuyển `PAID`, ghế `SOLD`, tạo bản ghi `PAYMENTS`. | **PASS** |
| **TC06** | Thu hồi đơn quá hạn 5 phút | Tạo đơn HELD quá hạn, gọi `PR_EXPIRE_HOLDS`. | Đơn chuyển `EXPIRED`, ghế giải phóng `AVAILABLE`. | **PASS** |
| **TC07** | Phân quyền Cookie Auth | Đăng nhập tài khoản Admin và Khách hàng. | Cấp đúng Claim Role `ADMIN` và `CUSTOMER`. | **PASS** |
| **TC08** | Chống trùng phòng chiếu | Tạo suất chiếu mới có giờ đè lên suất cũ. | Bị chặn bởi `CheckShowtimeOverlapAsync`. | **PASS** |
| **TC09** | Nhận diện poster thật | Gọi `PosterHelper` với tựa phim có ký tự `:`. | Tự động khớp file `.jpg` thật không lỗi. | **PASS** |
| **TC10** | Admin CRUD Rạp & Phòng | Thêm phòng chiếu mới vào cụm rạp. | Tự động sinh đủ 40 ghế vật lý vào bảng `SEATS`. | **PASS** |

Kết quả chạy lệnh `dotnet test` thực tế trên hệ thống:
```text
Passed!  - Failed: 0, Passed: 10, Skipped: 0, Total: 10, Duration: 2.4s - CineSeat.Tests.dll
```
100% các kịch bản kiểm thử cốt lõi và kiểm thử mở rộng đều vượt qua hoàn hảo.

4.1.2. Kiểm chứng các ca biên và ngoại lệ
- Đặt 0 ghế hoặc vượt quá 8 ghế: Phát lỗi `ORA-20001` ngay tại hàm định giá.
- Danh sách ghế có phần tử trùng lặp (ví dụ `[5, 5]`): `JSON_TABLE` phát hiện số lượng ghế phân biệt khác tổng số ghế, lập tức chặn bằng mã lỗi `ORA-20004`.
- Thanh toán đơn hàng đã quá hạn 5 phút: `PR_CONFIRM` kiểm tra điều kiện `expires_at > LOCALTIMESTAMP`, từ chối thanh toán với mã lỗi `ORA-20008`.

================================================================================
[TRANG 23]
================================================================================

4.2. Kiểm chứng kỹ thuật chống bán trùng ghế (Concurrency Test)

Để kiểm chứng khả năng bảo vệ dữ liệu khi có xung đột đồng thời cao, nhóm đã thiết lập kịch bản kiểm thử hai phiên (2 concurrent sessions) cùng truy cập tranh chấp 1 ghế:

Kịch bản thực thi:
- Phiên 1 (Session A - Khách hàng 1): Chọn Suất chiếu ID = 1, Ghế ID = 5 (đang `AVAILABLE`). Gọi `PKG_BOOKING.PR_HOLD` nhưng chưa thực hiện `COMMIT`.
- Phiên 2 (Session B - Khách hàng 2): Gần như đồng thời (sau 100ms), gửi yêu cầu giữ đúng Ghế ID = 5 trên Suất chiếu ID = 1.

Diễn biến xử lý bên trong Oracle Database 23ai:
1. Session A bước vào thủ tục `PR_HOLD`, thực thi câu lệnh:
   ```sql
   SELECT showtime_id INTO v_show_id FROM showtimes
    WHERE showtime_id = 1 AND is_active = 1 FOR UPDATE WAIT 5;
   ```
   Oracle cấp quyền khóa độc quyền (Exclusive Row Lock) trên bản ghi suất chiếu số 1 cho Session A.
2. Session B gọi `PR_HOLD` và thực thi câu lệnh `SELECT ... FOR UPDATE WAIT 5` trên cùng suất chiếu số 1. Do Session A đang giữ khóa, Session B bị đưa vào hàng đợi chờ giải phóng tài nguyên (chờ tối đa 5 giây).
3. Session A tiếp tục cập nhật ghế ID = 5 sang `HELD` thành công, tạo đơn hàng mới và gửi lệnh `COMMIT`. Ngay khi Session A Commit, khóa dòng trên suất chiếu được giải phóng.
4. Session B lập tức được cấp khóa và tiếp tục chạy câu lệnh cập nhật ghế:
   ```sql
   UPDATE showtime_seats SET status = 'HELD', booking_id = ...
    WHERE showtime_id = 1 AND seat_id = 5 AND status = 'AVAILABLE';
   ```
   Do ghế ID = 5 đã bị Session A chuyển sang trạng thái `HELD`, mệnh đề `status = 'AVAILABLE'` không còn thỏa mãn. Câu lệnh UPDATE trả về `SQL%ROWCOUNT = 0`.
5. Thủ tục phát hiện `SQL%ROWCOUNT <> 1`, lập tức ném ngoại lệ:
   ```sql
   RAISE_APPLICATION_ERROR(-20005, 'Ghe da duoc nguoi khac giu/ban hoac khong ton tai');
   ```
   Khối ngoại lệ kích hoạt `ROLLBACK TO sp_hold`, hủy bỏ mọi thao tác của Session B.
6. Session B nhận thông báo lỗi rõ ràng từ ứng dụng web: "Ghế đã có người đặt trước! Vui lòng chọn ghế khác."

Kết luận: Cơ chế kết hợp giữa khóa dòng suất chiếu `FOR UPDATE WAIT 5` và câu lệnh cập nhật nguyên tử có điều kiện `UPDATE ... WHERE status = 'AVAILABLE'` bảo đảm chống bán trùng ghế tuyệt đối 100%.

================================================================================
[TRANG 24]
================================================================================

4.3. Phân tích Execution Plan trước và sau tối ưu

Nhóm tiến hành phân tích kế hoạch thực thi câu lệnh (Execution Plan) thông qua tiện ích `EXPLAIN PLAN FOR` và hàm `DBMS_XPLAN.DISPLAY` trên 2 truy vấn có tần suất sử dụng cao nhất hệ thống.

4.3.1. Truy vấn Q1: Tra cứu suất chiếu theo phim và thời gian
Câu lệnh kiểm thử:
```sql
EXPLAIN PLAN FOR
SELECT s.showtime_id, s.start_at, s.base_price
  FROM showtimes s
 WHERE s.movie_id = 1 AND s.start_at >= LOCALTIMESTAMP;
SELECT * FROM TABLE(DBMS_XPLAN.DISPLAY);
```

1. Kế hoạch khi ép quét toàn bảng (Full Table Scan - HINT `/*+ FULL(s) */`):
```text
-------------------------------------------------------------------------------
| Id  | Operation         | Name      | Rows | Bytes | Cost (%CPU)| Time     |
-------------------------------------------------------------------------------
|   0 | SELECT STATEMENT  |           |    2 |    76 |     3   (0)| 00:00:01 |
|*  1 |  TABLE ACCESS FULL| SHOWTIMES |    2 |    76 |     3   (0)| 00:00:01 |
-------------------------------------------------------------------------------
Predicate Information:
  1 - filter("S"."START_AT">=LOCALTIMESTAMP AND "S"."MOVIE_ID"=1)
```
- Phân tích: Bộ tối ưu (Cost-Based Optimizer - CBO) thực hiện quét tuần tự toàn bộ bảng `SHOWTIMES`. Trên tập dữ liệu nhỏ (20 suất chiếu), chi phí ước tính `Cost = 3`, thời gian ước tính 1ms.

2. Kế hoạch khi sử dụng chỉ mục `ix_showtime_movie_date` (HINT `/*+ INDEX(s ix_showtime_movie_date) */`):
```text
-------------------------------------------------------------------------------------------------------
| Id  | Operation                           | Name                   | Rows | Bytes | Cost | Time     |
-------------------------------------------------------------------------------------------------------
|   0 | SELECT STATEMENT                    |                        |    2 |    76 |   10 | 00:00:01 |
|   1 |  TABLE ACCESS BY INDEX ROWID BATCHED| SHOWTIMES              |    2 |    76 |   10 | 00:00:01 |
|*  2 |   INDEX RANGE SCAN                  | IX_SHOWTIME_MOVIE_DATE |    2 |       |    2 | 00:00:01 |
-------------------------------------------------------------------------------------------------------
Predicate Information:
  2 - access("S"."MOVIE_ID"=1 AND "S"."START_AT">=LOCALTIMESTAMP)
```
- Phân tích: CBO thực hiện duyệt cây B-Tree của chỉ mục `INDEX RANGE SCAN` để lấy danh sách RowID, sau đó quay lại bảng đọc dữ liệu `TABLE ACCESS BY INDEX ROWID BATCHED`. Chi phí tính toán `Cost = 10`.
- Nhận xét khoa học: Trong CSDL Oracle, khi số lượng bản ghi của bảng còn rất nhỏ (20 dòng nằm trọn trong 1 Data Block), chi phí đọc trực tiếp block dữ liệu (Cost=3) luôn rẻ hơn việc vừa đọc block index vừa đọc block dữ liệu (Cost=10). Tuy nhiên, khi quy mô dữ liệu tăng lên hàng triệu suất chiếu, chỉ mục B-Tree sẽ phát huy ưu thế vượt trội khi chỉ đọc một số ít leaf block thay vì quét toàn bộ hàng ngàn data block.

================================================================================
[TRANG 25]
================================================================================

4.3.2. Truy vấn Q2: Lịch sử đơn hàng sắp xếp theo thời gian
Câu lệnh kiểm thử:
```sql
EXPLAIN PLAN FOR
SELECT b.booking_id, b.status, b.total_amount, b.created_at
  FROM bookings b WHERE b.user_id = 1
 ORDER BY b.created_at DESC;
SELECT * FROM TABLE(DBMS_XPLAN.DISPLAY);
```

1. Kế hoạch khi quét toàn bảng (Full Table Scan - HINT `/*+ FULL(b) */`):
```text
--------------------------------------------------------------------------------
| Id  | Operation          | Name     | Rows | Bytes | Cost (%CPU)| Time     |
--------------------------------------------------------------------------------
|   0 | SELECT STATEMENT   |          |    1 |    42 |     4  (25)| 00:00:01 |
|   1 |  SORT ORDER BY     |          |    1 |    42 |     4  (25)| 00:00:01 |
|*  2 |   TABLE ACCESS FULL| BOOKINGS |    1 |    42 |     3   (0)| 00:00:01 |
--------------------------------------------------------------------------------
Predicate Information:
  2 - filter("B"."USER_ID"=1)
```
- Phân tích: CBO phải quét toàn bộ bảng `BOOKINGS` (Cost=3), sau đó thực hiện thêm một thao tác sắp xếp trong bộ nhớ `SORT ORDER BY` tốn thêm 25% CPU, nâng tổng `Cost = 4`.

2. Kế hoạch khi sử dụng chỉ mục `ix_bookings_user_date`:
```text
-------------------------------------------------------------------------------------------------------
| Id  | Operation                           | Name                 | Rows | Bytes | Cost (%CPU)| Time |
-------------------------------------------------------------------------------------------------------
|   0 | SELECT STATEMENT                    |                      |    1 |    42 |     2   (0)| 00:01|
|   1 |  TABLE ACCESS BY INDEX ROWID BATCHED| BOOKINGS             |    1 |    42 |     2   (0)| 00:01|
|*  2 |   INDEX RANGE SCAN                  | IX_BOOKINGS_USER_DATE|    1 |       |     1   (0)| 00:01|
-------------------------------------------------------------------------------------------------------
Predicate Information:
  2 - access("B"."USER_ID"=1)
```
- Phân tích: Khi có chỉ mục ghép `(user_id, created_at)`, CBO chỉ cần duyệt chỉ mục `INDEX RANGE SCAN` với `Cost = 1` và truy xuất dữ liệu với tổng `Cost = 2` (giảm 50% chi phí so với quét toàn bảng). Hơn thế nữa, chỉ mục B-Tree đã tự động lưu dữ liệu có thứ tự, loại bỏ hoàn toàn bước tốn kém `SORT ORDER BY` trong bộ nhớ.

4.3.3. Bảng tổng hợp chi phí Cost và đánh giá chỉ mục

| Truy vấn | Đường dẫn truy cập (Full Scan) | Đường dẫn tối ưu (Index Scan) | Mức độ tối ưu Cost | Đánh giá kỹ thuật |
| :--- | :---: | :---: | :---: | :--- |
| **Q1 (Suất chiếu)** | Cost = 3 (Quét toàn bộ bảng) | Cost = 10 (`IX_SHOWTIME_MOVIE_DATE`) | Tăng trên bảng nhỏ, tối ưu trên bảng lớn | Index giúp định vị chính xác theo phim và ngày chiếu mà không phụ thuộc dung lượng bảng. |
| **Q2 (Lịch sử đơn)**| Cost = 4 (Quét bảng + Sort CPU) | Cost = 2 (`IX_BOOKINGS_USER_DATE`) | **Giảm 50% chi phí** | Loại bỏ hoàn toàn chi phí sắp xếp `SORT ORDER BY`, truy xuất tức thì lịch sử cá nhân. |

================================================================================
[TRANG 26]
================================================================================

4.4. Đánh giá chất lượng vận hành hệ thống

Qua quá trình kiểm thử tự động và vận hành thực tế trên môi trường Docker, hệ thống đạt được các kết quả ấn tượng:
1. Độ tin cậy giao dịch (Reliability): 100% các giao dịch đặt vé đều tuân thủ chặt chẽ ranh giới giao dịch; không tồn tại bản ghi rác, bản ghi mồ côi hay tình trạng lệch tổng tiền giữa bảng `BOOKINGS`, `BOOKING_SEATS` và `PAYMENTS`.
2. Kiểm soát xung đột (Concurrency Integrity): Thử nghiệm giả lập đa luồng gửi liên tục các request đặt cùng một vị trí ghế cho thấy CSDL xử lý tuần tự hoàn hảo; duy nhất 1 khách hàng nhận vé thành công, các khách hàng còn lại nhận đúng mã lỗi `ORA-20005` và hoàn tác sạch sẽ.
3. Cơ chế tự phục hồi (Self-Healing TTL): Các đơn hàng bị bỏ dở hoặc quá hạn 5 phút đều được thủ tục `PR_EXPIRE_HOLDS` và logic dọn dẹp trong `PR_HOLD` giải phóng ghế ngay lập tức về trạng thái `AVAILABLE`.
4. Trải nghiệm người dùng (UX/UI): Giao diện web tải mượt mà dưới 150ms trên mạng nội bộ, sơ đồ ghế 3D phản ánh chính xác trạng thái ghế thời gian thực từ CSDL Oracle, đầy đủ hình ảnh poster sắc nét, không có bất kỳ lỗi vỡ ảnh hay lỗi font chữ tiếng Việt (nhờ chuẩn UTF-8 `AL32UTF8`).

Bảng đối chiếu tiêu chí chất lượng:

| Tiêu chuẩn kỹ thuật | Mục tiêu thiết kế | Kết quả thực tế đạt được |
| :--- | :--- | :--- |
| **Chuẩn hóa dữ liệu** | Chuẩn 3NF, không dư thừa dữ liệu | Đạt 12 bảng chuẩn 3NF, phân tách lịch sử và trạng thái |
| **Khóa giao dịch** | Không bán trùng ghế | 100% không bán trùng ghế qua `FOR UPDATE WAIT 5` |
| **Thời hạn giữ chỗ** | Tự động hết hạn sau 5 phút | Đơn chuyển `EXPIRED`, ghế giải phóng `AVAILABLE` |
| **Toàn vẹn số tiền** | $\sum \text{Seat} = \text{Booking} = \text{Payment}$ | Nhất quán tuyệt đối, phân bổ phần dư chính xác |
| **Khả năng kiểm toán** | Lưu vết mọi thay đổi trạng thái đơn | Trigger `TRG_BOOKING_AUDIT` ghi log 100% biến động |
| **Kiểm thử tự động** | Tối thiểu 5 kịch bản kiểm thử | Đạt **10/10 Test Cases xUnit PASS 100%** |

================================================================================
[TRANG 27]
================================================================================

CHƯƠNG 5. KẾT LUẬN

5.1. Kết quả đạt được
Sau quá trình nghiên cứu, thiết kế và phát triển nghiêm túc, đề tài "Hệ thống đặt vé xem phim trực tuyến CineSeat" đã hoàn thành xuất sắc mọi mục tiêu đề ra:
1. Xây dựng hoàn chỉnh cơ sở dữ liệu quan hệ chuẩn 3NF gồm 12 bảng, 2 view, 4 chỉ mục trên Oracle Database 23ai Free chạy Docker.
2. Đóng gói trọn vẹn nghiệp vụ cốt lõi vào Package PL/SQL `PKG_BOOKING` gồm Function tính giá đa điều kiện `FN_TOTAL_PRICE`, các Procedure `PR_HOLD`, `PR_CONFIRM`, `PR_CANCEL` và `PR_EXPIRE_HOLDS`.
3. Kiểm soát đồng thời chặt chẽ với cơ chế khóa dòng `FOR UPDATE WAIT 5` và `UPDATE ... WHERE status = 'AVAILABLE'`, loại bỏ hoàn toàn nguy cơ bán trùng ghế.
4. Phát triển ứng dụng web C# ASP.NET Core MVC (.NET 8) hiện đại, kết nối trực tiếp CSDL qua ODP.NET Core, cung cấp đầy đủ Cổng Khách hàng và Cổng Quản trị Admin với dữ liệu thật 100%.
5. Xây dựng bộ kiểm thử tự động xUnit đạt 10/10 test cases PASS 100% và phân tích sâu sắc kế hoạch thực thi câu lệnh truy vấn (Execution Plan).

5.2. Hạn chế của đề tài
- Phương thức thanh toán hiện tại là thanh toán mô phỏng (`DEMO_QR`, `DEMO_CARD`, `DEMO_CASH`), chưa tích hợp cổng thanh toán ngân hàng trực tiếp (VNPay/MoMo) do yêu cầu pháp lý về tài khoản doanh nghiệp.
- Cơ chế khóa đồng thời đang áp dụng ở mức độ khóa toàn bộ suất chiếu (`FOR UPDATE` trên dòng `SHOWTIMES`). Với các rạp có quy mô cực lớn (hàng trăm nghìn người đặt vé cùng lúc), giải pháp này có thể gây nghẽn cổ chai hàng đợi, có thể tối ưu hóa sâu hơn xuống mức khóa từng ghế (`Row-level lock on seat_id`) theo thứ tự định sẵn.

5.3. Hướng phát triển trong tương lai
- Tích hợp Webhook kết nối cổng thanh toán trực tuyến thực tế (VNPay Sandbox / MoMo API) và cổng gửi vé qua Email / SMS Gateway.
- Nghiên cứu áp dụng công nghệ mới của Oracle 23ai: JSON Relational Duality Views hoặc AI Vector Search để gợi ý phim thông minh cho khán giả dựa trên lịch sử đặt vé.
- Nâng cấp cơ chế khóa dòng sang mức khóa ghế mịn (Fine-Grained Seat Locking) có sắp xếp danh sách `seat_id` tăng dần để triệt tiêu hoàn toàn khả năng Deadlock.

5.4. Bảng phân công và tỷ lệ đóng góp của thành viên

| STT | Họ và tên | MSSV | Nhiệm vụ đảm nhiệm chính | Tỷ lệ đóng góp |
| :---: | :--- | :---: | :--- | :---: |
| 1 | **TRẦN CÔNG CHÍNH TRỰC** | 24DH112970 | Trưởng nhóm. Phân tích kiến trúc hệ thống, xây dựng Package PL/SQL `PKG_BOOKING`, lập trình Backend ASP.NET Core MVC, viết bộ kiểm thử xUnit và tổng hợp báo cáo. | **100%** |
| 2 | **HUỲNH THỊ PHƯƠNG CHI** | 24DH113043 | Phân tích yêu cầu nghiệp vụ, thiết kế mô hình Use Case, Sequence, ERD 12 bảng chuẩn 3NF, thiết kế giao diện Razor UI và viết báo cáo. | **100%** |
| 3 | **NGUYỄN TRẦN DUY TÂN** | 24DH141765 | Cài đặt CSDL Oracle 23ai trên Docker, nạp dữ liệu mẫu UTF-8, cấu hình chỉ mục tối ưu, đo đạc và phân tích Execution Plan Q1/Q2. | **100%** |

================================================================================
[TRANG 28]
================================================================================

TÀI LIỆU THAM KHẢO

[1] ThS. Lê Thị Minh Nguyện (2026), "Ngân hàng 25 đề tài đồ án cuối kỳ môn Hệ Quản Trị Cơ Sở Dữ Liệu", Khoa Công nghệ Thông tin, Trường Đại học Ngoại ngữ - Tin học TP.HCM (HUFLIT).
[2] Oracle Corporation (2024), "Oracle Database 23ai PL/SQL Language Reference", Oracle Documentation: https://docs.oracle.com/en/database/oracle/oracle-database/23/lnpls/
[3] Oracle Corporation (2024), "Oracle Database 23ai SQL Language Reference", Oracle Documentation: https://docs.oracle.com/en/database/oracle/oracle-database/23/sqlrf/
[4] Oracle Corporation (2024), "Oracle Data Provider for .NET (ODP.NET) Core Documentation", Oracle .NET Developer Center: https://www.oracle.com/database/technologies/appdev/dotnet.html
[5] Microsoft Corporation (2024), "ASP.NET Core MVC Documentation (.NET 8.0)", Microsoft Learn: https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
[6] xUnit.net (2024), "Unit Testing .NET with xUnit.net documentation", https://xunit.net/
[7] Abraham Silberschatz, Henry F. Korth, S. Sudarshan (2020), "Database System Concepts", 7th Edition, McGraw-Hill.
[8] Thomas Connolly, Carolyn Begg (2015), "Database Systems: A Practical Approach to Design, Implementation, and Management", 6th Edition, Pearson.
