using MediaService.Data;
using MediaService.Mappings;
using MediaService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Security.Claims;
using System.Text;

namespace MediaService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();


            // Configure DbContext (PostgreSQL) - use connection string or default file
            var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<MediaDbContext>(options =>
                options.UseNpgsql(defaultConn, npgsqlOptions =>
                {
                    //        npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
                }));

            // Контроллеры
            builder.Services.AddControllers();

            // AutoMapper
            builder.Services.AddAutoMapper(typeof(MediaProfile));

            // Сервисы
            builder.Services.AddScoped<IMediaService, MediaStorageService>();
            builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

            // Настройка аутентификации JWT
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var jwtKey = jwtSection.GetValue<string>("Key");
            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new Exception("[MediaService] JWT Key is not configured!");
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
                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
            });

            builder.Services.AddAuthorization();

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

            var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), "storage");
            Directory.CreateDirectory(Path.Combine(storageRoot, "public"));
            Directory.CreateDirectory(Path.Combine(storageRoot, "private"));

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Media Service API v1");
                c.RoutePrefix = string.Empty; // Swagger UI будет на /
            });

            app.UseCors();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "storage/public")),
                RequestPath = "/media"
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

