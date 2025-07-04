namespace FintaChartsApi.Models.FintaChartsApi.Instruments
{
    public record PagingDto
    {
        public int Page { get; set; }
        public int Pages { get; set; }
        public int Items { get; set; }
    }
}
