using FintaChartsApi.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FintaChartsApi.Data.Repositories
{
    public class GenericRepository<TEntity, TId> : IGenericRepository<TEntity, TId> where TEntity : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
        }


        public async Task<TEntity> GetByIdAsync(TId id)
        {
            if (typeof(TId).IsGenericType && typeof(TId).GetGenericTypeDefinition() == typeof(ValueTuple<,>))
            {
                // Для кортежу (string, string)
                var tuple = (dynamic)id!; // Cast to dynamic to access Item1, Item2
                object[] keyParts = { tuple.Item1, tuple.Item2 };
                return await _dbSet.FindAsync(keyParts);
            }
            else
            {
                return await _dbSet.FindAsync(id);
            }
        }
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }
        public async Task AddAsync(TEntity entity)
        {

            _ = entity ?? throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");

            await _dbSet.AddAsync(entity);
        }
        public async Task UpdateAsync(TEntity entity, TId id)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            
            if (await GetByIdAsync(id) is null)
            {
                throw new KeyNotFoundException($"{nameof(TEntity)} with id {id} not found.");
            }

            _dbSet.Update(entity);
        }
        public async Task DeleteAsync(TId id)
        {
            var entity = await GetByIdAsync(id);
            if (entity is null)
            {
                throw new KeyNotFoundException($"{nameof(TEntity)} with id {id} not found.");
            }
            _dbSet.Remove(entity);
        }
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (!entities.Any()) return;

            await _dbSet.AddRangeAsync(entities);
        }
    }
}
