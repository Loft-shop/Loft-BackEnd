using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using monolith_version.DTOs;
using monolith_version.Models.Enums;
using monolith_version.Services;
using monolith_version.Services.ProductServices;
using monolith_version.Services.UserServices;

namespace monolith_version.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : BaseController
    {
        private readonly IProductService _service;
        private readonly IUserService _userService;

        public CategoryController(IProductService service, IUserService userService)
        {
            _service = service;
            _userService = userService;
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
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Create([FromBody] CategoryDto categoryDto)
        {
            var userId = GetUserId(); // Получаем ID пользователя из токена
            if (userId == null) return Unauthorized();

            // Получаем пользователя
            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return Unauthorized();

            // Проверяем роль: только модератор
            if (user.Role != Role.MODERATOR)
                return Forbid(); // 403 Forbidden

            var category = await _service.CreateCategory(categoryDto);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        // Обновление категории
        [HttpPut("{id}")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> Update(int id, [FromBody] CategoryDto categoryDto)
        {
            var userId = GetUserId(); // Получаем ID пользователя из токена
            if (userId == null) return Unauthorized();

            // Получаем пользователя
            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return Unauthorized();

            // Проверяем роль: только модератор
            if (user.Role != Role.MODERATOR)
                return Forbid(); // 403 Forbidden

            var updated = await _service.UpdateCategory(id, categoryDto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // Удаление категории
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

            await _service.DeleteCategory(id);
            return NoContent();
        }

        // Привязка атрибута к категории
        [HttpPost("{id}/attributes")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> AssignAttribute(int id, [FromQuery] int attributeId, [FromQuery] bool isRequired, [FromQuery] int orderIndex)
        {
            var userId = GetUserId(); // Получаем ID пользователя из токена
            if (userId == null) return Unauthorized();

            // Получаем пользователя
            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return Unauthorized();

            // Проверяем роль: только модератор
            if (user.Role != Role.MODERATOR)
                return Forbid(); // 403 Forbidden

            var categoryAttribute = await _service.AssignAttributeToCategory(id, attributeId, isRequired, orderIndex);
            return Ok(categoryAttribute);
        }

        // Удаление привязки атрибута
        [HttpDelete("{id}/attributes/{attributeId}")]
        [Authorize] // <-- Требуем аутентификацию
        public async Task<IActionResult> RemoveAttribute(int id, int attributeId)
        {
            var userId = GetUserId(); // Получаем ID пользователя из токена
            if (userId == null) return Unauthorized();

            // Получаем пользователя
            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return Unauthorized();

            // Проверяем роль: только модератор
            if (user.Role != Role.MODERATOR)
                return Forbid(); // 403 Forbidden

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
    }
}