using Microsoft.EntityFrameworkCore;
using monolith_version.Models.Entities;

namespace monolith_version.Data;

/// <summary>
/// Единый контекст БД для всего монолита.
/// Добавляйте сюда новые DbSet для других модулей (Products, Orders, Categories и т.д.)
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Users модуль
    public DbSet<User> Users => Set<User>();
    
    // TODO: Добавьте сюда DbSet для других модулей:
    // public DbSet<Product> Products => Set<Product>();
    // public DbSet<Order> Orders => Set<Order>();
    // public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User конфигурация
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.PasswordHash).IsRequired();
        });
        
        // TODO: Добавьте сюда конфигурации для других сущностей
    }
}
