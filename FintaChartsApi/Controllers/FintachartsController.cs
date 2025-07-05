using FintaChartsApi.Data.Repositories.Interfaces;
using FintaChartsApi.Models.Data;
using FintaChartsApi.Services.WebSocket.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FintaChartsApi.Controllers
{
    [ApiController]
    [Route("[controller]")] //Базовий шлях: /fintacharts
    public class FintachartsController : ControllerBase
    {
        private readonly ILogger<FintachartsController> _logger; // Додамо логування для діагностики
        private readonly IProviderRepository _providerRepository;   
        private readonly IInstrumentRepository _instrumentRepository;
        private readonly IInstrumentPriceRepository _instrumentPriceRepository;
        private readonly ISubscriptionManager _subscriptionManager; // Інжектуємо ISubscriptionManager

        private static readonly TimeSpan DataFreshnessThreshold = TimeSpan.FromMinutes(1);


        public FintachartsController(
            ILogger<FintachartsController> logger,
            IInstrumentRepository instrumentRepository,
            IProviderRepository providerRepository,
             IInstrumentPriceRepository instrumentPriceRepository,
            ISubscriptionManager subscriptionManager)
        {
            _providerRepository = providerRepository;
            _instrumentRepository = instrumentRepository;
            _instrumentPriceRepository = instrumentPriceRepository;
            _subscriptionManager = subscriptionManager;
            _logger = logger;
        }


        [HttpGet("instruments")] // Це HTTP GET запит
        [ProducesResponseType(typeof(IEnumerable<Instrument>), 200)] // Описуємо очікуваний тип відповіді (для Swagger/OpenAPI)
        [ProducesResponseType(500)] // Описуємо можливу помилку сервера
        public async Task<IActionResult> GetInstruments()
        {
            _logger.LogInformation("Received request to get all instruments from the database.");
            try
            {
                var instruments = await _instrumentRepository.GetAllAsync();
                _logger.LogInformation("Successfully retrieved {Count} instruments from the database.", instruments?.Count() ?? 0);
                return Ok(instruments); // Повертаємо HTTP 200 OK з даними
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting all instruments: {Message}", ex.Message);
                return StatusCode(500, "Internal server error: Could not retrieve instruments."); // Повертаємо HTTP 500
            }
        }


        [ProducesResponseType(typeof(IEnumerable<string>), 200)] // Описуємо очікуваний тип відповіді (для Swagger/OpenAPI)
        [ProducesResponseType(500)] // Описуємо можливу помилку сервера
        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            _logger.LogInformation("Received request to get all providers from the database.");
            try
            {
                var providers = await _providerRepository.GetAllAsync();
                _logger.LogInformation("Successfully retrieved {Count} providers from the database.", providers?.Count() ?? 0);
                return Ok(providers.Select(p => p.Id)); // Повертаємо HTTP 200 OK з даними
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting all providers: {Message}", ex.Message);
                return StatusCode(500, "Internal server error: Could not retrieve providers."); // Повертаємо HTTP 500
            }
        }


        [HttpGet("price")]
        [ProducesResponseType(typeof(InstrumentPrice), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Accepted)] // Можемо повертати 202 Accepted, якщо підписка ініційована
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAssetPrice(
           [FromQuery] string instrumentId,
           [FromQuery] string providerId,
           CancellationToken cancellationToken)
        {
            _logger.LogInformation("Request for asset price for InstrumentId: {InstrumentId}, ProviderId: {ProviderId}", instrumentId, providerId);

            try
            {
                // 1. Спроба отримати дані з БД
                var instrumentPrice = await _instrumentPriceRepository.GetByIdAsync((instrumentId, providerId));
                var currentTime = DateTimeOffset.UtcNow;

                // 2. Перевірка актуальності даних
                bool isDataFresh = instrumentPrice != null &&
                                   (currentTime - instrumentPrice.LastUpdated) <= DataFreshnessThreshold;

                if (isDataFresh)
                {
                    _logger.LogInformation("Found fresh data in DB for {InstrumentId} ({ProviderId}). Last updated: {LastUpdated}", instrumentId, providerId, instrumentPrice.LastUpdated);
                    return Ok(instrumentPrice);
                }

                // 3. Якщо дані відсутні або застаріли, ініціюємо підписку
                _logger.LogInformation("Data for {InstrumentId} ({ProviderId}) is stale or not found. Initiating subscription...", instrumentId, providerId);

                // Викликаємо метод підписки. Він внутрішньо чекатиме на перші дані.
                // Якщо підписка вже активна, він просто прологіює це і, можливо, все одно чекатиме на перші дані.
                await _subscriptionManager.SubscribeToInstrumentAsync(instrumentId, providerId);

                // Після підписки, спробуємо ще раз отримати дані з БД
                // Даємо невелику затримку або дозволяємо WebSocket'у час на обробку
                // (хоча SubscribeToInstrumentAsync вже чекає на перші дані).
                // Повторне отримання даних гарантує, що ми отримуємо їх з БД після обробки WebSocket'ом.
                instrumentPrice = await _instrumentPriceRepository.GetByIdAsync((instrumentId, providerId));
                await _subscriptionManager.UnsubscribeFromInstrumentAsync(instrumentId, providerId); 

                if (instrumentPrice != null && (currentTime - instrumentPrice.LastUpdated) <= DataFreshnessThreshold)
                {
                    _logger.LogInformation("Successfully retrieved fresh data after subscription for {InstrumentId} ({ProviderId}).", instrumentId, providerId);
                    return Ok(instrumentPrice);
                }
                else
                {
                    _logger.LogWarning("Even after attempting subscription, fresh data for {InstrumentId} ({ProviderId}) could not be retrieved within the threshold. Returning current DB state or 404.", instrumentId, providerId);
                    // Якщо навіть після спроби підписки ми не отримали свіжих даних,
                    // можна повернути те, що є в БД, або 404, залежно від бізнес-логіки.
                    // Тут повертаємо те, що є (якщо є) або 404.
                    if (instrumentPrice != null)
                    {
                        return Ok(instrumentPrice); // Повертаємо застарілі, але наявні дані
                    }
                    else
                    {
                        return NotFound(new { Message = $"Asset price not found for InstrumentId '{instrumentId}' and ProviderId '{providerId}', and fresh data could not be obtained." });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Request for asset price for {InstrumentId} ({ProviderId}) was cancelled.", instrumentId, providerId);
                return StatusCode((int)HttpStatusCode.RequestTimeout, "Request processing was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving asset price for InstrumentId: {InstrumentId}, ProviderId: {ProviderId}", instrumentId, providerId);
                return StatusCode((int)HttpStatusCode.InternalServerError, "An internal server error occurred.");
            }
        }
    }
}
