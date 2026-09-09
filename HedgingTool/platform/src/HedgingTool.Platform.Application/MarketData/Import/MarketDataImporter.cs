using HedgingTool.Platform.Application.MarketData.Repositories;

namespace HedgingTool.Platform.Application.MarketData.Import;

public sealed class MarketDataImporter
{
    private readonly IMarketDataFileReader _reader;
    private readonly MarketDataValidator _validator;
    private readonly IMarketDataRepository _repository;

    public MarketDataImporter(
        IMarketDataFileReader reader,
        MarketDataValidator validator,
        IMarketDataRepository repository)
    {
        _reader = reader;
        _validator = validator;
        _repository = repository;
    }

    public async Task<MarketDataImportResult> ImportAsync(
        string csvFilePath,
        CancellationToken cancellationToken = default)
    {
        var rows = await _reader.ReadAsync(csvFilePath, cancellationToken);
        var warnings = _validator.Validate(rows);

        var rowsToImport = rows
            .Where(row => row.OfficialClose is not null || row.Nav is not null)
            .ToArray();
        var writeResult = await _repository.ImportAsync(rowsToImport, cancellationToken);

        return new MarketDataImportResult(
            rows.Count,
            writeResult.InstrumentsCreated,
            writeResult.PricesWritten,
            writeResult.NavsWritten,
            warnings);
    }
}
