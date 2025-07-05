using FintaChartsApi.Models.WebSocket;
using FintaChartsApi.Services.WebSocket.Interfaces;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace FintaChartsApi.Services.WebSocket
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly IFintachartsWebSocketService _webSocketService;
        private readonly ILogger<SubscriptionManager> _logger;

        // Зберігаємо список інструментів, на які ми підписані
        private readonly ConcurrentDictionary<(string InstrumentId, string Provider), bool> _activeSubscriptions = new();

        // Для очікування первинних даних для інструменту після підписки
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingInstrumentData = new();

        public SubscriptionManager(
            IFintachartsWebSocketService webSocketService,
            ILogger<SubscriptionManager> logger)
        {
            _webSocketService = webSocketService;
            _logger = logger;

            // Підписуємося на подію перепідключення, щоб повторно відправити підписки
            _webSocketService.OnReconnected += ResubscribeAllInstrumentsAsync;
            // Також підписуємося на сирі повідомлення, щоб сигналізувати про отримання перших даних
            _webSocketService.OnRawMessageReceived += HandleIncomingMessageForSubscriptionStatus;
        }

        public ConcurrentDictionary<(string InstrumentId, string Provider), bool> GetActiveSubscriptions()
        {
            return _activeSubscriptions;
        }

        public async Task SubscribeToInstrumentAsync(string instrumentId, string provider)
        {
            if (_activeSubscriptions.TryAdd((instrumentId, provider), true))
            {
                _logger.LogInformation("Adding subscription for {InstrumentId} ({Provider}).", instrumentId, provider);
                await SendSubscriptionMessageAsync(instrumentId, provider, subscribe: true, CancellationToken.None);

                // Додаємо TaskCompletionSource для очікування первинних даних
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingInstrumentData.TryAdd(instrumentId, tcs);

                try
                {
                    // Чекаємо до 10 секунд на отримання перших даних
                    await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    _logger.LogInformation("Initial data received for {InstrumentId}.", instrumentId);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Timeout waiting for initial data for {InstrumentId}. It might not be available yet or subscription failed.", instrumentId);
                }
                finally
                {
                    _pendingInstrumentData.TryRemove(instrumentId, out _); // Завжди видаляємо TCS після завершення
                }
            }
            else
            {
                _logger.LogWarning("Instrument {InstrumentId} ({Provider}) is already subscribed.", instrumentId, provider);
            }
        }

        public async Task UnsubscribeFromInstrumentAsync(string instrumentId, string provider)
        {
            if (_activeSubscriptions.TryRemove((instrumentId, provider), out _))
            {
                _logger.LogInformation("Removing subscription for {InstrumentId} ({Provider}).", instrumentId, provider);
                await SendSubscriptionMessageAsync(instrumentId, provider, subscribe: false, CancellationToken.None);
            }
            else
            {
                _logger.LogWarning("Instrument {InstrumentId} ({Provider}) was not actively subscribed.", instrumentId, provider);
            }
        }

        private async Task ResubscribeAllInstrumentsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Resubscribing to {Count} active instruments...", _activeSubscriptions.Count);
            foreach (var subscription in _activeSubscriptions.Keys) // Перебираємо ключі (InstrumentId, Provider)
            {
                // Повторно надсилаємо запит на підписку
                await SendSubscriptionMessageAsync(subscription.InstrumentId, subscription.Provider, subscribe: true, cancellationToken);
            }
            _logger.LogInformation("Resubscription complete.");
        }

        private async Task SendSubscriptionMessageAsync(string instrumentId, string provider, bool subscribe, CancellationToken cancellationToken)
        {
            var subscriptionMessage = new L1SubscriptionMessage
            {
                InstrumentId = instrumentId,
                Provider = provider,
                Subscribe = subscribe,
                Kinds = new List<string> { "ask", "bid", "last", "volume" } // Підписуємося на всі види
            };

            var jsonMessage = JsonSerializer.Serialize(subscriptionMessage);
            var bytes = Encoding.UTF8.GetBytes(jsonMessage);

            _logger.LogDebug("Sending subscription message: {Message}", jsonMessage);
            await _webSocketService.SendMessageAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }

        // Метод, який реагує на сирі повідомлення для оновлення статусу очікування даних
        private Task HandleIncomingMessageForSubscriptionStatus(string message, CancellationToken cancellationToken)
        {
            try
            {
                // Для простоти, десеріалізуємо лише для InstrumentId
                var l1Message = JsonSerializer.Deserialize<L1Message>(message);

                if (l1Message?.Type == "l1-update" || l1Message?.Type == "l1-snapshot")
                {
                    if (_pendingInstrumentData.TryGetValue(l1Message.InstrumentId, out var tcs))
                    {
                        tcs.TrySetResult(true); // Сигналізуємо, що дані отримані
                    }
                }
            }
            catch (JsonException)
            {
                // Ігноруємо помилки десеріалізації тут, оскільки L1DataProcessor вже обробляє це для основної логіки
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message for subscription status.");
            }
            return Task.CompletedTask;
        }
    }

}

