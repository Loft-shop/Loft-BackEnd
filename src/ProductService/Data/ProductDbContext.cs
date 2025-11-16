using Loft.Common.Enums;
using Microsoft.EntityFrameworkCore;
using ProductService.Entities;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    // ������� �������
    public DbSet<Product> Products { get; set; }

    // ������� ���������
    public DbSet<Category> Categories { get; set; }

    // ������� �������� ��������� �������
    public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }

    // ������� ����� ��������� � ���������
    public DbSet<CategoryAttribute> CategoryAttributes { get; set; }

    // ������� ����������� (����������� � ����� ������� � ������������)
    public DbSet<MediaFile> MediaFiles { get; set; }

    // ������� ������������ � �������
    public DbSet<Comment> Comments { get; set; }

    // ������� ��������� (��������: ����, ������, ��������)
    public DbSet<AttributeEntity> AttributeEntity { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- PRODUCT ----------------
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id); // ��������� ����

            // ����� ����-��-������: Product -> Category
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // �� ������� ��������� ������ � �������

            // ����� ����-��-������: Product -> ProductAttributeValue
            entity.HasMany(p => p.AttributeValues)
                .WithOne(a => a.Product)
                .HasForeignKey(a => a.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // ������� ��� �������� ������ ��� �������� ������

            // ����� ����-��-������: Product -> MediaFiles
            entity.HasMany(p => p.MediaFiles)
                .WithOne(m => m.Product)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Cascade); //  ������� ����� ��� �������� ������

            // ����� ����-��-������: Product -> Comments
            entity.HasMany(p => p.Comments)
                .WithOne(c => c.Product)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // ������� ����������� ��� �������� ������
        });

        // ---------------- CATEGORY ----------------
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id); // ��������� ����

            // ����� ����-��-������: Category -> Products
            entity.HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // �� ������� ������ ��� �������� ���������

            // ����� ����-��-������: Category -> CategoryAttributes
            entity.HasMany(c => c.CategoryAttributes)
                .WithOne(ca => ca.Category)
                .HasForeignKey(ca => ca.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); // ������� ����� ��� �������� ���������

            entity.Property(c => c.Type)
                .HasDefaultValue(ProductType.Physical);

            // ����� ��� � ����� ��� ������������
            entity.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict); // �� ������� ������������ ��������� ������ � ���������
        });

        // ---------------- CATEGORY ATTRIBUTE ----------------
        modelBuilder.Entity<CategoryAttribute>(entity =>
        {
            entity.HasKey(ca => ca.Id); // ��������� ����

            // ����� � ����������
            entity.HasOne(ca => ca.Category)
                .WithMany(c => c.CategoryAttributes)
                .HasForeignKey(ca => ca.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); // ������� ����� ��� �������� ���������

            // ����� � ���������
            entity.HasOne(ca => ca.Attribute)
                .WithMany(a => a.CategoryAttributes)
                .HasForeignKey(ca => ca.AttributeId)
                .OnDelete(DeleteBehavior.Cascade); // ������� ����� ��� �������� ��������
        });

        // ---------------- PRODUCT ATTRIBUTE VALUE ----------------
        modelBuilder.Entity<ProductAttributeValue>(entity =>
        {
            entity.HasKey(pav => pav.Id); // ��������� ����

            // ����� � �������
            entity.HasOne(pav => pav.Product)
                .WithMany(p => p.AttributeValues)
                .HasForeignKey(pav => pav.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // ������� �������� ��������� ��� �������� ������

            // ����� � ���������
            entity.HasOne(pav => pav.Attribute)
                .WithMany(a => a.AttributeValues)
                .HasForeignKey(pav => pav.AttributeId)
                .OnDelete(DeleteBehavior.Restrict); // �� ������� ������� ��� �������� ��������
        });

        // ---------------- MEDIA FILE ----------------
        modelBuilder.Entity<MediaFile>(entity =>
        {
            entity.HasKey(m => m.Id); // ��������� ����

            // ����� � �������
            entity.HasOne(m => m.Product)
                .WithMany(p => p.MediaFiles)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.NoAction); // �� ������� ����� ��� �������� ������

            // ����� � ������������
            entity.HasOne(m => m.Comment)
                .WithMany(c => c.MediaFiles)
                .HasForeignKey(m => m.CommentId)
                .OnDelete(DeleteBehavior.Cascade); // ������� ����� ��� �������� �����������
        });

        // ---------------- COMMENT ----------------
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id); // ��������� ����

            // ����� � �������
            entity.HasOne(c => c.Product)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // ������� ����������� ��� �������� ������
        });

        // ---------------- ATTRIBUTE ENTITY ----------------
        modelBuilder.Entity<AttributeEntity>(entity =>
        {
            entity.HasKey(a => a.Id); // ��������� ����

            // ����� � CategoryAttributes
            entity.HasMany(a => a.CategoryAttributes)
                .WithOne(ca => ca.Attribute)
                .HasForeignKey(ca => ca.AttributeId)
                .OnDelete(DeleteBehavior.Cascade); // ������� ����� ��� �������� ��������

            // ����� � ProductAttributeValues
            entity.HasMany(a => a.AttributeValues)
                .WithOne(av => av.Attribute)
                .HasForeignKey(av => av.AttributeId)
                .OnDelete(DeleteBehavior.Restrict); // �� ������� ������� ��� �������� ��������
        });
    }
}