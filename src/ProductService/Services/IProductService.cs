using Loft.Common.DTOs;
using Loft.Common.Enums;
using ProductService.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Services;

public interface IProductService
{
    // ------------------ PRODUCTS ------------------
    Task<IEnumerable<ProductDto>> GetAllProducts(ProductFilterDto filter);

    Task<ProductDto?> GetProductById(int productId);

    Task<ProductDto> CreateProduct(ProductDto product);

    Task<ProductDto?> UpdateProduct(int productId, ProductDto product);

    Task DeleteProduct(int productId);


    // ------------------ CATEGORIES ------------------
    // Получение всех категорий (с пагинацией или фильтром по родителю)
    Task<IEnumerable<CategoryDto>> GetAllCategories();

    // Получение категории по ID
    Task<CategoryDto?> GetCategoryById(int categoryId);

    // Создание новой категории
    Task<CategoryDto> CreateCategory(CategoryDto category);

    // Обновление существующей категории
    Task<CategoryDto?> UpdateCategory(int categoryId, CategoryDto category);

    // Удаление категории
    Task DeleteCategory(int categoryId);


    // ------------------ ATTRIBUTES ------------------
    // Получение всех атрибутов
    Task<IEnumerable<AttributeDto>> GetAllAttributes();

    // Получение атрибута по ID
    Task<AttributeDto?> GetAttributeById(int attributeId);

    // Создание нового атрибута
    Task<AttributeDto> CreateAttribute(AttributeDto attribute);

    // Обновление атрибута
    Task<AttributeDto?> UpdateAttribute(int attributeId, AttributeDto attribute);

    // Удаление атрибута
    Task DeleteAttribute(int attributeId);

    // ------------------ CATEGORY ATTRIBUTES ------------------
    // Привязка атрибута к категории
    Task<CategoryAttributeDto> AssignAttributeToCategory(int categoryId, int attributeId, bool isRequired, int orderIndex);

    // Удаление привязки атрибута от категории
    Task RemoveAttributeFromCategory(int categoryId, int attributeId);

    // Получение атрибутов категории
    Task<IEnumerable<CategoryAttributeDto>> GetCategoryAttributes(int categoryId);

    //------------------ MODERATION ------------------

    // Получение товаров по статусу модерации
    Task<IEnumerable<ProductDto>> GetProductsByModerationStatus(ModerationStatus status);

    // Обновление статуса товара
    Task<ProductDto?> UpdateProductModerationStatus(int productId, ModerationStatus status);
}