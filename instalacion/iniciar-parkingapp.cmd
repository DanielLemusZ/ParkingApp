@echo off
rem Destino del acceso directo del escritorio. Ejecuta iniciar-parkingapp.ps1, que levanta ParkingApp.
rem La logica esta en PowerShell porque la consola no muestra bien las tildes desde un archivo .cmd.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0iniciar-parkingapp.ps1"
