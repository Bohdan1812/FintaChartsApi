namespace FintaChartsApi.Services.Authorization
{
    public interface ITokenProvider
    {
        /// <param name="forceRefresh">Якщо true, примусово отримує новий токен, ігноруючи кешований.</param>
        Task<string> GetAccessTokenAsync(bool forceRefresh = false);
    }
}
