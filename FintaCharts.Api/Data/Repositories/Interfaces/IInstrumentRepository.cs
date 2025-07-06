using FintaChartsApi.Models.Data;

namespace FintaChartsApi.Data.Repositories.Interfaces
{
    public interface IInstrumentRepository : IGenericRepository<Instrument, string>
    {
        Task UpdateRangeAsync(IEnumerable<Instrument> instruments);
    }
}
