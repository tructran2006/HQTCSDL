$endpoints = @(
    @{ Url = 'http://localhost:5000/'; Method = 'GET'; Name = 'User Home / Movies Index' },
    @{ Url = 'http://localhost:5000/Movies/Details/1'; Method = 'GET'; Name = 'User Movie Details' },
    @{ Url = 'http://localhost:5000/Movies/CinemaShowtimes'; Method = 'GET'; Name = 'User Showtimes by Cinema' },
    @{ Url = 'http://localhost:5000/Bookings/SelectSeats/1'; Method = 'GET'; Name = 'User Seat Selection' },
    @{ Url = 'http://localhost:5000/Bookings/History'; Method = 'GET'; Name = 'User Booking History' },
    @{ Url = 'http://localhost:5000/Admin/Dashboard'; Method = 'GET'; Name = 'Admin Dashboard' },
    @{ Url = 'http://localhost:5000/Admin/Movies'; Method = 'GET'; Name = 'Admin Movies Management' },
    @{ Url = 'http://localhost:5000/Admin/Showtimes'; Method = 'GET'; Name = 'Admin Showtimes Management' },
    @{ Url = 'http://localhost:5000/Admin/Bookings'; Method = 'GET'; Name = 'Admin Bookings Management' },
    @{ Url = 'http://localhost:5000/images/poster-1.jpg'; Method = 'GET'; Name = 'Static Poster 1 Image' },
    @{ Url = 'http://localhost:5000/images/poster-13.jpg'; Method = 'GET'; Name = 'Static Poster 13 Image' },
    @{ Url = 'http://localhost:5000/css/cineseat.css'; Method = 'GET'; Name = 'CineSeat Modern CSS' },
    @{ 
       Url = 'http://localhost:5000/api/bookings/calculate-price'; 
       Method = 'POST'; 
       Body = '{"showtimeId":1,"seatCount":2,"promoCode":"SALE10"}'; 
       ContentType = 'application/json';
       Name = 'API Calculate Price'
    }
)

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

if ($allPassed) {
    Write-Host "`n==================================================================" -ForegroundColor Cyan
    Write-Host ">>> TẤT CẢ 13/13 USER & ADMIN ROUTES ĐỀU PHẢN HỒI 200 OK SẠCH SẼ! <<<" -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Cyan
} else {
    exit 1
}
