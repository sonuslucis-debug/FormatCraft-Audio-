# Builds Images\app.ico from Images\logo.png, with every size Windows asks for.
# Run after replacing logo.png:  powershell -ExecutionPolicy Bypass -File build\make-icon.ps1

param(
    [string]$Source = (Join-Path $PSScriptRoot "..\Images\logo.png"),
    [string]$Target = (Join-Path $PSScriptRoot "..\Images\app.ico")
)

Add-Type -AssemblyName System.Drawing

$sizes = 16, 24, 32, 48, 64, 128, 256
$resolved = (Resolve-Path -LiteralPath $Source).Path
$src = [System.Drawing.Image]::FromFile($resolved)
$frames = New-Object System.Collections.ArrayList

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($src, 0, 0, $size, $size)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    [void]$frames.Add($ms.ToArray())
    $ms.Dispose()
}
$src.Dispose()

$fs = [System.IO.File]::Create($Target)
$bw = New-Object System.IO.BinaryWriter($fs)

# ICONDIR: reserved, type = icon, number of images
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)

$offset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $dim = if ($size -ge 256) { 0 } else { $size }   # 0 means 256 in an ICO entry
    $bw.Write([Byte]$dim)          # width
    $bw.Write([Byte]$dim)          # height
    $bw.Write([Byte]0)             # palette colours
    $bw.Write([Byte]0)             # reserved
    $bw.Write([UInt16]1)           # colour planes
    $bw.Write([UInt16]32)          # bits per pixel
    $bw.Write([UInt32]$frames[$i].Length)
    $bw.Write([UInt32]$offset)
    $offset += $frames[$i].Length
}

foreach ($frame in $frames) { $bw.Write($frame) }

$bw.Flush()
$bw.Close()
$fs.Close()

"Wrote $Target ($($sizes -join ', ') px)"
