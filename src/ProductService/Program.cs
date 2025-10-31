using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ProductService.Data;
using ProductService.Mappings;
using ProductService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Security.Claims;

namespace ProductService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Строка подключения
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // DbContext
            builder.Services.AddDbContext<ProductDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
                }));

            // Контроллеры и AutoMapper
            builder.Services.AddControllers();
            builder.Services.AddAutoMapper(typeof(ProductProfile));

            // Сервисы
            builder.Services.AddScoped<IProductService, ProductService.Services.ProductService>();

            // HttpClient для связи с UserService
            builder.Services.AddHttpClient("UserService", client =>
            {
                var userServiceUrl = builder.Configuration["Services:UserService"] ?? "http://localhost:5003";
                client.BaseAddress = new Uri(userServiceUrl);
            });
            // Настройка аутентификации JWT
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var jwtKey = jwtSection.GetValue<string>("Key");
            var jwtIssuer = jwtSection.GetValue<string>("Issuer");
            var jwtAudience = jwtSection.GetValue<string>("Audience");

            //Console.WriteLine($"[ProductService] JWT Configuration - Key: {(!string.IsNullOrEmpty(jwtKey) ? "SET" : "NOT SET")}, Issuer: {jwtIssuer}, Audience: {jwtAudience}");
            //Console.WriteLine($"[DEBUG] JWT Key length: {jwtKey?.Length ?? 0}, First 5 chars: {jwtKey?.Substring(0, Math.Min(5, jwtKey.Length))}");

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new Exception("[ProductService] JWT Key is not configured!");
            }

            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                    ValidAudience = jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                    RequireSignedTokens = true
                };

                // Для отладки
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authHeader = context.Request.Headers["Authorization"].ToString();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = authHeader.Substring("Bearer ".Length).Trim();
                        }
                        Console.WriteLine($"[ProductService] Received Authorization header: {(string.IsNullOrEmpty(authHeader) ? "EMPTY" : "SET")}");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"[ProductService] Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var userName = context.Principal?.Identity?.Name;
                        Console.WriteLine($"[ProductService] Token validated - UserId: {userId}, UserName: {userName}");
                        return Task.CompletedTask;
                    }
                };
            });

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProductService API", Version = "v1" });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your valid token"
                };
                c.AddSecurityDefinition("bearerAuth", securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "bearerAuth"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Только контроллеры из текущей сборки
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var cad = apiDesc.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                    return cad != null && cad.ControllerTypeInfo.Assembly == typeof(Program).Assembly;
                });

                // IFormFile для Swagger
                c.MapType<Microsoft.AspNetCore.Http.IFormFile>(() =>
                    new OpenApiSchema { Type = "string", Format = "binary" });
            });

            var app = builder.Build();

            // Конвейер запросов
            app.UseRouting();

            // Swagger UI доступен всегда
            app.UseSwagger();
            app.UseSwaggerUI();

            // JWT
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
