using System.Threading.Tasks;
using Loft.Common.DTOs;
using Microsoft.AspNetCore.Mvc;
using ProductService.Services;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductController(IProductService service)
        {
            _service = service;
        }

        // ��������� ������ ������� � �������� �� ���������/�������� � ����������
        [HttpPost("filter")]
        public async Task<IActionResult> GetFilteredProducts([FromBody] ProductFilterDto filter)
        {
            var products = await _service.GetAllProducts(filter);
            return Ok(products);
        }

        // ��������� ������ ������ �� ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _service.GetProductById(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // �������� ������ ������
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductDto productDto)
        {
            var product = await _service.CreateProduct(productDto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        // Обновление товара — требуется X-User-Id
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto productDto, [FromHeader(Name = "X-User-Id")] int? userId = null)
        {
            if (!userId.HasValue)
                return BadRequest(new { message = "X-User-Id header is required" });

            var updated = await _service.UpdateProduct(id, productDto, userId);
            if (updated == null)
                return Forbid();
            return Ok(updated);
        }

        // Удаление товара — требуется X-User-Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromHeader(Name = "X-User-Id")] int? userId = null)
        {
            if (!userId.HasValue)
                return BadRequest(new { message = "X-User-Id header is required" });

            var deleted = await _service.DeleteProduct(id, userId);
            if (!deleted)
                return Forbid();
            return NoContent();
        }

        // Проверка прав доступа к товару
        [HttpGet("{id}/can-modify")]
        public async Task<IActionResult> CanModify(int id, [FromHeader(Name = "X-User-Id")] int? userId = null)
        {
            if (!userId.HasValue)
                return BadRequest(new { message = "User ID is required" });

            var canModify = await _service.CanUserModifyProduct(id, userId.Value);
            return Ok(new { canModify });
        }
    }
}