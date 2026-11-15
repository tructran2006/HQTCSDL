-- =============================================================================
-- CINESEAT / ORACLE DATABASE 23ai - 02_LOGIC.SQL
-- PL/SQL Package PKG_BOOKING, Procedure độc lập PR_EXPIRE_HOLDS, Triggers.
-- Chạy trên schema riêng CINESEAT@FREEPDB1.
-- QUY TẮC CỐT LÕI: Package KHÔNG tự COMMIT. Tầng ứng dụng gọi COMMIT hoặc ROLLBACK.
-- =============================================================================

CREATE OR REPLACE PACKAGE pkg_booking AS
  -- Hàm tính tổng tiền theo giá suất, khung giờ tối (>=18h: +20%), cuối tuần (+10%), mã khuyến mãi.
  FUNCTION fn_total_price(p_showtime_id IN NUMBER,
                          p_seat_count  IN NUMBER,
                          p_promo_code  IN VARCHAR2 DEFAULT NULL) RETURN NUMBER;

  -- Giữ ghế trong 5 phút; danh sách ghế dạng JSON mảng ví dụ: '[1, 2, 3]'.
  PROCEDURE pr_hold(p_user_id     IN NUMBER,
                    p_showtime_id IN NUMBER,
                    p_seats_json  IN CLOB,
                    p_promo_code  IN VARCHAR2,
                    p_booking_id  OUT NUMBER,
                    p_total       OUT NUMBER,
                    p_expires     OUT TIMESTAMP);

  -- Xác nhận thanh toán mô phỏng DEMO (chuyển HELD -> SOLD, ghi nhận PAYMENTS, chuyển BOOKING -> PAID).
  PROCEDURE pr_confirm(p_booking_id IN NUMBER,
                       p_method     IN VARCHAR2 DEFAULT 'DEMO_QR');

  -- Hủy đơn giữ ghế (chuyển HELD -> AVAILABLE, chuyển BOOKING -> CANCELLED).
  PROCEDURE pr_cancel(p_booking_id IN NUMBER);

  -- Thu hồi các đơn và ghế quá hạn 5 phút.
  PROCEDURE pr_expire_holds;
END pkg_booking;
/

CREATE OR REPLACE PACKAGE BODY pkg_booking AS

  FUNCTION fn_total_price(p_showtime_id IN NUMBER,
                          p_seat_count  IN NUMBER,
                          p_promo_code  IN VARCHAR2 DEFAULT NULL) RETURN NUMBER IS
    v_base       NUMBER;
    v_time       TIMESTAMP;
    v_discount   NUMBER := 0;
    v_multiplier NUMBER := 1;
  BEGIN
    IF p_seat_count < 1 OR p_seat_count > 8 THEN
      RAISE_APPLICATION_ERROR(-20001, 'Moi don chi dat tu 1 den 8 ghe');
    END IF;

    SELECT base_price, start_at INTO v_base, v_time
      FROM showtimes
     WHERE showtime_id = p_showtime_id AND is_active = 1;

    -- Suất bắt đầu >= 18 giờ: +20%; Suất vào thứ Bảy/Chủ Nhật: +10% nữa
    IF TO_NUMBER(TO_CHAR(v_time, 'HH24')) >= 18 THEN
      v_multiplier := 1.20;
    END IF;

    IF TRIM(TO_CHAR(v_time, 'DY', 'NLS_DATE_LANGUAGE=ENGLISH')) IN ('SAT', 'SUN') THEN
      v_multiplier := v_multiplier * 1.10;
    END IF;

    IF TRIM(p_promo_code) IS NOT NULL THEN
      BEGIN
        SELECT discount_pct INTO v_discount
          FROM promotions
         WHERE UPPER(promo_code) = UPPER(TRIM(p_promo_code))
           AND valid_until >= TRUNC(SYSDATE)
           AND is_active = 1;
      EXCEPTION
        WHEN NO_DATA_FOUND THEN
          RAISE_APPLICATION_ERROR(-20002, 'Ma khuyen mai khong hop le/da het han');
      END;
    END IF;

    -- Làm tròn đến hàng nghìn đồng
    RETURN ROUND(v_base * v_multiplier * p_seat_count * (100 - v_discount) / 100, -3);
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20003, 'Khong tim thay suat chieu');
  END fn_total_price;

  PROCEDURE pr_hold(p_user_id     IN NUMBER,
                    p_showtime_id IN NUMBER,
                    p_seats_json  IN CLOB,
                    p_promo_code  IN VARCHAR2,
                    p_booking_id  OUT NUMBER,
                    p_total       OUT NUMBER,
                    p_expires     OUT TIMESTAMP) IS
    v_show_id       NUMBER;
    v_dummy         NUMBER;
    v_count         NUMBER := 0;
    v_promotion_id  NUMBER;
    v_seat_index    NUMBER := 0;
    v_allocated     NUMBER := 0;
    v_unit_price    NUMBER;
  BEGIN
    SAVEPOINT sp_hold;

    -- Kiểm tra người dùng tồn tại
    SELECT 1 INTO v_dummy FROM app_users WHERE user_id = p_user_id;

    -- Khóa bản ghi suất chiếu bằng SELECT FOR UPDATE WAIT 5 để chống tranh chấp đồng thời
    SELECT showtime_id INTO v_show_id
      FROM showtimes
     WHERE showtime_id = p_showtime_id AND is_active = 1 AND start_at > LOCALTIMESTAMP
       FOR UPDATE WAIT 5;

    -- Quét và thu hồi các ghế/đơn quá hạn trước khi kiểm tra ghế mới
    UPDATE bookings SET status = 'EXPIRED'
     WHERE showtime_id = p_showtime_id AND status = 'HELD'
       AND expires_at <= LOCALTIMESTAMP;

    UPDATE showtime_seats SET status = 'AVAILABLE', booking_id = NULL, held_until = NULL
     WHERE showtime_id = p_showtime_id AND status = 'HELD'
       AND held_until <= LOCALTIMESTAMP;

    -- Phân tích mảng ghế JSON và kiểm tra tính hợp lệ
    SELECT COUNT(*), COUNT(DISTINCT seat_id) INTO v_count, v_dummy
      FROM JSON_TABLE(p_seats_json, '$[*]' COLUMNS (seat_id NUMBER PATH '$'));

    IF v_count < 1 OR v_count > 8 OR v_count <> v_dummy THEN
      RAISE_APPLICATION_ERROR(-20004, 'Danh sach ghe khong hop le hoac bi trung');
    END IF;

    -- Tính tổng tiền
    p_total := fn_total_price(p_showtime_id, v_count, p_promo_code);

    IF TRIM(p_promo_code) IS NOT NULL THEN
      SELECT promotion_id INTO v_promotion_id
        FROM promotions
       WHERE UPPER(promo_code) = UPPER(TRIM(p_promo_code));
    END IF;

    -- Thiết lập thời hạn giữ ghế là 5 phút
    p_expires := LOCALTIMESTAMP + NUMTODSINTERVAL(5, 'MINUTE');

    INSERT INTO bookings(user_id, showtime_id, promotion_id, status, total_amount, expires_at)
      VALUES (p_user_id, p_showtime_id, v_promotion_id, 'HELD', p_total, p_expires)
      RETURNING booking_id INTO p_booking_id;

    -- Giữ từng ghế trong SHOWTIME_SEATS và ghi BOOKING_SEATS
    FOR v_seat IN (
      SELECT seat_id FROM JSON_TABLE(p_seats_json, '$[*]'
        COLUMNS (seat_id NUMBER PATH '$')) ORDER BY seat_id
    ) LOOP
      UPDATE showtime_seats
         SET status = 'HELD', booking_id = p_booking_id, held_until = p_expires
       WHERE showtime_id = p_showtime_id AND seat_id = v_seat.seat_id
         AND status = 'AVAILABLE';

      IF SQL%ROWCOUNT <> 1 THEN
        RAISE_APPLICATION_ERROR(-20005, 'Ghe da duoc nguoi khac giu/ban hoac khong ton tai');
      END IF;

      -- Phân bổ giá vé: ghế cuối nhận phần dư để tổng chính xác 100%
      v_seat_index := v_seat_index + 1;
      IF v_seat_index = v_count THEN
        v_unit_price := p_total - v_allocated;
      ELSE
        v_unit_price := TRUNC(p_total / v_count);
      END IF;
      v_allocated := v_allocated + v_unit_price;

      INSERT INTO booking_seats(booking_id, seat_id, unit_price)
        VALUES(p_booking_id, v_seat.seat_id, v_unit_price);
    END LOOP;

  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      ROLLBACK TO sp_hold;
      RAISE_APPLICATION_ERROR(-20006, 'Khong tim thay nguoi dung hoac suat chieu con han');
    WHEN OTHERS THEN
      ROLLBACK TO sp_hold;
      RAISE;
  END pr_hold;

  PROCEDURE pr_confirm(p_booking_id IN NUMBER,
                       p_method     IN VARCHAR2 DEFAULT 'DEMO_QR') IS
    v_show           NUMBER;
    v_status         VARCHAR2(15);
    v_expires        TIMESTAMP;
    v_total          NUMBER;
    v_lock           NUMBER;
    v_expected_count NUMBER;
  BEGIN
    SAVEPOINT sp_confirm;

    SELECT showtime_id INTO v_show FROM bookings WHERE booking_id = p_booking_id;

    -- Khóa nhất quán theo thứ tự: SHOWTIMES trước, BOOKINGS sau
    SELECT showtime_id INTO v_lock FROM showtimes
     WHERE showtime_id = v_show FOR UPDATE WAIT 5;

    SELECT status, expires_at, total_amount INTO v_status, v_expires, v_total
      FROM bookings WHERE booking_id = p_booking_id FOR UPDATE WAIT 5;

    IF v_status <> 'HELD' THEN
      RAISE_APPLICATION_ERROR(-20007, 'Don khong o trang thai dang giu ghe');
    END IF;

    IF v_expires <= LOCALTIMESTAMP THEN
      RAISE_APPLICATION_ERROR(-20008, 'Het 5 phut giu ghe. Vui long dat lai');
    END IF;

    IF p_method NOT IN ('DEMO_CARD', 'DEMO_QR', 'DEMO_CASH') THEN
      RAISE_APPLICATION_ERROR(-20009, 'Phuong thuc thanh toan DEMO khong hop le');
    END IF;

    -- Chuyển trạng thái ghế sang SOLD
    UPDATE showtime_seats SET status = 'SOLD', held_until = NULL
     WHERE showtime_id = v_show AND booking_id = p_booking_id AND status = 'HELD';
    v_expected_count := SQL%ROWCOUNT;

    SELECT COUNT(*) INTO v_lock FROM booking_seats WHERE booking_id = p_booking_id;
    IF v_expected_count <> v_lock THEN
      RAISE_APPLICATION_ERROR(-20010, 'Ghe khong con du; huy giao dich');
    END IF;

    -- Ghi nhận giao dịch thanh toán
    INSERT INTO payments(booking_id, amount, method, payment_status)
      VALUES(p_booking_id, v_total, p_method, 'PAID');

    -- Cập nhật đơn thành PAID
    UPDATE bookings SET status = 'PAID', paid_at = LOCALTIMESTAMP
      WHERE booking_id = p_booking_id;

  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK TO sp_confirm;
      RAISE;
  END pr_confirm;

  PROCEDURE pr_cancel(p_booking_id IN NUMBER) IS
    v_show   NUMBER;
    v_status VARCHAR2(15);
    v_lock   NUMBER;
  BEGIN
    SAVEPOINT sp_cancel;

    SELECT showtime_id INTO v_show FROM bookings WHERE booking_id = p_booking_id;

    SELECT showtime_id INTO v_lock FROM showtimes
     WHERE showtime_id = v_show FOR UPDATE WAIT 5;

    SELECT status INTO v_status FROM bookings
     WHERE booking_id = p_booking_id FOR UPDATE WAIT 5;

    IF v_status <> 'HELD' THEN
      RAISE_APPLICATION_ERROR(-20011, 'Chi huy duoc don dang giu ghe');
    END IF;

    -- Trả lại trạng thái AVAILABLE cho ghế
    UPDATE showtime_seats SET status = 'AVAILABLE', booking_id = NULL, held_until = NULL
     WHERE showtime_id = v_show AND booking_id = p_booking_id AND status = 'HELD';

    UPDATE bookings SET status = 'CANCELLED' WHERE booking_id = p_booking_id;

  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK TO sp_cancel;
      RAISE;
  END pr_cancel;

  PROCEDURE pr_expire_holds IS
    CURSOR c_showtimes IS
      SELECT DISTINCT showtime_id FROM bookings
       WHERE status = 'HELD' AND expires_at <= LOCALTIMESTAMP
       ORDER BY showtime_id;
    v_lock NUMBER;
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
      EXCEPTION
        WHEN OTHERS THEN
          ROLLBACK TO sp_expire_one;
          DBMS_OUTPUT.PUT_LINE('Khong xu ly duoc showtime=' || s.showtime_id || ': ' || SQLERRM);
      END;
    END LOOP;
  END pr_expire_holds;

END pkg_booking;
/

-- TRIGGER 1: AUDIT THAY ĐỔI TRẠNG THÁI BOOKING
CREATE OR REPLACE TRIGGER trg_booking_audit
AFTER INSERT OR UPDATE OF status ON bookings
FOR EACH ROW
BEGIN
  IF INSERTING OR NVL(:OLD.status, '~') <> :NEW.status THEN
    INSERT INTO booking_audit(booking_id, old_status, new_status, description)
      VALUES(:NEW.booking_id, :OLD.status, :NEW.status, 'Thay doi trang thai booking');
  END IF;
END;
/

-- TRIGGER 2: BẢO VỆ CHUYỂN TRẠNG THÁI GHẾ
CREATE OR REPLACE TRIGGER trg_seat_state_guard
BEFORE UPDATE OF status, booking_id ON showtime_seats
FOR EACH ROW
BEGIN
  IF :OLD.status = 'SOLD' AND :NEW.status <> 'SOLD' THEN
    RAISE_APPLICATION_ERROR(-20012, 'Ghe da ban khong the tra lai neu chua co hoan tien');
  END IF;
  IF :OLD.status = 'AVAILABLE' AND :NEW.status = 'SOLD' THEN
    RAISE_APPLICATION_ERROR(-20013, 'Phai giu ghe HELD truoc khi ban');
  END IF;
  IF :OLD.status = 'HELD' AND :NEW.status = 'SOLD'
      AND NVL(:OLD.booking_id, -1) <> NVL(:NEW.booking_id, -2) THEN
    RAISE_APPLICATION_ERROR(-20014, 'Khong the doi chu ghe khi thanh toan');
  END IF;
END;
/

-- PROCEDURE ĐỘC LẬP CHO CRON / APPLICATION GỌI
CREATE OR REPLACE PROCEDURE pr_expire_holds AS
BEGIN
  pkg_booking.pr_expire_holds;
END;
/
