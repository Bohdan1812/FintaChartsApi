using FintaChartsApi.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace FintaChartsApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet для провайдерів
        public DbSet<Provider> Providers { get; set; } = null!; 

        // DbSet для інструментів
        public DbSet<Instrument> Instruments { get; set; } = null!;

        public DbSet<InstrumentPrice> InstrumentPrices { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Provider>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Provider>().
                HasMany(p => p.Prices)
                .WithOne(ip => ip.Provider)
                .HasForeignKey(ip => ip.ProviderId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<Instrument>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<Instrument>()
                .HasIndex(i => i.Symbol)
                .IsUnique(); 

            modelBuilder.Entity<Instrument>()
                .HasMany(i => i.Prices)
                .WithOne(ip => ip.Instrument)
                .HasForeignKey(ip => ip.InstrumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InstrumentPrice>()
                .HasKey(ip => new { ip.InstrumentId, ip.ProviderId });

        }
    }
}
