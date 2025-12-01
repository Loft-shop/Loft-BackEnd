namespace Loft.Common.DTOs;


public class ShippingAddressDTO
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public record ShippingAddressCreateDTO(
    string Address,
    string City,
    string PostalCode,
    string Country,
    string? RecipientName = null,
    bool IsDefault = false
);

public record ShippingAddressUpdateDTO(
    string Address,
    string City,
    string PostalCode,
    string Country,
    string? RecipientName = null,
    bool? IsDefault = null
);
