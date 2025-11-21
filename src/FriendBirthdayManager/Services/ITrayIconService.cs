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
    /// リポジトリから直近の誕生日を取得してアイコンを更新
    /// </summary>
    Task UpdateTrayIconFromRepositoryAsync();

    /// <summary>
    /// バルーンチップを表示
    /// </summary>
    void ShowBalloonTip(string title, string message);

    /// <summary>
    /// メニューとツールチップを更新（言語変更時に使用）
    /// </summary>
    void UpdateMenu();

    /// <summary>
    /// リソースを破棄
    /// </summary>
    void Dispose();
}
