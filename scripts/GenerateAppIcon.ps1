$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$iconPath = Join-Path (Split-Path -Parent $scriptDirectory) 'assets\Shutdown.ico'
$iconDirectory = Split-Path -Parent $iconPath
if (-not (Test-Path -LiteralPath $iconDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $iconDirectory -Force | Out-Null
}

function New-RoundedRectanglePath {
    param([float]$X, [float]$Y, [float]$Width, [float]$Height, [float]$Radius)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconPng {
    param([int]$Size)

    $bitmap = New-Object System.Drawing.Bitmap(
        $Size,
        $Size,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $tilePath = $null
    $backgroundBrush = $null
    $borderPen = $null
    $glowPen = $null
    $ringPen = $null
    $highlightPen = $null
    $stream = New-Object System.IO.MemoryStream

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.ScaleTransform($Size / 256.0, $Size / 256.0)

        $tilePath = New-RoundedRectanglePath 12 12 232 232 48
        $backgroundBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            (New-Object System.Drawing.Rectangle(12, 12, 232, 232)),
            ([System.Drawing.Color]::FromArgb(32, 49, 75)),
            ([System.Drawing.Color]::FromArgb(12, 19, 32)),
            45.0)
        $graphics.FillPath($backgroundBrush, $tilePath)

        $borderPen = New-Object System.Drawing.Pen(
            ([System.Drawing.Color]::FromArgb(130, 72, 139, 205)),
            3.0)
        $graphics.DrawPath($borderPen, $tilePath)

        $glowPen = New-Object System.Drawing.Pen(
            ([System.Drawing.Color]::FromArgb(55, 56, 188, 255)),
            25.0)
        $glowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $glowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawArc($glowPen, 68, 80, 120, 120, 315, 270)
        $graphics.DrawLine($glowPen, 128, 42, 128, 112)

        $ringPen = New-Object System.Drawing.Pen(
            ([System.Drawing.Color]::FromArgb(255, 58, 177, 238)),
            16.0)
        $ringPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $ringPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawArc($ringPen, 68, 80, 120, 120, 315, 270)
        $graphics.DrawLine($ringPen, 128, 42, 128, 108)

        $highlightPen = New-Object System.Drawing.Pen(
            ([System.Drawing.Color]::FromArgb(255, 182, 242, 255)),
            5.0)
        $highlightPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $highlightPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawLine($highlightPen, 128, 49, 128, 96)

        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return ,$stream.ToArray()
    }
    finally {
        if ($highlightPen) { $highlightPen.Dispose() }
        if ($ringPen) { $ringPen.Dispose() }
        if ($glowPen) { $glowPen.Dispose() }
        if ($borderPen) { $borderPen.Dispose() }
        if ($backgroundBrush) { $backgroundBrush.Dispose() }
        if ($tilePath) { $tilePath.Dispose() }
        $graphics.Dispose()
        $bitmap.Dispose()
        $stream.Dispose()
    }
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = @()
foreach ($size in $sizes) {
    $images += ,(New-IconPng $size)
}

$temporaryPath = $iconPath + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
$backupPath = $iconPath + '.' + [Guid]::NewGuid().ToString('N') + '.bak'
$stream = $null
$writer = $null
try {
    $stream = [System.IO.File]::Open(
        $temporaryPath,
        [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None)
    $writer = New-Object System.IO.BinaryWriter($stream)
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$images.Count)

    $imageOffset = 6 + (16 * $images.Count)
    for ($index = 0; $index -lt $images.Count; $index++) {
        $size = $sizes[$index]
        $dimension = if ($size -eq 256) { [byte]0 } else { [byte]$size }
        $image = [byte[]]$images[$index]
        $writer.Write($dimension)
        $writer.Write($dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$image.Length)
        $writer.Write([UInt32]$imageOffset)
        $imageOffset += $image.Length
    }
    foreach ($image in $images) {
        $writer.Write([byte[]]$image)
    }
    $writer.Flush()
    $writer.Dispose()
    $writer = $null
    $stream.Dispose()
    $stream = $null

    if ([System.IO.File]::Exists($iconPath)) {
        [System.IO.File]::Replace($temporaryPath, $iconPath, $backupPath)
    }
    else {
        [System.IO.File]::Move($temporaryPath, $iconPath)
    }
}
finally {
    if ($writer) { $writer.Dispose() }
    elseif ($stream) { $stream.Dispose() }
    if ([System.IO.File]::Exists($temporaryPath)) {
        [System.IO.File]::Delete($temporaryPath)
    }
    if ([System.IO.File]::Exists($backupPath)) {
        [System.IO.File]::Delete($backupPath)
    }
}

Write-Host "Generated branded multi-resolution application icon: $iconPath"
