-- =============================================================================
-- CINESEAT / ORACLE DATABASE 23ai - 04_VERIFY.SQL
-- Xác minh tính hợp lệ và trạng thái VALID của toàn bộ đối tượng trong schema CINESEAT.
-- Chạy trên schema riêng CINESEAT@FREEPDB1.
-- =============================================================================

SET SERVEROUTPUT ON;

PROMPT =========================================================================
PROMPT KIEM TRA TRANG THAI CAC BANG (12 BANG CHUAN 3NF)
PROMPT =========================================================================

SELECT table_name, num_rows, status
FROM user_tables
WHERE table_name IN (
    'APP_USERS', 'MOVIES', 'CINEMAS', 'ROOMS', 'SEATS', 'SHOWTIMES',
    'PROMOTIONS', 'BOOKINGS', 'SHOWTIME_SEATS', 'BOOKING_SEATS',
    'PAYMENTS', 'BOOKING_AUDIT'
)
ORDER BY table_name;

PROMPT =========================================================================
PROMPT KIEM TRA TRANG THAI PACKAGE, PROCEDURE, TRIGGER, VIEW
PROMPT =========================================================================

SELECT object_name, object_type, status
FROM user_objects
WHERE object_type IN ('PACKAGE', 'PACKAGE BODY', 'PROCEDURE', 'TRIGGER', 'VIEW')
ORDER BY object_type, object_name;

PROMPT =========================================================================
PROMPT KIEM TRA DOI TUONG INVALID (BAT BUOC PHAI LA 0)
PROMPT =========================================================================

SELECT object_name, object_type, status
FROM user_objects
WHERE status <> 'VALID';

PROMPT =========================================================================
PROMPT TONG KET SO LUONG DOI TUONG
PROMPT =========================================================================

SELECT object_type, COUNT(*) AS object_count
FROM user_objects
GROUP BY object_type
ORDER BY object_type;
