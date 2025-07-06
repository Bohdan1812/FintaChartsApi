using FintaChartsApi.Data.Repositories.Interfaces;
using FintaChartsApi.Models.Data;
using FintaChartsApi.Services.Price;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IInstrumentPriceService _instrumentPriceSerice;

        private static readonly TimeSpan DataFreshnessThreshold = TimeSpan.FromMinutes(1);


        public FintachartsController(
            ILogger<FintachartsController> logger,
            IInstrumentRepository instrumentRepository,
            IProviderRepository providerRepository,
            IInstrumentPriceRepository instrumentPriceRepository,
            IInstrumentPriceService instrumentPriceSerice)
        {
            _providerRepository = providerRepository;
            _instrumentRepository = instrumentRepository;
            _instrumentPriceRepository = instrumentPriceRepository;
            _logger = logger;
            _instrumentPriceSerice = instrumentPriceSerice; 
        }

        [HttpGet("instrumentPrice")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(404)] 
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPrice([FromQuery]string instrumentId, [FromQuery]string provider)
        {
            var price = await _instrumentPriceSerice.GetLatestPriceAsync(instrumentId, provider);

            if (price is null)
                return NotFound($"Ціну для інструменту {instrumentId} не знайдено або не вдалося отримати.");

            return Ok(price);
        }



        [HttpGet("instruments")] // Це HTTP GET запит
        [ProducesResponseType(typeof(IEnumerable<Instrument>), 200)] // Описуємо очікуваний тип відповіді (для Swagger/OpenAPI)
        [ProducesResponseType(404)]
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


        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)] 
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


        
    }
}
