using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FintaChartsApi.Models.Data
{
    public class InstrumentPrice
    {
        [Required]
        public string InstrumentId { get; set; }  = string.Empty;   

        [Required]
        public string ProviderId { get; set; } = string.Empty;

        [Column(TypeName = "numeric(18, 8)")]
        public decimal? Ask { get; set; }

        [Column(TypeName = "numeric(18, 8)")]
        public decimal? Bid { get; set; }

        [Column(TypeName = "numeric(18, 8)")]
        public decimal? Last { get; set; }

        public long? Volume { get; set; }

        [Required]
        public DateTimeOffset? LastUpdated { get; set; }

        public virtual Instrument Instrument { get; set; } = null!;

        public virtual Provider Provider { get; set; } = null!;


    }
}
