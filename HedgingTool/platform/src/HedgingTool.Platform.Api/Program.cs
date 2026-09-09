using HedgingTool.Platform.Api.Components;
using HedgingTool.Platform.Application.MarketData.Import;
using HedgingTool.Platform.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<CsvMarketDataReader>();
builder.Services.AddSingleton<BloombergWideMarketDataReader>();
builder.Services.AddSingleton<IMarketDataFileReader, AutoDetectMarketDataFileReader>();
builder.Services.AddSingleton<MarketDataValidator>();
builder.Services.AddScoped<MarketDataImporter>();

var connectionString = builder.Configuration.GetConnectionString("Hedging")
    ?? Environment.GetEnvironmentVariable("HEDGING_DB_CONNECTION_STRING");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddPlatformInfrastructure(connectionString);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
