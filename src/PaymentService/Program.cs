using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Mappings;
using PaymentService.Data;
using Microsoft.EntityFrameworkCore;
using PaymentService.Services;

namespace PaymentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добавляем сервисы контроллеров
            builder.Services.AddControllers();
            builder.Services.AddAutoMapper(typeof(PaymentProfile));
            builder.Services.AddSwaggerGen();

            // DbContext: используем только реальную строку подключения к PostgreSQL из конфигурации.
            var conn = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(conn))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured. PaymentService requires a PostgreSQL connection string (set DefaultConnection in configuration or environment).");
            }

            builder.Services.AddDbContext<PaymentDbContext>(options =>
                options.UseNpgsql(conn));

            // Регистрация сервисов
            builder.Services.AddScoped<IPaymentService, PaymentService.Services.PaymentService>();
            builder.Services.AddHttpClient();

            var app = builder.Build();
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