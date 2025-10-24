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
        public async Task<IActionResult> Create([FromBody] ProductDto productDto)
        {
            var product = await _service.CreateProduct(productDto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        // Обновление товара
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto productDto)
        {
            var updated = await _service.UpdateProduct(id, productDto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // Удаление товара
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteProduct(id);
            return NoContent();
        }
    }
}