using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Services;
using System.Threading.Tasks;
using UserService.Services;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : BaseController
    {
        private readonly IProductService _service;

        public ProductController(IProductService service)
        {
            _service = service;
        }

        // Получение списка товаров с фильтром по категории/продавцу и пагинацией
        [HttpPost("filter")]
        public async Task<IActionResult> GetFilteredProducts([FromBody] ProductFilterDto filter)
        {
            var products = await _service.GetAllProducts(filter);

            return Ok(products);
        }

        // Получение одного товара по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _service.GetProductById(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // Создание нового товара
        [HttpPost("create")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Create([FromBody] ProductDto productDto)
        {
            var userId = GetUserId(); // Получаем ID пользователя из токена
            if (userId == null) return Unauthorized();

            productDto.IdUser = (int)userId; // Устанавливаем ID пользователя в DTO
            productDto.Status = Loft.Common.Enums.ModerationStatus.Pending; // Устанавливаем статус на PENDING по умолчанию

            var product = await _service.CreateProduct(productDto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        // Обновление товара
        [HttpPut("{id}")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto productDto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var product = await _service.GetProductById(id);
            if (product == null) return NotFound();

            if (product.IdUser != userId)
                return Forbid();

            var updated = await _service.UpdateProduct(id, productDto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // Удаление товара
        [HttpDelete("{id}")]
        [Authorize] // Требуем аутентификацию
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var product = await _service.GetProductById(id);
            if (product == null) return NotFound();

            if (product.IdUser != userId)
                return Forbid();

            await _service.DeleteProduct(id);
            return NoContent();
        }
    }
}