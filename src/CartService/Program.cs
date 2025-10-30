using CartService.Data;
using CartService.Mappings;
using CartService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CartService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добавляем сервисы контроллеров
            builder.Services.AddControllers();
            builder.Services.AddAutoMapper(typeof(CartProfile));
            
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

            // Swagger/OpenAPI
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "CartService API", Version = "v1" });
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var cad = apiDesc.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                    if (cad == null) return false;
                    return cad.ControllerTypeInfo.Assembly == typeof(Program).Assembly;
                });
                c.MapType<Microsoft.AspNetCore.Http.IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "binary" });
            });
            
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<CartDbContext>(options =>
                options.UseNpgsql(connectionString, // UseNpgsql использует строку подключения
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
                    }));
            
            // Сервис корзины
            builder.Services.AddScoped<ICartService, CartService.Services.CartService>();
            
            
            // HttpClient для связи с ProductService
            builder.Services.AddHttpClient();

            

            var app = builder.Build();
            
            app.UseCors("AllowAll");

            // Swagger middleware
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