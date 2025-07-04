using FintaChartsApi.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace FintaChartsApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Bar> Bars { get; set; }

        // DbSet для провайдерів
        public DbSet<Provider> Providers { get; set; }

        // DbSet для інструментів
        public DbSet<Instrument> Instruments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Конфігурація для таблиці Bars ---
            // Встановлюємо складений первинний ключ для Bar: InstrumentId, Resolution, T (Timestamp)
            modelBuilder.Entity<Bar>()
                .HasKey(b => new { b.InstrumentId, b.Resolution, b.T });

            // Зазначаємо, що Id генерується базою даних при додаванні нового запису
            modelBuilder.Entity<Bar>()
                .Property(b => b.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Bar>()
                .HasOne(b => b.Instrument)
                .WithMany(i => i.Bars)
                .HasForeignKey(b => b.InstrumentId);

            modelBuilder.Entity<Bar>()
                .HasOne<Provider>()
                .WithMany(p => p.Bars)
                .HasForeignKey(b => b.ProviderId)
                .IsRequired();

            modelBuilder.Entity<Provider>()
                .HasIndex(p => p.Id)
                .IsUnique(); // Додаємо унікальний індекс для Id провайдера

            modelBuilder.Entity<Instrument>()
                .HasIndex(i => i.Id)
                .IsUnique(); // Додаємо унікальний індекс для Id інструменту

            modelBuilder.Entity<Instrument>()
                .HasIndex(i => i.Symbol)
                .IsUnique(); // Додаємо унікальний індекс для Symbol інструменту

        }
    }
}
