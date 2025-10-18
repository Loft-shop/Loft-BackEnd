using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace ApiGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Конфигурация Ocelot по окружениям
            builder.Configuration
                .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

            builder.Services.AddOcelot();

            // Добавляем контроллеры (для /api/gateway и прочих локальных эндпоинтов)
            builder.Services.AddControllers();

            var app = builder.Build();

            // Перехват корня и health до Ocelot
            app.Use(async (ctx, next) =>
            {
                var path = ctx.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
                if (path == string.Empty || path == "/")
                {
                    await ctx.Response.WriteAsJsonAsync(new { status = "ok", service = "ApiGateway" });
                    return;
                }
                if (path == "/healthz")
                {
                    await ctx.Response.WriteAsync("healthy");
                    return;
                }
                await next();
            });

            // Включаем контроллеры (например, /api/gateway)
            app.MapControllers();

            // Проксирование запросов по конфигурации Ocelot
            app.UseOcelot().Wait();

            app.Run();
        }
    }
}
