using FintaChartsApi.Clients;
using FintaChartsApi.Models.FintaChartsApi.Bars;
using FintaChartsApi.Models.FintaChartsApi.Instruments;
using Microsoft.AspNetCore.Mvc;
using Refit;

namespace FintaChartsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FintachartsController : ControllerBase
    {
        private readonly IFintaChartsApi _fintaChartsApi;
        private readonly ILogger<FintachartsController> _logger; // Додамо логування для діагностики

        public FintachartsController(IFintaChartsApi fintaChartsApi, ILogger<FintachartsController> logger)
        {
            _fintaChartsApi = fintaChartsApi;
            _logger = logger;
        }


        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            try
            {
                var response = await _fintaChartsApi.GetProviders();
                // Тепер ви можете безпечно звертатись до response.Data
                _logger.LogInformation("Successfully retrieved {Count} providers.", response?.Data?.Count ?? 0);
                return Ok(response);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Refit API error occurred while getting providers: Status Code {StatusCode}, Content: {Content}", ex.StatusCode, ex.Content);
                return StatusCode((int)ex.StatusCode, new { Message = ex.Message, Details = ex.Content }); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while getting providers.");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}"); 
            }
        }
    }
}
