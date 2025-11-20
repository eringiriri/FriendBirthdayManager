namespace FriendBirthdayManager.Services;

/// <summary>
/// CSV エクスポート/インポートサービスのインターフェース
/// </summary>
public interface ICsvService
{
    /// <summary>
    /// CSVファイルにエクスポート
    /// </summary>
    Task<bool> ExportAsync(string filePath);

    /// <summary>
    /// CSVファイルからインポート
    /// </summary>
    Task<ImportResult> ImportAsync(string filePath);
}

/// <summary>
/// インポート結果
/// </summary>
public record ImportResult(
    int SuccessCount,
    int UpdateCount,
    int FailureCount,
    List<string> Errors
);
