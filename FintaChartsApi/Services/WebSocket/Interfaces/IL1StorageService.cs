using FintaChartsApi.Models.WebSocket;

namespace FintaChartsApi.Services.WebSocket.Interfaces
{
    public interface IL1StorageService
    {
        Task UpdateDatabaseAsync(L1Message l1Message, CancellationToken cancellationToken);
    }
}
