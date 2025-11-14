using System.Timers;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using Timer = System.Timers.Timer;

namespace FriendBirthdayManager.Services;

/// <summary>
/// 通知サービスの実装
/// </summary>
public class NotificationService : INotificationService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationService> _logger;
    private Timer? _notificationTimer;
    private bool _disposed;

    public NotificationService(IServiceProvider serviceProvider, ILogger<NotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 通知サービスを開始
    /// </summary>
    public void Start()
    {
        try
        {
            _logger.LogInformation("Starting notification service...");

            // 初回チェックを実行
            _ = CheckAndNotifyAsync();

            // タイマーを設定（1時間ごとにチェック）
            _notificationTimer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds);
            _notificationTimer.Elapsed += async (s, e) => await CheckAndNotifyAsync();
            _notificationTimer.AutoReset = true;
            _notificationTimer.Start();

            _logger.LogInformation("Notification service started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start notification service");
        }
    }

    /// <summary>
    /// 通知サービスを停止
    /// </summary>
    public void Stop()
    {
        try
        {
            _logger.LogInformation("Stopping notification service...");

            _notificationTimer?.Stop();
            _notificationTimer?.Dispose();
            _notificationTimer = null;

            _logger.LogInformation("Notification service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop notification service");
        }
    }

    public async Task CheckAndNotifyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking for birthday notifications...");

            // スコープを作成してDbContextを取得
            using var scope = _serviceProvider.CreateScope();
            var friendRepository = scope.ServiceProvider.GetRequiredService<IFriendRepository>();
            var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            var notificationHistoryRepository = scope.ServiceProvider.GetRequiredService<INotificationHistoryRepository>();

            var settings = await settingsRepository.GetAppSettingsAsync();
            var now = DateTime.Now;
            var today = now.Date;

            // 設定された通知時刻かどうかをチェック
            var targetTime = settings.NotificationTime;
            var currentTime = now.TimeOfDay;

            // 通知時刻の前後30分以内であれば通知を実行
            var timeDiff = Math.Abs((currentTime - targetTime).TotalMinutes);
            if (timeDiff > 30)
            {
                _logger.LogInformation("Not notification time yet. Current: {Current}, Target: {Target}",
                    currentTime, targetTime);
                return;
            }

            // 通知対象の友人を取得
            var targets = await friendRepository.GetNotificationTargetsAsync(today, settings.DefaultNotifyDaysBefore);

            _logger.LogInformation("Found {Count} notification targets", targets.Count);

            var notificationCount = 0;
            foreach (var friend in targets)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var daysUntil = friend.CalculateDaysUntilBirthday(today) ?? 0;
                var notificationDate = today.ToString("yyyy-MM-dd");

                // 既に通知済みかチェック
                var isNotified = await notificationHistoryRepository.IsNotifiedAsync(friend.Id, notificationDate);
                if (isNotified)
                {
                    _logger.LogInformation("Already notified: {FriendName} on {Date}", friend.Name, notificationDate);
                    continue;
                }

                // 通知を表示
                var success = await ShowNotificationAsync(friend, daysUntil);
                if (success)
                {
                    // 通知履歴に記録
                    await notificationHistoryRepository.AddAsync(friend.Id, notificationDate);
                    notificationCount++;
                }
            }

            _logger.LogInformation("Notification check completed. Sent {Count} notifications", notificationCount);

            // 古い履歴をクリーンアップ
            await notificationHistoryRepository.CleanupOldHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check and notify");
        }
    }

    public async Task<bool> ShowNotificationAsync(Friend friend, int daysUntil)
    {
        try
        {
            _logger.LogInformation("Showing notification for {FriendName}, days until: {DaysUntil}",
                friend.Name, daysUntil);

            // トースト通知の内容を作成
            string title;
            string message;

            if (daysUntil == 0)
            {
                title = "🎉 今日は誕生日！";
                message = $"今日は{friend.Name}さんの誕生日です！";
            }
            else if (daysUntil == 1)
            {
                title = "🎂 明日は誕生日！";
                message = $"明日は{friend.Name}さんの誕生日です！";
            }
            else
            {
                title = "📅 誕生日が近づいています";
                message = $"{friend.Name}さんの誕生日まで あと{daysUntil}日です";
            }

            // 誕生日の表示
            var birthdayDisplay = friend.GetBirthdayDisplayString();

            // トースト通知を作成
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .AddText($"誕生日: {birthdayDisplay}")
                .AddButton(new ToastButton()
                    .SetContent("詳細を見る")
                    .AddArgument("action", "viewFriend")
                    .AddArgument("friendId", friend.Id.ToString()))
                .Show();

            _logger.LogInformation("Notification shown successfully for {FriendName}", friend.Name);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show notification for {FriendName}", friend.Name);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
