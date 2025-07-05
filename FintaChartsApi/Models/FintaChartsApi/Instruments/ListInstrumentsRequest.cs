using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsApi.Instruments
{
    public record ListInstrumentsRequest
    {
        [JsonPropertyName("provider")]
        public string? Provider { get; init; } 
        [JsonPropertyName("kind")]
        public string? Kind { get; init; }  
        [JsonPropertyName("symbol")]
        public string? Symbol { get; init; }

        // Page: 1
        [JsonPropertyName("page")]
        public int Page { get; init; } = 1; // Значення за замовчуванням

        // Size: 10
        [JsonPropertyName("size")]
        public int Size { get; init; } = 10; // Значення за замовчуванням
    }
}
