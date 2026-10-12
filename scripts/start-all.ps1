# Levanta los cinco microservicios juntos (Windows).
#
#   powershell -ExecutionPolicy Bypass -File scripts\start-all.ps1
#
# Compila la solución una sola vez y abre una ventana por servicio, con sus logs. Cuando los cinco
# responden, muestra sus URLs. Enter (o cerrar esta ventana con Ctrl+C) detiene los cinco.
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

# Nombre y puerto de cada servicio (los de launchSettings.json).
$services = [ordered]@{
    Products      = 5001
    Users         = 5002
    Orders        = 5003
    Cart          = 5004
    Notifications = 5005
}

foreach ($service in $services.GetEnumerator()) {
    if (Get-NetTCPConnection -LocalPort $service.Value -State Listen -ErrorAction SilentlyContinue) {
        Write-Host "El puerto $($service.Value) ya está en uso (¿$($service.Key).API ya está levantado?). Detenelo y volvé a intentar." -ForegroundColor Red
        exit 1
    }
}

Write-Host 'Compilando la solución...'
dotnet build ECommerce.slnx -v q --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$processes = @()
try {
    foreach ($service in $services.GetEnumerator()) {
        $processes += Start-Process dotnet -PassThru -ArgumentList @(
            'run', '--project', "src\$($service.Key).API", '--launch-profile', 'http', '--no-build')
    }

    # Espera a que cada servicio responda su /health/live (hasta 60 segundos cada uno).
    foreach ($service in $services.GetEnumerator()) {
        $url = "http://localhost:$($service.Value)/health/live"
        $ready = $false
        for ($i = 0; $i -lt 60 -and -not $ready; $i++) {
            try {
                Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 2 | Out-Null
                $ready = $true
            }
            catch {
                Start-Sleep -Seconds 1
            }
        }
        if (-not $ready) { throw "$($service.Key).API no respondió en el puerto $($service.Value)." }
    }

    Write-Host ''
    Write-Host 'Los cinco servicios están levantados:' -ForegroundColor Green
    foreach ($service in $services.GetEnumerator()) {
        Write-Host ('  {0,-18} http://localhost:{1}/swagger' -f "$($service.Key).API", $service.Value)
    }
    Write-Host ''
    Read-Host 'Enter para detenerlos'
}
finally {
    # "dotnet run" lanza la API como proceso hijo: taskkill /T detiene el árbol completo.
    Write-Host 'Deteniendo los servicios...'
    foreach ($process in $processes) {
        taskkill /PID $process.Id /T /F 2>&1 | Out-Null
    }
}
