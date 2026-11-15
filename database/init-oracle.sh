#!/bin/bash
# =============================================================================
# CineSeat - Automated Database Initialization Script for Oracle 23ai Free
# =============================================================================
set -e

echo ">>> Dang khoi tao co so du lieu CineSeat tren Oracle 23ai Free..."
sqlplus -s / as sysdba << 'EOF'
@/opt/oracle/scripts/setup/00_master_init.sql
EXIT;
EOF

echo ">>> Hoan tat khoi tao CineSeat thanh cong!"
