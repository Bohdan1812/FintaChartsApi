using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.WebSocket
{
    public record L1SubscriptionMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "l1-subscription";

        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("instrumentId")]
        public string InstrumentId { get; set; } = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("subscribe")]
        public bool Subscribe { get; set; } 
        
        [JsonPropertyName("kinds")]
        public List<string> Kinds { get; set; } = new List<string>
        {
            "ask", "bid", "last"
        };

    }
}
