namespace FriendBirthdayManager;

/// <summary>
/// アプリケーション全体で使用される定数
/// </summary>
public static class Constants
{
    /// <summary>
    /// 日付関連の定数
    /// </summary>
    public static class DateValidation
    {
        public const int MinYear = 1900;
        public const int MaxYear = 2100;
        public const int MinMonth = 1;
        public const int MaxMonth = 12;
        public const int MinDay = 1;
        public const int MaxDay = 31;
    }

    /// <summary>
    /// 通知設定関連の定数
    /// </summary>
    public static class Notification
    {
        /// <summary>
        /// 通知日数マッピング配列
        /// インデックス: 0=デフォルト, 1=通知無効, 2以降=具体的な日数
        /// </summary>
        public static readonly int[] DaysBeforeMapping = { 0, 0, 1, 2, 3, 5, 7, 14, 30 };
    }
}
