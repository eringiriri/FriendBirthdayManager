using FriendBirthdayManager.Models;

namespace FriendBirthdayManager.Data;

/// <summary>
/// 通知履歴のリポジトリインターフェース
/// </summary>
public interface INotificationHistoryRepository
{
    /// <summary>
    /// 通知履歴を追加
    /// </summary>
    Task AddAsync(int friendId, string notificationDate);

    /// <summary>
    /// 通知済みかどうかを確認
    /// </summary>
    Task<bool> IsNotifiedAsync(int friendId, string notificationDate);

    /// <summary>
    /// 古い履歴を削除（30日より古いもの）
    /// </summary>
    Task CleanupOldHistoryAsync();
}
