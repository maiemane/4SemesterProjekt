using Microsoft.Extensions.DependencyInjection;
using HedgingTool.Platform.Application.MarketData.Repositories;
using HedgingTool.Platform.Infrastructure.Configuration;
using HedgingTool.Platform.Infrastructure.Repositories;

namespace HedgingTool.Platform.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformInfrastructure(
        this IServiceCollection services,
        string sqlServerConnectionString)
    {
        var normalizedConnectionString = SqlServerConnectionStringConverter.FromJdbcIfNeeded(sqlServerConnectionString);

        services.AddSingleton<IMarketDataRepository>(_ =>
            new SqlServerMarketDataRepository(normalizedConnectionString));

        return services;
    }
}
