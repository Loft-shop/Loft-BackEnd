using Microsoft.AspNetCore.Mvc;
//test12355
namespace UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetUsers()
        {
            return Ok(new[] { "User1", "User2", "User3" });
        }
    }
}