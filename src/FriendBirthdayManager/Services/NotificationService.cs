using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Services;

/// <summary>
/// 通知サービスの実装
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IServiceProvider serviceProvider, ILogger<NotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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

            var settings = await settingsRepository.GetAppSettingsAsync();
            var today = DateTime.Now.Date;

            // 通知対象の友人を取得
            var targets = await friendRepository.GetNotificationTargetsAsync(today, settings.DefaultNotifyDaysBefore);

            _logger.LogInformation("Found {Count} notification targets", targets.Count);

            foreach (var friend in targets)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var daysUntil = friend.CalculateDaysUntilBirthday(today) ?? 0;
                await ShowNotificationAsync(friend, daysUntil);
            }
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

            // TODO: Windowsトースト通知の実装（Phase 5で実装）
            // 現在はログ出力のみ

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show notification for {FriendName}", friend.Name);
            return false;
        }
    }
}
