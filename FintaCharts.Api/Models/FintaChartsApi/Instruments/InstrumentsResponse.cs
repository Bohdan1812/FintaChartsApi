namespace FintaChartsApi.Models.FintaChartsApi.Instruments
{
    public record InstrumentsResponse
    {
        public PagingDto? Paging { get; set; }
        public List<InstrumentDto>? Data { get; set; }
    }
}
