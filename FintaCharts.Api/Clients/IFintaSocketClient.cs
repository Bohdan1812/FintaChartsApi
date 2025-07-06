using FintaChartsApi.Models.WebSocket;

namespace FintaChartsApi.Clients
{
    public interface IFintaSocketClient
    {
        Task<L1Message?> SubscribeOnceAsync(string instrumentId, string provider, TimeSpan timeout);

    }
}
