namespace FintaChartsApi.Models.FintaChartsApi.Bars
{
    public record BarDto
    {
        // 't' представляє час відкриття свічки (timestamp).
        // Використовуємо DateTime для коректної обробки дати та часу.
        public DateTime T { get; init; }

        // 'o' представляє ціну відкриття.
        public decimal O { get; init; }

        // 'h' представляє найвищу ціну за період свічки.
        public decimal H { get; init; }

        // 'l' представляє найнижчу ціну за період свічки.
        public decimal L { get; init; }

        // 'c' представляє ціну закриття.
        public decimal C { get; init; }

        // 'v' представляє об'єм торгів за період свічки.
        public decimal V { get; init; }
    }
}
