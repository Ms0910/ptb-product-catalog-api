# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se restauran primero solo los .csproj, para que esta capa quede en caché
# hasta que cambie alguna dependencia (no el código).
COPY src/ProductCatalog.Domain/ProductCatalog.Domain.csproj src/ProductCatalog.Domain/
COPY src/ProductCatalog.Application/ProductCatalog.Application.csproj src/ProductCatalog.Application/
COPY src/ProductCatalog.Infrastructure/ProductCatalog.Infrastructure.csproj src/ProductCatalog.Infrastructure/
COPY src/ProductCatalog.Api/ProductCatalog.Api.csproj src/ProductCatalog.Api/
RUN dotnet restore src/ProductCatalog.Api/ProductCatalog.Api.csproj

COPY src/ src/
RUN dotnet publish src/ProductCatalog.Api/ProductCatalog.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Runtime ----
# Imagen "chiseled": sin shell ni gestor de paquetes, corre como usuario no-root por defecto.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ProductCatalog.Api.dll"]
