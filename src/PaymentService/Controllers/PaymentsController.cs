using Microsoft.AspNetCore.Mvc;
using PaymentService.Services;
using Loft.Common.DTOs;
using Loft.Common.Enums;
//
namespace PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Создать новый платеж
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDTO dto)
    {
        try
        {
            var payment = await _paymentService.CreatePaymentAsync(dto);
            return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, payment);
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Подтвердить платеж
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmPayment(long id)
    {
        try
        {
            var payment = await _paymentService.ConfirmPaymentAsync(id);
            return Ok(payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить платеж по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPaymentById(long id)
    {
        try
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            return Ok(payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить все платежи по заказу
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetPaymentsByOrder(long orderId)
    {
        try
        {
            var payments = await _paymentService.GetPaymentsByOrderIdAsync(orderId);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Сделать возврат платежа
    /// </summary>
    [HttpPost("{id}/refund")]
    public async Task<IActionResult> RefundPayment(long id)
    {
        try
        {
            var payment = await _paymentService.RefundPaymentAsync(id);
            return Ok(payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить доступные методы оплаты
    /// </summary>
    [HttpGet("methods")]
    public IActionResult GetPaymentMethods()
    {
        var methods = Enum.GetValues<PaymentMethod>()
            .Select(m => new { value = (int)m, name = m.ToString() });
        return Ok(methods);
    }
}