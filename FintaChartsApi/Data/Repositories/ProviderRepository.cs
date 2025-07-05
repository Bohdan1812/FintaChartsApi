using FintaChartsApi.Data.Repositories.Interfaces;
using FintaChartsApi.Models.Data;

namespace FintaChartsApi.Data.Repositories
{
    public class ProviderRepository: GenericRepository<Provider, string>, IProviderRepository
    {
        public ProviderRepository(AppDbContext context) : base(context)
        {
        }
    }
}
