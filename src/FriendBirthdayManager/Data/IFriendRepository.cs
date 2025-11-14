using FriendBirthdayManager.Models;

namespace FriendBirthdayManager.Data;

/// <summary>
/// 友人情報のリポジトリインターフェース
/// </summary>
public interface IFriendRepository
{
    /// <summary>
    /// すべての友人を取得
    /// </summary>
    Task<List<Friend>> GetAllAsync();

    /// <summary>
    /// IDで友人を取得
    /// </summary>
    Task<Friend?> GetByIdAsync(int id);

    /// <summary>
    /// 友人を追加
    /// </summary>
    Task<int> AddAsync(Friend friend);

    /// <summary>
    /// 友人を更新
    /// </summary>
    Task UpdateAsync(Friend friend);

    /// <summary>
    /// 友人を削除
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// キーワードで検索（名前、エイリアス、メモ）
    /// </summary>
    Task<List<Friend>> SearchAsync(string keyword);

    /// <summary>
    /// 直近の誕生日を取得
    /// </summary>
    Task<List<Friend>> GetUpcomingBirthdaysAsync(DateTime referenceDate, int count);

    /// <summary>
    /// 通知対象の友人を取得
    /// </summary>
    Task<List<Friend>> GetNotificationTargetsAsync(DateTime targetDate, int daysBefore);
}
