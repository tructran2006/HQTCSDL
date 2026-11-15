# ==============================================================================
# CineSeat - Kịch bản kiểm thử tự động toàn diện (TC01 -> TC06)
# ==============================================================================
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "   CineSeat Online Movie Booking - Automated Test Suite (xUnit)   " -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/3] Kiểm tra trạng thái cơ sở dữ liệu Oracle 23ai..." -ForegroundColor Yellow
$containerStatus = docker inspect --format="{{.State.Health.Status}}" oracle-23ai 2>$null
if ($containerStatus -eq "healthy") {
    Write-Host "  -> Docker container 'oracle-23ai' dang chay va HEALTHY." -ForegroundColor Green
} else {
    Write-Host "  [CANH BAO] Container 'oracle-23ai' co trang thai: $containerStatus. Thu nghiem van tiep tuc..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[2/3] Build solution va chay bo kiem thu nghiep vu..." -ForegroundColor Yellow
dotnet test CineSeat.slnx --logger "console;verbosity=normal"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Green
    Write-Host "   TAT CA 6/6 TEST CASES NGHIỆP VỤ ĐÃ VƯỢT QUA XUẤT SẮC (PASSED)  " -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Green
    Write-Host "  - TC01: Tinh gia ve (suat thuong, suat toi >= 18h +20%, cuoi tuan +10%, ma SALE10 -10%)" -ForegroundColor White
    Write-Host "  - TC02: Bat loi ma khuyen mai sai (ORA-20002) va hien thi thong bao than thien" -ForegroundColor White
    Write-Host "  - TC03: Giu ghe (HELD) -> Huy don (CANCELLED) -> Ghe tro ve AVAILABLE" -ForegroundColor White
    Write-Host "  - TC04: Concurrency - Chong ban trung ghe bang SELECT FOR UPDATE WAIT 5 (ORA-20005)" -ForegroundColor White
    Write-Host "  - TC05: Giu ghe -> Thanh toan (DEMO_QR) -> Don PAID, Ghe SOLD, PAYMENTS & AUDIT ghi nhan" -ForegroundColor White
    Write-Host "  - TC06: Tu dong thu hoi ghe qua han 5 phut bang thu tuc PR_EXPIRE_HOLDS" -ForegroundColor White
    Write-Host "==================================================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Red
    Write-Host "   CO TEST CASE THAT BAI (FAILED). VUI LONG KIEM TRA LOGS!        " -ForegroundColor Red
    Write-Host "==================================================================" -ForegroundColor Red
    exit 1
}
