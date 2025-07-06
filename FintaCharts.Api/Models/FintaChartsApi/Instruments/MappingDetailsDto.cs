namespace FintaChartsApi.Models.FintaChartsApi.Instruments
{
    public record MappingDetailsDto
    {
        public string? Symbol { get; set; }
        public string? Exchange { get; set; }
        public int DefaultOrderSize { get; set; }
        public int? MaxOrderSize { get; set; } 
        public TradingHoursDto? TradingHours { get; set; }
    }
}
