using Microsoft.AspNetCore.Mvc;

namespace monolith_version.DTOs.Auth
{
    public class UploadAvatarRequest
    {
        [FromForm(Name = "avatar")]
        public IFormFile Avatar { get; set; }
    }
}
