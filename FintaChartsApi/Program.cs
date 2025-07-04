using Refit;
using FintaChartsApi.Clients;
using FintaChartsApi.Services.Authorization;

var builder = WebApplication.CreateBuilder(args);

var fintachartsConfig = builder.Configuration.GetSection("Fintacharts");

var apiBaseUrl = fintachartsConfig["ApiBaseUrl"] ?? throw new InvalidOperationException("Fintacharts:ApiBaseUrl is not configured.");

// Add services to the container.

builder.Services.AddSingleton<AuthTokenManager>();

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

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
