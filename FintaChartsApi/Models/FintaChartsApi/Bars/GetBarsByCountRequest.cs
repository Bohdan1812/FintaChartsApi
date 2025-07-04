using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsApi.Bars
{
    public class GetBarsByCountRequest
    {
        // Унікальний ідентифікатор інструменту (приклад: "ebefe2c7-5ac9-43bb-a8b7-4a97bf2c2576")
        [JsonPropertyName("instrumentId")]
        public string InstrumentId { get; init; } = string.Empty; 

        // Провайдер даних (наприклад, "oanda")
        [JsonPropertyName("provider")]
        public string Provider { get; init; } = "oanda"; 

        // Числове значення інтервалу (наприклад, 1 для "1 minute")
        [JsonPropertyName("interval")]
        public int Interval { get; init; } = 1; 

        // Одиниці періодичності (наприклад, "minute", "hour", "day")
        [JsonPropertyName("periodicity")]
        public string Periodicity { get; init; } = "minute"; 

        // Кількість свічок, які потрібно отримати
        [JsonPropertyName("barsCount")]
        public int BarsCount { get; init; } // Обов'язкове поле
    }
}
