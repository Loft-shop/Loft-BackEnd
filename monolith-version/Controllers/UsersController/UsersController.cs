using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using monolith_version.DTOs.Auth;
using monolith_version.Services;
using monolith_version.Services.UserServices;

namespace monolith_version.Controllers.UsersController;

[ApiController]
[Route("api/users")]
public class UsersController : BaseController
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
        if (userId == null) return Unauthorized();

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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarRequest request)
    {
        var avatar = request.Avatar;
        if (avatar == null || avatar.Length == 0) return BadRequest(new { message = "No file uploaded" });

        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var existing = await _userService.GetUserById(userId.Value);
        if (existing == null) return NotFound();

        const long maxBytes = 5 * 1024 * 1024;
        if (!string.IsNullOrEmpty(avatar.ContentType) && !avatar.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only image files are allowed" });
        if (avatar.Length > maxBytes)
            return BadRequest(new { message = "File is too large. Max 5 MB allowed" });

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

        // ”даление старого аватара
        if (!string.IsNullOrEmpty(existing.AvatarUrl))
        {
            var oldPath = Path.Combine(webRoot, existing.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldPath)) try { System.IO.File.Delete(oldPath); } catch { }
        }

        var relativeUrl = $"/avatars/{fileName}";
        var updatedUser = await _userService.UpdateUser(userId.Value, existing with { AvatarUrl = relativeUrl });

        return Ok(new { avatarUrl = relativeUrl, user = updatedUser });
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

    protected long? GetUserIdFromClaims()
    {
        // ѕопробуем несколько общих типов соответственно возможным картам claim-ов:
        var tryTypes = new[]
        {
            ClaimTypes.NameIdentifier, // "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            "nameid",                  // иногда сокращЄнное им€
            JwtRegisteredClaimNames.Sub, // "sub"
            "id",
            "user_id",
            ClaimTypes.Name,
            ClaimTypes.Email
        };

        foreach (var t in tryTypes)
        {
            var claim = User.FindFirst(t)?.Value;
            if (!string.IsNullOrEmpty(claim) && long.TryParse(claim, out var id)) return id;
        }

        // –езервный метод: найти любой числовой claim в токене
        foreach (var c in User.Claims)
        {
            if (!string.IsNullOrEmpty(c.Value) && long.TryParse(c.Value, out var id)) return id;
        }

        return null;
    }
}
