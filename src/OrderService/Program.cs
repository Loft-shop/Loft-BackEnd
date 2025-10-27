using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderService.Data;
using OrderService.Mappings;
using OrderService.Services;
using System.IO;

namespace OrderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добавляем сервисы контроллеров
            builder.Services.AddControllers();
            builder.Services.AddAutoMapper(typeof(OrderProfile));

            // Configure DbContext (PostgreSQL) - use connection string or default file
            var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(defaultConn));
            
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

            // Создаём/мигрируем базу данных при старте и логируем строку подключения
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                try
                {
                    var conn = builder.Configuration.GetConnectionString("DefaultConnection");
                    Console.WriteLine($"[UserService] Using PostgreSQL connection: {conn}");
                    db.Database.Migrate();
                    Console.WriteLine("[UserService] Database migration applied successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UserService] Database migration failed: {ex}");
                    throw;
                }
            }

            // Настраиваем конвейер обработки запросов
            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}