using System.Collections.Concurrent;

namespace FintaChartsApi.Services.WebSocket.Interfaces
{
    public interface ISubscriptionManager
    {
        Task SubscribeToInstrumentAsync(string instrumentId, string provider);
        Task UnsubscribeFromInstrumentAsync(string instrumentId, string provider);

        // Для зовнішнього доступу, якщо потрібно знати, на що підписані
        ConcurrentDictionary<(string InstrumentId, string Provider), bool> GetActiveSubscriptions();
    }
}
