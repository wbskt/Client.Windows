# ----------------------------------------
# uninstall-service.ps1
# Description: Stops and removes the WbsktClient Windows service
# ----------------------------------------

$ServiceName = "WbsktClient"

Write-Host "Attempting to uninstall service '$ServiceName'..." -ForegroundColor Cyan

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service '$ServiceName' not found. Nothing to uninstall." -ForegroundColor Green
    return
}

# Try stopping the service
try {
    Write-Host "Stopping service..."
    Stop-Service -Name $ServiceName -Force -ErrorAction Stop
    Write-Host "Service stopped."
} catch {
    Write-Warning "Could not stop service or it may already be stopped: $_"
}

Start-Sleep -Seconds 2

# Delete the service
Write-Host "Deleting service..."
sc.exe delete $ServiceName | Out-Null

# Wait until it is gone
Write-Host "Waiting for service to be fully removed..."
$timeout = 10
while ($timeout -gt 0) {
    $check = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $check) {
        Write-Host "Service '$ServiceName' successfully uninstalled!" -ForegroundColor Green
        return
    }
    Start-Sleep -Seconds 1
    $timeout--
}

Write-Warning "Service is still marked for deletion. Try restarting your system if the issue persists."
