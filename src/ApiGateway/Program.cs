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

            // Добавляем политику CORS, читаем разрешённые origin'ы из конфигурации
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddOcelot();

            // Добавляем контроллеры (для /api/gateway и прочих локальных эндпоинтов)
            builder.Services.AddControllers();

            // Swagger/OpenAPI
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ApiGateway API", Version = "v1" });
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var cad = apiDesc.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                    if (cad == null) return false;
                    return cad.ControllerTypeInfo.Assembly == typeof(Program).Assembly;
                });
                c.MapType<Microsoft.AspNetCore.Http.IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "binary" });
            });

            var app = builder.Build();

            // Swagger middleware (должно идти до Ocelot middleware)
            app.UseSwagger();
            app.UseSwaggerUI();

            // CORS должен быть настроен ДО Ocelot
            // Обрабатываем preflight запросы (OPTIONS) вручную до Ocelot
            app.Use(async (context, next) =>
            {
                // Проверяем, есть ли Origin в запросе
                var origin = context.Request.Headers["Origin"].ToString();
                
                if (!string.IsNullOrEmpty(origin))
                {
                    // Получаем разрешенные origins
                    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
                    
                    // Проверяем, разрешен ли этот origin
                    if (allowedOrigins.Contains(origin) || allowedOrigins.Contains("*"))
                    {
                        context.Response.Headers.Add("Access-Control-Allow-Origin", origin);
                        context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                        context.Response.Headers.Add("Access-Control-Allow-Headers", 
                            context.Request.Headers["Access-Control-Request-Headers"].ToString());
                        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, PATCH");
                    }
                }

                // Если это preflight запрос (OPTIONS), возвращаем 200 и не идем дальше в Ocelot
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    return;
                }

                await next();
            });

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
