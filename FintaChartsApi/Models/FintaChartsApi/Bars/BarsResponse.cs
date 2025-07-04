namespace FintaChartsApi.Models.FintaChartsApi.Bars
{
    public record BarsResponse
    {
        public List<BarDto>? Data { get; init; }
    }
}
