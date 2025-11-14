using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Services;

/// <summary>
/// タスクトレイアイコンサービスの実装
/// </summary>
public class TrayIconService : ITrayIconService, IDisposable
{
    private readonly ILogger<TrayIconService> _logger;
    private bool _disposed;

    public TrayIconService(ILogger<TrayIconService> logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing tray icon service...");
        // TODO: タスクトレイアイコンの初期化（Phase 4で実装）
    }

    public void UpdateIcon(int? daysUntilNextBirthday)
    {
        _logger.LogInformation("Updating tray icon, days until next birthday: {Days}", daysUntilNextBirthday);
        // TODO: アイコン更新の実装（Phase 4で実装）
    }

    public void ShowBalloonTip(string title, string message)
    {
        _logger.LogInformation("Showing balloon tip: {Title} - {Message}", title, message);
        // TODO: バルーンチップの実装（Phase 4で実装）
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing tray icon service...");
        // TODO: リソースのクリーンアップ（Phase 4で実装）

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
