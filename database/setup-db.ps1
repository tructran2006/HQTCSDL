# CineSeat - Script khoi tao / nap lai CSDL tren Windows
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host ">>> Dang thuc thi 00_master_init.sql vao container oracle-23ai..." -ForegroundColor Cyan

docker exec -e NLS_LANG=AMERICAN_AMERICA.AL32UTF8 -e LANG=C.UTF-8 -i oracle-23ai bash -c "sqlplus -s / as sysdba @/container-entrypoint-initdb.d/00_master_init.sql"

Write-Host ">>> Kiem tra trang thai schema CINESEAT..." -ForegroundColor Cyan
docker exec -e NLS_LANG=AMERICAN_AMERICA.AL32UTF8 -e LANG=C.UTF-8 -i oracle-23ai bash -c "sqlplus -s CINESEAT/CineSeat123@//localhost:1521/FREEPDB1 << 'EOF'
SET LINESIZE 200;
SELECT object_type, status, COUNT(*) AS count FROM user_objects GROUP BY object_type, status ORDER BY object_type;
EXIT;
EOF"

Write-Host ">>> Khoi tao CSDL CineSeat hoan tat!" -ForegroundColor Green
