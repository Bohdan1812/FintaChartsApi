using FintaChartsApi.Data.Repositories.Interfaces;
using FintaChartsApi.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace FintaChartsApi.Data.Repositories
{
    public class InstrumentRepository : GenericRepository<Instrument, string>, IInstrumentRepository
    {
        public InstrumentRepository(AppDbContext context) : base(context)
        {
        }

        public Task UpdateRangeAsync(IEnumerable<Instrument> instruments)
        {

            _dbSet.UpdateRange(instruments);
            return Task.CompletedTask;     
        }
    }
}
