using System.Collections.Generic;
using System.Threading.Tasks;
using Loft.Common.DTOs;
using ProductService.Entities;

namespace ProductService.Services;

public interface IProductService
{
    // ------------------ PRODUCTS ------------------
    Task<IEnumerable<ProductDto>> GetAllProducts(ProductFilterDto filter);

    Task<ProductDto?> GetProductById(int productId);

    Task<ProductDto> CreateProduct(ProductDto product);

    Task<ProductDto?> UpdateProduct(int productId, ProductDto product, int? currentUserId = null);

    Task<bool> DeleteProduct(int productId, int? currentUserId = null);
    
    Task<bool> CanUserModifyProduct(int productId, int userId);


    // ------------------ CATEGORIES ------------------
    // ��������� ���� ��������� (� ���������� ��� �������� �� ��������)
    Task<IEnumerable<CategoryDto>> GetAllCategories();

    // ��������� ��������� �� ID
    Task<CategoryDto?> GetCategoryById(int categoryId);

    // �������� ����� ���������
    Task<CategoryDto> CreateCategory(CategoryDto category);

    // ���������� ������������ ���������
    Task<CategoryDto?> UpdateCategory(int categoryId, CategoryDto category);

    // �������� ���������
    Task DeleteCategory(int categoryId);


    // ------------------ ATTRIBUTES ------------------
    // ��������� ���� ���������
    Task<IEnumerable<AttributeDto>> GetAllAttributes();

    // ��������� �������� �� ID
    Task<AttributeDto?> GetAttributeById(int attributeId);

    // �������� ������ ��������
    Task<AttributeDto> CreateAttribute(AttributeDto attribute);

    // ���������� ��������
    Task<AttributeDto?> UpdateAttribute(int attributeId, AttributeDto attribute);

    // �������� ��������
    Task DeleteAttribute(int attributeId);

    // ------------------ CATEGORY ATTRIBUTES ------------------
    // �������� �������� � ���������
    Task<CategoryAttributeDto> AssignAttributeToCategory(int categoryId, int attributeId, bool isRequired, int orderIndex);

    // �������� �������� �������� �� ���������
    Task RemoveAttributeFromCategory(int categoryId, int attributeId);

    // ��������� ��������� ���������
    Task<IEnumerable<CategoryAttributeDto>> GetCategoryAttributes(int categoryId);
    
    // ��������� ��������� ��������� (����, ������� � t.)
    Task<IEnumerable<CategoryAttributeFullDto>> GetCategoryAttributesWithDetails(int categoryId);
}