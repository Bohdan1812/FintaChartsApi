using FintaChartsApi.Models.WebSocket;
using FintaChartsApi.Services.WebSocket.Interfaces;
using System.Text.Json;

namespace FintaChartsApi.Services.WebSocket
{
    public class L1DataProcessor
    {
        private readonly ILogger<L1DataProcessor> _logger;
        private readonly IFintachartsWebSocketService _webSocketService;
        private readonly IL1StorageService _l1StorageService; // Ін'єктуємо сервіс для збереження в БД

        public L1DataProcessor(
            ILogger<L1DataProcessor> logger,
            IFintachartsWebSocketService webSocketService,
            IL1StorageService l1StorageService)
        {
            _logger = logger;
            _webSocketService = webSocketService;
            _l1StorageService = l1StorageService;

            // Підписуємося на подію отримання повідомлень від WebSocketService
            _webSocketService.OnRawMessageReceived += HandleIncomingMessage;
        }

        private async Task HandleIncomingMessage(string message, CancellationToken cancellationToken)
        {
            try
            {
                var l1Message = JsonSerializer.Deserialize<L1Message>(message);

                if (l1Message == null || string.IsNullOrEmpty(l1Message.InstrumentId))
                {
                    _logger.LogWarning("Failed to deserialize WebSocket message or missing InstrumentId: {Message}", message);
                    return;
                }

                switch (l1Message.Type)
                {
                    case "l1-update":
                    case "l1-snapshot":
                        _logger.LogTrace("Processing L1 data for {InstrumentId} from {Provider}. Type: {Type}", l1Message.InstrumentId, l1Message.Provider, l1Message.Type);
                        await _l1StorageService.UpdateDatabaseAsync(l1Message, cancellationToken); // Передаємо L1StorageService
                        break;
                    case "ack":
                        _logger.LogDebug("Received ACK message: {Message}", message);
                        break;
                    case "hello":
                        _logger.LogInformation("Received 'hello' message from Fintacharts WebSocket: {Message}", message);
                        break;
                    default:
                        _logger.LogWarning("Received unhandled message type '{MessageType}'. Message: {Message}", l1Message.Type, message);
                        break;
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize WebSocket message into L1Message: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling incoming WebSocket message: {Message}", message);
            }
        }
    }
}

