# Imagen de la API. Se construye desde la raíz del repositorio:
#   docker build -f src/Inventory.Api/Dockerfile .

# ---- Compilación ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los proyectos para que la restauración de paquetes quede en caché.
COPY Directory.Build.props .editorconfig ./
COPY src/Inventory.Domain/Inventory.Domain.csproj src/Inventory.Domain/
COPY src/Inventory.Application/Inventory.Application.csproj src/Inventory.Application/
COPY src/Inventory.Infrastructure/Inventory.Infrastructure.csproj src/Inventory.Infrastructure/
COPY src/Inventory.Api/Inventory.Api.csproj src/Inventory.Api/
RUN dotnet restore src/Inventory.Api/Inventory.Api.csproj

COPY src/ src/
RUN dotnet publish src/Inventory.Api/Inventory.Api.csproj -c Release --no-restore -o /app/publish

# ---- Ejecución ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# La API escribe sus logs en /app/logs; se corre con el usuario sin privilegios de la imagen.
RUN mkdir -p /app/logs && chown "$APP_UID" /app/logs
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
