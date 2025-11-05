# ====== STAGE 1: Base runtime image ======
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ====== STAGE 2: Build and publish ======
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем глобальные настройки
COPY ["Directory.Build.props", "./"]

# Копируем только необходимые проекты
COPY ["src/OrderService/OrderService.csproj", "OrderService/"]
COPY ["src/Common/Loft.Common/Loft.Common.csproj", "Common/Loft.Common/"]

# Восстанавливаем зависимости только для OrderService
RUN dotnet restore "OrderService/OrderService.csproj"

# Копируем исходники только нужных проектов
COPY ["src/Common/Loft.Common/", "Common/Loft.Common/"]
COPY ["src/OrderService/", "OrderService/"]

WORKDIR /src/OrderService

# Сборка и публикация
RUN dotnet build "OrderService.csproj" -c $BUILD_CONFIGURATION -o /app/build
RUN dotnet publish "OrderService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ====== STAGE 3: Final runtime image ======
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
