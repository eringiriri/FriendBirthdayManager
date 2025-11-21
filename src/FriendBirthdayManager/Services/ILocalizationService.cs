namespace FriendBirthdayManager.Services;

/// <summary>
/// 多言語化サービスのインターフェース
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// 現在の言語を取得
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// 言語を変更する
    /// </summary>
    /// <param name="languageCode">言語コード (ja-JP, en-US, ko-KR)</param>
    void ChangeLanguage(string languageCode);

    /// <summary>
    /// リソース文字列を取得する
    /// </summary>
    /// <param name="key">リソースキー</param>
    /// <returns>ローカライズされた文字列</returns>
    string GetString(string key);

    /// <summary>
    /// システムの言語設定からデフォルト言語を取得する
    /// </summary>
    /// <returns>言語コード (ja-JP, en-US, ko-KR)</returns>
    string GetSystemLanguage();
}
