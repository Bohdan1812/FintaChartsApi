using FintaChartsApi.Models.FintaChartsWebSocket;
using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.WebSocket
{
    public record L1Message : BaseWebSocketMessage
    {
        [JsonPropertyName("instrumentId")]
        public string InstrumentId { get; set; } = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        // Поля для "l1-update" (одиничні оновлення)
        [JsonPropertyName("ask")]
        public L1PriceData? Ask { get; set; }

        [JsonPropertyName("bid")]
        public L1PriceData? Bid { get; set; }

        [JsonPropertyName("last")]
        public L1PriceData? Last { get; set; }

        // Поле для "l1-snapshot" (вкладений об'єкт "quote")
        [JsonPropertyName("quote")]
        public L1QuoteData? Quote { get; set; }
    }
}
