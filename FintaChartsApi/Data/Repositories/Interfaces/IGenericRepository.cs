namespace FintaChartsApi.Data.Repositories.Interfaces
{
    public interface IGenericRepository<TEntity, TId> where TEntity : class
    {
        Task<TEntity> GetByIdAsync(TId id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        Task UpdateAsync(TEntity entity, TId id);
        Task DeleteAsync(TId id);
        Task<int> SaveChangesAsync();

    }
}
