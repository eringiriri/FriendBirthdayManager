using System.ComponentModel.DataAnnotations;

namespace FriendBirthdayManager.Models;

/// <summary>
/// 友人情報を表すモデルクラス
/// </summary>
public class Friend
{
    /// <summary>
    /// 固有ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 友人の名前（必須）
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// 誕生年（例: 2000）
    /// </summary>
    public int? BirthYear { get; set; }

    /// <summary>
    /// 誕生月（1-12）
    /// </summary>
    public int? BirthMonth { get; set; }

    /// <summary>
    /// 誕生日（1-31）
    /// </summary>
    public int? BirthDay { get; set; }

    /// <summary>
    /// メモ
    /// </summary>
    [MaxLength(5000)]
    public string? Memo { get; set; }

    /// <summary>
    /// 個人通知設定（NULL = デフォルト使用）
    /// </summary>
    public int? NotifyDaysBefore { get; set; }

    /// <summary>
    /// 通知有効フラグ
    /// </summary>
    public bool NotifyEnabled { get; set; } = true;

    /// <summary>
    /// 音声通知（NULL = デフォルト）
    /// </summary>
    public bool? NotifySoundEnabled { get; set; }

    /// <summary>
    /// 作成日時（ISO 8601）
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新日時（ISO 8601）
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// エイリアスのコレクション
    /// </summary>
    public ICollection<Alias> Aliases { get; set; } = new List<Alias>();

    /// <summary>
    /// 月と日の両方が入力されている場合のみ通知対象
    /// </summary>
    public bool HasValidBirthdayForNotification()
    {
        return BirthMonth.HasValue && BirthDay.HasValue;
    }

    /// <summary>
    /// 誕生日までの日数を計算
    /// </summary>
    /// <param name="referenceDate">基準日</param>
    /// <returns>日数（月日が未設定の場合はnull）</returns>
    public int? CalculateDaysUntilBirthday(DateTime referenceDate)
    {
        if (!HasValidBirthdayForNotification()) return null;

        int year = referenceDate.Year;
        int month = BirthMonth!.Value;
        int day = BirthDay!.Value;

        // うるう年処理: 2月29日生まれで平年の場合は2月28日にフォールバック
        if (month == 2 && day == 29 && !DateTime.IsLeapYear(year))
        {
            day = 28;
        }

        try
        {
            var nextBirthday = new DateTime(year, month, day);

            if (nextBirthday.Date < referenceDate.Date)
            {
                // 来年の誕生日を計算（うるう年処理を再適用）
                year++;
                if (month == 2 && BirthDay == 29 && !DateTime.IsLeapYear(year))
                {
                    day = 28;
                }
                else
                {
                    day = BirthDay!.Value;
                }
                nextBirthday = new DateTime(year, month, day);
            }

            return (nextBirthday.Date - referenceDate.Date).Days;
        }
        catch (ArgumentOutOfRangeException)
        {
            // 無効な日付の場合はnullを返す
            return null;
        }
    }

    /// <summary>
    /// 表示用の誕生日文字列を生成
    /// </summary>
    public string GetBirthdayDisplayString()
    {
        if (BirthYear.HasValue && BirthMonth.HasValue && BirthDay.HasValue)
            return $"{BirthYear:0000}-{BirthMonth:00}-{BirthDay:00}";
        if (BirthMonth.HasValue && BirthDay.HasValue)
            return $"{BirthMonth:00}-{BirthDay:00}";
        if (BirthYear.HasValue)
            return $"{BirthYear}年";
        if (BirthMonth.HasValue)
            return $"{BirthMonth}月";
        if (BirthDay.HasValue)
            return $"{BirthDay}日";
        return "未設定";
    }
}
