using FriendBirthdayManager.Models;

namespace FriendBirthdayManager.Services;

/// <summary>
/// 通知サービスのインターフェース
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 通知対象をチェックして通知を送信
    /// </summary>
    Task CheckAndNotifyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定した友人に通知を表示
    /// </summary>
    Task<bool> ShowNotificationAsync(Friend friend, int daysUntil);
}
