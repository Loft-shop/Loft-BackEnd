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

            // Строка подключения: берём из конфигурации или используем локальную базу order.db
            var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=order.db";
            Console.WriteLine($"[OrderService] Using SQLite database: {defaultConn}");
            builder.Services.AddDbContext<OrderDbContext>(options => options.UseSqlite(defaultConn));
            
            // Регистрируем сервисы
            builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();

            var app = builder.Build();

            // Создаём базу данных при старте (для локальной разработки)
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                db.Database.EnsureCreated();
            }

            // Настраиваем конвейер обработки запросов
            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}