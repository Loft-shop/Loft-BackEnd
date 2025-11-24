using Loft.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace UserService.Controllers;

/// <summary>
/// Контроллер для управления адресами доставки в профиле пользователя
/// Проксирует запросы к ShippingAddressService
/// </summary>
[ApiController]
[Route("api/users/me/shipping-addresses")]
[Authorize]
public class ProfileShippingAddressController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProfileShippingAddressController> _logger;

    public ProfileShippingAddressController(
        IHttpClientFactory httpClientFactory,
        ILogger<ProfileShippingAddressController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private long? GetUserIdFromClaims()
    {
        var tryTypes = new[]
        {
            ClaimTypes.NameIdentifier,
            "nameid",
            JwtRegisteredClaimNames.Sub,
            "id",
            "user_id",
            ClaimTypes.Name,
            ClaimTypes.Email
        };

        foreach (var t in tryTypes)
        {
            var claim = User.FindFirst(t)?.Value;
            if (!string.IsNullOrEmpty(claim) && long.TryParse(claim, out var id)) return id;
        }

        foreach (var c in User.Claims)
        {
            if (!string.IsNullOrEmpty(c.Value) && long.TryParse(c.Value, out var id)) return id;
        }

        return null;
    }

    /// <summary>
    /// Получить все адреса доставки текущего пользователя
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShippingAddressDTO>>> GetMyAddresses()
    {
        var userId = GetUserIdFromClaims();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ShippingAddressService");
            var response = await client.GetAsync($"/api/shipping-addresses/customer/{userId.Value}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get addresses from ShippingAddressService: {StatusCode}", response.StatusCode);
                return StatusCode((int)response.StatusCode, new { message = "Failed to retrieve addresses" });
            }

            var addresses = await response.Content.ReadFromJsonAsync<IEnumerable<ShippingAddressDTO>>();
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipping addresses for user {UserId}", userId.Value);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить дефолтный адрес доставки
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<ShippingAddressDTO>> GetMyDefaultAddress()
    {
        var userId = GetUserIdFromClaims();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ShippingAddressService");
            var response = await client.GetAsync($"/api/shipping-addresses/customer/{userId.Value}/default");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = "No default address found" });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get default address from ShippingAddressService: {StatusCode}", response.StatusCode);
                return StatusCode((int)response.StatusCode, new { message = "Failed to retrieve default address" });
            }

            var address = await response.Content.ReadFromJsonAsync<ShippingAddressDTO>();
            return Ok(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default shipping address for user {UserId}", userId.Value);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить конкретный адрес по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ShippingAddressDTO>> GetAddressById(long id)
    {
        var userId = GetUserIdFromClaims();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ShippingAddressService");
            var response = await client.GetAsync($"/api/shipping-addresses/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = $"Address with ID {id} not found" });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get address {AddressId} from ShippingAddressService: {StatusCode}", id, response.StatusCode);
                return StatusCode((int)response.StatusCode, new { message = "Failed to retrieve address" });
            }

            var address = await response.Content.ReadFromJsonAsync<ShippingAddressDTO>();
            
            // Проверка прав доступа
            if (address != null && address.CustomerId != userId.Value)
            {
                return Forbid();
            }

            return Ok(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipping address {AddressId} for user {UserId}", id, userId.Value);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Создать новый адрес доставки
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ShippingAddressDTO>> CreateAddress([FromBody] ShippingAddressCreateDTO addressDto)
    {
        var userId = GetUserIdFromClaims();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ShippingAddressService");
            var response = await client.PostAsJsonAsync($"/api/shipping-addresses/customer/{userId.Value}", addressDto);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to create address in ShippingAddressService: {StatusCode}", response.StatusCode);
                return StatusCode((int)response.StatusCode, new { message = "Failed to create address" });
            }

            var created = await response.Content.ReadFromJsonAsync<ShippingAddressDTO>();
            return CreatedAtAction(nameof(GetAddressById), new { id = created!.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shipping address for user {UserId}", userId.Value);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Обновить существующий адрес доставки
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ShippingAddressDTO>> UpdateAddress(long id, [FromBody] ShippingAddressUpdateDTO addressDto)
    {
        var userId = GetUserIdFromClaims();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            // Сначала проверяем, что адрес принадлежит пользователю
            var client = _httpClientFactory.CreateClient("ShippingAddressService");
            var checkResponse = await client.GetAsync($"/api/shipping-addresses/{id}");
            
            if (checkResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = $"Address with ID {id} not found" });
            }

            if (checkResponse.IsSuccessStatusCode)
            {
                var existingAddress = await checkResponse.Content.ReadFromJsonAsync<ShippingAddressDTO>();
                if (existingAddress != null && existingAddress.CustomerId != userId.Value)
                {
                    return Forbid();
                }
            }

            // Обновляем адрес
            var response = await client.PutAsJsonAsync($"/api/shipping-addresses/{id}", addressDto);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = $"Address with ID {id} not found or access denied" });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to update address {AddressId} in ShippingAddressService: {StatusCode}", id, response.StatusCode);
                return StatusCode((int)response.StatusCode, new { message = "Failed to update address" });
            }

            var updated = await response.Content.ReadFromJsonAsync<ShippingAddressDTO>();
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipping address {AddressId} for user {UserId}", id, userId.Value);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Удалить адрес доставки
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAddress(long id)
    {
        var userId = GetUserIdFromClaims();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            // Сначала проверяем, что адрес принадлежит пользователю
            var client = _httpClientFactory.CreateClient("ShippingAddressService");
            var checkResponse = await client.GetAsync($"/api/shipping-addresses/{id}");
            
            if (checkResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = $"Address with ID {id} not found" });
            }

            if (checkResponse.IsSuccessStatusCode)
            {
                var existingAddress = await checkResponse.Content.ReadFromJsonAsync<ShippingAddressDTO>();
                if (existingAddress != null && existingAddress.CustomerId != userId.Value)
                {
                    return Forbid();
                }
            }

            // Удаляем адрес
            var response = await client.DeleteAsync($"/api/shipping-addresses/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = $"Address with ID {id} not found or access denied" });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to delete address {AddressId} in ShippingAddressService: {StatusCode}", id, response.StatusCode);
                return StatusCode((int)response.StatusCode, new { message = "Failed to delete address" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shipping address {AddressId} for user {UserId}", id, userId.Value);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Установить адрес как дефолтный
    /// </summary>
    [HttpPost("{id}/set-default")]
    public async Task<IActionResult> SetDefaultAddress(long id)
    {
        var userId = GetUserIdFromClaims();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            // Сначала проверяем, что адрес принадлежит пользователю
            var client = _httpClientFactory.CreateClient("ShippingAddressService");
            var checkResponse = await client.GetAsync($"/api/shipping-addresses/{id}");
            
            if (checkResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = $"Address with ID {id} not found" });
            }

            if (checkResponse.IsSuccessStatusCode)
            {
                var existingAddress = await checkResponse.Content.ReadFromJsonAsync<ShippingAddressDTO>();
                if (existingAddress != null && existingAddress.CustomerId != userId.Value)
                {
                    return Forbid();
                }
            }

            // Устанавливаем как дефолтный
            var response = await client.PostAsync($"/api/shipping-addresses/{id}/set-default", null);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = $"Address with ID {id} not found or access denied" });
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to set default address {AddressId} in ShippingAddressService: {StatusCode}", id, response.StatusCode);
                return StatusCode((int)response.StatusCode, new { message = "Failed to set default address" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default shipping address {AddressId} for user {UserId}", id, userId.Value);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

