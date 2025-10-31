using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Controllers;
using ProductService.Services;
using UserService.Services;

namespace monolith_version.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModerationController : BaseController
    {
        private readonly IProductService _service;

        public ModerationController(IProductService service)
        {
            _service = service;
        }


        [HttpGet("products/pending")]
        [Authorize]
        public async Task<IActionResult> GetPendingProducts()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (!IsModerator()) return Forbid();

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

            if (!IsModerator()) return Forbid();

            var updated = await _service.UpdateProductModerationStatus(id, status);
            if (updated == null) return NotFound();

            return Ok(updated);
        }
    }
}