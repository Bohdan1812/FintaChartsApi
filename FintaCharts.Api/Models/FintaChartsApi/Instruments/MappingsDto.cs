namespace FintaChartsApi.Models.FintaChartsApi.Instruments
{
    public record MappingsDto
    {
        public MappingDetailsDto? Dxfeed { get; set; }
        public MappingDetailsDto? Oanda { get; set; }
        public MappingDetailsDto? Simulation { get; set; }
    }
}
