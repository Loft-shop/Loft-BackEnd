using Loft.Common.Enums;

namespace PaymentService.Services.Providers;

/// <summary>
/// Базовый интерфейс для платежных провайдеров
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Поддерживаемый метод оплаты
    /// </summary>
    PaymentMethod SupportedMethod { get; }
    
    /// <summary>
    /// Создать платеж и получить ID транзакции
    /// </summary>
    Task<string> CreatePaymentAsync(decimal amount, long orderId);
    
    /// <summary>
    /// Подтвердить платеж
    /// </summary>
    Task<bool> ConfirmPaymentAsync(string transactionId);
    
    /// <summary>
    /// Вернуть платеж
    /// </summary>
    Task<bool> RefundPaymentAsync(string transactionId);
}

