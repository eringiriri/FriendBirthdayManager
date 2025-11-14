using System.ComponentModel.DataAnnotations;

namespace FriendBirthdayManager.Models;

/// <summary>
/// アプリケーション設定を表すモデルクラス
/// </summary>
public class Setting
{
    /// <summary>
    /// 設定キー
    /// </summary>
    [Key]
    [MaxLength(100)]
    public required string Key { get; set; }

    /// <summary>
    /// 設定値（JSON形式も可）
    /// </summary>
    [Required]
    public required string Value { get; set; }

    /// <summary>
    /// 更新日時（ISO 8601）
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// アプリケーション設定の具体的な値を保持するクラス
/// </summary>
public class AppSettings
{
    /// <summary>
    /// デフォルト通知日数（1-30）
    /// </summary>
    public int DefaultNotifyDaysBefore { get; set; } = 1;

    /// <summary>
    /// デフォルト音声通知（true/false）
    /// </summary>
    public bool DefaultNotifySound { get; set; } = true;

    /// <summary>
    /// 通知時刻（24時間形式）
    /// </summary>
    public TimeSpan NotificationTime { get; set; } = new TimeSpan(12, 0, 0);

    /// <summary>
    /// スタートアップ登録（true/false）
    /// </summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// 言語設定（"ja-JP", "en-US" など）
    /// </summary>
    public string Language { get; set; } = "ja-JP";
}
