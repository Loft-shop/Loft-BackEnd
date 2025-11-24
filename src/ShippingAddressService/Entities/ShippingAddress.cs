using UserService.Entities;

namespace ShippingAddressService.Entities;

public class ShippingAddress
{
    public long Id { get; set; }
    public long CustomerId { get; set; } 
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property (внешняя сущность)
    public User? Customer { get; set; }
}