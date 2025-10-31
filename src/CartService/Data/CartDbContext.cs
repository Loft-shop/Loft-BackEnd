using CartService.Entities;
using Microsoft.EntityFrameworkCore;

namespace CartService.Data;

public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options)
    {
    }

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cart>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.CustomerId).IsUnique();
            b.Ignore(c => c.Customer); // Внешняя сущность из UserService, не маппим её здесь
            b.HasMany(c => c.CartItems).WithOne(ci => ci.Cart).HasForeignKey(ci => ci.CartId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(b =>
        {
            b.HasKey(ci => ci.Id);
            b.Ignore(ci => ci.Product); // Внешняя сущность из ProductService
            
            // Настраиваем типы данных для оптимизации производительности
            b.Property(ci => ci.Price).HasColumnType("decimal(18,2)");
            b.Property(ci => ci.ProductName).HasMaxLength(500);
            b.Property(ci => ci.ProductDescription).HasMaxLength(2000);
        });
    }
}