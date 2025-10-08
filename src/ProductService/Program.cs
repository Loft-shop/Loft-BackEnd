using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductService.Data;
using ProductService.Mappings;
using ProductService.Services;
using Microsoft.EntityFrameworkCore;

namespace ProductService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добавляем контекст с локальной базой (например, SQL Server или SQLite)
            builder.Services.AddDbContext<ProductDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Добавляем сервисы контроллеров
            builder.Services.AddControllers();
            builder.Services.AddAutoMapper(typeof(ProductProfile));

            // Сервисы
            builder.Services.AddScoped<IProductService, ProductService.Services.ProductService>();

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