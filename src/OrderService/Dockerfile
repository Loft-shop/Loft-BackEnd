FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Копируем глобальные настройки (Directory.Build.props) прежде, чем делать restore
COPY ["Directory.Build.props", "./"]
# Копируем все .csproj из src, чтобы dotnet restore увидел все ProjectReference (включая транзитивные)
COPY ["src/ApiGateway/ApiGateway.csproj", "ApiGateway/"]
COPY ["src/CartService/CartService.csproj", "CartService/"]
COPY ["src/Common/Loft.Common/Loft.Common.csproj", "Common/Loft.Common/"]
COPY ["src/OrderService/OrderService.csproj", "OrderService/"]
COPY ["src/PaymentService/PaymentService.csproj", "PaymentService/"]
COPY ["src/ProductService/ProductService.csproj", "ProductService/"]
COPY ["src/ShippingAddressService/ShippingAddressService.csproj", "ShippingAddressService/"]
COPY ["src/UserService/UserService.csproj", "UserService/"]

RUN dotnet restore "OrderService/OrderService.csproj"

# Копируем все исходники
COPY src/OrderService/ ./OrderService/
WORKDIR /src/OrderService
RUN dotnet restore "OrderService.csproj"
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

WORKDIR /src/OrderService
RUN dotnet build "OrderService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/OrderService
RUN dotnet publish "OrderService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
