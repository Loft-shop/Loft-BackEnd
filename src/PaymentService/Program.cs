using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Mappings;
using PaymentService.Data;
using Microsoft.EntityFrameworkCore;
using PaymentService.Services;
using PaymentService.Services.Providers;
//
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

            // Регистрация платежных провайдеров
            builder.Services.AddSingleton<IPaymentProvider, RealStripeProvider>();
            builder.Services.AddSingleton<IPaymentProvider, MockCreditCardProvider>();
            builder.Services.AddSingleton<IPaymentProvider, MockCashOnDeliveryProvider>();

            // Фабрика провайдеров
            builder.Services.AddSingleton<PaymentProviderFactory>();

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

            Console.WriteLine("=== Payment Service Started ===");
            Console.WriteLine("Supported payment methods:");
            Console.WriteLine("  - STRIPE (Real - Test Mode)");
            Console.WriteLine("  - CREDIT_CARD (Mock)");
            Console.WriteLine("  - CASH_ON_DELIVERY (Mock)");
            Console.WriteLine("================================");

            app.Run();
        }
    }
}