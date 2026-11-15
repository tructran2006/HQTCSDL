Add-Type -AssemblyName System.Drawing

$imgDir = "src/CineSeat.Web/wwwroot/images"
if (-not (Test-Path $imgDir)) {
    New-Item -ItemType Directory -Path $imgDir -Force | Out-Null
}

$movies = @(
    @{ Id = 1;  Title = "DUNE: PART TWO"; Sub = "Khoa học viễn tưởng • 166 phút"; Rating = "T13"; C1 = "#1A0F00"; C2 = "#C97010"; C3 = "#0A0500" },
    @{ Id = 2;  Title = "OPPENHEIMER"; Sub = "Tiểu sử / Chính kịch • 180 phút"; Rating = "T16"; C1 = "#140A00"; C2 = "#D84315"; C3 = "#080400" },
    @{ Id = 3;  Title = "INSIDE OUT 2"; Sub = "Hoạt hình • 96 phút"; Rating = "P"; C1 = "#1A0033"; C2 = "#7B1FA2"; C3 = "#0D47A1" },
    @{ Id = 4;  Title = "KUNG FU PANDA 4"; Sub = "Hoạt hình / Hài • 94 phút"; Rating = "P"; C1 = "#002B11"; C2 = "#2E7D32"; C3 = "#F9A825" },
    @{ Id = 5;  Title = "SPIDER-MAN: SPIDER-VERSE"; Sub = "Hoạt hình / Phiêu lưu • 140 phút"; Rating = "T13"; C1 = "#1A002C"; C2 = "#C2185B"; C3 = "#0097A7" },
    @{ Id = 6;  Title = "INTERSTELLAR"; Sub = "Khoa học viễn tưởng • 169 phút"; Rating = "T13"; C1 = "#020712"; C2 = "#1565C0"; C3 = "#000B1E" },
    @{ Id = 7;  Title = "GODZILLA X KONG"; Sub = "Hành động / Viễn tưởng • 115 phút"; Rating = "T13"; C1 = "#210008"; C2 = "#C62828"; C3 = "#AD1457" },
    @{ Id = 8;  Title = "LẬT MẶT 7"; Sub = "Gia đình / Tâm lý • 138 phút"; Rating = "K"; C1 = "#2C1400"; C2 = "#EF6C00"; C3 = "#4E342E" },
    @{ Id = 9;  Title = "MAI"; Sub = "Tâm lý / Tình cảm • 131 phút"; Rating = "T18"; C1 = "#260515"; C2 = "#880E4F"; C3 = "#1A000E" },
    @{ Id = 10; Title = "DEADPOOL & WOLVERINE"; Sub = "Hành động / Hài hước • 128 phút"; Rating = "T18"; C1 = "#1A0000"; C2 = "#B71C1C"; C3 = "#FBC02D" },
    @{ Id = 11; Title = "DESPICABLE ME 4"; Sub = "Hoạt hình / Hài • 95 phút"; Rating = "P"; C1 = "#001E3D"; C2 = "#FBC02D"; C3 = "#0277BD" },
    @{ Id = 12; Title = "A QUIET PLACE: DAY ONE"; Sub = "Kinh dị / Giật gân • 99 phút"; Rating = "T16"; C1 = "#0B0C10"; C2 = "#45A29E"; C3 = "#1F2833" },
    @{ Id = 13; Title = "FURIOSA: A MAD MAX SAGA"; Sub = "Hành động / Phiêu lưu • 148 phút"; Rating = "T18"; C1 = "#1E0D00"; C2 = "#E65100"; C3 = "#BF360C" }
)

$width = 600
$height = 900

foreach ($m in $movies) {
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    # 1. Gradient Background
    $color1 = [System.Drawing.ColorTranslator]::FromHtml($m.C1)
    $color2 = [System.Drawing.ColorTranslator]::FromHtml($m.C2)
    $rect = New-Object System.Drawing.Rectangle 0, 0, $width, $height
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $color1, $color2, 60.0
    $g.FillRectangle($brush, $rect)

    # 2. Dark Overlay for readability
    $darkColor = [System.Drawing.Color]::FromArgb(180, 10, 10, 15)
    $darkBrush = New-Object System.Drawing.SolidBrush $darkColor
    $g.FillRectangle($darkBrush, 0, ($height - 350), $width, 350)

    # 3. Top branding
    $brandFont = [System.Drawing.Font]::new("Arial", [float]11, [System.Drawing.FontStyle]::Bold)
    $brandBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml("#E50914"))
    $g.DrawString("CINESEAT CINEMA EXCLUSIVE", $brandFont, $brandBrush, 40, 40)

    # Decorative line
    $pen = New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml("#E50914")), 2
    $g.DrawLine($pen, 40, 65, 140, 65)

    # 4. Rating Badge
    $badgeBgColor = switch ($m.Rating) {
        "P"   { "#2E7D32" } # Green
        "K"   { "#0288D1" } # Blue
        "T13" { "#F57C00" } # Orange
        "T16" { "#E65100" } # Dark Orange
        "T18" { "#C62828" } # Red
        Default { "#757575" }
    }
    $badgeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($badgeBgColor))
    $badgeRect = New-Object System.Drawing.Rectangle ($width - 110), 38, 70, 32
    $g.FillRectangle($badgeBrush, $badgeRect)

    $badgeFont = [System.Drawing.Font]::new("Arial", [float]12, [System.Drawing.FontStyle]::Bold)
    $whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString($m.Rating, $badgeFont, $whiteBrush, [System.Drawing.RectangleF]$badgeRect, $sf)

    # 5. Middle Cinematic Abstract Shape / Film Icon
    $accentColor = [System.Drawing.ColorTranslator]::FromHtml($m.C3)
    $accentBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(60, $accentColor.R, $accentColor.G, $accentColor.B))
    $g.FillEllipse($accentBrush, 100, 200, 400, 400)

    # Film Reel outline
    $reelPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(80, 255, 255, 255)), 3
    $g.DrawEllipse($reelPen, 180, 280, 240, 240)
    $g.DrawEllipse($reelPen, 260, 360, 80, 80)

    # 6. Movie Title
    $titleFont = [System.Drawing.Font]::new("Arial", [float]26, [System.Drawing.FontStyle]::Bold)
    $titleRect = New-Object System.Drawing.RectangleF 40, ($height - 240), ($width - 80), 90
    $titleSf = New-Object System.Drawing.StringFormat
    $titleSf.Alignment = [System.Drawing.StringAlignment]::Near
    $titleSf.LineAlignment = [System.Drawing.StringAlignment]::Near

    # Text shadow
    $shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 0, 0, 0))
    $shadowRect = New-Object System.Drawing.RectangleF 42, ($height - 238), ($width - 80), 90
    $g.DrawString($m.Title, $titleFont, $shadowBrush, $shadowRect, $titleSf)
    $g.DrawString($m.Title, $titleFont, $whiteBrush, $titleRect, $titleSf)

    # 7. Subtitle / Genre & Duration
    $subFont = [System.Drawing.Font]::new("Arial", [float]13, [System.Drawing.FontStyle]::Regular)
    $subBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml("#CCCCCC"))
    $g.DrawString($m.Sub, $subFont, $subBrush, 40, ($height - 120))

    # 8. Bottom Gold Accent Bar
    $goldBarBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml("#F5C518"))
    $g.FillRectangle($goldBarBrush, 0, ($height - 8), $width, 8)

    $outPath = Join-Path $imgDir "poster-$($m.Id).jpg"
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Jpeg)

    $g.Dispose()
    $bmp.Dispose()
    Write-Host "Created $outPath" -ForegroundColor Green
}

# Default Fallback Poster
$fallbackPath = Join-Path $imgDir "default-poster.jpg"
Copy-Item (Join-Path $imgDir "poster-1.jpg") $fallbackPath -Force
Write-Host "Created $fallbackPath" -ForegroundColor Cyan
