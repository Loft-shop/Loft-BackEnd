using Microsoft.AspNetCore.Mvc;
using PaymentService.Services;
using Loft.Common.DTOs;
using Loft.Common.Enums;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }
        
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var p = await _paymentService.GetPaymentById(id);
            if (p == null) return NotFound();
            return Ok(p);
        }

        [HttpGet("by-order/{orderId:long}")]
        public async Task<IActionResult> GetByOrder(long orderId)
        {
            var p = await _paymentService.GetPaymentByOrderId(orderId);
            if (p == null) return NotFound();
            return Ok(p);
        }

        [HttpPost("process")]
        public async Task<IActionResult> Process([FromQuery] long orderId, [FromQuery] PaymentMethod method)
        {
            var result = await _paymentService.ProcessPayment(orderId, method);
            return Ok(result);
        }

        [HttpPost("refund/{id:long}")]
        public async Task<IActionResult> Refund(long id)
        {
            var result = await _paymentService.RefundPayment(id);
            return Ok(result);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] PaymentDTO payment)
        {
            var created = await _paymentService.CreatePayment(payment);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("methods")]
        public IActionResult GetMethods()
        {
            // Возвращаем все методы оплаты, определённые в Loft.Common.Enums.PaymentMethod
            var methods = Enum.GetNames(typeof(PaymentMethod));
            return Ok(methods);
        }
    }
}