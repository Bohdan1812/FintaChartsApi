using FintaChartsApi.Clients;
using FintaChartsApi.Models.FintaChartsApi.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace FintaChartsApi.Services.Authorization
{
    public class AuthTokenManager
    {

        private readonly Lazy<IFintaChartsApi> _lazyFintaChartsApi;
        private readonly IConfiguration _configuration;
        private string? _cachedAccessToken;
        private DateTime _tokenExpiryTime;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1); // Для запобігання race conditions

        public AuthTokenManager(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _lazyFintaChartsApi = new Lazy<IFintaChartsApi>(() =>
                serviceProvider.GetRequiredService<IFintaChartsApi>());

            _configuration = configuration;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            await _lock.WaitAsync();
            try
            {
                // Отримання username, password, realm з конфігурації
                var username = _configuration["Fintacharts:Username"] ?? throw new InvalidOperationException("Fintacharts:Username not configured.");
                var password = _configuration["Fintacharts:Password"] ?? throw new InvalidOperationException("Fintacharts:Password not configured.");
                var realm = _configuration["Fintacharts:Realm"] ?? throw new InvalidOperationException("Fintacharts:Realm not configured.");

                var request = new AuthTokenRequest
                {
                    Username = username,
                    Password = password
                };

                
                var response = await _lazyFintaChartsApi.Value.GetAuthToken(realm, request); // Передаємо realm та request
                _cachedAccessToken = response.AccessToken;
                _tokenExpiryTime = DateTime.UtcNow.AddSeconds(response.ExpiresIn);

                return _cachedAccessToken;
            }
            catch (Refit.ApiException ex) // Спіймаємо помилки, що надходять від Refit (HTTP-помилки)
            {
                Debug.WriteLine("--- AuthTokenManager Error Details (Refit.ApiException) ---");
                Debug.WriteLine($"Error Status Code: {ex.StatusCode}");
                Debug.WriteLine($"Error Message: {ex.Message}");
                Debug.WriteLine($"Error Response Content: {ex.Content}"); // Тіло відповіді, де API Fintacharts пояснить помилку
                Debug.WriteLine("--------------------------------------------------");
                throw; // Важливо: перевикиньте виняток, щоб проблема поширилась далі по стеку
            }
            catch (Exception ex) // Спіймаємо будь-які інші непередбачені помилки
            {
                Debug.WriteLine("--- AuthTokenManager Error Details (General Exception) ---");
                Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                Debug.WriteLine("--------------------------------------------------");
                throw; // Важливо: перевикиньте виняток
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
