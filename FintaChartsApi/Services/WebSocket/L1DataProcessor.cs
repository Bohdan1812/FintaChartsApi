using FintaChartsApi.Clients;
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
        private readonly FintaChartsWebSocketClient _wsClient;

        public L1DataProcessor(
         ILogger<L1DataProcessor> logger,
         IFintachartsWebSocketService webSocketService, // IFintachartsWebSocketService вже не має OnRawMessageReceived
         IL1StorageService l1StorageService,
         FintaChartsWebSocketClient wsClient // Потрібно ін'єктувати FintaChartsWebSocketClient напряму
     )
        {
            _logger = logger;
            _webSocketService = webSocketService; // Зберегти для інших цілей, якщо потрібно
            _l1StorageService = l1StorageService;
            _wsClient = wsClient;

            // Підписуємося на подію отримання повідомлень безпосередньо від FintaChartsWebSocketClient
            // L1DataProcessor тепер напряму слухає _wsClient
            _wsClient.OnMessageReceived += HandleIncomingMessage;
        }

        private async Task HandleIncomingMessage(string message) // Змінено: прибрано CancellationToken
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
                        // Передаємо CancellationToken.None, оскільки HandleIncomingMessage більше не отримує токен
                        // Якщо потрібно кероване скасування, L1DataProcessor має мати власний CancellationTokenSource
                        await _l1StorageService.UpdateDatabaseAsync(l1Message, CancellationToken.None);
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

