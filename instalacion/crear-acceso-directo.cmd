@echo off
setlocal
rem Crea en el escritorio el acceso directo "ParkingApp", con el icono del MVP.
rem Si mueves la carpeta del proyecto, vuelve a ejecutarlo.
rem Los mensajes van sin tildes porque la consola no las muestra bien desde un archivo .cmd.

set "CARPETA=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$escritorio = [Environment]::GetFolderPath('Desktop');" ^
  "$acceso = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $escritorio 'ParkingApp.lnk'));" ^
  "$acceso.TargetPath = '%CARPETA%iniciar-parkingapp.cmd';" ^
  "$acceso.WorkingDirectory = '%CARPETA%';" ^
  "$acceso.IconLocation = '%CARPETA%parkingapp.ico';" ^
  "$acceso.Description = 'Inicia ParkingApp y lo abre en el navegador';" ^
  "$acceso.Save()"
if errorlevel 1 (
    echo No se pudo crear el acceso directo.
    pause
    exit /b 1
)

echo Acceso directo ParkingApp creado en el escritorio.
pause
