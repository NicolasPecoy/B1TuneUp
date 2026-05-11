param(
    [string]$InstallDir = "C:\Program Files\B1TuneUp",
    [string]$ServiceName = "B1TuneUpWorker",
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"
$logDir = Join-Path $InstallDir "install-logs"
if (!(Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("worker-service-{0:yyyyMMddHHmmss}.log" -f (Get-Date))
Start-Transcript -Path $logFile -Force | Out-Null

try {
    $exe = Join-Path $InstallDir "B1TuneUp.WorkerService.exe"
    if ($Uninstall) {
        $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($svc) {
            if ($svc.Status -ne "Stopped") { Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue }
            sc.exe delete $ServiceName | Out-Null
        }
        Write-Host "Worker service removed."
        return
    }

    if (!(Test-Path $exe)) { throw "Worker executable not found: $exe" }
    $existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existing) {
        if ($existing.Status -ne "Stopped") { Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue }
        sc.exe delete $ServiceName | Out-Null
        Start-Sleep -Seconds 2
    }

    $binPath = "`"$exe`" --service"
    sc.exe create $ServiceName binPath= $binPath start= auto DisplayName= "B1TuneUp Worker" | Out-Null
    sc.exe description $ServiceName "B1TuneUp scheduler, report, integration and queue worker." | Out-Null
    Start-Service -Name $ServiceName
    Write-Host "Worker service installed and started."
}
finally {
    Stop-Transcript | Out-Null
}
