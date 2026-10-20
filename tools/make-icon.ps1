param([string]$OutIco, [string]$OutPng)
Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 256.0

    # Speech bubble body (rounded rect) + tail
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $x = 16*$s; $y = 28*$s; $w = 224*$s; $h = 168*$s; $r = 64*$s
    $path.AddArc($x, $y, $r, $r, 180, 90)
    $path.AddArc($x+$w-$r, $y, $r, $r, 270, 90)
    $path.AddArc($x+$w-$r, $y+$h-$r, $r, $r, 0, 90)
    $path.AddLine($x+$w-$r, $y+$h, 104*$s, $y+$h)
    $path.AddLine(104*$s, $y+$h, 64*$s, 236*$s)
    $path.AddLine(64*$s, 236*$s, 72*$s, $y+$h)
    $path.AddArc($x, $y+$h-$r, $r, $r, 90, 90)
    $path.CloseFigure()
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0,0), (New-Object System.Drawing.PointF $size,$size), ([System.Drawing.Color]::FromArgb(255,255,143,186)), ([System.Drawing.Color]::FromArgb(255,180,140,255))
    $g.FillPath($brush, $path)
    if ($size -ge 32) {
        $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,255,255,255)), ([Math]::Max(1, 8*$s))
        $g.DrawPath($pen, $path)
    }

    # Heart
    $heart = New-Object System.Drawing.Drawing2D.GraphicsPath
    $cx = 128*$s; $cy = 104*$s; $hs = 46*$s
    $heart.AddBezier($cx, $cy+$hs*0.9, $cx-$hs*1.6, $cy-$hs*0.1, $cx-$hs*0.7, $cy-$hs*1.3, $cx, $cy-$hs*0.45)
    $heart.AddBezier($cx, $cy-$hs*0.45, $cx+$hs*0.7, $cy-$hs*1.3, $cx+$hs*1.6, $cy-$hs*0.1, $cx, $cy+$hs*0.9)
    $heart.CloseFigure()
    $g.FillPath([System.Drawing.Brushes]::White, $heart)

    # Sparkle
    if ($size -ge 48) {
        $sp = New-Object System.Drawing.Drawing2D.GraphicsPath
        $sx = 206*$s; $sy = 62*$s; $a = 20*$s; $b = 5*$s
        $sp.AddPolygon([System.Drawing.PointF[]]@(
            (New-Object System.Drawing.PointF $sx, ($sy-$a)), (New-Object System.Drawing.PointF ($sx+$b), ($sy-$b)),
            (New-Object System.Drawing.PointF ($sx+$a), $sy), (New-Object System.Drawing.PointF ($sx+$b), ($sy+$b)),
            (New-Object System.Drawing.PointF $sx, ($sy+$a)), (New-Object System.Drawing.PointF ($sx-$b), ($sy+$b)),
            (New-Object System.Drawing.PointF ($sx-$a), $sy), (New-Object System.Drawing.PointF ($sx-$b), ($sy-$b))))
        $g.FillPath([System.Drawing.Brushes]::White, $sp)
    }
    $g.Dispose()
    return $bmp
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = foreach ($sz in $sizes) {
    $bmp = New-IconBitmap $sz
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($sz -eq 256 -and $OutPng) { $bmp.Save($OutPng, [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
    ,$ms.ToArray()
}

$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $out
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]; $len = $pngs[$i].Length
    $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz }))); $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))
    $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$len); $bw.Write([UInt32]$offset)
    $offset += $len
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($OutIco, $out.ToArray())
"wrote $OutIco ($($out.Length) bytes)"
