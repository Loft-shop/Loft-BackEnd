using Microsoft.AspNetCore.Mvc;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/stripe")]
public class StripeCheckoutController : ControllerBase
{
    private readonly IStripeCheckoutService _checkoutService;
    private readonly ILogger<StripeCheckoutController> _logger;

    public StripeCheckoutController(
        IStripeCheckoutService checkoutService,
        ILogger<StripeCheckoutController> logger)
    {
        _checkoutService = checkoutService;
        _logger = logger;
    }

    /// <summary>
    /// Создать Stripe Checkout Session для оплаты заказа
    /// </summary>
    /// <param name="request">Данные для создания сессии</param>
    /// <returns>ID сессии и URL для редиректа</returns>
    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            var sessionId = await _checkoutService.CreateCheckoutSessionAsync(
                request.OrderId,
                request.Amount,
                request.SuccessUrl,
                request.CancelUrl
            );

            return Ok(new
            {
                sessionId = sessionId,
                // Publishable key нужен фронтенду для инициализации Stripe.js
                publishableKey = Request.HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()["Stripe:PublishableKey"]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for order {OrderId}", request.OrderId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить информацию о Checkout Session
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        try
        {
            var session = await _checkoutService.GetCheckoutSessionAsync(sessionId);
            
            return Ok(new
            {
                id = session.Id,
                paymentStatus = session.PaymentStatus,
                customerEmail = session.CustomerEmail,
                amountTotal = session.AmountTotal,
                currency = session.Currency,
                paymentIntentId = session.PaymentIntentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", sessionId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Webhook endpoint для обработки событий от Stripe
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            var payload = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            var success = await _checkoutService.HandleWebhookAsync(payload, signature);

            if (success)
            {
                return Ok();
            }
            else
            {
                return BadRequest("Webhook validation failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook");
            return StatusCode(500);
        }
    }
}

/// <summary>
/// Запрос на создание Checkout Session
/// </summary>
public class CreateCheckoutSessionRequest
{
    /// <summary>
    /// ID заказа
    /// </summary>
    public long OrderId { get; set; }
    
    /// <summary>
    /// Сумма платежа
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// URL для редиректа после успешной оплаты
    /// </summary>
    public string SuccessUrl { get; set; } = "";
    
    /// <summary>
    /// URL для редиректа при отмене оплаты
    /// </summary>
    public string CancelUrl { get; set; } = "";
}

