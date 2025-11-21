using System.Globalization;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Services;

/// <summary>
/// 多言語化サービスの実装
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private string _currentLanguage;

    public string CurrentLanguage => _currentLanguage;

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;
        _currentLanguage = GetSystemLanguage();
    }

    public void ChangeLanguage(string languageCode)
    {
        try
        {
            _logger.LogInformation("Changing language to: {Language}", languageCode);

            // 有効な言語コードかチェック
            if (!IsValidLanguage(languageCode))
            {
                _logger.LogWarning("Invalid language code: {Language}, using default", languageCode);
                languageCode = "ja-JP";
            }

            _currentLanguage = languageCode;

            // リソースディクショナリのパスを構築
            var resourcePath = $"/FriendBirthdayManager;component/Resources/Strings.{languageCode}.xaml";

            // 新しいリソースディクショナリを読み込む
            var newDict = new ResourceDictionary
            {
                Source = new Uri(resourcePath, UriKind.Relative)
            };

            // 既存のリソースディクショナリを削除
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existingDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString.Contains("Strings.") == true);

                if (existingDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(existingDict);
                }

                // 新しいリソースディクショナリを追加
                Application.Current.Resources.MergedDictionaries.Add(newDict);
            });

            _logger.LogInformation("Language changed successfully to: {Language}", languageCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change language to: {Language}", languageCode);
            throw;
        }
    }

    public string GetString(string key)
    {
        try
        {
            if (Application.Current.Resources.Contains(key))
            {
                return Application.Current.Resources[key]?.ToString() ?? key;
            }

            _logger.LogWarning("Resource key not found: {Key}", key);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource string for key: {Key}", key);
            return key;
        }
    }

    public string GetSystemLanguage()
    {
        try
        {
            var culture = CultureInfo.CurrentUICulture;
            var languageName = culture.Name;

            _logger.LogInformation("System language detected: {Language}", languageName);

            // システム言語に基づいて対応言語を選択
            if (languageName.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            {
                return "ja-JP";
            }
            else if (languageName.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            {
                return "ko-KR";
            }
            else if (languageName.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                     languageName.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                     languageName.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-TW";
            }
            else if (languageName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                return "en-US";
            }
            else if (languageName.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            {
                return "es-ES";
            }

            // デフォルトは日本語
            _logger.LogInformation("Unsupported system language, using default: ja-JP");
            return "ja-JP";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system language, using default: ja-JP");
            return "ja-JP";
        }
    }

    private bool IsValidLanguage(string languageCode)
    {
        var validLanguages = new[] { "ja-JP", "en-US", "ko-KR", "zh-TW", "es-ES" };
        return validLanguages.Contains(languageCode);
    }
}
