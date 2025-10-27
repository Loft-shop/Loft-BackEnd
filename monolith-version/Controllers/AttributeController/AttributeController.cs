using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using monolith_version.DTOs;
using monolith_version.Models.Entities;
using monolith_version.Models.Enums;
using monolith_version.Services;
using monolith_version.Services.ProductServices;
using monolith_version.Services.UserServices;

namespace monolith_version.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttributeController : BaseController
    {
        private readonly IProductService _service;
        private readonly IUserService _userService;

        public AttributeController(IProductService service, IUserService userService)
        {
            _service = service;
            _userService = userService;
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
            var userId = GetUserId(); // Получаем ID пользователя из токена
            if (userId == null) return Unauthorized();

            // Получаем пользователя
            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return Unauthorized();

            // Проверяем роль: только модератор
            if (user.Role != Role.MODERATOR)
                return Forbid(); // 403 Forbidden

            var attribute = await _service.CreateAttribute(attributeDto);
            return CreatedAtAction(nameof(GetById), new { id = attribute.Id }, attribute);
        }

        // Обновление атрибута
        [HttpPut("{id}")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Update(int id, [FromBody] AttributeDto attributeDto)
        {
            var userId = GetUserId(); // Получаем ID пользователя из токена
            if (userId == null) return Unauthorized();

            // Получаем пользователя
            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return Unauthorized();

            // Проверяем роль: только модератор
            if (user.Role != Role.MODERATOR)
                return Forbid(); // 403 Forbidden

            var updated = await _service.UpdateAttribute(id, attributeDto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // Удаление атрибута
        [HttpDelete("{id}")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId(); // Получаем ID пользователя из токена
            if (userId == null) return Unauthorized();

            // Получаем пользователя
            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return Unauthorized();

            // Проверяем роль: только модератор
            if (user.Role != Role.MODERATOR)
                return Forbid(); // 403 Forbidden

            await _service.DeleteAttribute(id);
            return NoContent();
        }
    }
}
