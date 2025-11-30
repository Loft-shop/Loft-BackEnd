using Loft.Common.Enums;
//
namespace PaymentService.Services.Providers;

/// <summary>
/// Mock провайдер для оплаты кредитной картой
/// </summary>
public class MockCreditCardProvider : IPaymentProvider
{
    private readonly ILogger<MockCreditCardProvider> _logger;

    public PaymentMethod SupportedMethod => PaymentMethod.CREDIT_CARD;

    public MockCreditCardProvider(ILogger<MockCreditCardProvider> logger)
    {
        _logger = logger;
    }

    public Task<string> CreatePaymentAsync(decimal amount, long orderId)
    {
        var transactionId = $"card_mock_{Guid.NewGuid():N}";
        _logger.LogInformation("[MOCK CREDIT CARD] Created payment {TransactionId} for order {OrderId}, amount {Amount}", 
            transactionId, orderId, amount);
        return Task.FromResult(transactionId);
    }

    public Task<bool> ConfirmPaymentAsync(string transactionId)
    {
        _logger.LogInformation("[MOCK CREDIT CARD] Confirmed payment {TransactionId}", transactionId);
        return Task.FromResult(true);
    }

    public Task<bool> RefundPaymentAsync(string transactionId)
    {
        _logger.LogInformation("[MOCK CREDIT CARD] Refunded payment {TransactionId}", transactionId);
        return Task.FromResult(true);
    }
}

