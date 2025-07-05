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
        private Uri? _webSocketUri;

        private CancellationTokenSource? _stoppingCts;
        private Task? _executingTask;//?

        public event Func<string, CancellationToken, Task>? OnRawMessageReceived;
        public event Func<CancellationToken, Task>? OnReconnected;
        public event Func<Task>? OnWebSocketConnected; 
        // FintaChartsApi/Clients/FintaChartsWebSocketClient.cs
        public event Action? OnConnected; // Змінено з Func<Task> на Action
        public event Action<WebSocketCloseStatus?, string?>? OnDisconnected;
        public event Func<string, Task>? OnMessageReceived; // Це коректно

        private void HandleWebSocketConnected() // Змінено з async Task на void
        {
            _logger.LogInformation("FintaChartsWebSocketClient reported connection. Invoking OnWebSocketConnected.");

            // Викликаємо асинхронну подію, але сам обробник void, тому не можемо await
            // Краще запустити виклик події як "вогонь і забудь"
            _ = OnWebSocketConnected?.Invoke(); // Використовуємо "_" для ігнорування поверненого Task
        }

        private void HandleWebSocketDisconnected(WebSocketCloseStatus? closeStatus, string? description)
        {
            _logger.LogWarning("FintaChartsWebSocketClient reported disconnection. Status: {Status}, Description: {Description}", closeStatus, description);
            // Якщо відключення відбулося, executeAsync цикл автоматично спробує перепідключитися.
            // Додаткова логіка перепідключення може бути додана тут, якщо ExecuteAsync не справляється.
            // Для IHostedService, ExecuteAsync постійно перезапускається.
        }


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

            // Підписуємося на події від FintaChartsWebSocketClient
            _wsClient.OnConnected += HandleWebSocketConnected;
            _wsClient.OnDisconnected += HandleWebSocketDisconnected;
            // _wsClient.OnMessageReceived буде підписаний L1DataProcessor, тут не потрібно
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FintachartsWebSocketService is starting.");
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = ExecuteAsync(_stoppingCts.Token);
            return Task.CompletedTask;
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Отримуємо URI з токеном
                    var accessToken = await _tokenProvider.GetAccessTokenAsync();
                    var webSocketUri = _configuration["Fintacharts:WebSocketUri"]
                                       ?? throw new InvalidOperationException("Fintacharts:WebSocketUri not configured.");
                    var uriBuilder = new UriBuilder(webSocketUri) { Query = $"token={accessToken}" };
                    _webSocketUri = uriBuilder.Uri; // Зберігаємо URI для можливих повторних підключень

                    // Підключаємось
                    await _wsClient.ConnectAsync(_webSocketUri, stoppingToken);

                    // Після успішного підключення та запуску StartReceiving у _wsClient,
                    // цей метод має просто чекати, доки не буде запиту на зупинку або відключення.
                    // _wsClient.OnConnected Event вже викликає OnWebSocketConnected.
                    // _wsClient.OnDisconnected Event тепер оброблятиме розриви.
                    _logger.LogInformation("FintachartsWebSocketService connected and receiving loop initiated.");

                    // Чекаємо, доки завдання не буде скасоване або з'єднання не розірветься ззовні
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("FintachartsWebSocketService is shutting down due to cancellation.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Connection error in FintachartsWebSocketService. Attempting to reconnect...");
                    // Не потрібно викликати CloseAsync тут, оскільки ClientWebSocket вже недійсний
                    // і InitializeWebSocketAndTcs() в Client's ConnectAsync вже потурбується про Dispose попереднього.
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Затримка перед повторною спробою
                }
            }
            _logger.LogInformation("FintachartsWebSocketService background task finished.");
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
                _stoppingCts?.Cancel(); // Сигналізуємо про скасування фонового завдання
            }
            finally
            {
                // Чекаємо завершення _executingTask або скасування поточного StopAsync токеном
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));

                // Закриваємо WebSocket-з'єднання, якщо воно ще відкрито.
                // Використовуємо CancellationToken.None, щоб гарантувати спробу закриття під час зупинки.
                if (_wsClient.State == WebSocketState.Open || _wsClient.State == WebSocketState.CloseReceived)
                {
                    await _wsClient.CloseAsync(CancellationToken.None);
                }
                _stoppingCts?.Dispose();
                _logger.LogInformation("FintachartsWebSocketService stopped.");
            }
        }


        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return _wsClient.CloseAsync(cancellationToken); ;
        }

        public async Task SendMessageAsync(byte[] messageBytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            try
            {
                // _wsClient.SendAsync внутрішньо використає WaitUntilConnectedAsync
                await _wsClient.SendAsync(messageBytes, messageType, endOfMessage, cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("WebSocket is not open"))
            {
                _logger.LogWarning("WebSocket is not open, cannot send message: {Message}", ex.Message);
                // Тут можна реалізувати логіку кешування повідомлень, які не вдалося відправити
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Sending message was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WebSocket message through FintachartsWebSocketService.");
                throw; // Можливо, захочете перекинути виняток далі
            }
        }
    }
}
