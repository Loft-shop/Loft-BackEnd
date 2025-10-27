using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using monolith_version.DTOs;
using monolith_version.Models.Enums;
using monolith_version.Services.ProductServices;
using monolith_version.Services.UserServices;

namespace monolith_version.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModerationController : BaseController
    {
        private readonly IProductService _service;
        private readonly IUserService _userService;

        public ModerationController(IProductService service, IUserService userService)
        {
            _service = service;
            _userService = userService;
        }

        
        [HttpGet("products/pending")]
        [Authorize]
        public async Task<IActionResult> GetPendingProducts()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var user = await _userService.GetUserById(userId.Value);

            if (user == null || user.Role != Role.MODERATOR) return Forbid();

            var products = await _service.GetProductsByModerationStatus(ModerationStatus.Pending);
            return Ok(products);
        }

        // Обновление статуса товара
        [HttpPut("products/{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateProductStatus(int id, [FromQuery] ModerationStatus status)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var user = await _userService.GetUserById(userId.Value);
            if (user == null || user.Role != Role.MODERATOR) return Forbid();

            var updated = await _service.UpdateProductModerationStatus(id, status);
            if (updated == null) return NotFound();

            return Ok(updated);
        }
    }
}