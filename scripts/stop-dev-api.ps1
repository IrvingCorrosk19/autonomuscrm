# Libera el puerto 5154 si otra instancia de la API sigue en ejecución (dotnet run, VS, etc.)
$port = 5154
$connections = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
if (-not $connections) {
    Write-Host "Puerto $port libre."
    exit 0
}
$processIds = $connections.OwningProcess | Sort-Object -Unique
foreach ($processId in $processIds) {
    $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "Deteniendo $($proc.ProcessName) (PID $processId) en puerto $port..."
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
}
# También procesos AutonomusCRM.API sin escuchar en el puerto (VS debug colgado)
Get-Process -Name "AutonomusCRM.API" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Deteniendo $($_.ProcessName) (PID $($_.Id))..."
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 1
Write-Host "Listo. Vuelve a pulsar F5 en Visual Studio."
