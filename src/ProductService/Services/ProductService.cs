using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AutoMapper;
using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductService.Data;
using ProductService.Entities;

namespace ProductService.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _db; // �������� ���� ������ EF Core
    private readonly IMapper _mapper;      // AutoMapper ��� �������������� ��������� � DTO � �������
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ProductDbContext db, IMapper mapper, IHttpClientFactory httpClientFactory, ILogger<ProductService> logger)
    {
        _db = db;
        _mapper = mapper;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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
        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products).ToList();
        
        // Автоматически загружаем данные продавцов
        await LoadSellerInfo(productDtos);
        
        return productDtos;
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

        var productDto = _mapper.Map<ProductDto?>(product);
        
        // Загружаем данные продавца для одного товара
        if (productDto != null)
        {
            await LoadSellerInfo(new List<ProductDto> { productDto });
        }
        
        return productDto;
    }

    // Новый метод: автоматическая загрузка данных продавцов из UserService
    private async Task LoadSellerInfo(IEnumerable<ProductDto> products)
    {
        var client = _httpClientFactory.CreateClient("UserService");
        
        foreach (var product in products)
        {
            if (product.IdUser.HasValue && product.IdUser.Value > 0)
            {
                try
                {
                    var userResponse = await client.GetAsync($"/api/users/{product.IdUser.Value}");
                    if (userResponse.IsSuccessStatusCode)
                    {
                        var user = await userResponse.Content.ReadFromJsonAsync<UserDTO>();
                        if (user != null)
                        {
                            // Используем только FirstName продавца; если пусто — показываем Email
                            var displayName = !string.IsNullOrWhiteSpace(user.FirstName)
                                ? user.FirstName
                                : user.Email;
                            product.SellerName = displayName;
                            product.SellerEmail = user.Email;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to load seller info for user {product.IdUser}");
                    // Продолжаем без данных продавца
                }
            }
        }
    }

    // Проверка прав доступа: может ли пользователь изменять товар
    public async Task<bool> CanUserModifyProduct(int productId, int userId)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return false;
        
        // Пользователь может изменять только свой товар
        return product.IdUser == userId;
    }

    // =============================================================================
    // ЛОКАЛЬНАЯ РАЗРАБОТКА: Простое создание товара (текущая)
    // =============================================================================
    // Создание нового товара
    public async Task<ProductDto> CreateProduct(ProductDto productDto)
    {
        // =============================================================================
        // ПРОВЕРКА: Может ли пользователь продавать товары?
        // =============================================================================
        if (productDto.IdUser.HasValue && productDto.IdUser.Value > 0)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var canSellResponse = await client.GetAsync($"http://userservice:8080/api/users/{productDto.IdUser.Value}/can-sell");
                
                if (canSellResponse.IsSuccessStatusCode)
                {
                    var canSellContent = await canSellResponse.Content.ReadAsStringAsync();
                    var canSellData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(canSellContent);
                    
                    if (canSellData.TryGetProperty("canSell", out var canSellValue))
                    {
                        var canSell = canSellValue.GetBoolean();
                        if (!canSell)
                        {
                            _logger.LogWarning($"User {productDto.IdUser} attempted to create product but CanSell=false");
                            throw new InvalidOperationException("User is not authorized to sell products. Please enable seller status first.");
                        }
                        _logger.LogInformation($"User {productDto.IdUser} verified as seller (CanSell=true)");
                    }
                }
                else
                {
                    _logger.LogWarning($"Failed to check CanSell status for user {productDto.IdUser}: {canSellResponse.StatusCode}");
                }
            }
            catch (InvalidOperationException)
            {
                throw; // Пробрасываем ошибку авторизации дальше
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not verify seller status for user {productDto.IdUser}, allowing creation");
                // Не блокируем создание товара если не удалось проверить статус (для разработки)
            }
        }

        var product = _mapper.Map<Product>(productDto);
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        // Загружаем категорию после создания товара
        await _db.Entry(product).Reference(p => p.Category).LoadAsync();

        return _mapper.Map<ProductDto>(product);
    }

    // =============================================================================
    // ГЛОБАЛЬНАЯ ПРОДАКШЕН: Создание товара с валидацией и транзакцией (закомментировано)
    // =============================================================================
    // Для PostgreSQL - валидируем категорию и атрибуты, используем транзакцию
    /*
    public async Task<ProductDto> CreateProduct(ProductDto productDto)
    {
        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                // Проверяем существование категории
                var categoryExists = await _db.Categories
                    .AnyAsync(c => c.Id == productDto.CategoryId && c.Status == ModerationStatus.Approved);
                
                if (!categoryExists)
                {
                    _logger.LogError($"Category {productDto.CategoryId} not found or not approved");
                    throw new ArgumentException($"Category {productDto.CategoryId} not found or not approved");
                }

                // Получаем обязательные атрибуты для категории
                var requiredAttributes = await _db.CategoryAttributes
                    .Where(ca => ca.CategoryId == productDto.CategoryId && ca.IsRequired)
                    .Select(ca => ca.AttributeId)
                    .ToListAsync();

                // Проверяем что все обязательные атрибуты заполнены
                if (productDto.AttributeValues != null && requiredAttributes.Any())
                {
                    var providedAttributes = productDto.AttributeValues.Select(av => av.AttributeId).ToList();
                    var missingAttributes = requiredAttributes.Except(providedAttributes).ToList();
                    
                    if (missingAttributes.Any())
                    {
                        _logger.LogError($"Missing required attributes: {string.Join(", ", missingAttributes)}");
                        throw new ArgumentException($"Missing required attributes: {string.Join(", ", missingAttributes)}");
                    }
                }

                // Создаем товар
                var product = _mapper.Map<Product>(productDto);
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;
                product.Status = ModerationStatus.Pending; // На модерацию

                _db.Products.Add(product);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Product {product.Id} created by user {product.IdUser}");
                return _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
    */

    // ���������� ������������� ������
    public async Task<ProductDto?> UpdateProduct(int productId, ProductDto productDto, int? currentUserId = null)
    {
        // �������� ����� �� �� � ���������� � �����
        var product = await _db.Products
            .Include(p => p.AttributeValues)
            .Include(p => p.MediaFiles)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) return null; // ���� ������ ���, ���������� null

        // Проверка прав доступа: пользователь может редактировать только свой товар
        if (currentUserId.HasValue && product.IdUser != currentUserId.Value)
        {
            _logger.LogWarning($"User {currentUserId} attempted to update product {productId} owned by user {product.IdUser}");
            return null; // Нет прав доступа
        }

        _mapper.Map(productDto, product); // ��������� ���� �������� �� DTO
        product.UpdatedAt = DateTime.UtcNow; // ��������� ����

        await _db.SaveChangesAsync(); // ��������� ���������
        return _mapper.Map<ProductDto>(product); // ���������� DTO
    }

    // �������� ������
    public async Task<bool> DeleteProduct(int productId, int? currentUserId = null)
    {
        // ��������� ������� � �������������
        var product = await _db.Products
            .Include(p => p.MediaFiles) // ����� ������ ��������
            .Include(p => p.Comments)
                .ThenInclude(c => c.MediaFiles) // ����� �� ������������
            .Include(p => p.AttributeValues) // �������� ���������
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return false; // Товар не найден

        // Проверка прав доступа: пользователь может удалять только свой товар
        if (currentUserId.HasValue && product.IdUser != currentUserId.Value)
        {
            _logger.LogWarning($"User {currentUserId} attempted to delete product {productId} owned by user {product.IdUser}");
            return false; // Нет прав доступа
        }

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
        return true; // Успешно удалено
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
    
    /*
public async Task<ProductDto> CreateProduct(ProductDto productDto)
{
    using (var transaction = await _db.Database.BeginTransactionAsync())
    {
        try
        {
            // Валидация категории
            // Проверка обязательных атрибутов
            // Транзакция для целостности данных
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
*/

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
    
    // =============================================================================
    // ГЛОБАЛЬНАЯ ПРОДАКШЕН: Удаление с проверкой использования (закомментировано)
    // =============================================================================
    /*
    public async Task DeleteAttribute(int attributeId)
    {
        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                var attribute = await _db.AttributeEntity
                    .Include(a => a.AttributeValues)
                    .Include(a => a.CategoryAttributes)
                    .FirstOrDefaultAsync(a => a.Id == attributeId);

                if (attribute == null)
                {
                    _logger.LogWarning($"Attribute {attributeId} not found");
                    return;
                }

                // Проверяем используется ли атрибут в товарах
                if (attribute.AttributeValues != null && attribute.AttributeValues.Any())
                {
                    var productsCount = attribute.AttributeValues.Select(av => av.ProductId).Distinct().Count();
                    _logger.LogWarning($"Cannot delete attribute {attributeId}: used in {productsCount} products");
                    throw new InvalidOperationException($"Cannot delete attribute: it's used in {productsCount} products");
                }

                // Сначала удаляем связи с категориями
                if (attribute.CategoryAttributes != null && attribute.CategoryAttributes.Any())
                {
                    _db.CategoryAttributes.RemoveRange(attribute.CategoryAttributes);
                    _logger.LogInformation($"Removed {attribute.CategoryAttributes.Count} category-attribute links");
                }

                // Затем удаляем сам атрибут
                _db.AttributeEntity.Remove(attribute);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Attribute {attributeId} deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting attribute {attributeId}");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
    */

    // ------------------ CATEGORY ATTRIBUTES ------------------

    // =============================================================================
    // ЛОКАЛЬНАЯ РАЗРАБОТКА: Простое добавление атрибута к категории (текущая)
    // =============================================================================
    // Привязать атрибут к категории
    public async Task<CategoryAttributeDto> AssignAttributeToCategory(int categoryId, int attributeId, bool isRequired, int orderIndex)
    {
        // Проверяем, есть ли уже такая связка
        var existing = await _db.CategoryAttributes
            .FirstOrDefaultAsync(ca => ca.CategoryId == categoryId && ca.AttributeId == attributeId);

        if (existing != null)
        {
            // Если есть, обновляем параметры
            existing.IsRequired = isRequired;
            existing.OrderIndex = orderIndex;
            await _db.SaveChangesAsync();
            return _mapper.Map<CategoryAttributeDto>(existing);
        }

        // Если нет, создаём новую
        var categoryAttribute = new CategoryAttribute
        {
            CategoryId = categoryId,
            AttributeId = attributeId,
            IsRequired = isRequired,
            OrderIndex = orderIndex,
            Status = ModerationStatus.Pending
        };

        _db.CategoryAttributes.Add(categoryAttribute);
        await _db.SaveChangesAsync();
        return _mapper.Map<CategoryAttributeDto>(categoryAttribute);
    }

    // =============================================================================
    // ГЛОБАЛЬНАЯ ПРОДАКШЕН: Привязка атрибута с транзакцией (закомментировано)
    // =============================================================================
    /*
    public async Task<CategoryAttributeDto> AssignAttributeToCategory(int categoryId, int attributeId, bool isRequired, int orderIndex)
    {
        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                // Блокируем запись для проверки (в PostgreSQL - FOR UPDATE)
                var existing = await _db.CategoryAttributes
                    .Where(ca => ca.CategoryId == categoryId && ca.AttributeId == attributeId)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    // Обновляем существующую связь
                    existing.IsRequired = isRequired;
                    existing.OrderIndex = orderIndex;
                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation($"Updated category-attribute link: CategoryId={categoryId}, AttributeId={attributeId}");
                    return _mapper.Map<CategoryAttributeDto>(existing);
                }

                // Создаем новую связь
                var categoryAttribute = new CategoryAttribute
                {
                    CategoryId = categoryId,
                    AttributeId = attributeId,
                    IsRequired = isRequired,
                    OrderIndex = orderIndex,
                    Status = ModerationStatus.Approved // На продакшене можно сразу одобрять
                };

                _db.CategoryAttributes.Add(categoryAttribute);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation($"Created category-attribute link: CategoryId={categoryId}, AttributeId={attributeId}");
                return _mapper.Map<CategoryAttributeDto>(categoryAttribute);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning attribute {attributeId} to category {categoryId}");
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
    */

    // Удалить привязку атрибута от категории
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

    // Получение всех атрибутов категории
    public async Task<IEnumerable<CategoryAttributeDto>> GetCategoryAttributes(int categoryId)
    {
        var attributes = await _db.CategoryAttributes
            .Include(ca => ca.Attribute) // Подгружаем сам атрибут
            .Where(ca => ca.CategoryId == categoryId)
            .ToListAsync();

        return _mapper.Map<IEnumerable<CategoryAttributeDto>>(attributes);
    }

    // Получение атрибутов категории с полной информацией (для UI форм)
    public async Task<IEnumerable<CategoryAttributeFullDto>> GetCategoryAttributesWithDetails(int categoryId)
    {
        var attributes = await _db.CategoryAttributes
            .Include(ca => ca.Attribute)
            .Where(ca => ca.CategoryId == categoryId)
            .OrderBy(ca => ca.OrderIndex)
            .ToListAsync();

        return attributes.Select(ca => new CategoryAttributeFullDto
        {
            AttributeId = ca.AttributeId,
            AttributeName = ca.Attribute.Name,
            DisplayName = ca.Attribute.DisplayName,
            Type = ca.Attribute.Type,
            TypeDisplayName = ca.Attribute.TypeDisplayName,
            OptionsJson = ca.Attribute.OptionsJson,
            IsRequired = ca.IsRequired,
            OrderIndex = ca.OrderIndex
        });
    }
}
