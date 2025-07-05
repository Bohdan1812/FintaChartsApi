using System.Net.WebSockets;

namespace FintaChartsApi.Services.WebSocket.Interfaces
{
    public interface IFintachartsWebSocketService
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task CloseAsync(CancellationToken cancellationToken);
        Task SendMessageAsync(byte[] messageBytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);

        // Подія, яку інші сервіси можуть підписати, щоб отримувати сирі повідомлення
        event Func<string, CancellationToken, Task> OnRawMessageReceived;

        // Подія для сигналізації про відновлення з'єднання
        event Func<CancellationToken, Task> OnReconnected;

        WebSocketState State { get; }
    }
}
