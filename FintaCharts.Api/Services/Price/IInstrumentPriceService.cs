

using FintaChartsApi.Models.Data;

namespace FintaChartsApi.Services.Price
{
    public interface IInstrumentPriceService
    {
        Task<InstrumentPrice?> GetLatestPriceAsync(string instrumentId, string provider);
    }
}
