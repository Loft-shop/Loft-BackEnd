namespace monolith_version.DTOs;

public record ShippingAddressDto(long Id,long CustomerId,string Address,string City,string PostalCode,string Country);
