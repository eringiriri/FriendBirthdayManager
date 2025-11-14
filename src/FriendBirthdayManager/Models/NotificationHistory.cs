using System.ComponentModel.DataAnnotations;

namespace FriendBirthdayManager.Models;

/// <summary>
/// 通知履歴を表すモデルクラス（重複通知防止用）
/// </summary>
public class NotificationHistory
{
    /// <summary>
    /// 履歴ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 友人ID
    /// </summary>
    public int FriendId { get; set; }

    /// <summary>
    /// 通知対象日（YYYY-MM-DD）
    /// </summary>
    [Required]
    [MaxLength(10)]
    public required string NotificationDate { get; set; }

    /// <summary>
    /// 通知実行日時（ISO 8601）
    /// </summary>
    public DateTime NotifiedAt { get; set; }

    // Navigation property
    /// <summary>
    /// 関連する友人
    /// </summary>
    public Friend Friend { get; set; } = null!;
}
