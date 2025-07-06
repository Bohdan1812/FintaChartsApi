using FintaChartsApi.Models.WebSocket;
using System.Text.Json;
using Websocket.Client;

namespace WebSocketClientWithLibrary.Clients
{
    public class FintaWebSocketClient
    {
        private readonly Uri _uri;
        private WebsocketClient? _client;

        public event Action<L1Message>? OnMessageReceived;

        public FintaWebSocketClient(string token)
        {
            var url = $"wss://platform.fintacharts.com/api/streaming/ws/v1/realtime?token={token}";
            _uri = new Uri(url);
        }

        public async Task ConnectAsync()
        {
            _client = new WebsocketClient(_uri);
            _client.ReconnectTimeout = TimeSpan.FromSeconds(30);


            _client.MessageReceived.Subscribe(msg =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<L1Message>(msg.Text);
                    if (message != null)
                    {
                        OnMessageReceived?.Invoke(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ JSON помилка: {ex.Message}");
                }
            });

            await _client.Start();
        }

        public async Task SubscribeAsync(string instrumentId, string provider = "simulation")
        {
            var sub = new L1SubscriptionMessage
            {
                InstrumentId = instrumentId,
                Provider = provider,
                Subscribe = true
            };

            var json = JsonSerializer.Serialize(sub);
            await _client!.SendInstant(json);
        }

        public async Task UnsubscribeAsync(string instrumentId, string provider = "simulation")
        {
            var unsub = new L1SubscriptionMessage
            {
                InstrumentId = instrumentId,
                Provider = provider,
                Subscribe = false
            };

            var json = JsonSerializer.Serialize(unsub);
            await _client!.SendInstant(json);
        }


    }
}

