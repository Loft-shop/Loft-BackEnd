using Microsoft.AspNetCore.Mvc;

namespace ShippingAddressService.Controllers
{
    [ApiController]
    [Route("api/shipping-addresses")]
    public class ShippingAddressesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetShippingAddresses()
        {
            return Ok(new[] { "Address1", "Address2" });
        }
    }
}