using FintaChartsApi.Clients;
using FintaChartsApi.Data.Repositories.Interfaces;
using FintaChartsApi.Models.Data;
using FintaChartsApi.Models.WebSocket;


namespace FintaChartsApi.Services.Price
{
    public class InstrumentPriceService : IInstrumentPriceService
    {
        private readonly IFintaSocketClient _socket;
        private readonly IInstrumentPriceRepository _instrumentPriceRepository;
        private readonly ILogger<InstrumentPriceService> _logger;
        private readonly IProviderRepository _providerRepository;
        private readonly IInstrumentRepository _instrumentRepository;

        private TimeSpan DELAY = TimeSpan.FromSeconds(5);

        public InstrumentPriceService(
            IFintaSocketClient socket,
            IInstrumentPriceRepository repository,
            ILogger<InstrumentPriceService> logger,
            IProviderRepository providerRepository,
            IInstrumentRepository instrumentRepository)
        {
            _instrumentPriceRepository = repository;
            _socket = socket;
            _logger = logger;
            _providerRepository = providerRepository;
            _instrumentRepository = instrumentRepository;
        }

        private async Task<bool> InstrumentExists(string instrumentId)
        {
            return await _instrumentRepository.GetByIdAsync(instrumentId) is not null;
        }

        private async Task<bool> ProviderExists(string providerId)
        {
            return await _providerRepository.GetByIdAsync(providerId) is not null;
        }

     
        public async Task<InstrumentPrice?> GetLatestPriceAsync(string instrumentId, string provider)
        {
                if (!await InstrumentExists(instrumentId))
                {
                    _logger.LogWarning("Інструмент не знайдено: {InstrumentId}", instrumentId);
                    return null;
                }

                if (!await ProviderExists(provider))
                {
                    _logger.LogWarning("Провайдера не знайдено: {Provider}", provider);
                    return null;
                }
       
            try
            {
                var existingPrice = await _instrumentPriceRepository.GetByIdAsync((instrumentId, provider));

                var msg = await _socket.SubscribeOnceAsync(instrumentId, provider, DELAY);

                if (msg is null)
                {
                    _logger.LogWarning("⏱ Таймаут або помилка при отриманні ціни для {InstrumentId}", instrumentId);
                    return existingPrice;
                }

                var ask = msg.Type == "l1-update" ? msg.Ask : msg.Quote?.Ask;
                var bid = msg.Type == "l1-update" ? msg.Bid : msg.Quote?.Bid;
                var last = msg.Type == "l1-update" ? msg.Last : msg.Quote?.Last;



                var price = new InstrumentPrice
                {
                    InstrumentId = instrumentId,
                    ProviderId = provider,
                    Ask = ask?.Price,
                    Bid = bid?.Price,
                    Last = last?.Price,
                    Volume = last?.Volume ?? ask?.Volume ?? bid?.Volume,
                    LastUpdated = last?.TimeStamp ?? ask?.TimeStamp ?? bid?.TimeStamp ?? DateTimeOffset.UtcNow
                };

               

                if (existingPrice is null)
                {
                    await _instrumentPriceRepository.AddAsync(price);
                    _logger.LogInformation("📊 Додано нову ціну для {InstrumentId}: Ask={Ask}, Bid={Bid}, Last={Last}",
                        instrumentId, price.Ask, price.Bid, price.Last);
                }
                else if (existingPrice.LastUpdated <= price.LastUpdated)
                {
                    existingPrice.Ask = price.Ask;
                    existingPrice.Bid = price.Bid;
                    existingPrice.Last = price.Last;
                    existingPrice.Volume = price.Volume;
                    existingPrice.LastUpdated = price.LastUpdated;

                    await _instrumentPriceRepository.UpdateAsync(existingPrice, (instrumentId, provider));
                    _logger.LogInformation("📊 Оновлено ціну для {InstrumentId}, {Provider}: Ask={Ask}, Bid={Bid}, Last={Last}",
                        instrumentId, provider, price.Ask, price.Bid, price.Last);
                }
                else
                {
                    _logger.LogInformation("📊 Ціна для {InstrumentId} вже актуальна, не оновлюємо", instrumentId);
                    return existingPrice;
                }

                _logger.LogInformation("✅ Отримано актуальну ціну для {InstrumentId}: Ask={Ask}, Bid={Bid}, Last={Last}",
                    instrumentId, price.Ask, price.Bid, price.Last);

                int cnahgedRows = await _instrumentPriceRepository.SaveChangesAsync();

                _logger.LogInformation("📝 Збережено ціну для {InstrumentId} в БД, змінено рядків: {ChangedRows}",
                    instrumentId, cnahgedRows);  

                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка при отриманні ціни для {InstrumentId}", instrumentId);
                return null;
            }
        }
    }

}

