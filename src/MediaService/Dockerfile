FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Копируем глобальные настройки
COPY ["Directory.Build.props", "./"]
# Копируем файлы .csproj
COPY ["src/MediaService/MediaService.csproj", "MediaService/"]

RUN dotnet restore "MediaService/MediaService.csproj"

# Копируем все исходники
COPY src/ .

WORKDIR /src/MediaService
RUN dotnet build "MediaService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/MediaService
RUN dotnet publish "MediaService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Создаем директорию для загрузок
RUN mkdir -p /app/uploads && chmod 777 /app/uploads

ENTRYPOINT ["dotnet", "MediaService.dll"]

