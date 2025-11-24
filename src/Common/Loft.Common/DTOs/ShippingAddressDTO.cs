namespace Loft.Common.DTOs;

public record ShippingAddressDTO(
    long Id,
    long CustomerId,
    string Address,
    string City,
    string PostalCode,
    string Country,
    string? RecipientName = null,
    bool IsDefault = false,
    DateTime? CreatedAt = null
);

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
