using Microsoft.EntityFrameworkCore;
using ProductService.Entities;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    // Таблица товаров
    public DbSet<Product> Products { get; set; }

    // Таблица категорий
    public DbSet<Category> Categories { get; set; }

    // Таблица значений атрибутов товаров
    public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }

    // Таблица связи категорий и атрибутов
    public DbSet<CategoryAttribute> CategoryAttributes { get; set; }

    // Таблица медиафайлов (изображения и видео товаров и комментариев)
    public DbSet<MediaFile> MediaFiles { get; set; }

    // Таблица комментариев к товарам
    public DbSet<Comment> Comments { get; set; }

    // Таблица атрибутов (например: цвет, размер, материал)
    public DbSet<AttributeEntity> AttributeEntity { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- PRODUCT ----------------
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id); // Первичный ключ

            // Связь один-ко-многим: Product -> Category
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Не удалять категорию вместе с товаром

            // Связь один-ко-многим: Product -> ProductAttributeValue
            entity.HasMany(p => p.AttributeValues)
                .WithOne(a => a.Product)
                .HasForeignKey(a => a.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять все атрибуты товара при удалении товара

            // Связь один-ко-многим: Product -> MediaFiles
            entity.HasMany(p => p.MediaFiles)
                .WithOne(m => m.Product)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Cascade); //  удалять медиа при удалении товара

            // Связь один-ко-многим: Product -> Comments
            entity.HasMany(p => p.Comments)
                .WithOne(c => c.Product)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять комментарии при удалении товара
        });

        // ---------------- CATEGORY ----------------
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id); // Первичный ключ

            // Связь один-ко-многим: Category -> Products
            entity.HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Не удалять товары при удалении категории

            // Связь один-ко-многим: Category -> CategoryAttributes
            entity.HasMany(c => c.CategoryAttributes)
                .WithOne(ca => ca.Category)
                .HasForeignKey(ca => ca.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять связи при удалении категории

            // Связь сам с собой для подкатегорий
            entity.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Не удалять родительскую категорию вместе с дочерними
        });

        // ---------------- CATEGORY ATTRIBUTE ----------------
        modelBuilder.Entity<CategoryAttribute>(entity =>
        {
            entity.HasKey(ca => ca.Id); // Первичный ключ

            // Связь с категорией
            entity.HasOne(ca => ca.Category)
                .WithMany(c => c.CategoryAttributes)
                .HasForeignKey(ca => ca.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять связи при удалении категории

            // Связь с атрибутом
            entity.HasOne(ca => ca.Attribute)
                .WithMany(a => a.CategoryAttributes)
                .HasForeignKey(ca => ca.AttributeId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять связи при удалении атрибута
        });

        // ---------------- PRODUCT ATTRIBUTE VALUE ----------------
        modelBuilder.Entity<ProductAttributeValue>(entity =>
        {
            entity.HasKey(pav => pav.Id); // Первичный ключ

            // Связь с товаром
            entity.HasOne(pav => pav.Product)
                .WithMany(p => p.AttributeValues)
                .HasForeignKey(pav => pav.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять значения атрибутов при удалении товара

            // Связь с атрибутом
            entity.HasOne(pav => pav.Attribute)
                .WithMany(a => a.AttributeValues)
                .HasForeignKey(pav => pav.AttributeId)
                .OnDelete(DeleteBehavior.Restrict); // Не удалять атрибут при удалении значения
        });

        // ---------------- MEDIA FILE ----------------
        modelBuilder.Entity<MediaFile>(entity =>
        {
            entity.HasKey(m => m.Id); // Первичный ключ

            // Связь с товаром
            entity.HasOne(m => m.Product)
                .WithMany(p => p.MediaFiles)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.NoAction); // не удалять медиа при удалении товара

            // Связь с комментарием
            entity.HasOne(m => m.Comment)
                .WithMany(c => c.MediaFiles)
                .HasForeignKey(m => m.CommentId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять медиа при удалении комментария
        });

        // ---------------- COMMENT ----------------
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id); // Первичный ключ

            // Связь с товаром
            entity.HasOne(c => c.Product)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять комментарии при удалении товара
        });

        // ---------------- ATTRIBUTE ENTITY ----------------
        modelBuilder.Entity<AttributeEntity>(entity =>
        {
            entity.HasKey(a => a.Id); // Первичный ключ

            // Связь с CategoryAttributes
            entity.HasMany(a => a.CategoryAttributes)
                .WithOne(ca => ca.Attribute)
                .HasForeignKey(ca => ca.AttributeId)
                .OnDelete(DeleteBehavior.Cascade); // Удалять связи при удалении атрибута

            // Связь с ProductAttributeValues
            entity.HasMany(a => a.AttributeValues)
                .WithOne(av => av.Attribute)
                .HasForeignKey(av => av.AttributeId)
                .OnDelete(DeleteBehavior.Restrict); // Не удалять атрибут при удалении значения
        });
    }
}