using FintaChartsApi.Clients;
using FintaChartsApi.Services.Authorization;
using FintaChartsApi.Services.WebSocket.Interfaces;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace FintaChartsApi.Services.WebSocket
{
    public class FintachartsWebSocketService: IHostedService, IFintachartsWebSocketService
    {
        private readonly FintaChartsWebSocketClient _wsClient;
        private readonly ITokenProvider _tokenProvider;
        private readonly ILogger<FintachartsWebSocketService> _logger;
        private readonly IConfiguration _configuration;

        private CancellationTokenSource? _stoppingCts;
        private Task? _executingTask;//?

        public event Func<string, CancellationToken, Task>? OnRawMessageReceived;
        public event Func<CancellationToken, Task>? OnReconnected;

        public WebSocketState State => _wsClient.State;

        public FintachartsWebSocketService(
           FintaChartsWebSocketClient wsClient,
           ITokenProvider tokenProvider,
           ILogger<FintachartsWebSocketService> logger,
           IConfiguration configuration)
        {
            _wsClient = wsClient;
            _tokenProvider = tokenProvider;
            _logger = logger;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FintachartsWebSocketService is starting.");
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = ExecuteAsync(_stoppingCts.Token);
            return Task.CompletedTask;
        }

        private async Task? ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync(stoppingToken);
                    if (OnReconnected != null)
                    {
                        await OnReconnected.Invoke(stoppingToken); // Сигналізуємо про перепідключення
                    }
                    await ReceiveLoopAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("WebSocket operation cancelled. Shutting down FintachartsWebSocketService.");
                    break;
                }
                catch (WebSocketException wsEx)
                {
                    _logger.LogError(wsEx, "WebSocket connection lost or failed. Status: {Status}. Attempting to reconnect in 5 seconds...", wsEx.WebSocketErrorCode);
                    await _wsClient.CloseAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred in FintachartsWebSocketService. Attempting to reconnect in 10 seconds...");
                    await _wsClient.CloseAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            const int bufferSize = 4096;
            var buffer = new byte[bufferSize];
            var messageBuffer = new List<byte>();

            while (_wsClient.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await _wsClient.ReceiveMessageAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("WebSocket receive loop cancelled.");
                    break;
                }
                catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely || wsEx.WebSocketErrorCode == WebSocketError.InvalidState)
                {
                    _logger.LogWarning("WebSocket connection closed prematurely or in invalid state. Reconnecting. Error: {Error}", wsEx.Message);
                    throw; // Викликає логіку перепідключення у ExecuteAsync
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving WebSocket message. Reconnecting.");
                    throw; // Викликає логіку перепідключення
                }

                messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    var fullMessageBytes = messageBuffer.ToArray();
                    messageBuffer.Clear();

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(fullMessageBytes);
                        if (OnRawMessageReceived != null)
                        {
                            await OnRawMessageReceived.Invoke(message, cancellationToken); // Передаємо сире повідомлення
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket received close message. Status: {Status}, Description: {Description}", result.CloseStatus, result.CloseStatusDescription);
                        throw new WebSocketException($"WebSocket closed by peer: {result.CloseStatusDescription}");
                    }
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FintachartsWebSocketService is stopping.");
            if (_executingTask == null) return;

            try
            {
                _stoppingCts?.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));//?
                await _wsClient.CloseAsync(CancellationToken.None);
                _logger.LogInformation("FintachartsWebSocketService stopped.");
            }
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_wsClient.State == WebSocketState.Open) return;

            var accessToken = await _tokenProvider.GetAccessTokenAsync();
            var webSocketUri = _configuration["Fintacharts:WebSocketUri"]
                               ?? throw new InvalidOperationException("Fintacharts:WebSocketUri not configured.");
            var uriBuilder = new UriBuilder(webSocketUri);
            uriBuilder.Query = $"token={accessToken}";

            await _wsClient.ConnectAsync(uriBuilder.Uri, cancellationToken);
            _logger.LogInformation("Connected to Fintacharts WebSocket.");
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return _wsClient.CloseAsync(cancellationToken); ;
        }

        public async Task SendMessageAsync(byte[] messageBytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (_wsClient.State != WebSocketState.Open)
            {
                _logger.LogWarning("WebSocket is not open, cannot send message.");
                // Тут можна додати логіку для кешування повідомлень, які не вдалося відправити
                // і спроби відправити їх після перепідключення. Поки що просто ігноруємо.
                return;
            }
            await _wsClient.SendAsync(messageBytes, messageType, endOfMessage, cancellationToken);
        }
    }
}
