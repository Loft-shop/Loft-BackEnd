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

            // Настраиваем конвейер обработки запросов
            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}