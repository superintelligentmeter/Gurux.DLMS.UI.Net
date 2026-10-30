param([Parameter(Mandatory=$true)][string]$Output)
# superintelligentmeter contributors, 2026-10-30. Screenshot montage only. GPL-2.0-only.
Add-Type -AssemblyName System.Drawing
$cases=Get-Content (Join-Path $Output 'layout-cases.json') -Raw|ConvertFrom-Json
$folder=Join-Path $Output 'contacts';New-Item -ItemType Directory -Force -Path $folder|Out-Null
$index=[Collections.Generic.List[object]]::new()
foreach($kind in @('ObjectView','Dialog')) {
    $entries=@(foreach($case in $cases|Where-Object{$_.Kind -eq $kind -and $_.State -eq 'representative'}|Sort-Object Type) {
        $data=Get-Content $case.Result -Raw|ConvertFrom-Json
        $wide=$data.Captures|Where-Object{($_.RequestedWidth -eq 1500 -or $_.Width -eq 1500) -and $_.Variant -notmatch 'tab|bottom|restored'}|Select-Object -First 1
        $narrow=$data.Captures|Where-Object{($_.RequestedWidth -eq 620 -or $_.Width -eq 620) -and $_.Variant -notmatch 'tab|bottom|restored'}|Select-Object -First 1
        if($wide -and $narrow){[pscustomobject]@{Type=$case.Type;Dpi=$narrow.Dpi;Wide=$wide.Path;Narrow=$narrow.Path;Kind=$kind}}
    })
    $prefix=if($kind -eq 'ObjectView'){'objects'}else{'dialogs'}
    for($page=0;$page*8 -lt $entries.Count;$page++) {
        $bitmap=[Drawing.Bitmap]::new(2000,1560);$bitmap.SetResolution(96,96)
        $g=[Drawing.Graphics]::FromImage($bitmap);$g.Clear([Drawing.Color]::White)
        $font=[Drawing.Font]::new('Segoe UI',13,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
        for($cell=0;$cell -lt 8 -and $page*8+$cell -lt $entries.Count;$cell++) {
            $entry=$entries[$page*8+$cell];$x=($cell%2)*1000;$y=[math]::Floor($cell/2)*390
            $g.DrawString(($entry.Type.Split('.')[-1]+' / '+$entry.Dpi+' DPI / wide1500 + narrow620'),$font,[Drawing.Brushes]::Black,[Drawing.RectangleF]::new($x+5,$y+3,990,22))
            foreach($side in @(@{Path=$entry.Wide;X=$x+5;W=600},@{Path=$entry.Narrow;X=$x+615;W=380})) {
                $image=[Drawing.Bitmap]::FromFile($side.Path);$image.SetResolution(96,96)
                $factor=[math]::Min($side.W/[double]$image.Width,355/[double]$image.Height)
                $g.DrawImage($image,[Drawing.Rectangle]::new($side.X,$y+28,[int]($image.Width*$factor),[int]($image.Height*$factor)))
                $image.Dispose()
            }
            $index.Add([pscustomobject]@{Type=$entry.Type;Kind=$kind;Dpi=$entry.Dpi;Sheet=$prefix+'-'+($page+1).ToString('00')+'.png';Cell=$cell;Wide=$entry.Wide;Narrow=$entry.Narrow})
        }
        $bitmap.Save((Join-Path $folder ($prefix+'-'+($page+1).ToString('00')+'.png')))
        $font.Dispose();$g.Dispose();$bitmap.Dispose()
    }
}
$index|Export-Csv -NoTypeInformation (Join-Path $folder 'contact-index.csv')
$index|Group-Object Kind|Select-Object Name,Count
