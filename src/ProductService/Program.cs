using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductService.Data;
using ProductService.Mappings;
using ProductService.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ProductService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<ProductDbContext>(options =>
                options.UseNpgsql(connectionString, // UseNpgsql использует строку подключения
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName);
                    }));

            // Добавляем сервисы контроллеров
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

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Swagger только в режиме Development 
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Настраиваем конвейер обработки запросов
            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}