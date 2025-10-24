using AutoMapper;
using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using ProductService.Data;
using ProductService.Entities;

namespace ProductService.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _db; // �������� ���� ������ EF Core
    private readonly IMapper _mapper;      // AutoMapper ��� �������������� ��������� � DTO � �������

    public ProductService(ProductDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // ------------------ PRODUCTS ------------------

    // ��������� ������ ������� � �������� �� ���������, ��������, ���� � ��������� + ���������
    public async Task<IEnumerable<ProductDto>> GetAllProducts(ProductFilterDto filter)
    {
        // ������� ������ � ���������� ��������� ������
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.AttributeValues).ThenInclude(av => av.Attribute)
            .Include(p => p.MediaFiles)
            .Include(p => p.Comments).ThenInclude(c => c.MediaFiles)
            .AsQueryable();

        // ������ �� ��������� (���� ������ � > 0)
        if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        // ������ �� �������� (���� ������ � > 0)
        if (filter.SellerId.HasValue && filter.SellerId.Value > 0)
            query = query.Where(p => p.IdUser == filter.SellerId.Value);

        // ������ �� ����������� ���� (���� ������� � > 0)
        if (filter.MinPrice.HasValue && filter.MinPrice.Value > 0)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        // ������ �� ������������ ���� (���� ������� � > 0)
        if (filter.MaxPrice.HasValue && filter.MaxPrice.Value > 0)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        // ������ �� ��������� (���� ���� ���� �� ���� � ����������� �������)
        if (filter.AttributeFilters != null && filter.AttributeFilters.Any())
        {
            foreach (var af in filter.AttributeFilters)
            {
                if (af.AttributeId > 0 && !string.IsNullOrWhiteSpace(af.Value))
                {
                    query = query.Where(p => p.AttributeValues
                        .Any(av => av.AttributeId == af.AttributeId && av.Value == af.Value));
                }
            }
        }

        // ��������� (�� ��������� �������� 1 � ������ �������� 20)
        var skip = (filter.Page > 0 ? filter.Page - 1 : 0) * (filter.PageSize > 0 ? filter.PageSize : 20);
        var take = filter.PageSize > 0 ? filter.PageSize : 20;

        query = query.Skip(skip).Take(take);

        // ��������� ������ � ������ � DTO
        var products = await query.ToListAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    // ��������� ������ ������ �� ID
    public async Task<ProductDto?> GetProductById(int productId)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.AttributeValues).ThenInclude(av => av.Attribute)
            .Include(p => p.MediaFiles)
            .Include(p => p.Comments).ThenInclude(c => c.MediaFiles)
            .FirstOrDefaultAsync(p => p.Id == productId);

        return _mapper.Map<ProductDto?>(product);
    }

    // �������� ������ ������
    public async Task<ProductDto> CreateProduct(ProductDto productDto)
    {
        var product = _mapper.Map<Product>(productDto); // ����������� DTO � ��������
        product.CreatedAt = DateTime.UtcNow; // ������������� ���� ��������
        product.UpdatedAt = DateTime.UtcNow; // ������������� ���� ����������

        _db.Products.Add(product); // ��������� � ��������
        await _db.SaveChangesAsync(); // ��������� � ��

        return _mapper.Map<ProductDto>(product); // ���������� DTO � ����������� Id
    }

    // ���������� ������������� ������
    public async Task<ProductDto?> UpdateProduct(int productId, ProductDto productDto)
    {
        // �������� ����� �� �� � ���������� � �����
        var product = await _db.Products
            .Include(p => p.AttributeValues)
            .Include(p => p.MediaFiles)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) return null; // ���� ������ ���, ���������� null

        _mapper.Map(productDto, product); // ��������� ���� �������� �� DTO
        product.UpdatedAt = DateTime.UtcNow; // ��������� ����

        await _db.SaveChangesAsync(); // ��������� ���������
        return _mapper.Map<ProductDto>(product); // ���������� DTO
    }

    // �������� ������
    public async Task DeleteProduct(int productId)
    {
        // ��������� ������� � �������������
        var product = await _db.Products
            .Include(p => p.MediaFiles) // ����� ������ ��������
            .Include(p => p.Comments)
                .ThenInclude(c => c.MediaFiles) // ����� �� ������������
            .Include(p => p.AttributeValues) // �������� ���������
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return; // ��� throw new KeyNotFoundException("������� �� ������");

        // ������� ����� �� ������������
        var commentMedia = product.Comments.SelectMany(c => c.MediaFiles).ToList();
        if (commentMedia.Any())
            _db.MediaFiles.RemoveRange(commentMedia);

        // ������� �����������
        if (product.Comments.Any())
            _db.Comments.RemoveRange(product.Comments);

        // ������� ����� ������ ��������
        if (product.MediaFiles.Any())
            _db.MediaFiles.RemoveRange(product.MediaFiles);

        // ������� �������� ���������
        if (product.AttributeValues.Any())
            _db.ProductAttributeValues.RemoveRange(product.AttributeValues);

        // ������� ��� �������
        _db.Products.Remove(product);

        await _db.SaveChangesAsync();
    }

    // ------------------ CATEGORIES ------------------

    // ��������� ��������� ��� ��������� � ����������� ���������    
    public async Task<IEnumerable<CategoryDto>> GetAllCategories()
    {
        // ��������� ��� ��������� ����� ��������
        var categories = await _db.Categories
            .Include(c => c.CategoryAttributes).ThenInclude(ca => ca.Attribute)
            .Include(c => c.Products)
            .AsNoTracking()
            .ToListAsync();

        // ������ ��� ��������� � DTO
        var categoryDtos = _mapper.Map<List<CategoryDto>>(categories);

        // ������ ������� ��� �������� ������ �������� �� Id
        var lookup = categoryDtos.ToDictionary(c => c.Id);

        // ������ ��� �������� ������ �������� ������ ���������
        List<CategoryDto> rootCategories = new();

        foreach (var category in categoryDtos)
        {
            if (category.ParentCategoryId == null)
            {
                // ���� ��� �������� � ��� �������� ���������
                rootCategories.Add(category);
            }
            else if (lookup.TryGetValue(category.ParentCategoryId.Value, out var parent))
            {
                // ��������� ������������ � ������������
                parent.SubCategories ??= new List<CategoryDto>();
                parent.SubCategories.Add(category);
            }
        }

        return rootCategories;
    }

    // ��������� ��������� �� ID
    public async Task<CategoryDto?> GetCategoryById(int categoryId)
    {
        var category = await _db.Categories
            .Include(c => c.CategoryAttributes).ThenInclude(ca => ca.Attribute)
            .Include(c => c.Products)
            .Include(c => c.SubCategories)
            .ThenInclude(sc => sc.CategoryAttributes).ThenInclude(ca => ca.Attribute)
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        return _mapper.Map<CategoryDto?>(category);
    }

    // �������� ���������
    public async Task<CategoryDto> CreateCategory(CategoryDto dto)
    {

        // ������� �������� ���������
        var category = new Category
        {
            Name = dto.Name,
            ImageUrl = dto.ImageUrl,
            ParentCategoryId = dto.ParentCategoryId, // ���� null, ��������� ����� �������� ������
            Status = 0,
            ViewCount = 0
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(); // �������� Id ���������

        // ���������� DTO ��������� � ������� �������������� � ����������
        var result = _mapper.Map<CategoryDto>(category);

        return result;
    }

    // ���������� ���������
    public async Task<CategoryDto?> UpdateCategory(int categoryId, CategoryDto categoryDto)
    {
        var category = await _db.Categories
            .Include(c => c.SubCategories) // ���������� ������������
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null) return null;

        _mapper.Map(categoryDto, category);

        // ����������/���������� ������������
        if (categoryDto.SubCategories != null)
        {
            foreach (var subDto in categoryDto.SubCategories)
            {
                // ���� ������������ ��� ����, ���������
                var existingSub = category.SubCategories.FirstOrDefault(sc => sc.Id == subDto.Id);
                if (existingSub != null)
                {
                    _mapper.Map(subDto, existingSub);
                }
                else
                {
                    // ����� ������������
                    var newSub = _mapper.Map<Category>(subDto);
                    newSub.ParentCategoryId = category.Id;
                    _db.Categories.Add(newSub);
                }
            }
        }

        await _db.SaveChangesAsync();
        return _mapper.Map<CategoryDto>(category);
    }

    // �������� ���������
    public async Task DeleteCategory(int categoryId)
    {
        var category = await _db.Categories.FindAsync(categoryId);
        if (category != null)
        {
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
        }
    }

    // ------------------ ATTRIBUTES ------------------

    // ��������� ���� ��������� 
    public async Task<IEnumerable<AttributeDto>> GetAllAttributes()
    {
        // ��������� ��� �������� �� ����
        var attributes = await _db.AttributeEntity
            .AsNoTracking() // ��� ��������� � ������ ������ ��������
            .ToListAsync();

        // ������ �������� � DTO
        return _mapper.Map<IEnumerable<AttributeDto>>(attributes);
    }

    // ��������� �������� �� ID
    public async Task<AttributeDto?> GetAttributeById(int attributeId)
    {
        var attribute = await _db.AttributeEntity.FindAsync(attributeId);
        return _mapper.Map<AttributeDto?>(attribute);
    }

    // �������� ������ ��������
    public async Task<AttributeDto> CreateAttribute(AttributeDto attributeDto)
    {
        var attribute = _mapper.Map<AttributeEntity>(attributeDto);
        _db.AttributeEntity.Add(attribute);
        await _db.SaveChangesAsync();
        return _mapper.Map<AttributeDto>(attribute);
    }

    // ���������� ��������
    public async Task<AttributeDto?> UpdateAttribute(int attributeId, AttributeDto attributeDto)
    {
        var attribute = await _db.AttributeEntity.FindAsync(attributeId);
        if (attribute == null) return null;

        _mapper.Map(attributeDto, attribute);
        await _db.SaveChangesAsync();
        return _mapper.Map<AttributeDto>(attribute);
    }

    // �������� ��������
    public async Task DeleteAttribute(int attributeId)
    {
        // ��������� ������� ������ � ��� ����������
        var attribute = await _db.AttributeEntity
            .Include(a => a.AttributeValues) // ��� ProductAttributeValues
            .Include(a => a.CategoryAttributes) // ��� ����� � �����������
            .FirstOrDefaultAsync(a => a.Id == attributeId);

        if (attribute != null)
        {
            // ������� ��� �������� �������� � �������
            if (attribute.AttributeValues.Any())
                _db.ProductAttributeValues.RemoveRange(attribute.AttributeValues);

            // ������� ��� ����� � �����������
            if (attribute.CategoryAttributes.Any())
                _db.CategoryAttributes.RemoveRange(attribute.CategoryAttributes);

            // ������� ��� �������
            _db.AttributeEntity.Remove(attribute);

            await _db.SaveChangesAsync();
        }
    }

    // ------------------ CATEGORY ATTRIBUTES ------------------

    // �������� �������� � ���������
    public async Task<CategoryAttributeDto> AssignAttributeToCategory(int categoryId, int attributeId, bool isRequired, int orderIndex)
    {
        // ���������, ���� �� ��� ����� ��������
        var existing = await _db.CategoryAttributes
            .FirstOrDefaultAsync(ca => ca.CategoryId == categoryId && ca.AttributeId == attributeId);

        if (existing != null)
        {
            // ���� ����, ��������� ���������
            existing.IsRequired = isRequired;
            existing.OrderIndex = orderIndex;
            await _db.SaveChangesAsync();
            return _mapper.Map<CategoryAttributeDto>(existing);
        }

        // ���� ���, ������ �����
        var categoryAttribute = new CategoryAttribute
        {
            CategoryId = categoryId,
            AttributeId = attributeId,
            IsRequired = isRequired,
            OrderIndex = orderIndex,
            Status = ModerationStatus.Pending // �� ��������� � �������� ���������
        };

        _db.CategoryAttributes.Add(categoryAttribute);
        await _db.SaveChangesAsync();
        return _mapper.Map<CategoryAttributeDto>(categoryAttribute);
    }

    // �������� �������� �������� �� ���������
    public async Task RemoveAttributeFromCategory(int categoryId, int attributeId)
    {
        var categoryAttribute = await _db.CategoryAttributes
            .FirstOrDefaultAsync(ca => ca.CategoryId == categoryId && ca.AttributeId == attributeId);

        if (categoryAttribute != null)
        {
            _db.CategoryAttributes.Remove(categoryAttribute);
            await _db.SaveChangesAsync();
        }
    }

    // ��������� ���� ��������� ���������
    public async Task<IEnumerable<CategoryAttributeDto>> GetCategoryAttributes(int categoryId)
    {
        var attributes = await _db.CategoryAttributes
            .Include(ca => ca.Attribute) // ���������� ��� �������
            .Where(ca => ca.CategoryId == categoryId)
            .ToListAsync();

        return _mapper.Map<IEnumerable<CategoryAttributeDto>>(attributes);
    }
}