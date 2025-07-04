namespace FintaChartsApi.Models.FintaChartsApi.Instruments
{
    public record InstrumentDto
    {
        public string? Id { get; set; }
        public string? Symbol { get; set; }
        public string? Kind { get; set; }
        public string? Description { get; set; }
        public decimal? TickSize { get; set; }
        public string? Currency { get; set; }
        public string? BaseCurrency { get; set; } // Додано з вашого прикладу

        // Вкладені об'єкти
        public MappingsDto? Mappings { get; set; }
        public ProfileDto? Profile { get; set; }
    }
}
