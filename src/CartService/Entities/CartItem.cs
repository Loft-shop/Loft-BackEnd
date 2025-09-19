using ProductService.Entities;

namespace CartService.Entities;

public class CartItem
{
    public long Id { get; set; }
    public long CartId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public Cart Cart { get; set; }
    public Product Product { get; set; }
}