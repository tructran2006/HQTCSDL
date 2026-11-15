-- =============================================================================
-- CINESEAT / ORACLE 23ai - 05_FIX_VIETNAMESE_UTF8.SQL
-- Cập nhật dữ liệu tiếng Việt chuẩn UTF-8 (AL32UTF8) cho toàn bộ
-- MOVIES, APP_USERS, CINEMAS, ROOMS.
-- =============================================================================

SET DEFINE OFF;
ALTER SESSION SET CURRENT_SCHEMA = CINESEAT;

-- 1. CẬP NHẬT 2 CỤM RẠP & 3 PHÒNG CHIẾU
UPDATE cinemas SET cinema_name = N'CineSeat Nguyễn Huệ', address = N'Quận 1, TP. Hồ Chí Minh' WHERE cinema_id = 1;
UPDATE cinemas SET cinema_name = N'CineSeat Thủ Đức', address = N'Thành phố Thủ Đức, TP. Hồ Chí Minh' WHERE cinema_id = 2;

UPDATE rooms SET room_name = N'Phòng A1' WHERE room_id = 1;
UPDATE rooms SET room_name = N'Phòng A2' WHERE room_id = 2;
UPDATE rooms SET room_name = N'Phòng B1' WHERE room_id = 3;

-- 2. CẬP NHẬT 13 PHIM
UPDATE movies SET
  title = N'Dune: Part Two',
  genre = N'Khoa học viễn tưởng',
  duration_min = 166,
  age_rating = 'T13',
  synopsis = N'Paul Atreides tiếp tục hành trình trên hành tinh Arrakis cùng người Fremen.',
  poster_file = 'poster-1.jpg'
WHERE movie_id = 1;

UPDATE movies SET
  title = N'Oppenheimer',
  genre = N'Tiểu sử / Chính kịch',
  duration_min = 180,
  age_rating = 'T16',
  synopsis = N'Câu chuyện về nhà vật lý J. Robert Oppenheimer và dự án Manhattan.',
  poster_file = 'poster-2.jpg'
WHERE movie_id = 2;

UPDATE movies SET
  title = N'Inside Out 2',
  genre = N'Hoạt hình',
  duration_min = 96,
  age_rating = 'P',
  synopsis = N'Riley đối diện với những cảm xúc mới ở tuổi thiếu niên như Lo Âu, Ganh Tị.',
  poster_file = 'poster-3.jpg'
WHERE movie_id = 3;

UPDATE movies SET
  title = N'Kung Fu Panda 4',
  genre = N'Hoạt hình / Hài',
  duration_min = 94,
  age_rating = 'P',
  synopsis = N'Po cần tìm người kế nhiệm Thần Long Đại Hiệp và đối mặt Tắc Kè Bông.',
  poster_file = 'poster-4.jpg'
WHERE movie_id = 4;

UPDATE movies SET
  title = N'Spider-Man: Across the Spider-Verse',
  genre = N'Hoạt hình / Phiêu lưu',
  duration_min = 140,
  age_rating = 'T13',
  synopsis = N'Miles Morales du hành qua các chiều không gian và đối đầu Spider-Society.',
  poster_file = 'poster-5.jpg'
WHERE movie_id = 5;

UPDATE movies SET
  title = N'Interstellar',
  genre = N'Khoa học viễn tưởng',
  duration_min = 169,
  age_rating = 'T13',
  synopsis = N'Nhóm phi hành gia vượt qua hố sâu không gian để tìm kiếm tương lai cho nhân loại.',
  poster_file = 'poster-6.jpg'
WHERE movie_id = 6;

UPDATE movies SET
  title = N'Godzilla x Kong: The New Empire',
  genre = N'Hành động / Viễn tưởng',
  duration_min = 115,
  age_rating = 'T13',
  synopsis = N'Hai quái thú huyền thoại hợp lực chống lại hiểm họa từ Trái Đất Rỗng.',
  poster_file = 'poster-7.jpg'
WHERE movie_id = 7;

UPDATE movies SET
  title = N'Lật Mặt 7: Một Điều Ước',
  genre = N'Gia đình / Tâm lý',
  duration_min = 138,
  age_rating = 'K',
  synopsis = N'Câu chuyện cảm động về tình mẫu tử của người mẹ già cùng 5 người con.',
  poster_file = 'poster-8.jpg'
WHERE movie_id = 8;

UPDATE movies SET
  title = N'Mai',
  genre = N'Tâm lý / Tình cảm',
  duration_min = 131,
  age_rating = 'T18',
  synopsis = N'Câu chuyện về cuộc đời và tình yêu đầy trắc trở của Mai và Dương.',
  poster_file = 'poster-9.jpg'
WHERE movie_id = 9;

UPDATE movies SET
  title = N'Deadpool & Wolverine',
  genre = N'Hành động / Hài hước',
  duration_min = 128,
  age_rating = 'T18',
  synopsis = N'Deadpool cùng Wolverine thực hiện nhiệm vụ giải cứu dòng thời gian.',
  poster_file = 'poster-10.jpg'
WHERE movie_id = 10;

UPDATE movies SET
  title = N'Despicable Me 4',
  genre = N'Hoạt hình / Hài',
  duration_min = 95,
  age_rating = 'P',
  synopsis = N'Gia đình Gru chào đón thành viên mới và đối đầu kẻ thù nguy hiểm Maxime Le Mal.',
  poster_file = 'poster-11.jpg'
WHERE movie_id = 11;

UPDATE movies SET
  title = N'A Quiet Place: Day One',
  genre = N'Kinh dị / Giật gân',
  duration_min = 99,
  age_rating = 'T16',
  synopsis = N'Nguồn gốc ngày đầu tiên quái vật thính giác xâm chiếm thành phố New York.',
  poster_file = 'poster-12.jpg'
WHERE movie_id = 12;

UPDATE movies SET
  title = N'Furiosa: A Mad Max Saga',
  genre = N'Hành động / Phiêu lưu',
  duration_min = 148,
  age_rating = 'T18',
  synopsis = N'Hành trình thời trẻ của nữ chiến binh Furiosa trong vùng đất hoang tàn.',
  poster_file = 'poster-13.jpg'
WHERE movie_id = 13;

-- 3. CẬP NHẬT 13 NGƯỜI DÙNG DEMO
UPDATE app_users SET full_name = N'Nguyễn Minh Anh' WHERE user_id = 1;
UPDATE app_users SET full_name = N'Trần Gia Huy' WHERE user_id = 2;
UPDATE app_users SET full_name = N'Lê Bảo Ngọc' WHERE user_id = 3;
UPDATE app_users SET full_name = N'Phạm Hoàng Nam' WHERE user_id = 4;
UPDATE app_users SET full_name = N'Đỗ Thùy Linh' WHERE user_id = 5;
UPDATE app_users SET full_name = N'Vũ Quốc Anh' WHERE user_id = 6;
UPDATE app_users SET full_name = N'Bùi Phương Thảo' WHERE user_id = 7;
UPDATE app_users SET full_name = N'Ngô Thanh Tùng' WHERE user_id = 8;
UPDATE app_users SET full_name = N'Hoàng Khánh Vy' WHERE user_id = 9;
UPDATE app_users SET full_name = N'Đặng Minh Khôi' WHERE user_id = 10;
UPDATE app_users SET full_name = N'Lý Hải Đăng' WHERE user_id = 11;
UPDATE app_users SET full_name = N'Trịnh Mỹ Duyên' WHERE user_id = 12;
UPDATE app_users SET full_name = N'Dương Tuấn Kiệt' WHERE user_id = 13;

COMMIT;
