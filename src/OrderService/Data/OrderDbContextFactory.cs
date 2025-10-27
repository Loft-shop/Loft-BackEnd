using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderService.Data;

public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();

        // Используем строку подключения из переменной окружения или значение по умолчанию
        var connectionString = Environment.GetEnvironmentVariable("ORDER_CONNECTION")
                             ?? "Host=172.17.4.22;Port=5432;Database=test_loft_shop;Username=developer;Password=Devp@ssw0rddB4589";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
        });

        return new OrderDbContext(optionsBuilder.Options);
    }
}

