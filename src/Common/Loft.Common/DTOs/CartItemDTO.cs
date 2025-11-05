namespace Loft.Common.DTOs;

public record CartItemDTO(
    long Id,
    long CartId,
    long ProductId,
    int Quantity,
    decimal Price = 0,
    string? ProductName = null,
    string? ImageUrl = null,
    
    // =============================================================================
    // НОВАЯ СИСТЕМА: Информация о категории товара
    // =============================================================================
    int? CategoryId = null,
    CategoryDto? Category = null,
    
    // Для обратной совместимости - название категории
    string? CategoryName = null,
    
    // =============================================================================
    // НОВАЯ СИСТЕМА: Динамические атрибуты товара (например: RAM=8GB, Size=L)
    // =============================================================================
    List<ProductAttributeValueDto>? AttributeValues = null,
    
    DateTime? AddedAt = null
);