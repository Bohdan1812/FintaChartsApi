using FintaChartsApi.Data.Repositories.Interfaces;
using FintaChartsApi.Models.Data;
using FintaChartsApi.Models.WebSocket;
using FintaChartsApi.Services.WebSocket.Interfaces;

namespace FintaChartsApi.Services.WebSocket
{
    public class L1StorageService : IL1StorageService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<L1StorageService> _logger;

        public L1StorageService(
            IServiceScopeFactory scopeFactory,
            ILogger<L1StorageService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task UpdateDatabaseAsync(L1Message l1Message, CancellationToken cancellationToken)
        {
            var instrumentIdString = l1Message.InstrumentId;
            var providerId = l1Message.Provider;

            L1PriceData? currentAsk = null;
            L1PriceData? currentBid = null;
            L1PriceData? currentLast = null;

            if (l1Message.Type == "l1-update")
            {
                currentAsk = l1Message.Ask;
                currentBid = l1Message.Bid;
                currentLast = l1Message.Last;
            }
            else if (l1Message.Type == "l1-snapshot" && l1Message.Quote != null)
            {
                currentAsk = l1Message.Quote.Ask;
                currentBid = l1Message.Quote.Bid;
                currentLast = l1Message.Quote.Last;
            }

            DateTimeOffset latestMessageTimestamp = DateTimeOffset.MinValue;
            if (currentAsk != null && currentAsk.TimeStamp > latestMessageTimestamp) latestMessageTimestamp = currentAsk.TimeStamp;
            if (currentBid != null && currentBid.TimeStamp > latestMessageTimestamp) latestMessageTimestamp = currentBid.TimeStamp;
            if (currentLast != null && currentLast.TimeStamp > latestMessageTimestamp) latestMessageTimestamp = currentLast.TimeStamp;

            using (var scope = _scopeFactory.CreateScope())
            {
                // Отримуємо репозиторії через ServiceProvider скоупу
                var instrumentRepository = scope.ServiceProvider.GetRequiredService<IGenericRepository<Instrument, string>>();
                var providerRepository = scope.ServiceProvider.GetRequiredService<IGenericRepository<Provider, string>>();
                var instrumentPriceRepository = scope.ServiceProvider.GetRequiredService<IGenericRepository<InstrumentPrice, (string, string)>>(); // Правильний тип TId

                try
                {
                    // 1. Перевірка та додавання Instrument
                    // Використовуємо GetByIdAsync, який вже є в GenericRepository
                    var instrument = await instrumentRepository.GetByIdAsync(instrumentIdString);
                    if (instrument == null)
                    {
                        instrument = new Instrument
                        {
                            Id = instrumentIdString,
                            Symbol = "UNKNOWN_SYMBOL",
                            Description = "Unknown Instrument",
                            Kind = "UNKNOWN"
                        };
                        await instrumentRepository.AddAsync(instrument);
                        _logger.LogInformation("Added new Instrument record for {InstrumentId}.", instrumentIdString);
                    }

                    // 2. Перевірка та додавання Provider
                    var provider = await providerRepository.GetByIdAsync(providerId);
                    if (provider == null)
                    {
                        provider = new Provider
                        {
                            Id = providerId
                        };
                        await providerRepository.AddAsync(provider);
                        _logger.LogInformation("Added new Provider record for {ProviderId}.", providerId);
                    }

                    // 3. Оновлення або додавання InstrumentPrice
                    // Використовуємо GetByIdAsync з кортежем для композитного ключа
                    var existingPrice = await instrumentPriceRepository.GetByIdAsync((instrumentIdString, providerId));

                    bool updated = false;

                    if (existingPrice == null)
                    {
                        var newPrice = new InstrumentPrice
                        {
                            InstrumentId = instrumentIdString,
                            ProviderId = providerId,
                            Ask = currentAsk?.Price,
                            Bid = currentBid?.Price,
                            Last = currentLast?.Price,
                            Volume = currentLast?.Volume ?? currentAsk?.Volume ?? currentBid?.Volume,
                            LastUpdated = latestMessageTimestamp
                        };
                        await instrumentPriceRepository.AddAsync(newPrice);
                        _logger.LogInformation("Added new price record for {InstrumentId} from {ProviderId}.", instrumentIdString, providerId);
                    }
                    else
                    {
                        // Логіка оновлення залишається тією ж
                        if (currentAsk != null && (existingPrice.Ask == null || currentAsk.TimeStamp > existingPrice.LastUpdated))
                        {
                            existingPrice.Ask = currentAsk.Price;
                            updated = true;
                        }
                        if (currentBid != null && (existingPrice.Bid == null || currentBid.TimeStamp > existingPrice.LastUpdated))
                        {
                            existingPrice.Bid = currentBid.Price;
                            updated = true;
                        }
                        if (currentLast != null && (existingPrice.Last == null || currentLast.TimeStamp > existingPrice.LastUpdated))
                        {
                            existingPrice.Last = currentLast.Price;
                            existingPrice.Volume = currentLast.Volume;
                            updated = true;
                        }

                        if (latestMessageTimestamp > existingPrice.LastUpdated)
                        {
                            existingPrice.LastUpdated = latestMessageTimestamp;
                            updated = true;
                        }

                        if (updated)
                        {
                            // Викликаємо UpdateAsync, передаючи об'єкт та його ID (кортеж)
                            await instrumentPriceRepository.UpdateAsync(existingPrice, (existingPrice.InstrumentId, existingPrice.ProviderId));
                            _logger.LogInformation("Updated price for {InstrumentId} from {ProviderId}.", instrumentIdString, providerId);
                        }
                        else
                        {
                            _logger.LogDebug("Received older or same timestamp data for {InstrumentId} from {ProviderId}. Skipping DB update.", instrumentIdString, providerId);
                        }
                    }

                    // !!! Важливо: Зберігаємо зміни в кінці транзакції скоупу !!!
                    await instrumentRepository.SaveChangesAsync(); // Викликаємо SaveChangesAsync() на будь-якому з репозиторіїв в цьому скоупі
                }
                catch (KeyNotFoundException knfEx)
                {
                    _logger.LogError(knfEx, "Data inconsistency detected: {Message}", knfEx.Message);
                    // Додаткова логіка обробки, якщо GetByIdAsync кидає KeyNotFoundException
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update database for instrument {InstrumentId} and provider {ProviderId}. Rolling back changes.", instrumentIdString, providerId);
                    // Тут не потрібен _context.Rollback(), EF Core за замовчуванням не зберігає зміни,
                    // якщо SaveChangesAsync не був викликаний або виникла помилка до його виклику.
                }
            }
        }
    }
}

