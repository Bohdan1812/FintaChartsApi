using FintaChartsApi.Data.Repositories.Interfaces;
using FintaChartsApi.Models.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace FintaChartsApi.Data.Repositories
{
    public class InstrumentPriceRepository : GenericRepository<InstrumentPrice, (string, string)>, IInstrumentPriceRepository
    {
        public InstrumentPriceRepository(AppDbContext context) : base(context)
        {
        }
    }
}
