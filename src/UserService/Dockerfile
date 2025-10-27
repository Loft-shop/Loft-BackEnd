FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Ensure Kestrel listens on port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Копируем глобальные настройки (Directory.Build.props) прежде, чем делать restore
COPY ["Directory.Build.props", "./"]
# Копируем файлы .csproj проекта и общего проекта
COPY ["src/UserService/UserService.csproj", "UserService/"]
COPY ["src/Common/Loft.Common/Loft.Common.csproj", "Common/Loft.Common/"]

RUN dotnet restore "UserService/UserService.csproj"

# Копируем все исходники
COPY src/ .

WORKDIR "/src/UserService"
RUN dotnet build "UserService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/UserService"
RUN dotnet publish "UserService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.dll"]