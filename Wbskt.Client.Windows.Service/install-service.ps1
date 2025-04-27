# ----------------------------------------
# install-service.ps1
# Description: Publishes and installs the WbsktClient Windows Service
# ----------------------------------------

# --- CONFIGURATION ---
$ServiceName = "WbsktClient"
$ProjectPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$PublishDir = Join-Path $ProjectPath "publish"
$ExeName = "Wbskt.Client.Windows.Host.exe"
$ExePath = Join-Path $PublishDir $ExeName

Write-Host "Publishing application..." -ForegroundColor Cyan
dotnet publish $ProjectPath -c Release -r win-x64 --output $PublishDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed!"
    exit $LASTEXITCODE
}

# --- STOP & DELETE EXISTING SERVICE ---
Write-Host "Checking for existing service '$ServiceName'..." -ForegroundColor Cyan
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    try {
        Stop-Service -Name $ServiceName -Force -ErrorAction Stop
        Write-Host "Service stopped."
    } catch {
        Write-Warning "Could not stop service: $_"
    }

    Start-Sleep -Seconds 2

    Write-Host "Deleting service..."
    sc.exe delete $ServiceName | Out-Null

    # Wait until the service is actually deleted
    Write-Host "Waiting for service to be fully deleted..."
    $timeout = 10
    while ($timeout -gt 0) {
        $check = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $check) {
            Write-Host "Service deleted successfully." -ForegroundColor Green
            break
        }
        Start-Sleep -Seconds 1
        $timeout--
    }

    if ($timeout -eq 0) {
        Write-Warning "Service is still marked for deletion. Try restarting your computer if this persists."
        exit 1
    }
} else {
    Write-Host "No existing service found." -ForegroundColor Green
}

# --- CREATE SERVICE ---
Write-Host "Creating Windows Service '$ServiceName'..." -ForegroundColor Cyan
$quotedExePath = "`"$ExePath`""
$createResult = sc.exe create $ServiceName binPath= $quotedExePath start= auto

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create the service."
    exit $LASTEXITCODE
}

# --- START SERVICE ---
Write-Host "Starting the service..." -ForegroundColor Cyan
Start-Service -Name $ServiceName
Start-Sleep -Seconds 2

# --- VERIFY ---
$running = (Get-Service -Name $ServiceName).Status -eq 'Running'
if ($running) {
    Write-Host "Service '$ServiceName' installed and running successfully!" -ForegroundColor Green
} else {
    Write-Warning "Service was created but did not start."
}

