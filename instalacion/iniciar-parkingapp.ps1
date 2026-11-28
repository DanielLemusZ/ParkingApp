# Levanta ParkingApp con Docker y abre la aplicación en el navegador.
# Lo ejecuta iniciar-parkingapp.cmd, que es el destino del acceso directo del escritorio.

$Host.UI.RawUI.WindowTitle = 'ParkingApp'

# Muestra el mensaje en rojo, espera a que la persona lo lea y termina.
function Salir-ConError {
    param([string[]] $Lineas)

    Write-Host ''
    foreach ($linea in $Lineas) {
        Write-Host $linea -ForegroundColor Red
    }
    Write-Host ''
    Read-Host 'Pulsa Enter para cerrar'
    exit 1
}

# La carpeta del proyecto es la de arriba de "instalacion": ahí están compose.yaml y .env.
$proyecto = Split-Path -Parent $PSScriptRoot
Set-Location $proyecto

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Salir-ConError @(
        'Docker no está instalado.',
        'Instala Docker Desktop desde https://www.docker.com/products/docker-desktop/',
        'o sigue la alternativa sin Docker del README.')
}

docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Salir-ConError @(
        'Docker Desktop no está abierto.',
        'Ábrelo, espera a que diga "Engine running" y vuelve a abrir el acceso directo.')
}

if (-not (Test-Path '.env')) {
    Salir-ConError @(
        'Falta el archivo .env.',
        'Copia .env.example como .env y cambia las contraseñas, como explica el README.')
}

# Puerto de la aplicación, tomado de APP_PORT en .env.
$lineaDelPuerto = Select-String -Path '.env' -Pattern '^APP_PORT=(\d+)' | Select-Object -First 1
if ($null -eq $lineaDelPuerto) {
    Salir-ConError @('Falta APP_PORT en .env. Cópiala de .env.example.')
}
$puerto = $lineaDelPuerto.Matches[0].Groups[1].Value
$direccion = "http://localhost:$puerto"

Write-Host 'Levantando ParkingApp. La primera vez tarda unos minutos: descarga .NET y compila la aplicación.'
Write-Host ''
docker compose up -d --build --wait
if ($LASTEXITCODE -ne 0) {
    Salir-ConError @('No se pudo levantar ParkingApp. Revisa el mensaje de arriba.')
}

Write-Host ''
Write-Host "Esperando a que la aplicación responda en $direccion ..."
$responde = $false
for ($intento = 1; $intento -le 60; $intento++) {
    try {
        # Se consulta 127.0.0.1 y no localhost: en Windows, localhost prueba primero IPv6 y cada intento se demora.
        Invoke-WebRequest -Uri "http://127.0.0.1:$puerto/cuenta/iniciar-sesion" -UseBasicParsing -TimeoutSec 5 | Out-Null
        $responde = $true
        break
    }
    catch {
        Start-Sleep -Seconds 2
    }
}

if (-not $responde) {
    Salir-ConError @(
        'La aplicación no respondió.',
        'Revisa sus registros con: docker compose logs aplicacion')
}

Start-Process $direccion
Write-Host "ParkingApp está en $direccion. Esta ventana se cierra sola."
Start-Sleep -Seconds 5
