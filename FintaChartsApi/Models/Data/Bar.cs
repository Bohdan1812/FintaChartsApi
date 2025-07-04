using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FintaChartsApi.Models.Data
{
    public class Bar
    {
        [Key] // Вказуємо, що це первинний ключ
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Генерується БД при додаванні
        public int Id { get; set; }

        // InstrumentId - ідентифікатор фінансового інструменту. Частина складеного ключа.
        public string InstrumentId { get; set; } = string.Empty;

        [ForeignKey(nameof(InstrumentId))]
        public Instrument Instrument { get; set; } = null!; 
        public string ProviderId { get; set; } = string.Empty;
  
        public Provider Provider { get; set; } = null!; 


        // Resolution - таймфрейм свічки (наприклад, "1m", "1h", "1d"). Частина складеного ключа.
        public string Resolution { get; set; } = string.Empty;

        // 'T' (Timestamp) - час відкриття свічки. Частина складеного ключа.
        public DateTime T { get; set; }

        // 'O' - Ціна відкриття свічки.
        [Column(TypeName = "numeric(18, 9)")] // Вказуємо точність для decimal у БД
        public decimal O { get; set; }

        // 'H' - Найвища ціна за період свічки.
        [Column(TypeName = "numeric(18, 9)")]
        public decimal H { get; set; }

        // 'L' - Найнижча ціна за період свічки.
        [Column(TypeName = "numeric(18, 9)")]
        public decimal L { get; set; }

        // 'C' - Ціна закриття свічки.
        [Column(TypeName = "numeric(18, 9)")]
        public decimal C { get; set; }

        // 'V' - Об'єм торгів за період свічки.
        [Column(TypeName = "numeric(18, 9)")]
        public decimal V { get; set; }
    }
}
