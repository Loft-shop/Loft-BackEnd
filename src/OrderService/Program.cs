using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderService.Data;
using OrderService.Mappings;
using OrderService.Services;

namespace OrderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Добавляем политику CORS, читаем разрешённые origin'ы из конфигурации
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[]
            {
                "http://localhost:3000",
                "https://www.loft-shop.pp.ua"
            };
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // Добавляем контроллеры и авторизацию (нужны для UseAuthorization)
            builder.Services.AddControllers();
            builder.Services.AddAuthorization();
            
            // Swagger/OpenAPI
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "OrderService API", Version = "v1" });
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var cad = apiDesc.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                    if (cad == null) return false;
                    return cad.ControllerTypeInfo.Assembly == typeof(Program).Assembly;
                });
                c.MapType<Microsoft.AspNetCore.Http.IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "binary" });
            });
            
            builder.Services.AddDbContext<OrderDbContext>(options =>
                options.UseNpgsql(connectionString, // UseNpgsql использует строку подключения
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
                    }));

            // Регистрируем AutoMapper
            builder.Services.AddAutoMapper(typeof(Program).Assembly);

            // Регистрируем сервисы
            builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();
            
            // HttpClient для связи с другими микросервисами
            builder.Services.AddHttpClient("CartService", client =>
            {
                var cartServiceUrl = builder.Configuration["Services:CartService"] ?? "http://localhost:5002";
                client.BaseAddress = new Uri(cartServiceUrl);
            });
            
            builder.Services.AddHttpClient("ProductService", client =>
            {
                var productServiceUrl = builder.Configuration["Services:ProductService"] ?? "http://localhost:5001";
                client.BaseAddress = new Uri(productServiceUrl);
            });
            
            builder.Services.AddHttpClient("UserService", client =>
            {
                var userServiceUrl = builder.Configuration["Services:UserService"] ?? "http://localhost:5003";
                client.BaseAddress = new Uri(userServiceUrl);
            });


            var app = builder.Build();

            // Swagger middleware
            app.UseSwagger();
            app.UseSwaggerUI();
            
            // Настраиваем конвейер обработки запросов
            app.UseRouting();

            // Включаем CORS
            app.UseCors("AllowAll");

            // Вызываем UseAuthorization только если зарегистрирован IAuthorizationService
            if (app.Services.GetService(typeof(Microsoft.AspNetCore.Authorization.IAuthorizationService)) != null)
            {
                app.UseAuthorization();
            }
             app.MapControllers();

             app.Run();
        }
    }
}