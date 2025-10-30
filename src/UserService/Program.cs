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

            // Добавляем политику CORS, читаем разрешённые origin'ы из конфигурации
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[]
            {
                "http://localhost:3000",
                "https://www.loft-shop.pp.ua"
            };
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
                Console.WriteLine($"[UserService] JWT Configuration - Key: {(!string.IsNullOrEmpty(jwtKey) ? "SET" : "NOT SET")}, Issuer: {issuer}, Audience: {audience}");
                
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
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role
                    };
                    
                    // Добавляем события для отладки
                    options.Events = new JwtBearerEvents
                    {
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
                        },
                        OnMessageReceived = context =>
                        {
                            var authHeader = context.Request.Headers["Authorization"].ToString();
                            Console.WriteLine($"[UserService] Received Authorization header: {(string.IsNullOrEmpty(authHeader) ? "EMPTY" : "SET")}");
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
                     { securityScheme, new string[] { } }
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
                    var maxAttempts = 5;
                    var attempt = 0;
                    var delaySeconds = 2;
                    while (true)
                    {
                        try
                        {
                            attempt++;
                            Console.WriteLine($"[UserService] Attempting DB connection (attempt {attempt}/{maxAttempts})...");
                            
                            // Сначала просто проверяем подключение
                            if (!db.Database.CanConnect())
                            {
                                throw new Exception("Cannot connect to database");
                            }

                            Console.WriteLine("[UserService] Database connection successful.");
                            
                            // Проверяем, есть ли pending миграции
                            var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                            var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
                            
                            Console.WriteLine($"[UserService] Applied migrations: {appliedMigrations.Count}");
                            Console.WriteLine($"[UserService] Pending migrations: {pendingMigrations.Count}");
                            
                            if (pendingMigrations.Any())
                            {
                                Console.WriteLine($"[UserService] Pending migrations: {string.Join(", ", pendingMigrations)}");
                                
                                try
                                {
                                    // Пытаемся применить миграции
                                    db.Database.Migrate();
                                    Console.WriteLine("[UserService] Database migrations applied successfully.");
                                }
                                catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42P07")
                                {
                                    // Ошибка "relation already exists" - таблица уже существует
                                    // Это означает, что миграция была применена вручную или частично
                                    Console.WriteLine("[UserService] WARNING: Tables already exist but migration history is incomplete.");
                                    Console.WriteLine("[UserService] Attempting to use EnsureCreated instead...");
                                    
                                    // Используем EnsureCreated для проверки/создания недостающих таблиц
                                    var created = db.Database.EnsureCreated();
                                    if (created)
                                    {
                                        Console.WriteLine("[UserService] Database schema created successfully.");
                                    }
                                    else
                                    {
                                        Console.WriteLine("[UserService] Database schema already exists, continuing...");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("[UserService] Database is up-to-date, no pending migrations.");
                            }
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[UserService] Database setup attempt {attempt} failed: {ex.Message}");
                            if (attempt >= maxAttempts)
                            {
                                Console.WriteLine("[UserService] Maximum attempts reached.");
                                Console.WriteLine("[UserService] WARNING: Starting application without complete database setup!");
                                Console.WriteLine("[UserService] Please manually fix database migration state.");
                                break; // Продолжаем запуск, чтобы приложение могло работать
                            }
                            var wait = TimeSpan.FromSeconds(delaySeconds * attempt);
                            Console.WriteLine($"[UserService] Waiting {wait.TotalSeconds} seconds before retrying...");
                            System.Threading.Thread.Sleep(wait);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UserService] Database setup error: {ex.Message}");
                    Console.WriteLine("[UserService] WARNING: Application will start but database may not be properly configured!");
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

            // Enable CORS for frontend apps (dev convenience) - after UseRouting and before auth
            app.UseCors("AllowFrontend");

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