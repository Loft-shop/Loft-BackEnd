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

            // Настройка CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(_ => true) // Разрешаем все origins
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials(); // Важно для работы с cookies/auth
                });
            });

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

            // CORS должен идти ПЕРВЫМ, до Ocelot
            app.UseCors("AllowAll");

            // Логирующий middleware для отладки входящих запросов (особенно /api/auth/*)
            app.Use(async (context, next) =>
            {
                try
                {
                    var path = context.Request.Path.Value ?? string.Empty;
                    Console.WriteLine($"[ApiGateway] Incoming request: {context.Request.Method} {path}");

                    // Логируем тело для POST/PUT (делаем буферизацию)
                    if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
                    {
                        context.Request.EnableBuffering();
                        using (var reader = new System.IO.StreamReader(context.Request.Body, System.Text.Encoding.UTF8, leaveOpen: true))
                        {
                            var body = await reader.ReadToEndAsync();
                            context.Request.Body.Position = 0;
                            if (!string.IsNullOrWhiteSpace(body))
                            {
                                Console.WriteLine($"[ApiGateway] Body: {body}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ApiGateway] Failed to log request: {ex}");
                }

                await next();
            });

            // Убираем trailing slash ДО Ocelot
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value;
                if (!string.IsNullOrEmpty(path) && path.Length > 1 && path.EndsWith("/"))
                {
                    context.Request.Path = path.TrimEnd('/');
                }
                await next();
            });

            // Swagger middleware (должно идти до Ocelot middleware)
            app.UseSwagger();
            app.UseSwaggerUI();

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
