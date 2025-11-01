using System;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserService.Data;
using UserService.Mappings;
using UserService.Services;
using Microsoft.EntityFrameworkCore;
//using UserService.Swagger;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UserService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добавляем сервисы контроллеров
            builder.Services.AddControllers();
            builder.Services.AddAutoMapper(typeof(UserProfile));

            // Configure DbContext (PostgreSQL) - use connection string or default file
            var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<UserDbContext>(options => 
                options.UseNpgsql(defaultConn, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
                }));

            // Добавляем TokenService и UserService
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IUserService, UserService.Services.UserService>();

            // Настройка аутентификации JWT
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var jwtKey = jwtSection.GetValue<string>("Key");
            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");

            if (!string.IsNullOrEmpty(jwtKey))
            {
                var key = Encoding.UTF8.GetBytes(jwtKey);
              //  Console.WriteLine($"[UserService] JWT Configuration - Key: {(!string.IsNullOrEmpty(jwtKey) ? "SET" : "NOT SET")}, Issuer: {issuer}, Audience: {audience}");
              //  Console.WriteLine($"[DEBUG] JWT Key length: {jwtKey?.Length ?? 0}, First 5 chars: {jwtKey?.Substring(0, Math.Min(5, jwtKey.Length))}");
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
                        ValidateIssuer = !string.IsNullOrEmpty(issuer),
                        ValidIssuer = issuer,
                        ValidateAudience = !string.IsNullOrEmpty(audience),
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5),
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role
                    };

                    // Добавляем события для отладки
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var authHeader = context.Request.Headers["Authorization"].ToString();
                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                            }

                            Console.WriteLine($"[UserService] Received Authorization header: {(string.IsNullOrEmpty(authHeader) ? "EMPTY" : "SET")}");
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"[UserService] Authentication failed: {context.Exception.Message}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            var userName = context.Principal?.Identity?.Name;
                            Console.WriteLine($"[UserService] Token validated - UserId: {userId}, UserName: {userName}");
                            return Task.CompletedTask;
                        }
                    };
                });
            }
            else
            {
                Console.WriteLine("[UserService] WARNING: JWT Key is not configured!");
            }

            // Swagger с поддержкой Bearer
            builder.Services.AddSwaggerGen(c =>
             {
                 c.SwaggerDoc("v1", new OpenApiInfo { Title = "UserService API", Version = "v1" });
                 var securityScheme = new OpenApiSecurityScheme
                 {
                     Name = "Authorization",
                     Type = SecuritySchemeType.Http,
                     Scheme = "bearer",
                     BearerFormat = "JWT",
                     In = ParameterLocation.Header,
                     Description = "Enter 'Bearer' [space] and then your valid token in the text input below."
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
                 Id = "bearerAuth"  // <- обязательно совпадает с AddSecurityDefinition
                   }
                   },
                  Array.Empty<string>()
                 }
                    });

                 // Only include controllers from this assembly (avoid controllers from referenced projects)
                 c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var cad = apiDesc.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                    if (cad == null) return false;
                    return cad.ControllerTypeInfo.Assembly == typeof(Program).Assembly;
                });

               // Map IFormFile to binary in OpenAPI - simple mapping without filter
                c.MapType<Microsoft.AspNetCore.Http.IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "binary" });
             });

            var app = builder.Build();

            // Создаём/мигрируем базу данных при старте и логируем строку подключения
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
                try
                {
                    var conn = builder.Configuration.GetConnectionString("DefaultConnection");
                    Console.WriteLine($"[UserService] Using PostgreSQL connection: {conn}");

                    // Попытки применения миграций с повторными попытками, чтобы дождаться старта Postgres
                    var maxAttempts = 10;
                    var attempt = 0;
                    var delaySeconds = 2;
                    while (true)
                    {
                        try
                        {
                            attempt++;
                            Console.WriteLine($"[UserService] Attempting DB migrate (attempt {attempt}/{maxAttempts})...");
                            db.Database.Migrate();
                            Console.WriteLine("[UserService] Database migration applied successfully.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[UserService] Database migration attempt {attempt} failed: {ex.Message}");
                            if (attempt >= maxAttempts)
                            {
                                Console.WriteLine("[UserService] Maximum migration attempts reached, aborting startup.");
                                throw;
                            }
                            var wait = TimeSpan.FromSeconds(delaySeconds * attempt);
                            Console.WriteLine($"[UserService] Waiting {wait.TotalSeconds} seconds before retrying...");
                            System.Threading.Thread.Sleep(wait);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UserService] Database migration failed: {ex}");
                    throw;
                }
            }

            // Ensure wwwroot exists so static files (avatars) can be served
            try
            {
                var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
                if (!Directory.Exists(wwwrootPath)) Directory.CreateDirectory(wwwrootPath);
            }
            catch { }

            // Настраиваем конвейер обработки запросов
            app.UseRouting();

            // CORS удалён - он обрабатывается только в ApiGateway

            app.UseSwagger();
            app.UseSwaggerUI();

            // Serve static files (wwwroot) so avatars can be retrieved
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}