FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Копируем глобальные настройки (Directory.Build.props) прежде, чем делать restore
COPY ["Directory.Build.props", "./"]
# Копируем файлы .csproj необходимых проектов так, чтобы restore нашёл ProjectReference
COPY ["src/ProductService/ProductService.csproj", "ProductService/"]
COPY ["src/Common/Loft.Common/Loft.Common.csproj", "Common/Loft.Common/"]
COPY ["src/UserService/UserService.csproj", "UserService/"]

RUN dotnet restore "ProductService/ProductService.csproj"

# Копируем все исходники
COPY src/ .

WORKDIR /src/ProductService
RUN dotnet build "ProductService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/ProductService
RUN dotnet publish "ProductService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]