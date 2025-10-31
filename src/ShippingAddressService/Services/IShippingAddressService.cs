using Loft.Common.DTOs;

namespace ShippingAddressService.Services;

public interface IShippingAddressService
{
    Task<IEnumerable<ShippingAddressService>> GetAddressesByUserId(long customerId);
    Task<ShippingAddressDTO?> GetAddressById(long addressId);
    Task<ShippingAddressDTO> AddAddress(long customerId, ShippingAddressDTO address);
    Task<ShippingAddressDTO?> UpdateAddress(long addressId, ShippingAddressDTO address);
    Task DeleteAddress(long addressId);
    Task SetDefaultAddress(long customerId, long addressId);
    
    /*
     * Примечания: AddAddress принимает ShippingAddressDTO
     * (с полем CustomerId можно игнорировать/проверять на реализации)
     */
}