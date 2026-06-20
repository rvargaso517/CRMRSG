# start-server.ps1
# Script para iniciar IIS Express localmente con soporte para este proyecto de ASP.NET MVC

$iisExpressPath = "C:\Program Files\IIS Express\iisexpress.exe"
if (-not (Test-Path $iisExpressPath)) {
    $iisExpressPath = "C:\Program Files (x86)\IIS Express\iisexpress.exe"
}

if (-not (Test-Path $iisExpressPath)) {
    Write-Error "Error: No se encontró IIS Express instalado en su sistema."
    Write-Host "Por favor instálelo o verifique su instalación para poder correr el servidor local."
    Exit 1
}

$port = 8080
$path = Resolve-Path "."

Write-Host "`n===============================================" -ForegroundColor Green
Write-Host "   SISTEMA CRM-RSG - SERVIDOR LOCAL IIS EXPRESS   " -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host "Ruta del proyecto: $path"
Write-Host "Puerto configurado: $port"
Write-Host "Iniciando servidor local..." -ForegroundColor Yellow
Write-Host "Abra su navegador en: http://localhost:$port/" -ForegroundColor Cyan
Write-Host "Presione CTRL+C para detener el servidor.`n"

# Iniciar IIS Express
& $iisExpressPath /path:"$path" /port:$port
