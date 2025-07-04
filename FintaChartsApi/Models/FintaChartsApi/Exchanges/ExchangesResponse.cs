namespace FintaChartsApi.Models.FintaChartsApi.Exchanges
{
    public record ExchangesResponse
    {
        public Dictionary<string, List<string>>? Data { get; init; }
    }
}
