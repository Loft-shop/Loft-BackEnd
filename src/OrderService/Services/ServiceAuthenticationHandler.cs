using System.Net.Http.Headers;

namespace OrderService.Services;

/// <summary>
/// DelegatingHandler для добавления Service-to-Service JWT токена во внутренние запросы
/// </summary>
public class ServiceAuthenticationHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceAuthenticationHandler> _logger;

    public ServiceAuthenticationHandler(IConfiguration configuration, ILogger<ServiceAuthenticationHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Получаем service token из конфигурации
        var serviceToken = _configuration["ServiceAuthentication:Token"];
        
        if (!string.IsNullOrEmpty(serviceToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
            _logger.LogDebug($"Added service token to request: {request.RequestUri}");
        }
        else
        {
            _logger.LogWarning("ServiceAuthentication:Token not configured - requests may fail with 401");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

