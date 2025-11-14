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
        ILogger<SettingsViewModel> logger)
    {
        _settingsRepository = settingsRepository;
        _startupService = startupService;
        _csvService = csvService;
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
            // TODO: CSV エクスポート（Phase 7で実装）
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export CSV");
        }
    }

    [RelayCommand]
    private async Task ImportCsvAsync()
    {
        try
        {
            _logger.LogInformation("Importing CSV...");
            // TODO: CSV インポート（Phase 7で実装）
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import CSV");
        }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Backing up database...");
            // TODO: データベースバックアップ（Phase 7で実装）
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup database");
        }
    }
}
