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
            var result = await _service.GetAllProducts(filter);
            return Ok(result);
        }

        // Получение одного товара по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            long? userId = GetUserId();        // может быть null, и это ок
            bool isModerator = false;

            if (userId != null)
            {
                isModerator = IsModerator();

                var productTmp = await _service.GetProductById(id, true);
                if (productTmp == null) return NotFound();

                // Если товар принадлежит авторизованному пользователю — даём ему права модератора
                if (productTmp.IdUser == userId)
                {
                    isModerator = true;
                }
            }

            var product = await _service.GetProductById(id, isModerator);
            if (product == null) return NotFound();

            return Ok(product);
        }

        [HttpGet("myproducts")]
        public async Task<IActionResult> GetMyProducts()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var products = await _service.GetAllMyProducts(userId.Value);

            return Ok(products);
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

            var product = await _service.GetProductById(id, true);
            if (product == null) return NotFound();

            if (product.IdUser != userId)
                return Forbid();

            productDto.IdUser = product.IdUser;
            productDto.Status = Loft.Common.Enums.ModerationStatus.Pending;

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

            var product = await _service.GetProductById(id, true);
            if (product == null) return NotFound();

            if (product.IdUser != userId)
                return Forbid();

            await _service.DeleteProduct(id);
            return NoContent();
        }

        // Обновление количества товара (используется OrderService при покупке)
        [HttpPut("{id}/quantity")]
        public async Task<IActionResult> UpdateQuantity(int id, [FromBody] UpdateQuantityRequest request)
        {
            var product = await _service.GetProductById(id, true);
            if (product == null) return NotFound();

            // Проверяем, что это физический товар
            if (product.Type == ProductType.Digital)
            {
                return BadRequest(new { error = "Cannot update quantity for digital products" });
            }

            // Проверяем, что новое количество не отрицательное
            if (request.Quantity < 0)
            {
                return BadRequest(new { error = "Quantity cannot be negative" });
            }

            var updated = await _service.UpdateProductQuantity(id, request.Quantity);
            if (updated == null) return NotFound();

            return Ok(updated);
        }

        // Уменьшение количества товара при покупке (используется OrderService)
        [HttpPut("{id}/reduce-quantity")]
        public async Task<IActionResult> ReduceQuantity(int id, [FromBody] ReduceQuantityRequest request)
        {
            var product = await _service.GetProductById(id, true);
            if (product == null) return NotFound();

            // Проверяем, что это физический товар
            if (product.Type == ProductType.Digital)
            {
                return Ok(new { message = "Digital product - quantity not changed", product });
            }

            // Проверяем, что достаточно товара на складе
            if (product.Quantity < request.Quantity)
            {
                return BadRequest(new { error = $"Insufficient quantity. Available: {product.Quantity}, Requested: {request.Quantity}" });
            }

            var newQuantity = product.Quantity - request.Quantity;
            var updated = await _service.UpdateProductQuantity(id, newQuantity);
            if (updated == null) return NotFound();

            return Ok(updated);
        }
    }

    public class UpdateQuantityRequest
    {
        public int Quantity { get; set; }
    }

    public class ReduceQuantityRequest
    {
        public int Quantity { get; set; }
    }
}
