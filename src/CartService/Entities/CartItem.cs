using ProductService.Entities;
using Loft.Common.Enums;

namespace CartService.Entities;

public class CartItem
{
    public long Id { get; set; }
    public long CartId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public Cart? Cart { get; set; }
    public Product? Product { get; set; }

    // =============================================================================
    // Сохраняем минимально необходимую информацию о товаре для отображения корзины
    // =============================================================================
    public decimal Price { get; set; }
    public string? ProductName { get; set; }
    public string? ImageUrl { get; set; }
    
    // Категория товара (для фильтрации и группировки)
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    
    // Тип товара (Physical/Digital) - для ограничения изменения количества
    public ProductType ProductType { get; set; }
    
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}