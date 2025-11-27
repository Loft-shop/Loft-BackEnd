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
            _logger.LogInformation($"✅ JWT token added to request: {request.RequestUri}");
        }
        else
        {
            _logger.LogWarning($"❌ ServiceAuthentication:Token NOT FOUND in configuration! Request to {request.RequestUri} will fail with 401");
            
            // Проверяем все ключи конфигурации для диагностики
            var allKeys = _configuration.AsEnumerable().Where(k => k.Key.Contains("ServiceAuthentication")).ToList();
            if (allKeys.Any())
            {
                _logger.LogWarning($"Found keys: {string.Join(", ", allKeys.Select(k => k.Key))}");
            }
            else
            {
                _logger.LogWarning("No ServiceAuthentication keys found in configuration at all!");
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

