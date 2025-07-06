using FintaChartsApi.Models.FintaChartsApi.Auth;
using FintaChartsApi.Models.FintaChartsApi.Exchanges;
using FintaChartsApi.Models.FintaChartsApi.FintachartsApiExplorer.Models.FintachartsApi;
using FintaChartsApi.Models.FintaChartsApi.Instruments;
using FintaChartsApi.Models.FintaChartsApi.Providers;
using Refit;

namespace FintaChartsApi.Clients
{
    public interface IFintaChartsApi
    {
        [Post("/identity/realms/{realm}/protocol/openid-connect/token")] // Оновлений шлях з {realm}
        [Headers("Content-Type: application/x-www-form-urlencoded")]
        Task<AuthTokenResponse>GetAuthToken(
           string realm, // Параметр для {realm} у шляху
           [Body(BodySerializationMethod.UrlEncoded)] AuthTokenRequest request);


        // Отримує список інструментів на основі заданих критеріїв фільтрації.
        [Get("/api/instruments/v1/instruments")]
        Task<InstrumentsResponse> GetInstruments([Query] ListInstrumentsRequest request);


        // Отримує список провайдерів, які надають фінансові інструменти.
        [Get("/api/instruments/v1/providers")]
        Task<ProvidersResponse> GetProviders();



        // Отримує список бірж на основі заданого провайдера.
        [Get("/api/instruments/v1/exchanges")]
        Task<ExchangesResponse> GetExchanges([Query] ListExchangesRequest request);
       
    }
}
