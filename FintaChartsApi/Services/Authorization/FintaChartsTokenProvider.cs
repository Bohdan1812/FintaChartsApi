using FintaChartsApi.Clients;
using FintaChartsApi.Models.FintaChartsApi.Auth;
using FintaChartsApi.Models.FintaChartsApi.FintachartsApiExplorer.Models.FintachartsApi;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace FintaChartsApi.Services.Authorization
{
    public class FintaChartsTokenProvider : ITokenProvider
    {

        private readonly Lazy<IFintaChartsApi> _lazyFintaChartsApi;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FintaChartsTokenProvider> _logger;
        private string? _cachedAccessToken;
        private DateTimeOffset _tokenExpiryTime;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1); // Для запобігання race conditions

        public FintaChartsTokenProvider(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<FintaChartsTokenProvider> logger)
        {
            _lazyFintaChartsApi = new Lazy<IFintaChartsApi>(() =>
                serviceProvider.GetRequiredService<IFintaChartsApi>());

            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && !string.IsNullOrEmpty(_cachedAccessToken) && _tokenExpiryTime > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                _logger.LogDebug("Using cached access token.");
                return _cachedAccessToken;
            }

            await _lock.WaitAsync(); // Використовуємо асинхронний wait для уникнення блокування потоку
          

            try
            {

                // --- Друга перевірка (всередині локу) на випадок, якщо інший потік вже оновив токен ---
                if (!forceRefresh && !string.IsNullOrEmpty(_cachedAccessToken) && _tokenExpiryTime > DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    _logger.LogDebug("Using cached access token after lock (another thread refreshed it).");
                    return _cachedAccessToken;
                }

                _logger.LogInformation("Refreshing Fintacharts access token...");

                // Отримання username, password, realm з конфігурації
                var username = _configuration["Fintacharts:Username"] ?? throw new InvalidOperationException("Fintacharts:Username not configured.");
                var password = _configuration["Fintacharts:Password"] ?? throw new InvalidOperationException("Fintacharts:Password not configured.");
                var grantType = _configuration["Fintacharts:GrantType"] ?? throw new InvalidOperationException("Fintacharts:grant type not configured."); ; // За замовчуванням "password", якщо не вказано
                var clientId = _configuration["Fintacharts:ClientId"] ?? throw new InvalidOperationException("Fintacharts:client id not configured."); ; // За замовчуванням "app-cli", якщо не вказано

                var realm = _configuration["Fintacharts:Realm"] ?? throw new InvalidOperationException("Fintacharts:Realm not configured.");

                var request = new AuthTokenRequest
                {
                    Username = username,
                    Password = password,
                    GrantType = grantType,
                    ClientId = clientId 
                };


                AuthTokenResponse response;
                try
                {
                    response = await _lazyFintaChartsApi.Value.GetAuthToken(realm, request); // Передаємо realm та request
                }
                catch (Refit.ApiException ex)
                {
                    _logger.LogError(ex, "Refit API error while refreshing token: Status {StatusCode}, Content: {Content}", ex.StatusCode, ex.Content);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred while calling Fintacharts API for token.");
                    throw;
                }

                if (string.IsNullOrEmpty(response.AccessToken))
                {
                    throw new Exception("Failed to get access token: Access token is null or empty in response.");
                }

                _cachedAccessToken = response.AccessToken;
                _tokenExpiryTime = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn);

                _logger.LogInformation("Fintacharts access token refreshed successfully. Expires in {Seconds} seconds.", response.ExpiresIn);
                return _cachedAccessToken;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
