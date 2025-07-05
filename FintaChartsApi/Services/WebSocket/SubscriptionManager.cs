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
        private readonly ConcurrentDictionary<(string InstrumentId, string Provider), TaskCompletionSource<bool>> _pendingInstrumentData = new();

        public SubscriptionManager(
              IFintachartsWebSocketService webSocketService,
              ILogger<SubscriptionManager> logger)
        {
            _webSocketService = webSocketService;
            _logger = logger;

            // Підписуємося на подію перепідключення, щоб повторно відправити підписки
            _webSocketService.OnWebSocketConnected += ResubscribeAllInstrumentsAsync; // Змінено з OnReconnected
                                                                                      // Також підписуємося на сирі повідомлення, щоб сигналізувати про отримання перших даних
            _webSocketService.OnRawMessageReceived += HandleIncomingMessageForSubscriptionStatus;
        }
        private async Task ResubscribeAllInstrumentsAsync() // Без CancellationToken, якщо OnWebSocketConnected не передає його
        {
            _logger.LogInformation("WebSocket reconnected. Resubscribing to {Count} active instruments...", _activeSubscriptions.Count);
            foreach (var subscription in _activeSubscriptions.Keys.ToList()) // Використовуйте ToList, щоб уникнути зміни колекції під час ітерації
            {
                // Повторно надсилаємо запит на підписку.
                // Можливо, тут не потрібно чекати на TCS, оскільки це фоновий процес перепідписки.
                await SendSubscriptionMessageAsync(subscription.InstrumentId, subscription.Provider, subscribe: true, CancellationToken.None);
            }
            _logger.LogInformation("Resubscription complete.");
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
                var key = (instrumentId, provider);
                _pendingInstrumentData.TryAdd(key, tcs);

                try
                {
                    
                    await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    _logger.LogInformation("Initial data received for {InstrumentId}.", instrumentId);
                    await UnsubscribeFromInstrumentAsync(instrumentId, provider);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Timeout waiting for initial data for {InstrumentId}. It might not be available yet or subscription failed.", instrumentId);
                }
                finally
                {
                    _pendingInstrumentData.TryRemove(key, out _); // Завжди видаляємо TCS після завершення
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


        private async Task SendSubscriptionMessageAsync(string instrumentId, string provider, bool subscribe, CancellationToken cancellationToken)
        {
            var subscriptionMessage = new L1SubscriptionMessage
            {
                InstrumentId = instrumentId,
                Provider = provider,
                Subscribe = subscribe,
                Kinds = new List<string> { "ask", "bid", "last" } // Підписуємося на всі види
            };

            var jsonMessage = JsonSerializer.Serialize(subscriptionMessage);
            var bytes = Encoding.UTF8.GetBytes(jsonMessage);

            _logger.LogDebug("Sending subscription message: {Message}", jsonMessage);
            await _webSocketService.SendMessageAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }

        // Метод, який реагує на сирі повідомлення для оновлення статусу очікування даних
        private async Task HandleIncomingMessageForSubscriptionStatus(string message, CancellationToken cancellationToken)
        {
            try
            {
                var l1Message = JsonSerializer.Deserialize<L1Message>(message);

                if ((l1Message?.Type == "l1-update" || l1Message?.Type == "l1-snapshot") 
                    && !string.IsNullOrEmpty(l1Message.InstrumentId) 
                    && !string.IsNullOrEmpty(l1Message.Provider))
                {
                    var key = (l1Message.InstrumentId, l1Message.Provider);
                    if (_pendingInstrumentData.TryGetValue(key, out var tcs))
                    {
                        tcs.TrySetResult(true);
                        // Ось тут викликайте відписку:
                       
                        _pendingInstrumentData.TryRemove(key, out _);
                    }
                }
            }
            catch (JsonException)
            {
                // Ігноруємо помилки десеріалізації
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message for subscription status.");
            }
        }
    }

}

