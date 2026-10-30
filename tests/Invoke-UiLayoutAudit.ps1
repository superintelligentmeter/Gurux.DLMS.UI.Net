param(
    [Parameter(Mandatory=$true)][string]$Binaries,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$Filter = '',
    [ValidateRange(1,2)][int]$Workers = 2,
    [int]$TimeoutSeconds = 90
)
# superintelligentmeter contributors, 2026-10-30. GPL-2.0-only.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
$Output=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path $Output | Out-Null
& (Join-Path $Binaries 'UiReviewTests.exe') inventory (Join-Path $Output 'inventory')
if($LASTEXITCODE -ne 0){throw 'Compiled inventory failed'}
$inventory=Get-Content (Join-Path $Output 'inventory\inventory.json') -Raw | ConvertFrom-Json
$screens=@([Windows.Forms.Screen]::AllScreens)
$screens|Select-Object DeviceName,Bounds,WorkingArea|ConvertTo-Json -Depth 3|Set-Content (Join-Path $Output 'physical-screens.json')
Get-FileHash (Join-Path $Binaries 'Gurux.DLMS.UI.dll'),(Join-Path $Binaries 'Gurux.DLMS.dll')|Select-Object Path,Hash|ConvertTo-Json|Set-Content (Join-Path $Output 'assembly-hashes.json')
$queue=[Collections.Generic.Queue[object]]::new()
foreach($state in @('representative','blank','structured')) {
    foreach($monitor in 0..($screens.Count-1)) {
        foreach($entry in $inventory.Screens) {
            if($Filter -and !@($Filter.Split(',')|Where-Object{$entry.Type.Contains($_)}).Count){continue}
            if($entry.Kind -eq 'Dialog' -and $state -ne 'representative'){continue}
            if($state -eq 'structured' -and $entry.ShortName -notin @('GXDLMSDataView','GXDLMSPppSetupView','GXDLMSIp4SetupView')){continue}
            if($entry.Kind -notin @('ObjectView','Dialog')){continue}
            $caseName=$entry.ShortName+'--'+$state+'--monitor'+$monitor
            $folder=Join-Path $Output ('cases\'+$caseName)
            $queue.Enqueue([pscustomobject]@{Kind=$entry.Kind;Type=$entry.Type;State=$state;Monitor=$monitor;Screen=$screens[$monitor].DeviceName;Folder=$folder;Case=$caseName})
        }
    }
}
$expected=$queue.Count
$active=[Collections.Generic.List[object]]::new()
$completed=[Collections.Generic.List[object]]::new()
while($queue.Count -or $active.Count) {
    while($queue.Count -and $active.Count -lt $Workers) {
        $case=$queue.Dequeue();New-Item -ItemType Directory -Force -Path $case.Folder|Out-Null
        $exe=if($case.Kind -eq 'ObjectView'){'UiReviewTests.exe'}else{'UiDialogReviewTests.exe'}
        $start=[Diagnostics.ProcessStartInfo]::new((Join-Path $Binaries $exe))
        $start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true;$start.WorkingDirectory=$case.Folder
        $arguments=if($case.Kind -eq 'ObjectView'){@('worker',$case.Type,$case.State,$case.Folder,[string]$case.Monitor,'layout')}else{@('worker',$case.Type,$case.Folder,'layout',[string]$case.Monitor)}
        foreach($argument in $arguments){$start.ArgumentList.Add($argument)}
        $process=[Diagnostics.Process]::Start($start)
        $active.Add([pscustomobject]@{Case=$case;Process=$process;Started=[DateTime]::UtcNow;Out=$process.StandardOutput.ReadToEndAsync();Err=$process.StandardError.ReadToEndAsync()})
    }
    foreach($job in @($active.ToArray())) {
        $timedOut=([DateTime]::UtcNow-$job.Started).TotalSeconds -gt $TimeoutSeconds
        if(!$job.Process.HasExited -and !$timedOut){continue}
        if(!$job.Process.HasExited){$job.Process.Kill($true);$job.Process.WaitForExit(5000)|Out-Null}
        ($job.Out.GetAwaiter().GetResult()+$job.Err.GetAwaiter().GetResult())|Set-Content (Join-Path $job.Case.Folder 'worker.log')
        $resultPath=Join-Path $job.Case.Folder 'result.json'
        $captureCount=0;$errorText=''
        try {$data=Get-Content $resultPath -Raw|ConvertFrom-Json;$captureCount=@($data.Captures).Count;if($job.Case.Kind -eq 'Dialog'){$errorText=$data.Error}else{if($data.Status -ne 'Passed'){$errorText=$data.Status+': '+($data.Errors -join ' | ')}}} catch {$errorText=$_.Exception.Message}
        if($timedOut){$errorText='Timeout: '+$errorText}
        $record=[pscustomobject]@{Kind=$job.Case.Kind;Type=$job.Case.Type;State=$job.Case.State;Monitor=$job.Case.Monitor;Screen=$job.Case.Screen;Case=$job.Case.Case;Result=$resultPath;ExitCode=$job.Process.ExitCode;Captures=$captureCount;Error=$errorText;Seconds=[math]::Round(([DateTime]::UtcNow-$job.Started).TotalSeconds,2)}
        $completed.Add($record);$active.Remove($job)|Out-Null;$job.Process.Dispose()
        ConvertTo-Json -InputObject @($completed.ToArray()) -Depth 6|Set-Content (Join-Path $Output 'layout-cases.json')
        Write-Output ($completed.Count.ToString()+'/'+$expected+' '+$record.Case+' captures='+$captureCount+' '+$errorText)
    }
    if($active.Count){Start-Sleep -Milliseconds 100}
}
if(@($completed|Where-Object{$_.ExitCode -ne 0 -or $_.Error}).Count){exit 1}
