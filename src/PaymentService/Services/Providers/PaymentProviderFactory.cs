using Loft.Common.Enums;

namespace PaymentService.Services.Providers;

/// <summary>
/// Фабрика для получения платежного провайдера по методу оплаты
/// </summary>
public class PaymentProviderFactory
{
    private readonly Dictionary<PaymentMethod, IPaymentProvider> _providers;
    private readonly ILogger<PaymentProviderFactory> _logger;

    public PaymentProviderFactory(
        IEnumerable<IPaymentProvider> providers,
        ILogger<PaymentProviderFactory> logger)
    {
        _logger = logger;
        _providers = providers.ToDictionary(p => p.SupportedMethod);
        
        _logger.LogInformation("Registered payment providers: {Methods}", 
            string.Join(", ", _providers.Keys));
    }

    public IPaymentProvider GetProvider(PaymentMethod method)
    {
        if (!_providers.TryGetValue(method, out var provider))
        {
            throw new NotSupportedException($"Payment method {method} is not supported");
        }
        return provider;
    }
}

