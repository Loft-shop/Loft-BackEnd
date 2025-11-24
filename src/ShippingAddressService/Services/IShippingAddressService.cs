using Loft.Common.DTOs;

namespace ShippingAddressService.Services;

public interface IShippingAddressService
{
    Task<IEnumerable<ShippingAddressDTO>> GetAddressesByUserId(long customerId);
    Task<ShippingAddressDTO?> GetAddressById(long addressId);
    Task<ShippingAddressDTO?> GetDefaultAddress(long customerId);
    Task<ShippingAddressDTO> AddAddress(long customerId, ShippingAddressCreateDTO address);
    Task<ShippingAddressDTO?> UpdateAddress(long addressId, long customerId, ShippingAddressUpdateDTO address);
    Task<bool> DeleteAddress(long addressId, long customerId);
    Task<bool> SetDefaultAddress(long customerId, long addressId);
}