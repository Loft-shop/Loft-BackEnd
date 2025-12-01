using Microsoft.EntityFrameworkCore;
using UserService.Entities;

namespace UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Chat> Chats { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<PasswordReset> PasswordResets { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            
            // Добавляем автоинкремент для PostgreSQL
            b.Property(u => u.Id)
                .UseIdentityColumn();
            
            b.Property(u => u.Email).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.PasswordHash).IsRequired();

            // массив избранных товаров
            b.Property(u => u.FavoriteProductIds)
             .HasColumnType("integer[]")
             .HasDefaultValue(Array.Empty<int>());
        });

        modelBuilder.Entity<Chat>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasMany(c => c.Messages)
             .WithOne(m => m.Chat)
             .HasForeignKey(m => m.ChatId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Конфигурация для ChatMessage
        modelBuilder.Entity<ChatMessage>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.MessageText).HasMaxLength(2000);
        });
    }
}