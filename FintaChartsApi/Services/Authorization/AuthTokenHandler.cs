using System.Net.Http.Headers;

namespace FintaChartsApi.Services.Authorization
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly AuthTokenManager _tokenManager;

        public AuthTokenHandler(AuthTokenManager tokenManager)
        {
            _tokenManager = tokenManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // !!! ВАЖЛИВА ЛОГІКА: Якщо це запит на отримання токена, не додаємо заголовок Authorization !!!
            if (request.RequestUri?.AbsolutePath.Contains("openid-connect/token") == true)
            {
                // Якщо це запит на токен, пропускаємо його без додавання заголовка авторизації.
                // Він буде оброблений базовим HttpClient.
                return await base.SendAsync(request, cancellationToken);
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
