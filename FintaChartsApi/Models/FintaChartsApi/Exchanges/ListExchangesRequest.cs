using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsApi.Exchanges
{
    public class ListExchangesRequest
    {
        [JsonPropertyName("provider")]
        public string Provider { get; init; } = "oanda";
    }
}
