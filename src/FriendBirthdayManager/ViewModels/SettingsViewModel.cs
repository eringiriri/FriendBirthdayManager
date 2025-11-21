using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
using FriendBirthdayManager.Services;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.ViewModels;

/// <summary>
/// 設定画面のViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IStartupService _startupService;
    private readonly ICsvService _csvService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private int _notificationHour = 12;

    [ObservableProperty]
    private int _notificationMinute = 0;

    [ObservableProperty]
    private int _defaultNotifyDaysBefore = 1;

    [ObservableProperty]
    private bool _defaultNotifySound = true;

    [ObservableProperty]
    private bool _startWithWindows = false;

    [ObservableProperty]
    private string _language = "ja-JP";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(
        ISettingsRepository settingsRepository,
        IStartupService startupService,
        ICsvService csvService,
        ILocalizationService localizationService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsRepository = settingsRepository;
        _startupService = startupService;
        _csvService = csvService;
        _localizationService = localizationService;
        _logger = logger;
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            _logger.LogInformation("Loading settings...");

            var settings = await _settingsRepository.GetAppSettingsAsync();

            NotificationHour = settings.NotificationTime.Hours;
            NotificationMinute = settings.NotificationTime.Minutes;
            DefaultNotifyDaysBefore = settings.DefaultNotifyDaysBefore;
            DefaultNotifySound = settings.DefaultNotifySound;
            StartWithWindows = settings.StartWithWindows;
            Language = settings.Language;

            StatusMessage = "設定を読み込みました";
            _logger.LogInformation("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            StatusMessage = "エラー: 設定の読み込みに失敗しました";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            _logger.LogInformation("Saving settings...");

            var settings = new AppSettings
            {
                NotificationTime = new TimeSpan(NotificationHour, NotificationMinute, 0),
                DefaultNotifyDaysBefore = DefaultNotifyDaysBefore,
                DefaultNotifySound = DefaultNotifySound,
                StartWithWindows = StartWithWindows,
                Language = Language
            };

            await _settingsRepository.SaveAppSettingsAsync(settings);

            // 言語設定の更新
            _localizationService.ChangeLanguage(Language);

            // スタートアップ登録の更新
            if (StartWithWindows)
            {
                await _startupService.RegisterAsync();
            }
            else
            {
                await _startupService.UnregisterAsync();
            }

            StatusMessage = "設定を保存しました";
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            StatusMessage = "エラー: 設定の保存に失敗しました";
        }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        try
        {
            _logger.LogInformation("Exporting CSV...");
            StatusMessage = "CSV エクスポート中...";

            // SaveFileDialogを使用してファイル保存先を選択
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "CSV エクスポート",
                Filter = "CSVファイル (*.csv)|*.csv",
                FileName = $"friends_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                DefaultExt = "csv",
                AddExtension = true
            };

            var result = saveFileDialog.ShowDialog();
            if (result != true)
            {
                StatusMessage = "CSV エクスポートがキャンセルされました";
                return;
            }

            var success = await _csvService.ExportAsync(saveFileDialog.FileName);

            if (success)
            {
                StatusMessage = $"CSV エクスポート完了: {saveFileDialog.FileName}";
                _logger.LogInformation("CSV exported successfully to {FilePath}", saveFileDialog.FileName);
            }
            else
            {
                StatusMessage = "エラー: CSV エクスポートに失敗しました";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export CSV");
            StatusMessage = "エラー: CSV エクスポートに失敗しました";
        }
    }

    [RelayCommand]
    private async Task ImportCsvAsync()
    {
        try
        {
            _logger.LogInformation("Importing CSV...");
            StatusMessage = "CSV インポート中...";

            // OpenFileDialogを使用してファイルを選択
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "CSV インポート",
                Filter = "CSVファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*",
                DefaultExt = "csv"
            };

            var result = openFileDialog.ShowDialog();
            if (result != true)
            {
                StatusMessage = "CSV インポートがキャンセルされました";
                return;
            }

            var importResult = await _csvService.ImportAsync(openFileDialog.FileName);

            if (importResult.Errors.Count > 0)
            {
                var errorMessage = string.Join("\n", importResult.Errors.Take(10)); // 最初の10件のエラーのみ表示
                if (importResult.Errors.Count > 10)
                {
                    errorMessage += $"\n...他 {importResult.Errors.Count - 10} 件のエラー";
                }

                _logger.LogWarning("CSV import completed with errors: {ErrorCount}", importResult.Errors.Count);
                StatusMessage = $"CSV インポート完了（新規: {importResult.SuccessCount}件、更新: {importResult.UpdateCount}件、失敗: {importResult.FailureCount}件）";

                // エラーメッセージをダイアログで表示
                System.Windows.MessageBox.Show(
                    $"インポート結果:\n\n新規登録: {importResult.SuccessCount}件\n更新: {importResult.UpdateCount}件\n失敗: {importResult.FailureCount}件\n\nエラー詳細:\n{errorMessage}",
                    "CSV インポート結果",
                    System.Windows.MessageBoxButton.OK,
                    importResult.FailureCount > 0 ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = $"CSV インポート完了: 新規 {importResult.SuccessCount}件、更新 {importResult.UpdateCount}件";
                _logger.LogInformation("CSV imported successfully: {SuccessCount} new, {UpdateCount} updated", importResult.SuccessCount, importResult.UpdateCount);

                System.Windows.MessageBox.Show(
                    $"新規登録: {importResult.SuccessCount}件\n更新: {importResult.UpdateCount}件",
                    "CSV インポート完了",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import CSV");
            StatusMessage = "エラー: CSV インポートに失敗しました";
            System.Windows.MessageBox.Show(
                $"CSV インポートに失敗しました:\n{ex.Message}",
                "エラー",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Backing up database...");
            StatusMessage = "データベースバックアップ中...";

            // データベースファイルのパスを取得
            var dbPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FriendBirthdayManager",
                "friends.db");

            if (!System.IO.File.Exists(dbPath))
            {
                StatusMessage = "エラー: データベースファイルが見つかりません";
                _logger.LogWarning("Database file not found: {DbPath}", dbPath);
                return;
            }

            // SaveFileDialogを使用してバックアップ先を選択
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "データベースバックアップ",
                Filter = "データベースファイル (*.db)|*.db|すべてのファイル (*.*)|*.*",
                FileName = $"friends_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                DefaultExt = "db",
                AddExtension = true
            };

            var result = saveFileDialog.ShowDialog();
            if (result != true)
            {
                StatusMessage = "バックアップがキャンセルされました";
                return;
            }

            // データベースファイルをコピー
            await Task.Run(() => System.IO.File.Copy(dbPath, saveFileDialog.FileName, true));

            StatusMessage = $"バックアップ完了: {saveFileDialog.FileName}";
            _logger.LogInformation("Database backed up to {FilePath}", saveFileDialog.FileName);

            System.Windows.MessageBox.Show(
                $"データベースをバックアップしました:\n{saveFileDialog.FileName}",
                "バックアップ完了",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup database");
            StatusMessage = "エラー: バックアップに失敗しました";
            System.Windows.MessageBox.Show(
                $"バックアップに失敗しました:\n{ex.Message}",
                "エラー",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
