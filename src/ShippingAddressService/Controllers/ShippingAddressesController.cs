using Loft.Common.DTOs;
using Microsoft.AspNetCore.Mvc;
using ShippingAddressService.Services;
using System.Security.Claims;

namespace ShippingAddressService.Controllers
{
    [ApiController]
    [Route("api/shipping-addresses")]
    public class ShippingAddressesController : ControllerBase
    {
        private readonly IShippingAddressService _addressService;
        private readonly ILogger<ShippingAddressesController> _logger;

        public ShippingAddressesController(
            IShippingAddressService addressService,
            ILogger<ShippingAddressesController> logger)
        {
            _addressService = addressService;
            _logger = logger;
        }

        /// <summary>
        /// Получить идентификатор пользователя из Claims (для авторизованных запросов)
        /// Если пользователь не авторизован, возвращает null
        /// </summary>
        private long? GetUserIdFromClaims()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("userId")?.Value;

            if (long.TryParse(idClaim, out var userId))
            {
                return userId;
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

            var addresses = await _addressService.GetAddressesByUserId(userId.Value);
            return Ok(addresses);
        }

        /// <summary>
        /// Получить адреса конкретного пользователя (по customerId в query параметре)
        /// </summary>
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<ShippingAddressDTO>>> GetAddressesByCustomerId(long customerId)
        {
            var addresses = await _addressService.GetAddressesByUserId(customerId);
            return Ok(addresses);
        }

        /// <summary>
        /// Получить конкретный адрес по ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ShippingAddressDTO>> GetAddressById(long id)
        {
            var address = await _addressService.GetAddressById(id);
            if (address == null)
            {
                return NotFound(new { message = $"Address with ID {id} not found" });
            }

            // Проверка прав доступа (опционально)
            var userId = GetUserIdFromClaims();
            if (userId.HasValue && address.CustomerId != userId.Value)
            {
                return Forbid();
            }

            return Ok(address);
        }

        /// <summary>
        /// Получить дефолтный адрес текущего пользователя
        /// </summary>
        [HttpGet("default")]
        public async Task<ActionResult<ShippingAddressDTO>> GetMyDefaultAddress()
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var address = await _addressService.GetDefaultAddress(userId.Value);
            if (address == null)
            {
                return NotFound(new { message = "No default address found" });
            }

            return Ok(address);
        }

        /// <summary>
        /// Получить дефолтный адрес конкретного пользователя
        /// </summary>
        [HttpGet("customer/{customerId}/default")]
        public async Task<ActionResult<ShippingAddressDTO>> GetDefaultAddressByCustomerId(long customerId)
        {
            var address = await _addressService.GetDefaultAddress(customerId);
            if (address == null)
            {
                return NotFound(new { message = $"No default address found for customer {customerId}" });
            }

            return Ok(address);
        }

        /// <summary>
        /// Создать новый адрес доставки для текущего пользователя
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ShippingAddressDTO>> CreateAddress([FromBody] ShippingAddressCreateDTO addressDto)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var created = await _addressService.AddAddress(userId.Value, addressDto);
            return CreatedAtAction(nameof(GetAddressById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Создать новый адрес для конкретного пользователя
        /// </summary>
        [HttpPost("customer/{customerId}")]
        public async Task<ActionResult<ShippingAddressDTO>> CreateAddressForCustomer(
            long customerId,
            [FromBody] ShippingAddressCreateDTO addressDto)
        {
            var created = await _addressService.AddAddress(customerId, addressDto);
            return CreatedAtAction(nameof(GetAddressById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Обновить адрес доставки
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ShippingAddressDTO>> UpdateAddress(
            long id,
            [FromBody] ShippingAddressUpdateDTO addressDto)
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var updated = await _addressService.UpdateAddress(id, userId.Value, addressDto);
            if (updated == null)
            {
                return NotFound(new { message = $"Address with ID {id} not found or access denied" });
            }

            return Ok(updated);
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

            var deleted = await _addressService.DeleteAddress(id, userId.Value);
            if (!deleted)
            {
                return NotFound(new { message = $"Address with ID {id} not found or access denied" });
            }

            return NoContent();
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

            var success = await _addressService.SetDefaultAddress(userId.Value, id);
            if (!success)
            {
                return NotFound(new { message = $"Address with ID {id} not found or access denied" });
            }

            return NoContent();
        }
    }
}