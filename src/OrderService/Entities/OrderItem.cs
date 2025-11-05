namespace OrderService.Entities;

public class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    
    // =============================================================================
    // НОВАЯ СИСТЕМА: Сохраняем информацию о товаре для истории заказа
    // =============================================================================
    // Важно! Товар может быть удален или изменен, но в заказе должна остаться
    // информация о том, что было куплено
    
    public string? ProductName { get; set; }
    public string? ImageUrl { get; set; }
    
    // Информация о категории на момент заказа
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    
    // Динамические атрибуты товара на момент заказа (JSON)
    // Пример: {"RAM":"8GB","Color":"Black","Storage":"256GB"}
    public string? ProductAttributesJson { get; set; }
}