using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShippingAddressService.Data;
using ShippingAddressService.Mappings;
using ShippingAddressService.Services;

namespace ShippingAddressService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добавляем сервисы контроллеров
            builder.Services.AddControllers();
            
            // AutoMapper
            builder.Services.AddAutoMapper(typeof(ShippingAddressProfile));

            // Swagger/OpenAPI
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
                { 
                    Title = "ShippingAddressService API", 
                    Version = "v1",
                    Description = "API для управления адресами доставки пользователей"
                });
                
                // Only include controllers from this assembly (avoid controllers from referenced projects)
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var cad = apiDesc.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                    if (cad == null) return false;
                    return cad.ControllerTypeInfo.Assembly == typeof(Program).Assembly;
                });
            });

            // Database connection
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ShippingAddressDbContext>(options =>
                options.UseNpgsql(connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
                    }));

            // Register services
            builder.Services.AddScoped<IShippingAddressService, Services.ShippingAddressService>();

            // HttpClient для связи с UserService (если понадобится)
            builder.Services.AddHttpClient("UserService", client =>
            {
                var userServiceUrl = builder.Configuration["Services:UserService"] ?? "http://userservice:8080";
                client.BaseAddress = new Uri(userServiceUrl);
            });

            var app = builder.Build();

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