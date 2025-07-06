using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsWebSocket
{
    public record BaseWebSocketMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}
