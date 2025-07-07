using System.Net.Http.Headers;

namespace FintaChartsApi.Services.Authorization
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly ILogger<AuthTokenHandler> _logger;
        private readonly ITokenProvider _tokenManager;

        public AuthTokenHandler(ITokenProvider tokenManager, ILogger<AuthTokenHandler> logger)
        {
            _tokenManager = tokenManager;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            
            if (request.RequestUri?.AbsolutePath.Contains("openid-connect/token") == true)
            {
                // Якщо це запит на токен, пропускаємо його без додавання заголовка авторизації.
                // Він буде оброблений базовим HttpClient.
                try 
                {                 
                    return await base.SendAsync(request, cancellationToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError("Помилка при отриманні токена! {ex}", ex.Message);
                    
                }
            }

            var accessToken = await _tokenManager.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
