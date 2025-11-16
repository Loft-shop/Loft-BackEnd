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

    // Получение списка товаров с фильтром по категории, продавцу, цене и атрибутам + пагинация
    public async Task<IEnumerable<ProductDto>> GetAllProducts(ProductFilterDto filter)
    {
        // Базовый запрос с подгрузкой связанных данных
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.AttributeValues).ThenInclude(av => av.Attribute)
            .Include(p => p.MediaFiles)
            .Include(p => p.Comments).ThenInclude(c => c.MediaFiles)
            .Where(p => p.Status == ModerationStatus.Approved)
            .AsQueryable();

        // Фильтр по категории (если указан и > 0)
        if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        // Фильтр по продавцу (если указан и > 0)
        if (filter.SellerId.HasValue && filter.SellerId.Value > 0)
            query = query.Where(p => p.IdUser == filter.SellerId.Value);

        // Фильтр по минимальной цене (если указана и > 0)
        if (filter.MinPrice.HasValue && filter.MinPrice.Value > 0)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        // Фильтр по максимальной цене (если указана и > 0)
        if (filter.MaxPrice.HasValue && filter.MaxPrice.Value > 0)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        // Фильтр по атрибутам (если есть хотя бы один с корректными данными)
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

        // Пагинация (по умолчанию страница 1 и размер страницы 20)
        var skip = (filter.Page > 0 ? filter.Page - 1 : 0) * (filter.PageSize > 0 ? filter.PageSize : 20);
        var take = filter.PageSize > 0 ? filter.PageSize : 20;

        query = query.Skip(skip).Take(take);

        // Выполняем запрос и маппим в DTO
        var products = await query.ToListAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    // Получение одного товара по ID
    public async Task<ProductDto?> GetProductById(int productId)
    {
        var product = await _db.Products
        .Include(p => p.Category)
        .Include(p => p.AttributeValues).ThenInclude(av => av.Attribute)
        .Include(p => p.MediaFiles)
        .Include(p => p.Comments).ThenInclude(c => c.MediaFiles)
        .FirstOrDefaultAsync(p => p.Id == productId && p.Status == ModerationStatus.Approved);

        if (product == null)
            return null;

        // Мгновенно увеличивает ViewCount без перезагрузки
        await _db.Products
            .Where(p => p.Id == productId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));

        product.ViewCount++; // Чтобы DTO отдал актуальное количество

        return _mapper.Map<ProductDto?>(product);
    }

    // Создание нового товара
    public async Task<ProductDto> CreateProduct(ProductDto productDto)
    {
        var product = _mapper.Map<Product>(productDto); // преобразуем DTO в сущность
        product.CreatedAt = DateTime.UtcNow; // устанавливаем дату создания
        product.UpdatedAt = DateTime.UtcNow; // устанавливаем дату обновления

        _db.Products.Add(product); // добавляем в контекст
        await _db.SaveChangesAsync(); // сохраняем в БД

        return _mapper.Map<ProductDto>(product); // возвращаем DTO с заполненным Id
    }

    // Обновление существующего товара
    public async Task<ProductDto?> UpdateProduct(int productId, ProductDto productDto)
    {
        // Получаем товар из БД с атрибутами и медиа
        var product = await _db.Products
            .Include(p => p.AttributeValues)
            .Include(p => p.MediaFiles)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) return null; // если товара нет, возвращаем null

        _mapper.Map(productDto, product); // обновляем поля сущности из DTO
        product.UpdatedAt = DateTime.UtcNow; // обновляем дату

        await _db.SaveChangesAsync(); // сохраняем изменения
        return _mapper.Map<ProductDto>(product); // возвращаем DTO
    }

    // Удаление товара
    public async Task DeleteProduct(int productId)
    {
        // Загружаем продукт с зависимостями
        var product = await _db.Products
            .Include(p => p.MediaFiles) // медиа самого продукта
            .Include(p => p.Comments)
                .ThenInclude(c => c.MediaFiles) // медиа из комментариев
            .Include(p => p.AttributeValues) // значения атрибутов
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return; // или throw new KeyNotFoundException("Продукт не найден");

        // Удаляем медиа из комментариев
        var commentMedia = product.Comments.SelectMany(c => c.MediaFiles).ToList();
        if (commentMedia.Any())
            _db.MediaFiles.RemoveRange(commentMedia);

        // Удаляем комментарии
        if (product.Comments.Any())
            _db.Comments.RemoveRange(product.Comments);

        // Удаляем медиа самого продукта
        if (product.MediaFiles.Any())
            _db.MediaFiles.RemoveRange(product.MediaFiles);

        // Удаляем значения атрибутов
        if (product.AttributeValues.Any())
            _db.ProductAttributeValues.RemoveRange(product.AttributeValues);

        // Удаляем сам продукт
        _db.Products.Remove(product);

        await _db.SaveChangesAsync();
    }

    // ------------------ CATEGORIES ------------------

    // Получение категорий все категории в древовидной структуре    
    public async Task<IEnumerable<CategoryDto>> GetAllCategories()
    {
        // Загружаем все категории одним запросом
        var categories = await _db.Categories
            .Include(c => c.CategoryAttributes).ThenInclude(ca => ca.Attribute)
            .Include(c => c.Products)
            .AsNoTracking()
            .ToListAsync();

        // Маппим все категории в DTO
        var categoryDtos = _mapper.Map<List<CategoryDto>>(categories);

        // Создаём словарь для быстрого поиска родителя по Id
        var lookup = categoryDtos.ToDictionary(c => c.Id);

        // Список для хранения только верхнего уровня категорий
        List<CategoryDto> rootCategories = new();

        foreach (var category in categoryDtos)
        {
            if (category.ParentCategoryId == null)
            {
                // Если нет родителя — это корневая категория
                rootCategories.Add(category);
            }
            else if (lookup.TryGetValue(category.ParentCategoryId.Value, out var parent))
            {
                // Добавляем подкатегорию в родительскую
                parent.SubCategories ??= new List<CategoryDto>();
                parent.SubCategories.Add(category);
            }
        }

        return rootCategories;
    }

    // Получение категории по ID
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

    // Создание категории
    public async Task<CategoryDto> CreateCategory(CategoryDto dto)
    {

        // Создаем сущность категории
        var category = new Category
        {
            Name = dto.Name,
            ImageUrl = dto.ImageUrl,
            ParentCategoryId = dto.ParentCategoryId, // если null, категория будет верхнего уровня
            Status = 0,
            ViewCount = 0
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(); // получаем Id категории

        // Возвращаем DTO категории с пустыми подкатегориями и атрибутами
        var result = _mapper.Map<CategoryDto>(category);

        return result;
    }

    // Обновление категории
    public async Task<CategoryDto?> UpdateCategory(int categoryId, CategoryDto categoryDto)
    {
        var category = await _db.Categories
            .Include(c => c.SubCategories) // подгружаем подкатегории
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null) return null;

        _mapper.Map(categoryDto, category);

        // Обновление/добавление подкатегорий
        if (categoryDto.SubCategories != null)
        {
            foreach (var subDto in categoryDto.SubCategories)
            {
                // Если подкатегория уже есть, обновляем
                var existingSub = category.SubCategories.FirstOrDefault(sc => sc.Id == subDto.Id);
                if (existingSub != null)
                {
                    _mapper.Map(subDto, existingSub);
                }
                else
                {
                    // Новая подкатегория
                    var newSub = _mapper.Map<Category>(subDto);
                    newSub.ParentCategoryId = category.Id;
                    _db.Categories.Add(newSub);
                }
            }
        }

        await _db.SaveChangesAsync();
        return _mapper.Map<CategoryDto>(category);
    }

    // Удаление категории
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

    // Получение всех атрибутов 
    public async Task<IEnumerable<AttributeDto>> GetAllAttributes()
    {
        // Загружаем все атрибуты из базы
        var attributes = await _db.AttributeEntity
            .AsNoTracking() // для ускорения — данные только читаются
            .ToListAsync();

        // Маппим сущности в DTO
        return _mapper.Map<IEnumerable<AttributeDto>>(attributes);
    }

    // Получение атрибута по ID
    public async Task<AttributeDto?> GetAttributeById(int attributeId)
    {
        var attribute = await _db.AttributeEntity.FindAsync(attributeId);
        return _mapper.Map<AttributeDto?>(attribute);
    }

    // Создание нового атрибута
    public async Task<AttributeDto> CreateAttribute(AttributeDto attributeDto)
    {
        var attribute = _mapper.Map<AttributeEntity>(attributeDto);
        _db.AttributeEntity.Add(attribute);
        await _db.SaveChangesAsync();
        return _mapper.Map<AttributeDto>(attribute);
    }

    // Обновление атрибута
    public async Task<AttributeDto?> UpdateAttribute(int attributeId, AttributeDto attributeDto)
    {
        var attribute = await _db.AttributeEntity.FindAsync(attributeId);
        if (attribute == null) return null;

        _mapper.Map(attributeDto, attribute);
        await _db.SaveChangesAsync();
        return _mapper.Map<AttributeDto>(attribute);
    }

    // Удаление атрибута
    public async Task DeleteAttribute(int attributeId)
    {
        // Загружаем атрибут вместе с его значениями
        var attribute = await _db.AttributeEntity
            .Include(a => a.AttributeValues) // все ProductAttributeValues
            .Include(a => a.CategoryAttributes) // все связи с категориями
            .FirstOrDefaultAsync(a => a.Id == attributeId);

        if (attribute != null)
        {
            // Удаляем все значения атрибута у товаров
            if (attribute.AttributeValues.Any())
                _db.ProductAttributeValues.RemoveRange(attribute.AttributeValues);

            // Удаляем все связи с категориями
            if (attribute.CategoryAttributes.Any())
                _db.CategoryAttributes.RemoveRange(attribute.CategoryAttributes);

            // Удаляем сам атрибут
            _db.AttributeEntity.Remove(attribute);

            await _db.SaveChangesAsync();
        }
    }

    // ------------------ CATEGORY ATTRIBUTES ------------------

    // Привязка атрибута к категории
    public async Task<CategoryAttributeDto> AssignAttributeToCategory(int categoryId, int attributeId, bool isRequired, int orderIndex)
    {
        // Проверяем, есть ли уже такая привязка
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
            Status = ModerationStatus.Pending // по умолчанию в ожидании модерации
        };

        _db.CategoryAttributes.Add(categoryAttribute);
        await _db.SaveChangesAsync();
        return _mapper.Map<CategoryAttributeDto>(categoryAttribute);
    }

    // Удаление привязки атрибута от категории
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
            .Include(ca => ca.Attribute) // подгружаем сам атрибут
            .Where(ca => ca.CategoryId == categoryId)
            .ToListAsync();

        return _mapper.Map<IEnumerable<CategoryAttributeDto>>(attributes);
    }

    // ------------------ MODERATION ------------------
    // Получение товаров по статусу модерации
    public async Task<IEnumerable<ProductDto>> GetProductsByModerationStatus(ModerationStatus status)
    {
        var products = await _db.Products
            .Where(p => p.Status == status)
            .Include(p => p.Category)
            .Include(p => p.AttributeValues).ThenInclude(av => av.Attribute)
            .Include(p => p.MediaFiles)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
    // Обновление статуса модерации товара
    public async Task<ProductDto?> UpdateProductModerationStatus(int productId, ModerationStatus status)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return null;

        product.Status = status;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return _mapper.Map<ProductDto>(product);
    }
}
