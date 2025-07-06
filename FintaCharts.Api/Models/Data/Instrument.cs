using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FintaChartsApi.Models.Data
{
    public class Instrument
    {
        [Key]
        [MaxLength(36)]// Первинний ключ для інструменту
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)] 
        public string Symbol { get; set; } = string.Empty; // Тікер інструменту (наприклад, "AAPL", "EUR/USD")

        [Required]
        [MaxLength(500)] // Обмеження довжини для Description (повна назва)
        public string Description { get; set; } = string.Empty; // Повна назва інструменту (наприклад, "Apple Inc.")
        [Required]
        [MaxLength(50)]
        public string Kind { get; set; } = string.Empty;

        // Додаткові поля, які можуть бути корисними з API Fintacharts
        [MaxLength(100)]
        public string? Currency { get; set; } // Валюта торгівлі (наприклад, "USD", "EUR")

        [MaxLength(10)]
        public string? BaseCurrency { get; set; }

        [Column(TypeName = "numeric(18, 9)")]
        public decimal? TickSize { get; set; }

        public virtual ICollection<InstrumentPrice> Prices { get; set; } = new List<InstrumentPrice>();
    }
}
