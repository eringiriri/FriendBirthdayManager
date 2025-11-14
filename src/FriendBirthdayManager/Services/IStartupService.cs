namespace FriendBirthdayManager.Services;

/// <summary>
/// Windowsスタートアップ登録サービスのインターフェース
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// スタートアップに登録されているか確認
    /// </summary>
    Task<bool> IsRegisteredAsync();

    /// <summary>
    /// スタートアップに登録
    /// </summary>
    Task<bool> RegisterAsync();

    /// <summary>
    /// スタートアップから登録解除
    /// </summary>
    Task<bool> UnregisterAsync();
}
