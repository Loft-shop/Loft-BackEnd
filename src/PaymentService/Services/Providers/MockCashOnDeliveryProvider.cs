using Loft.Common.Enums;

namespace PaymentService.Services.Providers;

/// <summary>
/// Mock провайдер для оплаты наличными при доставке
/// </summary>
public class MockCashOnDeliveryProvider : IPaymentProvider
{
    private readonly ILogger<MockCashOnDeliveryProvider> _logger;

    public PaymentMethod SupportedMethod => PaymentMethod.CASH_ON_DELIVERY;

    public MockCashOnDeliveryProvider(ILogger<MockCashOnDeliveryProvider> logger)
    {
        _logger = logger;
    }

    public Task<string> CreatePaymentAsync(decimal amount, long orderId)
    {
        var transactionId = $"cash_mock_{Guid.NewGuid():N}";
        _logger.LogInformation("[MOCK CASH ON DELIVERY] Created payment {TransactionId} for order {OrderId}, amount {Amount}", 
            transactionId, orderId, amount);
        return Task.FromResult(transactionId);
    }

    public Task<bool> ConfirmPaymentAsync(string transactionId)
    {
        _logger.LogInformation("[MOCK CASH ON DELIVERY] Confirmed payment {TransactionId}", transactionId);
        return Task.FromResult(true);
    }

    public Task<bool> RefundPaymentAsync(string transactionId)
    {
        _logger.LogInformation("[MOCK CASH ON DELIVERY] Refunded payment {TransactionId}", transactionId);
        return Task.FromResult(true);
    }
}

