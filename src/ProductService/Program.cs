using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductService.Data;
using ProductService.Mappings;
using ProductService.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ProductService
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


            builder.Services.AddDbContext<ProductDbContext>(options =>
                options.UseNpgsql(connectionString, // UseNpgsql использует строку подключения
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
                    }));

            // Добавляем сервисы контроллеров
            builder.Services.AddControllers();
            builder.Services.AddAutoMapper(typeof(ProductProfile));

            // Сервисы
            builder.Services.AddScoped<IProductService, ProductService.Services.ProductService>();
            
            // HttpClient для связи с UserService
            builder.Services.AddHttpClient("UserService", client =>
            {
                var userServiceUrl = builder.Configuration["Services:UserService"] ?? "http://localhost:5003";
                client.BaseAddress = new Uri(userServiceUrl);
            });

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ProductService API", Version = "v1" });
                
                // Only include controllers from this assembly (avoid controllers from referenced projects)
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var cad = apiDesc.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                    if (cad == null) return false;
                    return cad.ControllerTypeInfo.Assembly == typeof(Program).Assembly;
                });
            });

            var app = builder.Build();
            
            // Включаем CORS
            app.UseCors("AllowAll");

            // Swagger UI доступен всегда для локального тестирования
            app.UseSwagger();
            app.UseSwaggerUI();

            // Настраиваем конвейер обработки запросов
            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}