using Microsoft.EntityFrameworkCore;
using PaymentService.Entities;
//
namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Amount).HasColumnType("numeric(18,2)");
            b.Property(p => p.Method).IsRequired();
            b.Property(p => p.Status).IsRequired();
            b.Property(p => p.PaymentDate).IsRequired();
            b.HasIndex(p => p.OrderId).IsUnique(); // предполагаем один платеж на заказ
        });
    }
}