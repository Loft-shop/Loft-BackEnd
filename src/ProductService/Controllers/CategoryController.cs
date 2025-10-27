using System.Threading.Tasks;
using Loft.Common.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Services;

namespace ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IProductService _service;

        public CategoryController(IProductService service)
        {
            _service = service;
        }

        // Получение списка категорий с фильтром по родителю
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _service.GetAllCategories();
            return Ok(categories);
        }

        // Получение категории по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _service.GetCategoryById(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        // Создание категории
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryDto categoryDto)
        {
            var category = await _service.CreateCategory(categoryDto);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        // Обновление категории
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryDto categoryDto)
        {
            var updated = await _service.UpdateCategory(id, categoryDto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // Удаление категории
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteCategory(id);
            return NoContent();
        }

        // Привязка атрибута к категории
        [HttpPost("{id}/attributes")]
        public async Task<IActionResult> AssignAttribute(int id, [FromQuery] int attributeId, [FromQuery] bool isRequired, [FromQuery] int orderIndex)
        {
            var categoryAttribute = await _service.AssignAttributeToCategory(id, attributeId, isRequired, orderIndex);
            return Ok(categoryAttribute);
        }

        // Удаление привязки атрибута
        [HttpDelete("{id}/attributes/{attributeId}")]
        public async Task<IActionResult> RemoveAttribute(int id, int attributeId)
        {
            await _service.RemoveAttributeFromCategory(id, attributeId);
            return NoContent();
        }

        // Получение атрибутов категории
        [HttpGet("{id}/attributes")]
        public async Task<IActionResult> GetAttributes(int id)
        {
            var attributes = await _service.GetCategoryAttributes(id);
            return Ok(attributes);
        }

        // Получение полной информации об атрибутах для категории (включая детали атрибутов)
        [HttpGet("{id}/attributes/full")]
        public async Task<IActionResult> GetAttributesWithDetails(int id)
        {
            var attributesWithDetails = await _service.GetCategoryAttributesWithDetails(id);
            return Ok(attributesWithDetails);
        }
    }
}