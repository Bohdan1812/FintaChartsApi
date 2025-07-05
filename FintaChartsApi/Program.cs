using FintaChartsApi.Clients;
using FintaChartsApi.Data;
using FintaChartsApi.Data.Repositories;
using FintaChartsApi.Data.Repositories.Interfaces;
using FintaChartsApi.Services.Authorization;
using FintaChartsApi.Services.FintChartsApi;
using FintaChartsApi.Services.WebSocket;
using FintaChartsApi.Services.WebSocket.Interfaces;
using Microsoft.EntityFrameworkCore;
using Refit;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .Build())
        .Enrich.FromLogContext() // Додає корисну інформацію про потік логування
        .CreateLogger();

    // Логуємо початок запуску додатка
    Log.Information("Starting FintaChartsApi web host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    //builder.Services.AddScoped<IGenericRepository, GenericRepository>();
    builder.Services.AddScoped<IInstrumentRepository, InstrumentRepository>();
    builder.Services.AddScoped<IProviderRepository, ProviderRepository>();
    builder.Services.AddScoped<IInstrumentPriceRepository, InstrumentPriceRepository>();

    var fintachartsConfig = builder.Configuration.GetSection("Fintacharts");

    var apiBaseUrl = fintachartsConfig["ApiBaseUrl"] ?? throw new InvalidOperationException("Fintacharts:ApiBaseUrl is not configured.");

    // Add services to the container.
    
    builder.Services.AddSingleton<ITokenProvider, FintaChartsTokenProvider>();
    builder.Services.AddSingleton<FintaChartsWebSocketClient>();

    builder.Services.AddSingleton<IFintachartsWebSocketService, FintachartsWebSocketService>();
    builder.Services.AddHostedService(provider => (FintachartsWebSocketService)provider.GetRequiredService<IFintachartsWebSocketService>());
    builder.Services.AddSingleton<L1DataProcessor>();
    builder.Services.AddSingleton<IL1StorageService, L1StorageService>();
    builder.Services.AddSingleton<ISubscriptionManager, SubscriptionManager>();


    builder.Services.AddScoped<AuthTokenHandler>();


    builder.Services
        .AddRefitClient<IFintaChartsApi>()
        .ConfigureHttpClient(c =>
        {
            c.BaseAddress = new Uri(apiBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(30);
        })
        // Додаємо наш AuthTokenHandler для автоматичного додавання токена авторизації
        .AddHttpMessageHandler<AuthTokenHandler>()
        .SetHandlerLifetime(TimeSpan.FromMinutes(5));

    // Реєстрація Hosted Service
    builder.Services.AddHostedService<InstrumentSyncHostedService>();

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var app = builder.Build();

    var _ = app.Services.GetRequiredService<L1DataProcessor>();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    // Логуємо фатальні помилки, які виникли до ініціалізації хоста
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    // Гарантуємо, що всі логи будуть записані перед завершенням програми
    Log.CloseAndFlush();
}