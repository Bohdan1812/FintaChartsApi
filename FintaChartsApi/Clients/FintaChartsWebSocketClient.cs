using System.Net.WebSockets;
using System.Text;

namespace FintaChartsApi.Clients
{
    public class FintaChartsWebSocketClient : IDisposable
    {
        private ClientWebSocket? _ws;

        private readonly ILogger<FintaChartsWebSocketClient> _logger;
        // Семафор дозволяє лише одну операцію надсилання повідомлення за раз.
        // Це важливо для потокобезпеки та запобігання пошкодженню даних при одночасному надсиланні.
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<bool>? _connectionReadyTcs;
        private CancellationTokenSource? _receiveCts;


        public event Func<string, Task>? OnMessageReceived;
        public event Action? OnConnected;
        public event Action<WebSocketCloseStatus?, string?>? OnDisconnected;

        public WebSocketState State => _ws.State;

        public FintaChartsWebSocketClient(ILogger<FintaChartsWebSocketClient> logger)
        {
            _logger = logger;
        }

        private void InitializeWebSocketAndTcs()
        {
            _ws?.Dispose(); // Важливо: диспозуємо попередній екземпляр перед створенням нового
            _ws = new ClientWebSocket();
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            _connectionReadyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _receiveCts?.Dispose(); // Диспозуємо попередній, якщо є
            _receiveCts = new CancellationTokenSource(); // Створюємо новий для кожної сесії
        }
        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            InitializeWebSocketAndTcs(); // Завжди ініціалізуємо новий ClientWebSocket для нової спроби підключення
            _logger.LogInformation("Attempting to connect to WebSocket at {Uri}", uri);

            try
            {
                await _ws!.ConnectAsync(uri, cancellationToken); // _ws не буде null після InitializeWebSocketAndTcs

                if (_ws.State == WebSocketState.Open)
                {
                    _logger.LogInformation("Successfully connected to WebSocket.");
                    _connectionReadyTcs?.TrySetResult(true); // Сигналізуємо про успішне підключення
                    OnConnected?.Invoke(); // Сповіщаємо про підключення
                    _ = StartReceiving(_receiveCts!.Token); // Запускаємо прийом повідомлень у фоновому режимі, не чекаємо його
                }
                else
                {
                    _logger.LogWarning("WebSocket connection state is {State} after ConnectAsync. Setting TCS result to false.", _ws.State);
                    _connectionReadyTcs?.TrySetResult(false);
                    throw new InvalidOperationException($"WebSocket did not open. Current state: {_ws.State}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket connection attempt was canceled.");
                _connectionReadyTcs?.TrySetResult(false);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during WebSocket connection.");
                _connectionReadyTcs?.TrySetResult(false);
                throw;
            }
        }

        public async Task WaitUntilConnectedAsync(CancellationToken cancellationToken)
        {
            if (_ws == null || State != WebSocketState.Open)
            {
                if (_connectionReadyTcs == null || _connectionReadyTcs.Task.IsCompleted)
                {
                    _logger.LogWarning("WaitUntilConnectedAsync called when connection TaskCompletionSource is not pending. State: {State}", State);
                    throw new InvalidOperationException("WebSocket connection attempt was not properly initiated or already completed/failed. Call ConnectAsync first.");
                }

                _logger.LogDebug("Waiting for WebSocket connection to be ready...");
                await _connectionReadyTcs.Task.WaitAsync(cancellationToken);

                if (!_connectionReadyTcs.Task.Result)
                {
                    throw new InvalidOperationException("WebSocket connection failed to establish or was cancelled.");
                }
                _logger.LogDebug("WebSocket connection is ready.");
            }
        }
        private async Task StartReceiving(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192]; // Буфер для прийому частин повідомлення
            var messageBuffer = new List<byte>(); // Буфер для збирання повного повідомлення

            try
            {
                while (_ws != null && _ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("WebSocket receive operation was cancelled.");
                        break;
                    }
                    catch (WebSocketException wsEx)
                    {
                        _logger.LogError(wsEx, "WebSocket receive error: {Message}. Status: {Status}", wsEx.Message, wsEx.WebSocketErrorCode);
                        OnDisconnected?.Invoke(wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely ? WebSocketCloseStatus.NormalClosure : null, wsEx.Message);
                        break; // Вихід з циклу прийому при помилці
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An unexpected error occurred during WebSocket receive.");
                        OnDisconnected?.Invoke(null, ex.Message);
                        break;
                    }

                    messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                            _logger.LogInformation("Received WS message: {Message}", message);
                            if (OnMessageReceived != null)
                            {
                                await OnMessageReceived.Invoke(message);
                            }
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogInformation("WebSocket closed by remote host. Status: {Status}, Description: {Description}", result.CloseStatus, result.CloseStatusDescription);
                            OnDisconnected?.Invoke(result.CloseStatus, result.CloseStatusDescription);
                            break; // Виходимо з циклу прийому
                        }
                        messageBuffer.Clear(); // Очищаємо буфер для наступного повідомлення
                    }
                }
            }
            finally
            {
                // Цей блок виконається, коли цикл прийому завершиться (наприклад, з'єднання розірвано або скасовано)
                // Ми не ініціалізуємо новий ClientWebSocket тут, це завдання FintachartsWebSocketService.
                _logger.LogDebug("WebSocket receiving loop finished. Current state: {State}", _ws?.State);
            }
        }

        public async Task SendAsync(byte[] messageBytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            await _sendSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Чекаємо, доки з'єднання буде готове. Якщо з'єднання не вдасться, цей метод викине виняток.
                await WaitUntilConnectedAsync(cancellationToken);

                if (_ws == null || _ws.State != WebSocketState.Open) // _ws перевірка додана, хоча після WaitUntilConnectedAsync він має бути не null і Open
                {
                    _logger.LogError("Attempted to send message, but WebSocket is not in Open state after waiting. Current state: {State}", _ws?.State);
                    throw new InvalidOperationException($"WebSocket is not open for sending. Current state: {_ws?.State}");
                }

                await _ws.SendAsync(new ArraySegment<byte>(messageBytes), messageType, endOfMessage, cancellationToken);
                _logger.LogDebug("Sent WebSocket message (first 100 chars): {MessageContent}", Encoding.UTF8.GetString(messageBytes).Substring(0, Math.Min(messageBytes.Length, 100)));
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("WebSocket send operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WebSocket message.");
                throw;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public async Task<WebSocketReceiveResult> ReceiveMessageAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            // Необов'язково тут викликати WaitUntilConnectedAsync, оскільки StartReceiving вже робить це по суті,
            // і цей метод викликається з StartReceiving.
            // Проте, якщо цей метод може бути викликаний зовні, тоді це потрібно.
            // Для внутрішнього використання в StartReceiving можна припустити, що з'єднання відкрито.
            // await WaitUntilConnectedAsync(cancellationToken);

            if (_ws == null || _ws.State != WebSocketState.Open)
            {
                _logger.LogWarning("Attempted to receive message, but WebSocket is not open. Current state: {State}", _ws?.State);
                throw new InvalidOperationException($"WebSocket is not open for receiving. Current state: {_ws?.State}");
            }

            try
            {
                return await _ws.ReceiveAsync(buffer, cancellationToken);
            }
            catch (WebSocketException wsEx)
            {
                _logger.LogError(wsEx, "WebSocket receive error: {Message}. Status: {Status}", wsEx.Message, wsEx.WebSocketErrorCode);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("WebSocket receive operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during WebSocket receive.");
                throw;
            }
        }


        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (_ws == null) return; // Нічого закривати

            _receiveCts?.Cancel(); // Скасувати прийом повідомлень
            _receiveCts?.Dispose(); // Диспозувати CancellationTokenSource

            if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived)
            {
                _logger.LogInformation("Closing WebSocket connection. Current state: {State}", _ws.State);
                try
                {
                    await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    _logger.LogInformation("WebSocket connection closed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while cleanly closing WebSocket connection.");
                }
            }
            else
            {
                _logger.LogInformation("WebSocket is already in state {State}, no need to close.", _ws.State);
            }
        }
        public void Dispose()
        {
            _receiveCts?.Cancel();
            _receiveCts?.Dispose();
            _ws?.Dispose(); // Диспозуємо _ws, якщо він існує
            _sendSemaphore.Dispose();
            _logger.LogDebug("FintaChartsWebSocketClient disposed.");
            GC.SuppressFinalize(this);
        }
    }
}

// Додайте у L1DataProcessor перевірку:
