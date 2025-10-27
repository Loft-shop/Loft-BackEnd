using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Entities;
using ProductService.Services;

namespace ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttributeController : ControllerBase
    {
        private readonly IProductService _service;

        public AttributeController(IProductService service)
        {
            _service = service;
        }

        // Получение списка атрибутов
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var attributes = await _service.GetAllAttributes();
            return Ok(attributes);
        }

        // Получение атрибута по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var attribute = await _service.GetAttributeById(id);
            if (attribute == null) return NotFound();
            return Ok(attribute);
        }

        // Создание атрибута
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AttributeDto attributeDto)
        {
            var attribute = await _service.CreateAttribute(attributeDto);
            return CreatedAtAction(nameof(GetById), new { id = attribute.Id }, attribute);
        }

        // Обновление атрибута
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AttributeDto attributeDto)
        {
            var updated = await _service.UpdateAttribute(id, attributeDto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // Удаление атрибута
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAttribute(id);
            return NoContent();
        }
    }
}