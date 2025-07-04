using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsApi.Bars
{
    public record GetBarsByDateRangeRequest
    {
        // Унікальний ідентифікатор інструменту
        [JsonPropertyName("instrumentId")]
        public string InstrumentId { get; init; } = string.Empty;

        // Провайдер даних (наприклад, "oanda")
        [JsonPropertyName("provider")]
        public string Provider { get; init; } = "oanda"; 

        // Числове значення інтервалу (наприклад, 6 для "6 hours")
        [JsonPropertyName("interval")]
        public int Interval { get; init; } = 1;

        // Одиниці періодичності (наприклад, "minute", "hour", "day")
        [JsonPropertyName("periodicity")]
        public string Periodicity { get; init; } = "minute"; 

        // Дата початку діапазону (без часу)
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; init; } = DateTime.MinValue; 

        // Дата кінця діапазону (без часу)
        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; init; }
    }
}
