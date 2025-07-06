using FintaChartsApi.Models.WebSocket;
using FintaChartsApi.Services.Authorization;
using System.Collections.Concurrent;
using System.Text.Json;
using Websocket.Client;

namespace FintaChartsApi.Clients
{

    public record SubscriptionKey(string InstrumentId, string Provider);
    public class FintaSocketClient : IFintaSocketClient
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly IConfiguration _config;
        private readonly ILogger<FintaSocketClient> _logger;
        private WebsocketClient? _client;
        private readonly ConcurrentDictionary<SubscriptionKey, List<TaskCompletionSource<L1Message>>> _subscribers = new();
        private readonly SemaphoreSlim _lock = new(1, 1);


        public event Action<L1Message>? OnL1Message;

        public FintaSocketClient(ITokenProvider tokenProvider, IConfiguration config, ILogger<FintaSocketClient> logger)
        {
            _tokenProvider = tokenProvider;
            _config = config;
            _logger = logger;
        }

        public async Task<L1Message?> SubscribeOnceAsync(string instrumentId, string provider, TimeSpan timeout)
        {
            var key = new SubscriptionKey(instrumentId, provider);
            var tcs = new TaskCompletionSource<L1Message>(TaskCreationOptions.RunContinuationsAsynchronously);

            _subscribers.AddOrUpdate(key,
                _ => new List<TaskCompletionSource<L1Message>> { tcs },
                (_, list) =>
                {
                    list.Add(tcs);
                    return list;
                });

            await EnsureConnectedAsync();
            await SendSubscriptionAsync(key, subscribe: true);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout));

            _subscribers.TryGetValue(key, out var list);
            list?.Remove(tcs);

            if (list is { Count: 0 })
                await SendSubscriptionAsync(key, subscribe: false);

            return completed == tcs.Task ? tcs.Task.Result : null;
        }

        private void HandleMessage(ResponseMessage msg)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<L1Message>(msg.Text);
                if (parsed == null) return;

                var key = new SubscriptionKey(parsed.InstrumentId, parsed.Provider);

                if (_subscribers.TryGetValue(key, out var list))
                {
                    foreach (var tcs in list.ToList())
                    {
                        if (IsValidMessage(parsed))
                        {
                            _logger.LogInformation("📥 Отримано валідне повідомлення для {Key}", key);
                            tcs.TrySetResult(parsed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка при обробці повідомлення WebSocket");
            }
        }
        private static bool IsValidMessage(L1Message msg)
        {
            return (msg.Type == "l1-update" && (msg.Ask != null || msg.Bid != null || msg.Last != null))
                || (msg.Type == "l1-snapshot" && msg.Quote?.Ask != null && msg.Quote?.Bid != null && msg.Quote?.Last != null);
        }
        private async Task EnsureConnectedAsync()
        {
            if (_client is { IsRunning: true })
                return;

            await _lock.WaitAsync();
            try
            {
                if (_client is { IsRunning: true })
                    return;

                var token = await _tokenProvider.GetAccessTokenAsync();
                var baseUri = _config["Fintacharts:WebSocketUri"]!;
                var uri = new Uri($"{baseUri}?token={token}");

                _client = new WebsocketClient(uri);
                _client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                _client.MessageReceived.Subscribe(HandleMessage);

                await _client.Start();
                _logger.LogInformation("🔌 WebSocket клієнт підключено до {Uri}", uri);
            }
            finally
            {
                _lock.Release();
            }
        }
        private async Task SendSubscriptionAsync(SubscriptionKey key, bool subscribe)
        {
            var msg = new L1SubscriptionMessage
            {
                InstrumentId = key.InstrumentId,
                Provider = key.Provider,
                Subscribe = subscribe
            };

            var json = JsonSerializer.Serialize(msg);
            await _client!.SendInstant(json);
            _logger.LogInformation("{Action} підписку для {Key}", subscribe ? "▶️ Надіслано" : "⏹ Скасовано", key);
        }
    }
}
