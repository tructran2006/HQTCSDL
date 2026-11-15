$endpoints = @(
    @{ Url = 'http://localhost:5000/'; Method = 'GET' },
    @{ Url = 'http://localhost:5000/Movies/Showtimes/1'; Method = 'GET' },
    @{ Url = 'http://localhost:5000/Bookings/SelectSeats/1'; Method = 'GET' },
    @{ Url = 'http://localhost:5000/Admin/Dashboard'; Method = 'GET' },
    @{ Url = 'http://localhost:5000/Bookings/History'; Method = 'GET' },
    @{ Url = 'http://localhost:5000/api/bookings/1'; Method = 'GET' },
    @{ Url = 'http://localhost:5000/api/showtimes/1/seats'; Method = 'GET' },
    @{ 
       Url = 'http://localhost:5000/api/bookings/calculate-price'; 
       Method = 'POST'; 
       Body = '{"showtimeId":1,"seatCount":2,"promoCode":"SALE10"}'; 
       ContentType = 'application/json' 
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
        Write-Host "[$($res.StatusCode)] $($ep.Method) $($ep.Url) - OK ($($res.RawContentLength) bytes)" -ForegroundColor Green
    } catch {
        Write-Host "[FAIL] $($ep.Method) $($ep.Url) : $_" -ForegroundColor Red
        $allPassed = $false
    }
}

if ($allPassed) {
    Write-Host "`n>>> Tất cả 8/8 Web MVC & API endpoints đều phản hồi 200 OK thành công! <<<" -ForegroundColor Cyan
} else {
    exit 1
}
