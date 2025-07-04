using System.Text.Json;

namespace FintaChartsApi.Models.FintaChartsApi.Instruments
{
    public record ProfileDto
    {
        public string? Name { get; set; }
        // Gics є порожнім об'єктом. Якщо Fintacharts почне додавати туди дані,
        // вам потрібно буде створити відповідний клас.
        // Наразі JsonElement дозволить уникнути помилок десеріалізації.
        public JsonElement Gics { get; set; }
    }
}
