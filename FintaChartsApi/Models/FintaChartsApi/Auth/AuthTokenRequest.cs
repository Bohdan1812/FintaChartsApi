using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsApi.Auth
{
    public record AuthTokenRequest
    {
        [JsonPropertyName("grant_type")]
        public string GrantType { get; init; } = "password"; // Змінено на "password"

        [JsonPropertyName("client_id")]
        public string ClientId { get; init; } = "app-cli"; // Змінено на "app-cli"

        [JsonPropertyName("username")]
        public string Username { get; init; } = string.Empty; // Додано Username

        [JsonPropertyName("password")]
        public string Password { get; init; } = string.Empty; // Додано Password
    }
}
