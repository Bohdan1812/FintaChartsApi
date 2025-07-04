using System.ComponentModel.DataAnnotations;

namespace FintaChartsApi.Models.Data
{
    public class Provider
    {
        [Key] 
        [MaxLength(50)] 
        public string Id { get; set; } = string.Empty; // Ідентифікатор провайдера з Fintacharts API (наприклад, "simulation", "live")


        // Навігаційна властивість для зв'язку 1:Many з Bar
        public ICollection<Bar> Bars { get; set; } = new List<Bar>();
    }
}
