using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/gateway")]
    public class GatewayController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetGatewayStatus()
        {
            return Ok("Gateway is running");
        }
    }
}