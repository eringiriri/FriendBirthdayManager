namespace FriendBirthdayManager.Services;

/// <summary>
/// タスクトレイアイコンサービスのインターフェース
/// </summary>
public interface ITrayIconService
{
    /// <summary>
    /// タスクトレイアイコンを初期化
    /// </summary>
    void Initialize();

    /// <summary>
    /// アイコンを更新
    /// </summary>
    void UpdateIcon(int? daysUntilNextBirthday);

    /// <summary>
    /// バルーンチップを表示
    /// </summary>
    void ShowBalloonTip(string title, string message);

    /// <summary>
    /// リソースを破棄
    /// </summary>
    void Dispose();
}
