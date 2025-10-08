using Loft.Common.DTOs;

namespace CategoryService.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategories(int page = 1, int pageSize = 20);
    Task<CategoryDto?> GetCategoryById(long categoryId);
    Task<IEnumerable<CategoryDto>> GetSubcategories(long parentId);
    Task<CategoryDto> CreateCategory(CategoryDto category);
    Task<CategoryDto?> UpdateCategory(long categoryId, CategoryDto category);
    Task DeleteCategory(long categoryId);
    Task<bool> CategoryExists(long categoryId);
    
    /*
     * Примечания: пагинация по умолчанию; GetCategoryById возвращает nullable если не найдено.
     */
}