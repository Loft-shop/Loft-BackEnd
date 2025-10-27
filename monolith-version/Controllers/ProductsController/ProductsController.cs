using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using monolith_version.DTOs;
using monolith_version.Models.Enums;
using monolith_version.Services;
using monolith_version.Services.ProductServices;
using monolith_version.Services.UserServices;
using System.Security.Claims;

namespace monolith_version.Controllers.ProductsController
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : BaseController
    {
        private readonly IProductService _service;
        private readonly IUserService _userService;

        public ProductController(IProductService service, IUserService userService)
        {
            _service = service;
            _userService = userService;
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
            productDto.Status = Models.Enums.ModerationStatus.Pending; // Устанавливаем статус на PENDING по умолчанию

            var product = await _service.CreateProduct(productDto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        // Обновление товара
        [HttpPut("{id}")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto productDto)
        {
            var userId = GetUserId(); // Получаем ID пользователя из токена
            if (userId == null) return Unauthorized();

            // Загружаем данные пользователя
            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return Unauthorized();

            // Загружаем продукт
            var existingProduct = await _service.GetProductById(id);
            if (existingProduct == null) return NotFound();

            // Проверяем: владелец продукта или администратор
            if (existingProduct.IdUser != (int)userId && user.Role != Role.MODERATOR)
                return Forbid(); // 403 Forbidden

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

            // Загружаем данные пользователя
            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return Unauthorized();

            // Загружаем продукт
            var existingProduct = await _service.GetProductById(id);
            if (existingProduct == null) return NotFound();

            // Проверяем: владелец продукта или модератор
            if (existingProduct.IdUser != (int)userId && user.Role != Role.MODERATOR)
                return Forbid(); // 403 Forbidden

            await _service.DeleteProduct(id);
            return NoContent();
        }
    }
}