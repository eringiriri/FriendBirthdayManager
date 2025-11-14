using System.ComponentModel.DataAnnotations;

namespace FriendBirthdayManager.Models;

/// <summary>
/// エイリアス（別名）を表すモデルクラス
/// </summary>
public class Alias
{
    /// <summary>
    /// エイリアスID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 友人ID
    /// </summary>
    public int FriendId { get; set; }

    /// <summary>
    /// エイリアス
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string AliasName { get; set; }

    /// <summary>
    /// 作成日時（ISO 8601）
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation property
    /// <summary>
    /// 関連する友人
    /// </summary>
    public Friend Friend { get; set; } = null!;
}
