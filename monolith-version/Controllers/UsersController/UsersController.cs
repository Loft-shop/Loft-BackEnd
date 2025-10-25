using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using monolith_version.DTOs.Auth;
using monolith_version.Services;
using monolith_version.Services.UserServices;

namespace monolith_version.Controllers.UsersController;

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

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            var claims = string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
            Console.WriteLine($"[UsersController] Available claims: {claims}");
            return Unauthorized();
        }
        var user = await _userService.GetUserById(userId.Value);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var existing = await _userService.GetUserById(userId.Value);
        if (existing == null) return NotFound();

        var toUpdate = existing with
        {
            FirstName = request.FirstName ?? existing.FirstName,
            LastName = request.LastName ?? existing.LastName,
            Phone = request.Phone ?? existing.Phone
        };

        var updated = await _userService.UpdateUser(userId.Value, toUpdate);
        return Ok(updated);
    }

    [HttpPost("me/avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar)
    {
        if (avatar == null || avatar.Length == 0) return BadRequest(new { message = "No file uploaded" });
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var existing = await _userService.GetUserById(userId.Value);
        if (existing == null) return NotFound();

        const long maxBytes = 5 * 1024 * 1024;
        if (!string.IsNullOrEmpty(avatar.ContentType) && !avatar.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only image files are allowed" });
        }
        if (avatar.Length > maxBytes)
        {
            return BadRequest(new { message = "File is too large. Max 5 MB allowed" });
        }

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var avatarsDir = Path.Combine(webRoot, "avatars");
        if (!Directory.Exists(avatarsDir)) Directory.CreateDirectory(avatarsDir);

        var fileExt = Path.GetExtension(avatar.FileName);
        var fileName = $"avatar_{userId.Value}_{Guid.NewGuid()}{fileExt}";
        var filePath = Path.Combine(avatarsDir, fileName);

        try
        {
            await using var stream = System.IO.File.Create(filePath);
            await avatar.CopyToAsync(stream);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to save file", detail = ex.Message });
        }

        try
        {
            if (!string.IsNullOrEmpty(existing.AvatarUrl))
            {
                var relative = existing.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var oldPath = Path.Combine(webRoot, relative);
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
                }
            }
        }
        catch { /* ignore */ }

        var relativeUrl = $"/avatars/{fileName}";
        var toUpdate = existing with { AvatarUrl = relativeUrl };
        var updated = await _userService.UpdateUser(userId.Value, toUpdate);
        return Ok(new { avatarUrl = relativeUrl, user = updated });
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        await _userService.DeleteUser(userId.Value);
        return NoContent();
    }

    [HttpPost("me/toggle-seller")]
    [Authorize]
    public async Task<IActionResult> ToggleSellerStatus()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var result = await _userService.ToggleSellerStatus(userId.Value);
        if (result == null) return NotFound();

        return Ok(new
        {
            message = result.CanSell ? "You can now sell products" : "Seller status disabled",
            canSell = result.CanSell,
            user = result
        });
    }

    [HttpGet("{id}/can-sell")]
    public async Task<IActionResult> CanUserSell(long id)
    {
        var canSell = await _userService.CanUserSell(id);
        return Ok(new { userId = id, canSell });
    }

    private long? GetUserIdFromClaims()
    {
        var idClaims = User.FindAll(ClaimTypes.NameIdentifier).ToList();
        foreach (var claim in idClaims)
        {
            if (long.TryParse(claim.Value, out var id))
                return id;
        }
        var altIdClaim = User.FindFirst("nameid")?.Value;
        if (!string.IsNullOrEmpty(altIdClaim) && long.TryParse(altIdClaim, out var altId))
            return altId;
        return null;
    }
}
