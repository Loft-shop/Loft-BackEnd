using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Services;
using System.Security.Claims;
using UserService.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Loft.Common.DTOs; // добавлено для использования UserDTO

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;

        public UsersController(IUserService userService, IWebHostEnvironment env)
        {
            _userService = userService;
            _env = env;
        }

        // GET api/users/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                // Логируем доступные claims для отладки
                var claims = string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
                Console.WriteLine($"[UserService] Available claims: {claims}");
                Console.WriteLine($"[UserService] NameIdentifier claim not found!");
                return Unauthorized();
            }

            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // PUT api/users/me
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var existing = await _userService.GetUserById(userId.Value);
            if (existing == null) return NotFound();

            // create new DTO using 'with' to avoid modifying init-only properties
            var toUpdate = existing with
            {
                FirstName = request.FirstName ?? existing.FirstName,
                LastName = request.LastName ?? existing.LastName,
                Phone = request.Phone ?? existing.Phone
            };

            var updated = await _userService.UpdateUser(userId.Value, toUpdate);
            return Ok(updated);
        }

        // POST api/users/me/avatar
        [HttpPost("me/avatar")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0) return BadRequest(new { message = "No file uploaded" });
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var existing = await _userService.GetUserById(userId.Value);
            if (existing == null) return NotFound();

            // validate content type (should be image) and size (<=5MB)
            const long maxBytes = 5 * 1024 * 1024; // 5 MB
            if (!string.IsNullOrEmpty(avatar.ContentType) && !avatar.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only image files are allowed" });
            }
            if (avatar.Length > maxBytes)
            {
                return BadRequest(new { message = "File is too large. Max 5 MB allowed" });
            }

            // ensure wwwroot/avatars exists
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var avatarsDir = Path.Combine(webRoot, "avatars");
            if (!Directory.Exists(avatarsDir)) Directory.CreateDirectory(avatarsDir);

            var fileExt = Path.GetExtension(avatar.FileName);
            var fileName = $"avatar_{userId.Value}_{Guid.NewGuid()}{fileExt}";
            var filePath = Path.Combine(avatarsDir, fileName);

            try
            {
                using (var stream = System.IO.File.Create(filePath))
                {
                    await avatar.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                // log if logger available; return 500 to frontend
                return StatusCode(500, new { message = "Failed to save file", detail = ex.Message });
            }

            // delete old avatar file if exists and is local
            try
            {
                if (!string.IsNullOrEmpty(existing.AvatarUrl))
                {
                    // AvatarUrl is stored as relative path like "/avatars/xyz.png"
                    var relative = existing.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var oldPath = Path.Combine(webRoot, relative);
                    if (System.IO.File.Exists(oldPath))
                    {
                        try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
                    }
                }
            }
            catch { /* ignore */ }

            // use relative path for AvatarUrl
            var relativeUrl = $"/avatars/{fileName}";

            var toUpdate = existing with { AvatarUrl = relativeUrl };
            var updated = await _userService.UpdateUser(userId.Value, toUpdate);

            return Ok(new { avatarUrl = relativeUrl, user = updated });
        }

        // DELETE api/users/me
        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            await _userService.DeleteUser(userId.Value);
            return NoContent();
        }

        // POST api/users/me/toggle-seller
        [HttpPost("me/toggle-seller")]
        [Authorize]
        public async Task<IActionResult> ToggleSellerStatus()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var result = await _userService.ToggleSellerStatus(userId.Value);
            if (result == null) return NotFound();

            return Ok(new { 
                message = result.CanSell ? "You can now sell products" : "Seller status disabled",
                canSell = result.CanSell,
                user = result
            });
        }

        // GET api/users/{id}/can-sell
        [HttpGet("{id}/can-sell")]
        public async Task<IActionResult> CanUserSell(long id)
        {
            var canSell = await _userService.CanUserSell(id);
            return Ok(new { userId = id, canSell });
        }

        // GET api/users/me/seller-stats
        [HttpGet("me/seller-stats")]
        [Authorize]
        public async Task<IActionResult> GetMySellerStats()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return NotFound();

            if (!user.CanSell)
            {
                return Ok(new
                {
                    canSell = false,
                    message = "User is not a seller"
                });
            }

            try
            {
                // Запрашиваем статистику товаров из ProductService
                var client = new HttpClient();
                var filterRequest = new
                {
                    SellerId = (int)userId.Value,
                    Page = 1,
                    PageSize = 1000
                };

                var json = System.Text.Json.JsonSerializer.Serialize(filterRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://productservice:8080/api/products/filter", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var products = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);

                    var totalProducts = 0;
                    var activeProducts = 0;
                    var totalViews = 0;
                    var totalRevenue = 0m;

                    if (products.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        totalProducts = products.GetArrayLength();

                        foreach (var product in products.EnumerateArray())
                        {
                            if (product.TryGetProperty("quantity", out var quantity) && quantity.GetInt32() > 0)
                            {
                                activeProducts++;
                            }

                            if (product.TryGetProperty("viewCount", out var viewCount))
                            {
                                totalViews += viewCount.GetInt32();
                            }

                            if (product.TryGetProperty("price", out var price))
                            {
                                totalRevenue += price.GetDecimal();
                            }
                        }
                    }

                    return Ok(new
                    {
                        canSell = true,
                        totalProducts,
                        activeProducts,
                        totalViews,
                        averagePrice = totalProducts > 0 ? totalRevenue / totalProducts : 0m
                    });
                }

                return Ok(new
                {
                    canSell = true,
                    totalProducts = 0,
                    activeProducts = 0,
                    totalViews = 0,
                    averagePrice = 0m
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserService] Error getting seller stats: {ex.Message}");
                return Ok(new
                {
                    canSell = true,
                    totalProducts = 0,
                    activeProducts = 0,
                    totalViews = 0,
                    averagePrice = 0m,
                    error = "Failed to load statistics"
                });
            }
        }

        // Публичный эндпойнт: получить пользователя по ID (для межсервисных запросов)
        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        private long? GetUserIdFromClaims()
        {
            // ASP.NET Core маппит 'sub' на ClaimTypes.NameIdentifier, поэтому может быть два таких claim
            // Нам нужен тот, который содержит числовой ID (а не email)
            var idClaims = User.FindAll(ClaimTypes.NameIdentifier).ToList();
            
            foreach (var claim in idClaims)
            {
                if (long.TryParse(claim.Value, out var id))
                {
                    return id; // Нашли числовой ID
                }
            }
            
            // Если не нашли среди NameIdentifier, пробуем другие варианты
            var altIdClaim = User.FindFirst("nameid")?.Value;
            if (!string.IsNullOrEmpty(altIdClaim) && long.TryParse(altIdClaim, out var altId))
            {
                return altId;
            }
            
            Console.WriteLine($"[UserService] Failed to find user ID claim. Available claims: {string.Join(", ", User.Claims.Select(c => c.Type + "=" + c.Value))}");
            return null;
        }
    }
}