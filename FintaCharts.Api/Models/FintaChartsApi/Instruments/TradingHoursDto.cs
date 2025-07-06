namespace FintaChartsApi.Models.FintaChartsApi.Instruments
{
    public record TradingHoursDto
    {
        public string? RegularStart { get; set; }
        public string? RegularEnd { get; set; }
        public string? ElectronicStart { get; set; }
        public string? ElectronicEnd { get; set; }
    }
}
