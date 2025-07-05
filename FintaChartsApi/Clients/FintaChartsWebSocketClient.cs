using System.Net.WebSockets;
using System.Text;

namespace FintaChartsApi.Clients
{
    public class FintaChartsWebSocketClient : IDisposable
    {
        private readonly ClientWebSocket _ws;
        private readonly ILogger<FintaChartsWebSocketClient> _logger;
        // Семафор дозволяє лише одну операцію надсилання повідомлення за раз.
        // Це важливо для потокобезпеки та запобігання пошкодженню даних при одночасному надсиланні.
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<bool>? _connectionReadyTcs;
        public WebSocketState State => _ws.State;

        public FintaChartsWebSocketClient(ILogger<FintaChartsWebSocketClient> logger)
        {
            _ws = new ClientWebSocket();
            _logger = logger;
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to connect to WebSocket at {Uri}", uri);

            try
            {
                await _ws.ConnectAsync(uri, cancellationToken);

                if(_ws.State == WebSocketState.Open)
                {
                    _logger.LogInformation("Successfully connected to WebSocket.");
                }
                else
                {
                    _logger.LogWarning("WebSocket connection state is {State} after ConnectAsync.", _ws.State);
                }
            }
            catch (WebSocketException wsEx)
            {
                _logger.LogError(wsEx, "WebSocket connection failed: {Message}. Status: {Status}", wsEx.Message, wsEx.WebSocketErrorCode);
                throw; 
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket connection attempt was canceled.");
                throw; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during WebSocket connection.");
                throw;
            }
        }   

        public async Task SendAsync(byte[] messageBytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {

            await _sendSemaphore.WaitAsync(cancellationToken);

            try
            {
                if(_ws.State == WebSocketState.Open)
                {
                    _logger.LogWarning("Attempted to send message, but WebSocket is not open. Current state: {State}", _ws.State);
                    throw new InvalidOperationException($"WebSocket is not open. Current state: {_ws.State}");
                }

                _ws.SendAsync(new ArraySegment<byte>(messageBytes), messageType, endOfMessage, cancellationToken);
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
                _sendSemaphore.Release(); // Завжди звільняємо семафор
            }
        }

        public async Task<WebSocketReceiveResult> ReceiveMessageAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                return await _ws.ReceiveAsync(buffer, cancellationToken);
            }
            catch (WebSocketException wsEx)
            {
                _logger.LogError(wsEx, "WebSocket receive error: {Message}. Status: {Status}", wsEx.Message, wsEx.WebSocketErrorCode);
                throw; // Передаємо виняток для подальшої обробки
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
            if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived)
            {
                _logger.LogInformation("Closing WebSocket connection. Current state: {State}", _ws.State);
                try
                {
                    // Ініціюємо закриття з боку клієнта
                    await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    // Чекаємо, доки сервер підтвердить закриття (або тайм-аут)
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    _logger.LogInformation("WebSocket connection closed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while cleanly closing WebSocket connection.");
                }
            }
        }
        public void Dispose()
        {
            _ws.Dispose();
            _sendSemaphore.Dispose();
            _logger.LogDebug("FintaChartsWebSocketClient disposed.");
        }
    }
}
