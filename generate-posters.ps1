# =============================================================================
# CineSeat - Bộ sinh 13 Poster Phim Tự Thiết Kế Nghệ Thuật (Social Media Cinema Card)
# Không tải từ internet - Chuẩn tỉ lệ 2:3 (600x900px) - Ghi trực tiếp wwwroot/images/
# =============================================================================
Add-Type -AssemblyName System.Drawing

$imgDir = "src/CineSeat.Web/wwwroot/images"
if (-not (Test-Path $imgDir)) {
    New-Item -ItemType Directory -Path $imgDir -Force | Out-Null
}

$width = 600
$height = 900

$movies = @(
    @{
        Id = 1
        Title = "DUNE: PART TWO"
        Genre = "Khoa học viễn tưởng"
        Duration = "166 phút"
        Rating = "T13"
        Theme = "Desert Sun & Sand Dunes"
        BgTop = "#2B1100"; BgMid = "#B85D06"; BgBottom = "#0D0500"
        Accent = "#F5C518"; Motif = "Dune"
    },
    @{
        Id = 2
        Title = "OPPENHEIMER"
        Genre = "Tiểu sử / Chính kịch"
        Duration = "180 phút"
        Rating = "T16"
        Theme = "Atomic Fire & Charcoal"
        BgTop = "#1A0500"; BgMid = "#D84315"; BgBottom = "#080402"
        Accent = "#FF9E80"; Motif = "Atomic"
    },
    @{
        Id = 3
        Title = "INSIDE OUT 2"
        Genre = "Hoạt hình"
        Duration = "96 phút"
        Rating = "P"
        Theme = "Multicolor Emotion Orbs"
        BgTop = "#240046"; BgMid = "#7B2CBF"; BgBottom = "#0A092D"
        Accent = "#FFD166"; Motif = "Emotions"
    },
    @{
        Id = 4
        Title = "KUNG FU PANDA 4"
        Genre = "Hoạt hình / Hài"
        Duration = "94 phút"
        Rating = "P"
        Theme = "East Asian Red & Jade Gold"
        BgTop = "#4A0E00"; BgMid = "#B71C1C"; BgBottom = "#1B5E20"
        Accent = "#FFD54F"; Motif = "YinYang"
    },
    @{
        Id = 5
        Title = "SPIDER-MAN: SPIDER-VERSE"
        Genre = "Hoạt hình / Phiêu lưu"
        Duration = "140 phút"
        Rating = "T13"
        Theme = "Multiverse Glitch Neon"
        BgTop = "#18002E"; BgMid = "#D81B60"; BgBottom = "#006064"
        Accent = "#00E5FF"; Motif = "WebGlitch"
    },
    @{
        Id = 6
        Title = "INTERSTELLAR"
        Genre = "Khoa học viễn tưởng"
        Duration = "169 phút"
        Rating = "T13"
        Theme = "Cosmic Deep & Black Hole"
        BgTop = "#02071A"; BgMid = "#0D47A1"; BgBottom = "#00030A"
        Accent = "#E0E0E0"; Motif = "BlackHole"
    },
    @{
        Id = 7
        Title = "GODZILLA X KONG"
        Genre = "Hành động / Viễn tưởng"
        Duration = "115 phút"
        Rating = "T13"
        Theme = "Neon Pink & Electric Cyan"
        BgTop = "#2A0012"; BgMid = "#E91E63"; BgBottom = "#004D40"
        Accent = "#18FFFF"; Motif = "MonsterClash"
    },
    @{
        Id = 8
        Title = "LẬT MẶT 7: MỘT ĐIỀU ƯỚC"
        Genre = "Gia đình / Tâm lý"
        Duration = "138 phút"
        Rating = "K"
        Theme = "Warm Gold & Family Wood"
        BgTop = "#3E2723"; BgMid = "#D84315"; BgBottom = "#1A0C00"
        Accent = "#FFE082"; Motif = "FamilySun"
    },
    @{
        Id = 9
        Title = "MAI"
        Genre = "Tâm lý / Tình cảm"
        Duration = "131 phút"
        Rating = "T18"
        Theme = "Deep Rose & Romantic Night"
        BgTop = "#310518"; BgMid = "#880E4F"; BgBottom = "#12020B"
        Accent = "#F48FB1"; Motif = "RomanticMoon"
    },
    @{
        Id = 10
        Title = "DEADPOOL & WOLVERINE"
        Genre = "Hành động / Hài hước"
        Duration = "128 phút"
        Rating = "T18"
        Theme = "Crimson Red & Metallic Gold"
        BgTop = "#2E0000"; BgMid = "#B71C1C"; BgBottom = "#F57F17"
        Accent = "#FFD600"; Motif = "SlashDuel"
    },
    @{
        Id = 11
        Title = "DESPICABLE ME 4"
        Genre = "Hoạt hình / Hài"
        Duration = "95 phút"
        Rating = "P"
        Theme = "Bright Minion Yellow & Denim"
        BgTop = "#0D233A"; BgMid = "#01579B"; BgBottom = "#FBC02D"
        Accent = "#FFF176"; Motif = "Goggles"
    },
    @{
        Id = 12
        Title = "A QUIET PLACE: DAY ONE"
        Genre = "Kinh dị / Giật gân"
        Duration = "99 phút"
        Rating = "T16"
        Theme = "Smoke Gray & Warning Red"
        BgTop = "#121417"; BgMid = "#263238"; BgBottom = "#3E0000"
        Accent = "#FF5252"; Motif = "SoundWave"
    },
    @{
        Id = 13
        Title = "FURIOSA: A MAD MAX SAGA"
        Genre = "Hành động / Phiêu lưu"
        Duration = "148 phút"
        Rating = "T18"
        Theme = "Sandstorm Dust & Dark Steel"
        BgTop = "#331200"; BgMid = "#BF360C"; BgBottom = "#1C1412"
        Accent = "#FFAB40"; Motif = "ApocalypticWheel"
    }
)

foreach ($m in $movies) {
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    # 1. Gradient Nền Đa Tầng Điện Ảnh
    $cTop = [System.Drawing.ColorTranslator]::FromHtml($m.BgTop)
    $cMid = [System.Drawing.ColorTranslator]::FromHtml($m.BgMid)
    $cBottom = [System.Drawing.ColorTranslator]::FromHtml($m.BgBottom)
    $cAccent = [System.Drawing.ColorTranslator]::FromHtml($m.Accent)

    $rect = New-Object System.Drawing.Rectangle 0, 0, $width, $height
    $gradBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($rect, $cTop, $cMid, [float]55.0)
    $g.FillRectangle($gradBrush, $rect)

    # Lớp bóng đổ đa chiều
    $darkRect = New-Object System.Drawing.Rectangle 0, 350, $width, 550
    $darkBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($darkRect, [System.Drawing.Color]::FromArgb(40, 0, 0, 0), $cBottom, [float]90.0)
    $g.FillRectangle($darkBrush, $darkRect)

    # 2. Khung Viền Thẻ Cinema Card Hiện Đại (Card Border & Corner Accents)
    $cardPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(60, 255, 255, 255)), 1
    $g.DrawRectangle($cardPen, 24, 24, ($width - 48), ($height - 48))

    # Góc thẻ thời thượng
    $cornerPen = New-Object System.Drawing.Pen $cAccent, 3
    $cLen = 22
    # Top-Left
    $g.DrawLine($cornerPen, 24, 24, 24 + $cLen, 24)
    $g.DrawLine($cornerPen, 24, 24, 24, 24 + $cLen)
    # Top-Right
    $g.DrawLine($cornerPen, $width - 24, 24, $width - 24 - $cLen, 24)
    $g.DrawLine($cornerPen, $width - 24, 24, $width - 24, 24 + $cLen)
    # Bottom-Left
    $g.DrawLine($cornerPen, 24, $height - 24, 24 + $cLen, $height - 24)
    $g.DrawLine($cornerPen, 24, $height - 24, 24, $height - 24 - $cLen)
    # Bottom-Right
    $g.DrawLine($cornerPen, $width - 24, $height - 24, $width - 24 - $cLen, $height - 24)
    $g.DrawLine($cornerPen, $width - 24, $height - 24, $width - 24, $height - 24 - $cLen)

    # 3. Header Thẻ: Thương Hiệu CineSeat Exclusive & Badge Độ Tuổi
    $brandFont = [System.Drawing.Font]::new("Arial", [float]10, [System.Drawing.FontStyle]::Bold)
    $whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $accentBrush = New-Object System.Drawing.SolidBrush $cAccent
    $redBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml("#E50914"))

    $g.DrawString("CINESEAT CINEMA EXCLUSIVE", $brandFont, $accentBrush, 45, 45)

    $formatFont = [System.Drawing.Font]::new("Arial", [float]8, [System.Drawing.FontStyle]::Bold)
    $g.DrawString("IMAX • DOLBY ATMOS 2D", $formatFont, $whiteBrush, 45, 65)

    # Badge Độ Tuổi (Top-Right)
    $badgeColor = switch ($m.Rating) {
        "P"   { "#10B981" }
        "K"   { "#0288D1" }
        "T13" { "#F59E0B" }
        "T16" { "#EA580C" }
        "T18" { "#DC2626" }
        Default { "#757575" }
    }
    $badgeRect = New-Object System.Drawing.Rectangle ($width - 110), 42, 65, 30
    $badgeBgBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($badgeColor))
    $g.FillRectangle($badgeBgBrush, $badgeRect)
    $badgeFont = [System.Drawing.Font]::new("Arial", [float]11, [System.Drawing.FontStyle]::Bold)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString($m.Rating, $badgeFont, $whiteBrush, [System.Drawing.RectangleF]$badgeRect, $sf)

    # 4. Họa Tiết Nghệ Thuật Trung Tâm Đặc Trưng (Artistic Motif)
    switch ($m.Motif) {
        "Dune" {
            # Mặt trời sa mạc rực sáng + các đường cong cồn cát
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 245, 197, 24))), 180, 220, 240, 240)
            $dunePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 255, 171, 64)), 4
            $g.DrawArc($dunePen, 60, 320, 480, 260, 180, 180)
            $g.DrawArc($dunePen, 120, 360, 400, 220, 190, 160)
        }
        "Atomic" {
            # Quầng sáng vụ nổ hạt nhân & các vòng quỹ đạo nguyên tử
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 255, 87, 34))), 200, 240, 200, 200)
            $orbitPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(160, 255, 255, 255)), 2
            $g.DrawEllipse($orbitPen, 130, 270, 340, 140)
            $g.DrawEllipse($orbitPen, 230, 170, 140, 340)
        }
        "Emotions" {
            # Các quả cầu cảm xúc đa sắc rực rỡ
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(190, 255, 209, 102))), 180, 200, 150, 150) # Vui vẻ
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(190, 17, 138, 178))), 270, 230, 160, 160)  # Buồn bã
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(190, 239, 71, 111))), 210, 300, 170, 170)  # Lo âu
        }
        "YinYang" {
            # Thái cực đồ Á Đông vàng kim
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 255, 213, 79))), 190, 230, 220, 220)
            $yinPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 183, 28, 28)), 6
            $g.DrawArc($yinPen, 190, 230, 220, 220, 0, 360)
            $g.DrawLine($yinPen, 300, 230, 300, 450)
        }
        "WebGlitch" {
            # Lưới đa vũ trụ Spider-Verse + khung góc neon
            $webPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(160, 0, 229, 255)), 2
            for ($k = 0; $k -lt 6; $k++) {
                $step = $k * 30
                $g.DrawPolygon($webPen, @(
                    (New-Object System.Drawing.Point (300), (200 + $step)),
                    (New-Object System.Drawing.Point (420 - $step/2), (320 + $step/2)),
                    (New-Object System.Drawing.Point (300), (440 - $step)),
                    (New-Object System.Drawing.Point (180 + $step/2), (320 + $step/2))
                ))
            }
        }
        "BlackHole" {
            # Hố đen Gargantua & đĩa bồi tụ phát sáng
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Black)), 210, 250, 180, 180)
            $holePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 255, 255, 255)), 4
            $g.DrawEllipse($holePen, 100, 290, 400, 100)
            $g.DrawEllipse((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(120, 68, 138, 255)), 6), 195, 235, 210, 210)
        }
        "MonsterClash" {
            # Sóng xung kích đối đầu hai quái thú (Hồng neon & Xanh điện)
            $pPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 233, 30, 99)), 5
            $cPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 24, 255, 255)), 5
            $g.DrawArc($pPen, 80, 200, 300, 300, 270, 180)
            $g.DrawArc($cPen, 220, 200, 300, 300, 90, 180)
        }
        "FamilySun" {
            # Vầng sáng mặt trời ấm áp và vòng cung bảo bọc gia đình
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(190, 255, 224, 130))), 200, 220, 200, 200)
            $famPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 255, 112, 67)), 4
            $g.DrawArc($famPen, 150, 300, 300, 200, 180, 180)
        }
        "RomanticMoon" {
            # Ánh trăng hoa mai lãng mạn điện ảnh
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 244, 143, 177))), 210, 220, 190, 190)
            $moonPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(150, 255, 255, 255)), 3
            $g.DrawEllipse($moonPen, 200, 210, 210, 210)
        }
        "SlashDuel" {
            # Vết chém vuốt kim loại adamantium & hai dải màu Deadpool & Wolverine
            $slashRed = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 183, 28, 28))
            $slashGold = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 255, 214, 0))
            $g.FillPolygon($slashRed, @((New-Object System.Drawing.Point 160, 180), (New-Object System.Drawing.Point 280, 180), (New-Object System.Drawing.Point 210, 430), (New-Object System.Drawing.Point 130, 430)))
            $g.FillPolygon($slashGold, @((New-Object System.Drawing.Point 300, 180), (New-Object System.Drawing.Point 420, 180), (New-Object System.Drawing.Point 360, 430), (New-Object System.Drawing.Point 280, 430)))
            # Claw slashes
            $clawPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), 4
            $g.DrawLine($clawPen, 140, 240, 430, 330)
            $g.DrawLine($clawPen, 150, 270, 440, 360)
            $g.DrawLine($clawPen, 160, 300, 450, 390)
        }
        "Goggles" {
            # Kính bảo hộ Minion đặc trưng
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 255, 241, 118))), 160, 240, 280, 180)
            $gogPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 200, 200, 200)), 12
            $g.DrawEllipse($gogPen, 190, 250, 100, 100)
            $g.DrawEllipse($gogPen, 310, 250, 100, 100)
        }
        "SoundWave" {
            # Sóng âm thanh cảnh báo quái vật
            $wavePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 255, 82, 82)), 3
            for ($x = 80; $x -le 520; $x += 16) {
                $amp = [Math]::Sin(($x - 80) * 0.05) * 80 * [Math]::Sin(($x - 80) * 0.015)
                $g.DrawLine($wavePen, $x, [int](320 - [Math]::Abs($amp)), $x, [int](320 + [Math]::Abs($amp)))
            }
        }
        "ApocalypticWheel" {
            # Bánh xe cơ khí & bão cát hoang tàn Mad Max
            $wheelPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 255, 171, 64)), 6
            $g.DrawEllipse($wheelPen, 190, 220, 220, 220)
            for ($ang = 0; $ang -lt 360; $ang += 45) {
                $rad = $ang * [Math]::PI / 180
                $x2 = 300 + 110 * [Math]::Cos($rad)
                $y2 = 330 + 110 * [Math]::Sin($rad)
                $g.DrawLine($wheelPen, 300, 330, [int]$x2, [int]$y2)
            }
        }
    }

    # 5. Khung Kính Mờ Đáy Thẻ (Bottom Social Cinema Glass Card)
    $glassRect = New-Object System.Drawing.Rectangle 36, ($height - 300), ($width - 72), 260
    $glassBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(210, 10, 11, 16))
    $g.FillRectangle($glassBrush, $glassRect)

    # Viền trên thanh kính mờ (Dải màu điểm nhấn)
    $g.FillRectangle($accentBrush, 36, ($height - 300), ($width - 72), 4)

    # 6. Typography Tiêu Đề Phim Lớn & Sắc Nét
    $titleFont = [System.Drawing.Font]::new("Arial Black", [float]22, [System.Drawing.FontStyle]::Bold)
    $titleRect = New-Object System.Drawing.RectangleF 54, ($height - 280), ($width - 108), 75
    $titleSf = New-Object System.Drawing.StringFormat
    $titleSf.Alignment = [System.Drawing.StringAlignment]::Near
    $titleSf.LineAlignment = [System.Drawing.StringAlignment]::Near

    # Đổ bóng chữ
    $shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 0, 0, 0))
    $shadowRect = New-Object System.Drawing.RectangleF 56, ($height - 278), ($width - 108), 75
    $g.DrawString($m.Title, $titleFont, $shadowBrush, $shadowRect, $titleSf)
    $g.DrawString($m.Title, $titleFont, $whiteBrush, $titleRect, $titleSf)

    # 7. Thông Tin Thể Loại & Thời Lượng (Genre & Duration)
    $metaFont = [System.Drawing.Font]::new("Arial", [float]11, [System.Drawing.FontStyle]::Bold)
    $metaText = "$($m.Genre)  •  $($m.Duration)"
    $g.DrawString($metaText, $metaFont, $accentBrush, 54, ($height - 185))

    # Thanh tiến trình / Tag rạp
    $tagFont = [System.Drawing.Font]::new("Arial", [float]9, [System.Drawing.FontStyle]::Regular)
    $grayBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml("#AAAAAA"))
    $g.DrawString("CÔNG NGHỆ: DOLBY ATMOS • MÀN CHIẾU SIÊU SẮC NÉT", $tagFont, $grayBrush, 54, ($height - 150))

    # 8. Footer Thẻ Cinema: Đánh Giá 5 Sao & Mã Vé Minh Họa
    $starFont = [System.Drawing.Font]::new("Arial", [float]12, [System.Drawing.FontStyle]::Bold)
    $starBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml("#F5C518"))
    $g.DrawString("★ ★ ★ ★ ★", $starFont, $starBrush, 54, ($height - 110))

    $codeFont = [System.Drawing.Font]::new("Courier New", [float]9, [System.Drawing.FontStyle]::Bold)
    $g.DrawString("CINESEAT-CARD-2026-0$($m.Id)", $codeFont, $grayBrush, 310, ($height - 107))

    # Lưu File JPG
    $outPath = Join-Path $imgDir "poster-$($m.Id).jpg"
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Jpeg)

    $g.Dispose()
    $bmp.Dispose()
    Write-Host "Created $outPath" -ForegroundColor Green
}

# Poster Mặc Định
$fallbackPath = Join-Path $imgDir "default-poster.jpg"
Copy-Item (Join-Path $imgDir "poster-1.jpg") $fallbackPath -Force
Write-Host "Created $fallbackPath" -ForegroundColor Cyan
