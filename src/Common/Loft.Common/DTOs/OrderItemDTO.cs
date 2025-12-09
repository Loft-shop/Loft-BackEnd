using Loft.Common.Enums;

namespace Loft.Common.DTOs;

public record OrderItemDTO(
    long Id,
    long OrderId,
    long ProductId,
    int Quantity,
    decimal Price,
    
    // =============================================================================
    // НОВАЯ СИСТЕМА: Информация о товаре для отображения в заказе
    // =============================================================================
    string? ProductName = null,
    string? ImageUrl = null,
    
    // =============================================================================
    // НОВАЯ СИСТЕМА: Информация о категории товара
    // =============================================================================
    int? CategoryId = null,
    CategoryDto? Category = null,
    string? CategoryName = null,
    
    // Тип товара (Physical/Digital) - для контроля изменения количества на складе
    ProductType ProductType = ProductType.Physical,
    
    // =============================================================================
    // НОВАЯ СИСТЕМА: Динамические атрибуты товара (сохраняем для истории заказа)
    // =============================================================================
    List<ProductAttributeValueDto>? AttributeValues = null
);