FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "./"]
# Копируем файлы .csproj необходимых проектов так, чтобы restore нашёл ProjectReference
COPY ["src/CartService/CartService.csproj", "CartService/"]
COPY ["src/ProductService/ProductService.csproj", "ProductService/"]
COPY ["src/UserService/UserService.csproj", "UserService/"]

RUN dotnet restore "CartService/CartService.csproj"

# Копируем все исходники
COPY src/ .


WORKDIR "/src/CartService"
RUN dotnet build "CartService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/CartService
RUN dotnet publish "CartService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CartService.dll"]