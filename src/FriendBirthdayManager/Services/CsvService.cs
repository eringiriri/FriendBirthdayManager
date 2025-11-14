using FriendBirthdayManager.Data;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Services;

/// <summary>
/// CSV エクスポート/インポートサービスの実装
/// </summary>
public class CsvService : ICsvService
{
    private readonly IFriendRepository _friendRepository;
    private readonly ILogger<CsvService> _logger;

    public CsvService(IFriendRepository friendRepository, ILogger<CsvService> logger)
    {
        _friendRepository = friendRepository;
        _logger = logger;
    }

    public async Task<bool> ExportAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Exporting friends to CSV: {FilePath}", filePath);
            // TODO: CSV エクスポートの実装（Phase 7で実装）
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export CSV to {FilePath}", filePath);
            return false;
        }
    }

    public async Task<ImportResult> ImportAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Importing friends from CSV: {FilePath}", filePath);
            // TODO: CSV インポートの実装（Phase 7で実装）
            await Task.CompletedTask;
            return new ImportResult(0, 0, new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import CSV from {FilePath}", filePath);
            return new ImportResult(0, 0, new List<string> { ex.Message });
        }
    }
}
