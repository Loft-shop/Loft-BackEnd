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

            // Настраиваем конвейер обработки запросов
            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}