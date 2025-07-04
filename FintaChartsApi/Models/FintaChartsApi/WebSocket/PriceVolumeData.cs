using System.Text.Json.Serialization;

namespace FintaChartsApi.Models.FintaChartsApi.WebSocket
{
    public class PriceVolumeData
    {
        //Мітка часу, що вказує, коли ці дані були востаннє оновлені або зафіксовані.
        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; } = DateTime.MinValue;

        //Ціна активу. Це може бути ціна покупки (Bid), ціна продажу (Ask), або ціна останньої угоди (Last/Trade).

        [JsonPropertyName("price")]
        public decimal Price { get; set; } = 0.0m;


        /// Обсяг активу, пов'язаний з ціною.
        /// Наприклад, обсяг доступний для покупки за ціною Bid,
        /// обсяг для продажу за ціною Ask, або обсяг останньої угоди.
        [JsonPropertyName("volume")]
        public long Volume { get; set; }


        /// Зміна ціни (абсолютне значення) відносно попереднього значення.
        /// Це поле зазвичай присутнє тільки для даних "Last" або "Trade"
        /// і є nullable, оскільки може бути відсутнім для "Bid" або "Ask" даних.
        [JsonPropertyName("change")]
        public decimal? Change { get; set; }


        /// Відсоткова зміна ціни відносно попереднього значення.
        /// Як і "Change", це поле зазвичай присутнє тільки для даних "Last" або "Trade"
        /// і є nullable, оскільки може бути відсутнім для "Bid" або "Ask" даних.
        [JsonPropertyName("changePct")]
        public decimal? ChangePct { get; set; }
    }
}
