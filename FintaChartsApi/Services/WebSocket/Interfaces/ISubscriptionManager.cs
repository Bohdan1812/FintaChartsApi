using System.Collections.Concurrent;

namespace FintaChartsApi.Services.WebSocket.Interfaces
{
    public interface ISubscriptionManager
    {
        Task SubscribeToInstrumentAsync(string instrumentId, string provider);
        Task UnsubscribeFromInstrumentAsync(string instrumentId, string provider);

        // Додайте цей метод:
        ConcurrentDictionary<(string InstrumentId, string Provider), bool> GetActiveSubscriptions();
    }
}
