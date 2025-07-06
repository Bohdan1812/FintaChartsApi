using FintaChartsApi.Models.WebSocket;
using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsWebSocket
{
    public record L1QuoteData
    {
        [JsonPropertyName("ask")]
        public L1PriceData? Ask { get; set; }

        [JsonPropertyName("bid")]
        public L1PriceData? Bid { get; set; }

        [JsonPropertyName("last")]
        public L1PriceData? Last { get; set; }
    }
}
