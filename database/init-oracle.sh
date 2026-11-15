#!/bin/bash
# =============================================================================
# CineSeat - Automated Database Initialization Script for Oracle 23ai Free
# =============================================================================
set -e

export NLS_LANG=AMERICAN_AMERICA.AL32UTF8
export LANG=C.UTF-8

echo ">>> Dang khoi tao co so du lieu CineSeat tren Oracle 23ai Free..."
sqlplus -s / as sysdba << 'EOF'
@/container-entrypoint-initdb.d/00_master_init.sql
EXIT;
EOF

echo ">>> Hoan tat khoi tao CineSeat thanh cong!"
