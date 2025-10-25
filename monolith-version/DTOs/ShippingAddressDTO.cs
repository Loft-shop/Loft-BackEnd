namespace MonolithVersion.DTOs;

public record ShippingAddressDTO(long Id,long CustomerId,string Address,string City,string PostalCode,string Country);
