using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using monolith_version.Services;
using monolith_version.Services.UserServices;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/seller/products")]
    [Authorize]
    public class SellerProductsController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SellerProductsController> _logger;

        public SellerProductsController(
            IUserService userService, 
            IHttpClientFactory httpClientFactory,
            ILogger<SellerProductsController> logger)
        {
            _userService = userService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GET api/seller/products - Получить все товары продавца
        [HttpGet]
        public async Task<IActionResult> GetMyProducts()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            // Проверяем, может ли пользователь продавать
            var canSell = await _userService.CanUserSell(userId.Value);
            if (!canSell)
            {
                return Forbid("You need to enable seller mode first");
            }

            try
            {
                // Запрашиваем товары из ProductService
                var client = _httpClientFactory.CreateClient();
                var filterRequest = new
                {
                    SellerId = (int)userId.Value,
                    Page = 1,
                    PageSize = 100
                };

                var json = JsonSerializer.Serialize(filterRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync("http://productservice:8080/api/products/filter", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var products = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    return Ok(products);
                }

                _logger.LogWarning($"Failed to get products from ProductService: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, "Failed to retrieve products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving seller products");
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }

        // POST api/seller/products - Создать новый товар
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] JsonElement productData)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            // Проверяем, может ли пользователь продавать
            var canSell = await _userService.CanUserSell(userId.Value);
            if (!canSell)
            {
                return Forbid("You need to enable seller mode first");
            }

            try
            {
                // Добавляем userId в запрос
                var productJson = productData.GetRawText();
                var productDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(productJson);
                
                if (productDict == null)
                {
                    return BadRequest("Invalid product data");
                }

                productDict["IdUser"] = JsonSerializer.SerializeToElement((int)userId.Value);
                productDict["Status"] = JsonSerializer.SerializeToElement(0); // Pending

                var updatedJson = JsonSerializer.Serialize(productDict);
                var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");

                // Отправляем запрос в ProductService
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync("http://productservice:8080/api/products", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var product = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    _logger.LogInformation($"Product created by seller {userId}");
                    return CreatedAtAction(nameof(GetProductById), new { id = product.GetProperty("id").GetInt32() }, product);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to create product: {response.StatusCode} - {errorContent}");
                return StatusCode((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, "An error occurred while creating the product");
            }
        }

        // GET api/seller/products/{id} - Получить товар по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var canSell = await _userService.CanUserSell(userId.Value);
            if (!canSell)
            {
                return Forbid("You need to enable seller mode first");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"http://productservice:8080/api/products/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var product = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    // Проверяем, что товар принадлежит текущему пользователю
                    if (product.TryGetProperty("idUser", out var idUser))
                    {
                        if (idUser.GetInt32() != (int)userId.Value)
                        {
                            return Forbid("You can only view your own products");
                        }
                    }

                    return Ok(product);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound("Product not found");
                }

                return StatusCode((int)response.StatusCode, "Failed to retrieve product");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving product {id}");
                return StatusCode(500, "An error occurred while retrieving the product");
            }
        }

        // PUT api/seller/products/{id} - Обновить товар
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] JsonElement productData)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var canSell = await _userService.CanUserSell(userId.Value);
            if (!canSell)
            {
                return Forbid("You need to enable seller mode first");
            }

            try
            {
                // Сначала проверяем, что товар принадлежит текущему пользователю
                var client = _httpClientFactory.CreateClient();
                var getResponse = await client.GetAsync($"http://productservice:8080/api/products/{id}");

                if (!getResponse.IsSuccessStatusCode)
                {
                    if (getResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return NotFound("Product not found");
                    }
                    return StatusCode((int)getResponse.StatusCode, "Failed to retrieve product");
                }

                var existingProductContent = await getResponse.Content.ReadAsStringAsync();
                var existingProduct = JsonSerializer.Deserialize<JsonElement>(existingProductContent);

                if (existingProduct.TryGetProperty("idUser", out var idUser))
                {
                    if (idUser.GetInt32() != (int)userId.Value)
                    {
                        return Forbid("You can only update your own products");
                    }
                }

                // Обновляем товар
                var productJson = productData.GetRawText();
                var productDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(productJson);
                
                if (productDict == null)
                {
                    return BadRequest("Invalid product data");
                }

                // Убеждаемся, что IdUser не изменился
                productDict["IdUser"] = JsonSerializer.SerializeToElement((int)userId.Value);

                var updatedJson = JsonSerializer.Serialize(productDict);
                var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"http://productservice:8080/api/products/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var product = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    _logger.LogInformation($"Product {id} updated by seller {userId}");
                    return Ok(product);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to update product {id}: {response.StatusCode} - {errorContent}");
                return StatusCode((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product {id}");
                return StatusCode(500, "An error occurred while updating the product");
            }
        }

        // DELETE api/seller/products/{id} - Удалить товар
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var canSell = await _userService.CanUserSell(userId.Value);
            if (!canSell)
            {
                return Forbid("You need to enable seller mode first");
            }

            try
            {
                // Сначала проверяем, что товар принадлежит текущему пользователю
                var client = _httpClientFactory.CreateClient();
                var getResponse = await client.GetAsync($"http://productservice:8080/api/products/{id}");

                if (!getResponse.IsSuccessStatusCode)
                {
                    if (getResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return NotFound("Product not found");
                    }
                    return StatusCode((int)getResponse.StatusCode, "Failed to retrieve product");
                }

                var existingProductContent = await getResponse.Content.ReadAsStringAsync();
                var existingProduct = JsonSerializer.Deserialize<JsonElement>(existingProductContent);

                if (existingProduct.TryGetProperty("idUser", out var idUser))
                {
                    if (idUser.GetInt32() != (int)userId.Value)
                    {
                        return Forbid("You can only delete your own products");
                    }
                }

                // Удаляем товар
                var response = await client.DeleteAsync($"http://productservice:8080/api/products/{id}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product {id} deleted by seller {userId}");
                    return NoContent();
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to delete product {id}: {response.StatusCode} - {errorContent}");
                return StatusCode((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product {id}");
                return StatusCode(500, "An error occurred while deleting the product");
            }
        }

        private long? GetUserIdFromClaims()
        {
            var idClaims = User.FindAll(ClaimTypes.NameIdentifier).ToList();
            
            foreach (var claim in idClaims)
            {
                if (long.TryParse(claim.Value, out var id))
                {
                    return id;
                }
            }
            
            var altIdClaim = User.FindFirst("nameid")?.Value;
            if (!string.IsNullOrEmpty(altIdClaim) && long.TryParse(altIdClaim, out var altId))
            {
                return altId;
            }
            
            return null;
        }
    }
}