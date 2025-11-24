using Microsoft.EntityFrameworkCore;
using OrderService.Entities;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Настройка Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            
            entity.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(o => o.CustomerName)
                .HasMaxLength(200);
            
            entity.Property(o => o.CustomerEmail)
                .HasMaxLength(200);
            
            // Настройка полей адреса доставки
            entity.Property(o => o.ShippingAddress)
                .HasMaxLength(500);
            
            entity.Property(o => o.ShippingCity)
                .HasMaxLength(200);
            
            entity.Property(o => o.ShippingPostalCode)
                .HasMaxLength(20);
            
            entity.Property(o => o.ShippingCountry)
                .HasMaxLength(100);
            
            entity.Property(o => o.ShippingRecipientName)
                .HasMaxLength(200);
            
            // Индекс для быстрого поиска по адресу доставки
            entity.HasIndex(o => o.ShippingAddressId);
            
            // Связь Order -> OrderItems
            entity.HasMany(o => o.OrderItems)
                .WithOne()
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Настройка OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            
            entity.Property(oi => oi.Price)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(oi => oi.ProductName)
                .HasMaxLength(500);
            
            entity.Property(oi => oi.ImageUrl)
                .HasMaxLength(1000);
            
            entity.Property(oi => oi.CategoryName)
                .HasMaxLength(200);
        });
    }
}