using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using monolith_version.Data;
using monolith_version.Mappings;
using monolith_version.Services.ProductServices;
using monolith_version.Services.UserServices;
using ProductService.Mappings;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger с поддержкой IFormFile
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Monolith API", Version = "v1" });
    c.OperationFilter<SwaggerFileOperationFilter>();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT как: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(UserProfile));
builder.Services.AddAutoMapper(typeof(ProductProfile));

// EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("MonolithDb"));
}

// DI
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IProductService, monolith_version.Services.ProductServices.ProductService>();

// JWT
var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key") ?? "DevKey_ChangeMe_ForLocalOnly_1234567890";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

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
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = true,    
        ValidIssuer = "LoftUserService",
        ValidateAudience = true, 
        ValidAudience = "LoftUsers",
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            Console.WriteLine("[JWT] Authentication failed: " + ctx.Exception?.Message);
            if (ctx.Exception?.InnerException != null)
                Console.WriteLine("[JWT] Inner: " + ctx.Exception.InnerException.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            Console.WriteLine("[JWT] Token validated. Subject: " + ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            Console.WriteLine("[JWT] OnChallenge: error=" + ctx.Error + " desc=" + ctx.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// --- Auto-apply migrations ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        try
        {
            db.Database.Migrate();
            Console.WriteLine("Migrations applied successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed: {ex.Message}");
        }
    }
}

// --- Создаём wwwroot если нет ---
var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(wwwroot)) Directory.CreateDirectory(wwwroot);

// --- Middleware ---
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Swagger всегда доступен
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Monolith API V1");
    c.RoutePrefix = "swagger"; // Swagger UI теперь на /swagger/index.html
});

// Map controllers
app.MapControllers();

// URL явно (не обязательно, но удобно)
app.Urls.Clear();
app.Urls.Add("http://localhost:5096");
app.Urls.Add("https://localhost:7093");

app.Run();

// --- OperationFilter для IFormFile ---
public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formFileParameters = context.MethodInfo
            .GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        if (!formFileParameters.Any()) return;

        operation.Parameters.Clear(); // убираем стандартные параметры

        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = formFileParameters.ToDictionary(
                            p => string.IsNullOrEmpty(p.Name) ? "file" : p.Name,
                            p => new OpenApiSchema { Type = "string", Format = "binary" }
                        ),
                        Required = new HashSet<string>(formFileParameters.Select(p => string.IsNullOrEmpty(p.Name) ? "file" : p.Name))
                    }
                }
            }
        };
    }
}