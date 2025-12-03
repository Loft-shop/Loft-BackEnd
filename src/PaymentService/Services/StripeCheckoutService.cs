using Stripe.Checkout;
using Stripe;

namespace PaymentService.Services;

/// <summary>
/// Сервис для работы со Stripe Checkout Sessions
/// </summary>
public interface IStripeCheckoutService
{
    /// <summary>
    /// Создать Checkout Session для оплаты заказа
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(long orderId, decimal amount, string successUrl, string cancelUrl);
    
    /// <summary>
    /// Получить информацию о Checkout Session
    /// </summary>
    Task<Session> GetCheckoutSessionAsync(string sessionId);
    
    /// <summary>
    /// Обработать вебхук от Stripe
    /// </summary>
    Task<bool> HandleWebhookAsync(string payload, string signature);
}

public class StripeCheckoutService : IStripeCheckoutService
{
    private readonly ILogger<StripeCheckoutService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SessionService _sessionService;

    public StripeCheckoutService(
        IConfiguration configuration,
        ILogger<StripeCheckoutService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var apiKey = configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Stripe:SecretKey is not configured");
        }

        StripeConfiguration.ApiKey = apiKey;
        _sessionService = new SessionService();
        
        _logger.LogInformation("[STRIPE CHECKOUT] Initialized");
    }

    public async Task<string> CreateCheckoutSessionAsync(long orderId, decimal amount, string successUrl, string cancelUrl)
    {
        try
        {
            var amountInCents = (long)(amount * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = amountInCents,
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order #{orderId}",
                                Description = $"Payment for order #{orderId}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", orderId.ToString() },
                    { "integration", "loft_payment_service" }
                },
                // Автоматическая отмена неоплаченных сессий через 24 часа
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            var session = await _sessionService.CreateAsync(options);

            _logger.LogInformation(
                "[STRIPE CHECKOUT] Created session {SessionId} for order {OrderId}, amount ${Amount}",
                session.Id, orderId, amount);

            return session.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[STRIPE CHECKOUT] Error creating session for order {OrderId}", orderId);
            throw new InvalidOperationException($"Stripe Checkout error: {ex.Message}", ex);
        }
    }

    public async Task<Session> GetCheckoutSessionAsync(string sessionId)
    {
        try
        {
            var session = await _sessionService.GetAsync(sessionId);
            
            _logger.LogInformation(
                "[STRIPE CHECKOUT] Retrieved session {SessionId}, status: {Status}",
                sessionId, session.PaymentStatus);

            return session;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[STRIPE CHECKOUT] Error retrieving session {SessionId}", sessionId);
            throw new InvalidOperationException($"Stripe Checkout error: {ex.Message}", ex);
        }
    }

    public async Task<bool> HandleWebhookAsync(string payload, string signature)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogWarning("[STRIPE CHECKOUT] Webhook secret not configured, skipping signature verification");
                return false;
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "[STRIPE CHECKOUT] Webhook signature verification failed");
                return false;
            }

            _logger.LogInformation("[STRIPE CHECKOUT] Webhook received: {EventType}", stripeEvent.Type);

            // Обрабатываем событие успешной оплаты
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null)
                {
                    await HandleCheckoutSessionCompletedAsync(session);
                }
            }
            else if (stripeEvent.Type == "checkout.session.expired")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null)
                {
                    HandleCheckoutSessionExpired(session);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STRIPE CHECKOUT] Error handling webhook");
            return false;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Session session)
    {
        try
        {
            var orderIdStr = session.Metadata.GetValueOrDefault("order_id");
            if (string.IsNullOrEmpty(orderIdStr) || !long.TryParse(orderIdStr, out var orderId))
            {
                _logger.LogWarning("[STRIPE CHECKOUT] Session {SessionId} has no valid order_id in metadata", session.Id);
                return;
            }

            _logger.LogInformation(
                "[STRIPE CHECKOUT] Session {SessionId} completed for order {OrderId}, payment status: {PaymentStatus}",
                session.Id, orderId, session.PaymentStatus);

            // Здесь можно обновить статус платежа в базе данных
            // Например, найти платеж по orderId и обновить его статус на COMPLETED
            
            if (session.PaymentStatus == "paid")
            {
                _logger.LogInformation(
                    "[STRIPE CHECKOUT] Payment confirmed for order {OrderId}, transaction: {PaymentIntent}",
                    orderId, session.PaymentIntentId);
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STRIPE CHECKOUT] Error handling completed session {SessionId}", session.Id);
        }
    }

    private void HandleCheckoutSessionExpired(Session session)
    {
        var orderIdStr = session.Metadata.GetValueOrDefault("order_id");
        if (string.IsNullOrEmpty(orderIdStr) || !long.TryParse(orderIdStr, out var orderId))
        {
            return;
        }

        _logger.LogInformation(
            "[STRIPE CHECKOUT] Session {SessionId} expired for order {OrderId}",
            session.Id, orderId);

        // Здесь можно обновить статус платежа на CANCELLED или удалить неоплаченный платеж
    }
}

