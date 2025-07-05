using System.ComponentModel.DataAnnotations;

namespace FintaChartsApi.Models.Data
{
    public class Provider
    {
        [Key] 
        [MaxLength(50)] 
        public string Id { get; set; } = string.Empty; // Ідентифікатор провайдера з Fintacharts API (наприклад, "simulation", "live")
           
        public virtual ICollection<InstrumentPrice> Prices { get; set; } = new List<InstrumentPrice>();
    }
}
