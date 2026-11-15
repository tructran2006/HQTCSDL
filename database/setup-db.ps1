# CineSeat - Script khoi tao / nap lai CSDL tren Windows
Write-Host ">>> Dang thuc thi 00_master_init.sql vao container oracle-23ai..." -ForegroundColor Cyan

Get-Content -Raw "$PSScriptRoot\00_master_init.sql" | docker exec -i oracle-23ai sqlplus / as sysdba

Write-Host ">>> Kiem tra trang thai schema CINESEAT..." -ForegroundColor Cyan
@'
SET LINESIZE 200;
SELECT object_type, status, COUNT(*) AS count FROM user_objects GROUP BY object_type, status ORDER BY object_type;
'@ | docker exec -i oracle-23ai sqlplus -s CINESEAT/CineSeat123@//localhost:1521/FREEPDB1

Write-Host ">>> Khoi tao CSDL CineSeat hoan tat!" -ForegroundColor Green
