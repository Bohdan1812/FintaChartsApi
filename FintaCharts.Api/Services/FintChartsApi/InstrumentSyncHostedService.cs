using FintaChartsApi.Clients;
using FintaChartsApi.Data.Repositories.Interfaces;
using FintaChartsApi.Models.Data;
using FintaChartsApi.Models.FintaChartsApi.Instruments;
using Refit;

namespace FintaChartsApi.Services.FintChartsApi
{
    public class InstrumentSyncHostedService : IHostedService
    {
        private readonly ILogger<InstrumentSyncHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public InstrumentSyncHostedService(
            ILogger<InstrumentSyncHostedService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("InstrumentSyncHostedService is starting.");

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var fintachartsApiClient = scope.ServiceProvider.GetRequiredService<IFintaChartsApi>();
                    var instrumentRepository = scope.ServiceProvider.GetRequiredService<IInstrumentRepository>();
                    var providerRepository = scope.ServiceProvider.GetRequiredService<IProviderRepository>();

                    await SynchronizeProvidersAsync(fintachartsApiClient, providerRepository, cancellationToken);
                    await SynchronizeInstrumentsAsync(fintachartsApiClient, instrumentRepository, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initial synchronization failed during application startup. Error: {Message}", ex.Message);
            }
            _logger.LogInformation("Instrument Sync Hosted Service has finished initial synchronization.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("InstrumentSyncHostedService is stopping.");
            return Task.CompletedTask;
        }

        private async Task SynchronizeProvidersAsync(
            IFintaChartsApi fintachartsApiClient,
            IProviderRepository providerRepository, 
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting synchronization of providers from Fintacharts API.");
            try
            {
                var response = await fintachartsApiClient.GetProviders();
                var fintachartsProviderIds = response?.Data; //Якщо response не null, то беремо Data

                if (fintachartsProviderIds == null || !fintachartsProviderIds.Any())
                {
                    _logger.LogWarning("No providers received from Fintacharts API. Provider synchronization skipped.");
                    return;
                }

                _logger.LogInformation("Fetched {Count} provider IDs from Fintacharts API.", fintachartsProviderIds.Count);

                var dbresult = await providerRepository.GetAllAsync();
                var existingProviders = dbresult.ToDictionary(p => p.Id);


                var newProviders = new List<Provider>();
                // Оновлення не потрібне, оскільки ми не маємо полів для оновлення (окрім ID)
                // var providersToUpdate = new List<Provider>();

                foreach (var providerId in fintachartsProviderIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!existingProviders.ContainsKey(providerId))
                    {
                        newProviders.Add(new Provider
                        {
                            Id = providerId,
                        });
                    }
                }

                if (newProviders.Any())
                {
                    await providerRepository.AddRangeAsync(newProviders);
                    var changedRows = await providerRepository.SaveChangesAsync();

                    _logger.LogInformation("Added {Count} new providers to the database.", newProviders.Count);
                    _logger.LogInformation("Provider database changes saved successfully.");
                }
                else
                {
                    _logger.LogInformation("No new providers found. Provider database is up to date.");
                }

            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Provider synchronization was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during provider synchronization: {Message}", ex.Message);
                throw;
            }
        }


        private async Task SynchronizeInstrumentsAsync(
            IFintaChartsApi fintachartsApiClient,
            IInstrumentRepository instrumentRepository, 
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting paginated instrument data synchronization from Fintacharts API.");

            int currentPage = 1;
            int totalPages = 1; // Буде оновлено після першого запиту
            const int pageSize = 100; // Це максимальна кількість елементів, яку ви хочете отримати за раз

            var allNewInstruments = new List<Instrument>();
            var instrumentsToUpdate = new List<Instrument>();
            var existingInstruments = (await instrumentRepository.GetAllAsync()).ToDictionary(i => i.Id);

            do
            {
                cancellationToken.ThrowIfCancellationRequested(); // Перевірка скасування на початку ітерації циклу пагінації

                InstrumentsResponse? apiResponse = null;

                try
                {
                    _logger.LogInformation("Fetching instruments page {CurrentPage} of {TotalPages} with page size {PageSize}.", currentPage, totalPages, pageSize); // Додав TotalPages до логу
                   
                    apiResponse = await fintachartsApiClient.GetInstruments(
                        new ListInstrumentsRequest
                        {
                            Page = currentPage,
                            Size = pageSize
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching instruments page {Page} from Fintacharts API: {Message}", currentPage, ex.Message);
                    throw;
                }

                if (apiResponse?.Data == null || !apiResponse.Data.Any())
                {
                    _logger.LogWarning("No data received for instruments page {Page}. Stopping synchronization.", currentPage);
                    
                    break;
                }

                if (apiResponse.Paging != null)
                {
                    totalPages = apiResponse.Paging.Pages;
                    _logger.LogInformation("Discovered {TotalPages} total pages and {TotalItems} total items for instruments.", apiResponse.Paging.Pages, apiResponse.Paging.Items);
                }
                else
                {
                    _logger.LogWarning("Paging information is missing from API response. Assuming single page response and stopping after current page.");
                    totalPages = currentPage;
                }

                foreach (var apiInstrumentDto in apiResponse.Data)
                {
                    cancellationToken.ThrowIfCancellationRequested(); // Перевірка скасування всередині циклу обробки елементів

                    // Перевірка на null Id, оскільки це ваш ключ
                    if (apiInstrumentDto.Id == null)
                    {
                        _logger.LogWarning("InstrumentDto with null Id received from Fintacharts API on page {Page}. Skipping this instrument.", currentPage);
                        continue;
                    }

                   
                    if (existingInstruments.TryGetValue(apiInstrumentDto.Id, out var existingInstrument))
                    {
                        
                        if (existingInstrument.Symbol != apiInstrumentDto.Symbol ||
                            existingInstrument.Description != apiInstrumentDto.Description ||
                            existingInstrument.Kind != apiInstrumentDto.Kind ||
                            existingInstrument.Currency != apiInstrumentDto.Currency ||
                            existingInstrument.BaseCurrency != apiInstrumentDto.BaseCurrency ||
                            existingInstrument.TickSize != apiInstrumentDto.TickSize)
                        {
                            existingInstrument.Symbol = apiInstrumentDto.Symbol ?? string.Empty;
                            existingInstrument.Description = apiInstrumentDto.Description ?? string.Empty;
                            existingInstrument.Kind = apiInstrumentDto.Kind ?? string.Empty;
                            existingInstrument.Currency = apiInstrumentDto.Currency ?? string.Empty;
                            existingInstrument.BaseCurrency = apiInstrumentDto.BaseCurrency ?? string.Empty;
                            existingInstrument.TickSize = apiInstrumentDto.TickSize;
                            instrumentsToUpdate.Add(existingInstrument);
                        }
                    }
                    else
                    {
                        
                        allNewInstruments.Add(new Instrument
                        {
                            Id = apiInstrumentDto.Id,
                            Symbol = apiInstrumentDto.Symbol ?? string.Empty,
                            Description = apiInstrumentDto.Description ?? string.Empty,
                            Kind = apiInstrumentDto.Kind ?? string.Empty,
                            Currency = apiInstrumentDto.Currency ?? string.Empty,
                            BaseCurrency = apiInstrumentDto.BaseCurrency ?? string.Empty,
                            TickSize = apiInstrumentDto.TickSize,
                        });
                    }
                }

                currentPage++; // Переходимо до наступної сторінки для наступної ітерації циклу

            } while (currentPage <= totalPages && !cancellationToken.IsCancellationRequested); // Оновлена умова циклу

           
            bool changesMade = false;
            if (allNewInstruments.Any())
            {
                await instrumentRepository.AddRangeAsync(allNewInstruments); 
                _logger.LogInformation("Added {Count} new instruments to the database.", allNewInstruments.Count);
                changesMade = true;
            }
            if (instrumentsToUpdate.Any())
            {
                await instrumentRepository.UpdateRangeAsync(instrumentsToUpdate); 
                _logger.LogInformation("Updated {Count} existing instruments in the database.", instrumentsToUpdate.Count);
                changesMade = true;
            }

            // SaveChangesAsync викликається ОДИН РАЗ, якщо були зміни.
            if (changesMade)
            {
                await instrumentRepository.SaveChangesAsync();
                _logger.LogInformation("Instrument database changes saved successfully.");
            }
            else
            {
                _logger.LogInformation("No new or updated instruments found. Instrument database is up to date.");
            }

            _logger.LogInformation("Instrument data synchronization completed.");
        }
    }
}



