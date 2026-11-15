-- =============================================================================
-- CINESEAT / ORACLE DATABASE 23ai - 03_SEED.SQL
-- Dữ liệu mẫu hoàn chỉnh: Cụm rạp, phòng chiếu, 120 ghế vật lý, 13 phim,
-- 13 người dùng, 5 mã khuyến mãi, 20 suất chiếu tương lai, khởi tạo ghế theo suất.
-- Chạy trên schema riêng CINESEAT@FREEPDB1.
-- =============================================================================

-- 1. DANH SÁCH CỤM RẠP & PHÒNG CHIẾU
INSERT INTO cinemas(cinema_id, cinema_name, address)
VALUES(1, N'CineSeat Nguyễn Huệ', N'Quận 1, TP. Hồ Chí Minh');
INSERT INTO cinemas(cinema_id, cinema_name, address)
VALUES(2, N'CineSeat Thủ Đức', N'Thành phố Thủ Đức, TP. Hồ Chí Minh');

INSERT INTO rooms(room_id, cinema_id, room_name) VALUES(1, 1, N'Phòng A1');
INSERT INTO rooms(room_id, cinema_id, room_name) VALUES(2, 1, N'Phòng A2');
INSERT INTO rooms(room_id, cinema_id, room_name) VALUES(3, 2, N'Phòng B1');

-- 2. TẠO 120 GHẾ VẬT LÝ CHO 3 PHÒNG (Mỗi phòng 5 hàng A-E, 8 ghế/hàng, D-E là VIP)
BEGIN
  FOR r IN 1..3 LOOP
    FOR rowno IN 1..5 LOOP
      FOR n IN 1..8 LOOP
        INSERT INTO seats(room_id, row_label, seat_number, seat_type)
        VALUES(r, CHR(64 + rowno), n, CASE WHEN rowno >= 4 THEN 'VIP' ELSE 'STANDARD' END);
      END LOOP;
    END LOOP;
  END LOOP;
END;
/

-- 3. DANH SÁCH 13 PHIM
INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(1, N'Dune: Part Two', N'Khoa học viễn tưởng', 166, 'T13', N'Paul Atreides tiếp tục hành trình trên hành tinh Arrakis cùng người Fremen.', 'poster-1.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(2, N'Oppenheimer', N'Tiểu sử / Chính kịch', 180, 'T16', N'Câu chuyện về nhà vật lý J. Robert Oppenheimer và dự án Manhattan.', 'poster-2.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(3, N'Inside Out 2', N'Hoạt hình', 96, 'P', N'Riley đối diện với những cảm xúc mới ở tuổi thiếu niên như Lo Âu, Ganh Tị.', 'poster-3.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(4, N'Kung Fu Panda 4', N'Hoạt hình / Hài', 94, 'P', N'Po cần tìm người kế nhiệm Thần Long Đại Hiệp và đối mặt Tắc Kè Bông.', 'poster-4.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(5, N'Spider-Man: Across the Spider-Verse', N'Hoạt hình / Phiêu lưu', 140, 'T13', N'Miles Morales du hành qua các chiều không gian và đối đầu Spider-Society.', 'poster-5.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(6, N'Interstellar', N'Khoa học viễn tưởng', 169, 'T13', N'Nhóm phi hành gia vượt qua hố sâu không gian để tìm kiếm tương lai cho nhân loại.', 'poster-6.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(7, N'Godzilla x Kong: The New Empire', N'Hành động / Viễn tưởng', 115, 'T13', N'Hai quái thú huyền thoại hợp lực chống lại hiểm họa từ Trái Đất Rỗng.', 'poster-7.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(8, N'Lật Mặt 7: Một Điều Ước', N'Gia đình / Tâm lý', 138, 'K', N'Câu chuyện cảm động về tình mẫu tử của người mẹ già cùng 5 người con.', 'poster-8.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(9, N'Mai', N'Tâm lý / Tình cảm', 131, 'T18', N'Câu chuyện về cuộc đời và tình yêu đầy trắc trở của Mai và Dương.', 'poster-9.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(10, N'Deadpool & Wolverine', N'Hành động / Hài hước', 128, 'T18', N'Deadpool cùng Wolverine thực hiện nhiệm vụ giải cứu dòng thời gian.', 'poster-10.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(11, N'Despicable Me 4', N'Hoạt hình / Hài', 95, 'P', N'Gia đình Gru chào đón thành viên mới và đối đầu kẻ thù nguy hiểm Maxime Le Mal.', 'poster-11.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(12, N'A Quiet Place: Day One', N'Kinh dị / Giật gân', 99, 'T16', N'Nguồn gốc ngày đầu tiên quái vật thính giác xâm chiếm thành phố New York.', 'poster-12.jpg');

INSERT INTO movies(movie_id, title, genre, duration_min, age_rating, synopsis, poster_file)
VALUES(13, N'Furiosa: A Mad Max Saga', N'Hành động / Phiêu lưu', 148, 'T18', N'Hành trình thời trẻ của nữ chiến binh Furiosa trong vùng đất hoang tàn.', 'poster-13.jpg');

-- 4. DANH SÁCH 13 NGƯỜI DÙNG DEMO
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (1, N'Nguyễn Minh Anh', 'minhanh@demo.local', '0900000001');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (2, N'Trần Gia Huy', 'giahuy@demo.local', '0900000002');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (3, N'Lê Bảo Ngọc', 'baongoc@demo.local', '0900000003');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (4, N'Phạm Hoàng Nam', 'hoangnam@demo.local', '0900000004');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (5, N'Đỗ Thùy Linh', 'thuylinh@demo.local', '0900000005');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (6, N'Vũ Quốc Anh', 'quocanh@demo.local', '0900000006');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (7, N'Bùi Phương Thảo', 'phuongthao@demo.local', '0900000007');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (8, N'Ngô Thanh Tùng', 'thanhtung@demo.local', '0900000008');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (9, N'Hoàng Khánh Vy', 'khanhvy@demo.local', '0900000009');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (10, N'Đặng Minh Khôi', 'minhkhoi@demo.local', '0900000010');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (11, N'Lý Hải Đăng', 'haidang@demo.local', '0900000011');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (12, N'Trịnh Mỹ Duyên', 'myduyen@demo.local', '0900000012');
INSERT INTO app_users(user_id, full_name, email, phone) VALUES (13, N'Dương Tuấn Kiệt', 'tuankiet@demo.local', '0900000013');

-- 5. DANH SÁCH MÃ KHUYẾN MÃI
INSERT INTO promotions(promo_code, discount_pct, valid_until) VALUES('SALE10', 10, TRUNC(SYSDATE) + 365);
INSERT INTO promotions(promo_code, discount_pct, valid_until) VALUES('STUDENT', 15, TRUNC(SYSDATE) + 365);
INSERT INTO promotions(promo_code, discount_pct, valid_until) VALUES('WEEKEND20', 20, TRUNC(SYSDATE) + 180);
INSERT INTO promotions(promo_code, discount_pct, valid_until) VALUES('VIPMEMBER', 25, TRUNC(SYSDATE) + 365);
INSERT INTO promotions(promo_code, discount_pct, valid_until) VALUES('CINEFAN5', 5, TRUNC(SYSDATE) + 90);

-- 6. DANH SÁCH 20 SUẤT CHIẾU (1..10 ngày mai và ngày mốt; 11..20 ngày mai +3 và +4)
-- Suất 1..10
INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(1, 1, TRUNC(SYSDATE)+1 + INTERVAL '10' HOUR, TRUNC(SYSDATE)+1 + INTERVAL '12' HOUR + INTERVAL '50' MINUTE, 75000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(2, 1, TRUNC(SYSDATE)+1 + INTERVAL '14' HOUR, TRUNC(SYSDATE)+1 + INTERVAL '17' HOUR, 70000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(3, 1, TRUNC(SYSDATE)+1 + INTERVAL '19' HOUR, TRUNC(SYSDATE)+1 + INTERVAL '20' HOUR + INTERVAL '40' MINUTE, 80000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(4, 2, TRUNC(SYSDATE)+1 + INTERVAL '10' HOUR, TRUNC(SYSDATE)+1 + INTERVAL '11' HOUR + INTERVAL '40' MINUTE, 65000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(5, 2, TRUNC(SYSDATE)+1 + INTERVAL '15' HOUR, TRUNC(SYSDATE)+1 + INTERVAL '17' HOUR + INTERVAL '20' MINUTE, 90000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(6, 2, TRUNC(SYSDATE)+1 + INTERVAL '20' HOUR, TRUNC(SYSDATE)+1 + INTERVAL '22' HOUR + INTERVAL '50' MINUTE, 75000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(1, 3, TRUNC(SYSDATE)+2 + INTERVAL '11' HOUR, TRUNC(SYSDATE)+2 + INTERVAL '13' HOUR + INTERVAL '50' MINUTE, 80000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(2, 3, TRUNC(SYSDATE)+2 + INTERVAL '15' HOUR, TRUNC(SYSDATE)+2 + INTERVAL '18' HOUR, 65000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(4, 3, TRUNC(SYSDATE)+2 + INTERVAL '19' HOUR, TRUNC(SYSDATE)+2 + INTERVAL '20' HOUR + INTERVAL '40' MINUTE, 75000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(5, 1, TRUNC(SYSDATE)+2 + INTERVAL '11' HOUR, TRUNC(SYSDATE)+2 + INTERVAL '13' HOUR + INTERVAL '20' MINUTE, 85000);

-- Suất 11..20
INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(7, 1, TRUNC(SYSDATE)+3 + INTERVAL '10' HOUR, TRUNC(SYSDATE)+3 + INTERVAL '12' HOUR, 75000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(8, 1, TRUNC(SYSDATE)+3 + INTERVAL '13' HOUR + INTERVAL '30' MINUTE, TRUNC(SYSDATE)+3 + INTERVAL '16' HOUR, 80000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(9, 1, TRUNC(SYSDATE)+3 + INTERVAL '18' HOUR, TRUNC(SYSDATE)+3 + INTERVAL '20' HOUR + INTERVAL '30' MINUTE, 90000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(10, 2, TRUNC(SYSDATE)+3 + INTERVAL '11' HOUR, TRUNC(SYSDATE)+3 + INTERVAL '13' HOUR + INTERVAL '20' MINUTE, 85000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(11, 2, TRUNC(SYSDATE)+3 + INTERVAL '15' HOUR, TRUNC(SYSDATE)+3 + INTERVAL '16' HOUR + INTERVAL '45' MINUTE, 70000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(12, 2, TRUNC(SYSDATE)+3 + INTERVAL '19' HOUR, TRUNC(SYSDATE)+3 + INTERVAL '20' HOUR + INTERVAL '50' MINUTE, 85000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(13, 3, TRUNC(SYSDATE)+4 + INTERVAL '10' HOUR, TRUNC(SYSDATE)+4 + INTERVAL '12' HOUR + INTERVAL '40' MINUTE, 80000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(7, 3, TRUNC(SYSDATE)+4 + INTERVAL '14' HOUR, TRUNC(SYSDATE)+4 + INTERVAL '16' HOUR, 75000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(8, 3, TRUNC(SYSDATE)+4 + INTERVAL '18' HOUR + INTERVAL '30' MINUTE, TRUNC(SYSDATE)+4 + INTERVAL '21' HOUR, 85000);

INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price)
VALUES(10, 1, TRUNC(SYSDATE)+4 + INTERVAL '21' HOUR, TRUNC(SYSDATE)+4 + INTERVAL '23' HOUR + INTERVAL '20' MINUTE, 95000);

-- 7. ĐỒNG BỘ GHẾ TRỐNG CHO TẤT CẢ CÁC SUẤT CHIẾU (SHOWTIME_SEATS)
INSERT INTO showtime_seats(showtime_id, seat_id, status)
SELECT t.showtime_id, s.seat_id, 'AVAILABLE' 
FROM showtimes t
JOIN seats s ON s.room_id = t.room_id;

-- 8. TẠO 1 ĐƠN ĐẶT VÉ ĐÃ THANH TOÁN MẪU (Demo QR)
DECLARE
  v_id        NUMBER;
  v_total     NUMBER;
  v_exp       TIMESTAMP;
  v_seat_one  NUMBER;
BEGIN
  SELECT MIN(seat_id) INTO v_seat_one FROM seats WHERE room_id = 1;
  pkg_booking.pr_hold(1, 1, '[' || v_seat_one || ']', 'SALE10', v_id, v_total, v_exp);
  pkg_booking.pr_confirm(v_id, 'DEMO_QR');
END;
/

COMMIT;
