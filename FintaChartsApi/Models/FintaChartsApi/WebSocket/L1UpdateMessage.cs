using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsApi.WebSocket
{
    public class L1UpdateMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "l1-update"; // Тип повідомлення, завжди "l1-update"

        [JsonPropertyName("instrumentId")]
        public string InstrumentId { get; set; } = string.Empty; // ID фінансового інструменту

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty; // Постачальник даних

        [JsonPropertyName("bid")]
        public PriceVolumeData? Bid { get; set; } // Дані про ціну покупки

        [JsonPropertyName("ask")]
        public PriceVolumeData? Ask { get; set; } // Дані про ціну продажу

        [JsonPropertyName("last")]
        public PriceVolumeData? Last { get; set; } // Дані про останню угоду 

        [JsonPropertyName("trade")]
        public PriceVolumeData? Trade { get; set; } // Дані про останню торгівлю (також може містити change/changePct, якщо Fintacharts їх надсилає)
    }
}
