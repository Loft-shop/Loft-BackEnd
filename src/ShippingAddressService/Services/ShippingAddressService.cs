using AutoMapper;
using Loft.Common.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShippingAddressService.Data;
using ShippingAddressService.Entities;

namespace ShippingAddressService.Services;

public class ShippingAddressService : IShippingAddressService
{
    private readonly ShippingAddressDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ShippingAddressService> _logger;

    public ShippingAddressService(
        ShippingAddressDbContext context,
        IMapper mapper,
        ILogger<ShippingAddressService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ShippingAddressDTO>> GetAddressesByUserId(long customerId)
    {
        var addresses = await _context.ShippingAddresses
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ShippingAddressDTO>>(addresses);
    }

    public async Task<ShippingAddressDTO?> GetAddressById(long addressId)
    {
        var address = await _context.ShippingAddresses.FindAsync(addressId);
        return address == null ? null : _mapper.Map<ShippingAddressDTO>(address);
    }

    public async Task<ShippingAddressDTO?> GetDefaultAddress(long customerId)
    {
        var defaultAddress = await _context.ShippingAddresses
            .Where(a => a.CustomerId == customerId && a.IsDefault)
            .FirstOrDefaultAsync();

        return defaultAddress == null ? null : _mapper.Map<ShippingAddressDTO>(defaultAddress);
    }

    public async Task<ShippingAddressDTO> AddAddress(long customerId, ShippingAddressCreateDTO addressDto)
    {
        var address = _mapper.Map<ShippingAddress>(addressDto);
        address.CustomerId = customerId;
        address.CreatedAt = DateTime.UtcNow;

        // Если это первый адрес пользователя или помечен как дефолтный
        var hasExistingAddresses = await _context.ShippingAddresses
            .AnyAsync(a => a.CustomerId == customerId);

        if (!hasExistingAddresses)
        {
            // Первый адрес автоматически становится дефолтным
            address.IsDefault = true;
        }
        else if (address.IsDefault)
        {
            // Снимаем флаг IsDefault с других адресов этого пользователя
            await UnsetDefaultAddresses(customerId);
        }

        _context.ShippingAddresses.Add(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created shipping address {AddressId} for customer {CustomerId}", 
            address.Id, customerId);

        return _mapper.Map<ShippingAddressDTO>(address);
    }

    public async Task<ShippingAddressDTO?> UpdateAddress(long addressId, long customerId, ShippingAddressUpdateDTO addressDto)
    {
        var address = await _context.ShippingAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId);

        if (address == null)
        {
            _logger.LogWarning("Address {AddressId} not found for customer {CustomerId}", addressId, customerId);
            return null;
        }

        // Обновляем поля
        address.Address = addressDto.Address;
        address.City = addressDto.City;
        address.PostalCode = addressDto.PostalCode;
        address.Country = addressDto.Country;
        address.RecipientName = addressDto.RecipientName;

        // Если указано IsDefault и оно true
        if (addressDto.IsDefault.HasValue && addressDto.IsDefault.Value && !address.IsDefault)
        {
            await UnsetDefaultAddresses(customerId);
            address.IsDefault = true;
        }
        else if (addressDto.IsDefault.HasValue && !addressDto.IsDefault.Value)
        {
            address.IsDefault = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated shipping address {AddressId} for customer {CustomerId}", 
            addressId, customerId);

        return _mapper.Map<ShippingAddressDTO>(address);
    }

    public async Task<bool> DeleteAddress(long addressId, long customerId)
    {
        var address = await _context.ShippingAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId);

        if (address == null)
        {
            _logger.LogWarning("Address {AddressId} not found for customer {CustomerId}", addressId, customerId);
            return false;
        }

        var wasDefault = address.IsDefault;
        _context.ShippingAddresses.Remove(address);
        await _context.SaveChangesAsync();

        // Если удалили дефолтный адрес, назначаем дефолтным следующий (если есть)
        if (wasDefault)
        {
            var nextAddress = await _context.ShippingAddresses
                .Where(a => a.CustomerId == customerId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (nextAddress != null)
            {
                nextAddress.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        _logger.LogInformation("Deleted shipping address {AddressId} for customer {CustomerId}", 
            addressId, customerId);

        return true;
    }

    public async Task<bool> SetDefaultAddress(long customerId, long addressId)
    {
        var address = await _context.ShippingAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId);

        if (address == null)
        {
            _logger.LogWarning("Address {AddressId} not found for customer {CustomerId}", addressId, customerId);
            return false;
        }

        if (address.IsDefault)
        {
            // Уже дефолтный
            return true;
        }

        await UnsetDefaultAddresses(customerId);
        address.IsDefault = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Set address {AddressId} as default for customer {CustomerId}", 
            addressId, customerId);

        return true;
    }

    private async Task UnsetDefaultAddresses(long customerId)
    {
        var defaultAddresses = await _context.ShippingAddresses
            .Where(a => a.CustomerId == customerId && a.IsDefault)
            .ToListAsync();

        foreach (var addr in defaultAddresses)
        {
            addr.IsDefault = false;
        }
    }
}