# Imagen de ParkingApp: se compila con el SDK de .NET 10 y corre sobre la imagen de ASP.NET Core 10.
# Las versiones están fijadas para que la imagen salga igual en cualquier equipo.

# 1. Compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0.404 AS compilacion
WORKDIR /codigo

COPY Directory.Build.props global.json .editorconfig ./
COPY src/ src/

# Restaura y publica en modo Release, con los modelos del OCR. La restauración va después de copiar todo el código:
# el SDK agrega los archivos de Blazor (_framework/blazor.web.js) solo si ve las páginas .razor.
RUN dotnet publish src/ParkingApp.Web/ParkingApp.Web.csproj --configuration Release --output /publicado

# 2. Ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15
WORKDIR /app

# Carpeta de las claves que protegen las cookies de sesión. Compose le monta un volumen, para que reiniciar el
# contenedor no cierre las sesiones; se crea aquí para que pertenezca al usuario sin privilegios.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet

COPY --from=compilacion /publicado .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# La aplicación corre con el usuario "app" de la imagen, no como administrador.
USER app
ENTRYPOINT ["dotnet", "ParkingApp.Web.dll"]
