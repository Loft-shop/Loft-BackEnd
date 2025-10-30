using System.Threading.Tasks;
using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Entities;
using ProductService.Services;

namespace ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttributeController : BaseController
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
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Create([FromBody] AttributeDto attributeDto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (!IsModerator()) return Forbid();

            var attribute = await _service.CreateAttribute(attributeDto);
            return CreatedAtAction(nameof(GetById), new { id = attribute.Id }, attribute);
        }

        // Обновление атрибута
        [HttpPut("{id}")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Update(int id, [FromBody] AttributeDto attributeDto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (!IsModerator()) return Forbid();

            var updated = await _service.UpdateAttribute(id, attributeDto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // Удаление атрибута
        [HttpDelete("{id}")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (!IsModerator()) return Forbid();

            await _service.DeleteAttribute(id);
            return NoContent();
        }
    }
}