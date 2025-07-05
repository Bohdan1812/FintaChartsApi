using FintaChartsApi.Models.Data;

namespace FintaChartsApi.Data.Repositories.Interfaces
{
    /*
    public interface IInstrumentPriceRepository
    {
        Task<InstrumentPrice> GetByCompositeKeyAsync(string instrumentIdString, string providerId);
        Task AddAsync(InstrumentPrice entity);
        Task UpdateAsync(InstrumentPrice entity);
        Task DeleteAsync(InstrumentPrice entity);
        Task<int> SaveChangesAsync();
    }*/
    public interface IInstrumentPriceRepository : IGenericRepository<InstrumentPrice, (string, string)>;

}
