using Microsoft.EntityFrameworkCore;
using ShippingAddressService.Entities;

namespace ShippingAddressService.Data;

public class ShippingAddressDbContext : DbContext
{
    public ShippingAddressDbContext(DbContextOptions<ShippingAddressDbContext> options) : base(options)
    {
    }

    public DbSet<ShippingAddress> ShippingAddresses => Set<ShippingAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShippingAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Индекс для быстрого поиска по CustomerId
            entity.HasIndex(e => e.CustomerId);
            
            // Индекс для быстрого поиска дефолтного адреса
            entity.HasIndex(e => new { e.CustomerId, e.IsDefault });
            
            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(500);
                
            entity.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.PostalCode)
                .IsRequired()
                .HasMaxLength(20);
                
            entity.Property(e => e.Country)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.RecipientName)
                .HasMaxLength(200);
                
            entity.Property(e => e.IsDefault)
                .HasDefaultValue(false);
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Игнорируем навигационное свойство Customer (внешняя сущность)
            entity.Ignore(e => e.Customer);
        });
    }
}