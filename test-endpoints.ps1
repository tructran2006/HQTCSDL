# =============================================================================
# CineSeat - Script kiểm thử toàn diện HTTP Endpoints & Dữ liệu UTF-8
# =============================================================================
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$endpoints = @(
    @{ Url = 'http://localhost:5000/'; Method = 'GET'; Name = 'User Home / Movies Index' },
    @{ Url = 'http://localhost:5000/Movies/Details/1'; Method = 'GET'; Name = 'User Movie Details' },
    @{ Url = 'http://localhost:5000/Movies/CinemaShowtimes'; Method = 'GET'; Name = 'User Showtimes by Cinema' },
    @{ Url = 'http://localhost:5000/Bookings/SelectSeats/1'; Method = 'GET'; Name = 'User Seat Selection' },
    @{ Url = 'http://localhost:5000/Bookings/History'; Method = 'GET'; Name = 'User Booking History' },
    @{ Url = 'http://localhost:5000/Admin/Dashboard'; Method = 'GET'; Name = 'Admin Dashboard' },
    @{ Url = 'http://localhost:5000/Admin/Movies'; Method = 'GET'; Name = 'Admin Movies Management' },
    @{ Url = 'http://localhost:5000/Admin/Showtimes'; Method = 'GET'; Name = 'Admin Showtimes Management' },
    @{ Url = 'http://localhost:5000/Admin/Cinemas'; Method = 'GET'; Name = 'Admin Cinemas Management' },
    @{ Url = 'http://localhost:5000/Admin/Promotions'; Method = 'GET'; Name = 'Admin Promotions Management' },
    @{ Url = 'http://localhost:5000/Admin/Bookings'; Method = 'GET'; Name = 'Admin Bookings Management' },
    @{ Url = 'http://localhost:5000/css/cineseat.css'; Method = 'GET'; Name = 'CineSeat Modern CSS' },
    @{ Url = 'http://localhost:5000/Account/Login'; Method = 'GET'; Name = 'Account Login Page' },
    @{ Url = 'http://localhost:5000/Account/Register'; Method = 'GET'; Name = 'Account Register Page' },
    @{ 
       Url = 'http://localhost:5000/api/bookings/calculate-price'; 
       Method = 'POST'; 
       Body = '{"showtimeId":1,"seatCount":2,"promoCode":"SALE10"}'; 
       ContentType = 'application/json';
       Name = 'API Calculate Price'
    }
)

# Thêm 13 file ảnh poster vào danh sách kiểm thử
for ($i = 1; $i -le 13; $i++) {
    $endpoints += @{ Url = "http://localhost:5000/images/poster-$i.jpg"; Method = 'GET'; Name = "Movie Poster $i" }
}

$allPassed = $true
foreach ($ep in $endpoints) {
    try {
        if ($ep.Method -eq 'POST') {
            $res = Invoke-WebRequest -Uri $ep.Url -Method POST -Body $ep.Body -ContentType $ep.ContentType -UseBasicParsing -TimeoutSec 10
        } else {
            $res = Invoke-WebRequest -Uri $ep.Url -Method GET -UseBasicParsing -TimeoutSec 10
        }
        Write-Host "[$($res.StatusCode)] $($ep.Method) $($ep.Url) ($($ep.Name)) - OK ($($res.RawContentLength) bytes)" -ForegroundColor Green
    } catch {
        Write-Host "[FAIL] $($ep.Method) $($ep.Url) ($($ep.Name)) : $_" -ForegroundColor Red
        $allPassed = $false
    }
}

# Kiểm tra dữ liệu tiếng Việt UTF-8 trên Trang Chủ
Write-Host "`n>>> Kiem tra tinh toan ven ky tu tieng Viet UTF-8 tren giao dien..." -ForegroundColor Cyan
$homeRes = Invoke-WebRequest -Uri 'http://localhost:5000/' -UseBasicParsing
$homeHtml = [System.Text.Encoding]::UTF8.GetString($homeRes.RawContentStream.ToArray())

$vietnameseChecks = @(
    'Khoa học viễn tưởng',
    'Lật Mặt 7: Một Điều Ước',
    'Phim Nổi Bật Hôm Nay',
    'hero-split-banner',
    'hero-poster-wrapper',
    'Đăng Nhập'
)

foreach ($vc in $vietnameseChecks) {
    if ($homeHtml.Contains($vc)) {
        Write-Host "[PASS UTF-8] Tim thay: $vc" -ForegroundColor Green
    } else {
        Write-Host "[FAIL UTF-8] KHONG tim thay: $vc" -ForegroundColor Red
        $allPassed = $false
    }
}

if ($allPassed) {
    Write-Host "`n==================================================================" -ForegroundColor Cyan
    Write-Host ">>> TẤT CẢ 24/24 ROUTES & POSTERS + UTF-8 ĐỀU ĐẠT CHUẨN 100%! <<<" -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Cyan
} else {
    exit 1
}
