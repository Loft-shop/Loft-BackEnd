using UserService.Entities;

namespace ShippingAddressService.Entities;

public class ShippingAddress
{
    public long Id { get; set; }
    public long CustomerId { get; set; } 
    public string Address { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
    public string FullName { get; set; }
    public User Customer { get; set; } 
    public long? OrderId { get; set; }
}