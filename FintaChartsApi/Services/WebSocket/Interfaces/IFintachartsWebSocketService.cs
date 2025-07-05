using System.Net.WebSockets;

namespace FintaChartsApi.Services.WebSocket.Interfaces
{
    public interface IFintachartsWebSocketService
    {
        // Подія для сповіщення про підключення WebSocket
        event Func<Task> OnWebSocketConnected; // Може бути Func<CancellationToken, Task> якщо токен потрібен

        // Подія для передачі сирих повідомлень від WebSocket
        event Func<string, CancellationToken, Task>? OnRawMessageReceived;

        WebSocketState State { get; }

        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        Task SendMessageAsync(byte[] messageBytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
    }
}
