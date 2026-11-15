-- =============================================================================
-- CINESEAT / ORACLE DATABASE 23ai - 00_INIT_USER.SQL
-- Chạy với quyền SYSDBA trên Container Database (CDB) hoặc trực tiếp trên PDB FREEPDB1.
-- Tạo user/schema riêng CINESEAT với mật khẩu CineSeat123 và cấp đầy đủ quyền.
-- Tuyệt đối không chạy DDL/DML của dự án trực tiếp trên user SYS hoặc SYSTEM.
-- =============================================================================

ALTER SESSION SET CONTAINER = FREEPDB1;

DECLARE
   v_user_count NUMBER;
BEGIN
   SELECT COUNT(*) INTO v_user_count FROM all_users WHERE username = 'CINESEAT';
   IF v_user_count > 0 THEN
      EXECUTE IMMEDIATE 'DROP USER CINESEAT CASCADE';
      DBMS_OUTPUT.PUT_LINE('User CINESEAT da duoc xoa de khoi tao lai.');
   END IF;
END;
/

CREATE USER CINESEAT IDENTIFIED BY CineSeat123
  DEFAULT TABLESPACE USERS
  TEMPORARY TABLESPACE TEMP
  QUOTA UNLIMITED ON USERS;

GRANT CREATE SESSION TO CINESEAT;
GRANT CREATE TABLE TO CINESEAT;
GRANT CREATE VIEW TO CINESEAT;
GRANT CREATE PROCEDURE TO CINESEAT;
GRANT CREATE TRIGGER TO CINESEAT;
GRANT CREATE SEQUENCE TO CINESEAT;
GRANT CREATE SYNONYM TO CINESEAT;

COMMIT;

PROMPT >>> User CINESEAT da duoc tao va cap quyen thanh cong tren FREEPDB1!
