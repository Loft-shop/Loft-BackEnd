using System;
using System.Text;
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
            builder.Services.AddDbContext<UserDbContext>(options => options.UseNpgsql(defaultConn));

            // Add CORS policy for frontend development
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
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
                        ValidateLifetime = true
                    };
                });
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

                // Support file uploads in Swagger for endpoints with IFormFile
                //c.OperationFilter<FileUploadOperationFilter>();
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
                    db.Database.Migrate();
                    Console.WriteLine("[UserService] Database migration applied successfully.");
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

            // Enable CORS for frontend apps (dev convenience) - after UseRouting and before auth
            app.UseCors("AllowAll");

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