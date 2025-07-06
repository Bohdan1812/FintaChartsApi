using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsApi
{
    namespace FintachartsApiExplorer.Models.FintachartsApi
    {
        public record AuthTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            // Термін дії токена в секундах.
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            // Термін дії токена оновлення в секундах.

            [JsonPropertyName("refresh_expires_in")]
            public int RefreshExpiresIn { get; set; }

            // Токен для оновлення Access Token без повторної автентифікації за обліковими даними.
            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }

            // Тип токена (зазвичай "Bearer").
            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            // Поле "not-before-policy"
            // Використовуємо JsonPropertyName, бо в C# назва властивості не може містити дефіс.
            // Це вказує, що токен не є дійсним до певного моменту (якщо 0, то дійсний відразу).
            [JsonPropertyName("not-before-policy")]
            public int NotBeforePolicy { get; set; }

            // Стан сесії, унікальний ідентифікатор сесії.
            [JsonPropertyName("session_state")]
            public string? SessionState { get; set; }

            // Область дії токена (наприклад, "profile", "email", "openid").
            [JsonPropertyName("scope")]
            public string? Scope { get; set; }
        }
    }
}
