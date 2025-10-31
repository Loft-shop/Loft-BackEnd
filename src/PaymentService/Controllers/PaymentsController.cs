using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetPayments()
        {
            return Ok(new[] { "Payment1", "Payment2" });
        }
    }
}