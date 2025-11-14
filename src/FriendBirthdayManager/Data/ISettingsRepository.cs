using FriendBirthdayManager.Models;

namespace FriendBirthdayManager.Data;

/// <summary>
/// 設定情報のリポジトリインターフェース
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// 設定値を取得
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// 設定値を保存
    /// </summary>
    Task SetAsync(string key, string value);

    /// <summary>
    /// アプリケーション設定を取得
    /// </summary>
    Task<AppSettings> GetAppSettingsAsync();

    /// <summary>
    /// アプリケーション設定を保存
    /// </summary>
    Task SaveAppSettingsAsync(AppSettings settings);

    /// <summary>
    /// スキーマバージョンを取得
    /// </summary>
    Task<int> GetSchemaVersionAsync();

    /// <summary>
    /// スキーマバージョンを設定
    /// </summary>
    Task SetSchemaVersionAsync(int version);
}
