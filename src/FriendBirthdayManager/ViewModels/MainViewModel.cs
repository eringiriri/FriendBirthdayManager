using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
using FriendBirthdayManager.Services;
using FriendBirthdayManager.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.ViewModels;

/// <summary>
/// メイン画面（友人追加）のViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IFriendRepository _friendRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _birthYear;

    [ObservableProperty]
    private string? _birthMonth;

    [ObservableProperty]
    private string? _birthDay;

    [ObservableProperty]
    private string? _memo;

    [ObservableProperty]
    private int _notifyDaysBeforeIndex = 0; // 0 = デフォルト

    [ObservableProperty]
    private ObservableCollection<AliasItem> _aliases = new();

    [ObservableProperty]
    private ObservableCollection<UpcomingBirthdayItem> _upcomingBirthdays = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public MainViewModel(
        IFriendRepository friendRepository,
        IServiceProvider serviceProvider,
        ILocalizationService localizationService,
        ILogger<MainViewModel> logger)
    {
        _friendRepository = friendRepository;
        _serviceProvider = serviceProvider;
        _localizationService = localizationService;
        _logger = logger;

        _statusMessage = _localizationService.GetString("MessageReady");

        // 直近の誕生日を読み込み
        _ = LoadUpcomingBirthdaysAsync();
    }

    [RelayCommand]
    private void AddAlias()
    {
        Aliases.Add(new AliasItem { Value = string.Empty });
    }

    [RelayCommand]
    private void RemoveAlias(AliasItem alias)
    {
        Aliases.Remove(alias);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            // バリデーション
            if (string.IsNullOrWhiteSpace(Name))
            {
                StatusMessage = _localizationService.GetString("MessageNameRequired");
                MessageBox.Show("名前を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 名前の重複チェック
            var trimmedName = Name.Trim();
            var existingFriends = await _friendRepository.GetAllAsync();
            if (existingFriends.Any(f => f.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = string.Format(_localizationService.GetString("MessageErrorNameExists"), trimmedName);
                MessageBox.Show(
                    $"「{trimmedName}」は既に登録されています。\n別の名前を入力してください。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                _logger.LogWarning("Duplicate friend name rejected: {FriendName}", trimmedName);
                return;
            }

            // 友人オブジェクトの作成
            var friend = new Friend
            {
                Name = trimmedName
            };

            // 誕生年のバリデーション
            var yearValidation = FriendValidator.ValidateBirthYear(BirthYear);
            if (!yearValidation.IsValid)
            {
                StatusMessage = _localizationService.GetString("MessageErrorBirthYearRange");
                MessageBox.Show(yearValidation.ErrorMessage, "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!string.IsNullOrWhiteSpace(BirthYear))
            {
                friend.BirthYear = int.Parse(BirthYear);
            }

            // 誕生月のバリデーション
            var monthValidation = FriendValidator.ValidateBirthMonth(BirthMonth);
            if (!monthValidation.IsValid)
            {
                StatusMessage = _localizationService.GetString("MessageErrorBirthMonthRange");
                MessageBox.Show(monthValidation.ErrorMessage, "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!string.IsNullOrWhiteSpace(BirthMonth))
            {
                friend.BirthMonth = int.Parse(BirthMonth);
            }

            // 誕生日のバリデーション
            var dayValidation = FriendValidator.ValidateBirthDay(BirthDay);
            if (!dayValidation.IsValid)
            {
                StatusMessage = _localizationService.GetString("MessageErrorBirthDayRange");
                MessageBox.Show(dayValidation.ErrorMessage, "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!string.IsNullOrWhiteSpace(BirthDay))
            {
                friend.BirthDay = int.Parse(BirthDay);
            }

            // 日付の組み合わせの妥当性チェック
            var combinationValidation = FriendValidator.ValidateBirthdateCombination(friend.BirthYear, friend.BirthMonth, friend.BirthDay);
            if (!combinationValidation.IsValid)
            {
                StatusMessage = _localizationService.GetString("MessageErrorInvalidDate");
                MessageBox.Show(combinationValidation.ErrorMessage, "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // メモ
            if (!string.IsNullOrWhiteSpace(Memo))
            {
                friend.Memo = Memo.Trim();
            }

            // 通知設定
            if (NotifyDaysBeforeIndex == 1)
            {
                friend.NotifyEnabled = false;
            }
            else if (NotifyDaysBeforeIndex > 1)
            {
                friend.NotifyDaysBefore = FriendValidator.ConvertNotifyIndexToDays(NotifyDaysBeforeIndex);
            }

            // エイリアスを追加
            foreach (var aliasItem in Aliases.Where(a => !string.IsNullOrWhiteSpace(a.Value)))
            {
                friend.Aliases.Add(new Alias
                {
                    AliasName = aliasItem.Value!.Trim(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            // データベースに保存
            var friendId = await _friendRepository.AddAsync(friend);

            _logger.LogInformation("Friend added successfully: {FriendName} (ID: {FriendId})", friend.Name, friendId);
            StatusMessage = string.Format(_localizationService.GetString("MessageFriendAdded"), friend.Name);

            MessageBox.Show($"友人 '{friend.Name}' を追加しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

            // フォームをクリア
            ClearForm();

            // 直近の誕生日を再読み込み
            await LoadUpcomingBirthdaysAsync();

            // タスクトレイアイコンを更新
            var trayIconService = _serviceProvider.GetService<ITrayIconService>();
            if (trayIconService != null)
            {
                await trayIconService.UpdateTrayIconFromRepositoryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save friend");
            StatusMessage = _localizationService.GetString("MessageErrorAddFriend");
            MessageBox.Show($"友人の追加に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ShowList()
    {
        try
        {
            _logger.LogInformation("ShowList command executed");

            var listWindow = Application.Current.Windows.OfType<Views.ListWindow>().FirstOrDefault();
            if (listWindow == null)
            {
                listWindow = _serviceProvider.GetRequiredService<Views.ListWindow>();
                listWindow.Show();
            }
            else
            {
                listWindow.Show();
                listWindow.WindowState = WindowState.Normal;
                listWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show list window");
            MessageBox.Show("一覧画面の表示に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task EditFriend(UpcomingBirthdayItem friend)
    {
        try
        {
            _logger.LogInformation("Edit friend from upcoming list: {FriendId}", friend.Id);

            // 既に同じ友人の編集ウィンドウが開いているかチェック
            var existingWindow = Application.Current.Windows.OfType<Views.EditWindow>()
                .FirstOrDefault(w => w.FriendId == friend.Id);
            if (existingWindow != null)
            {
                existingWindow.Activate();
                _logger.LogInformation("Edit window already open for friend: {FriendId}", friend.Id);
                return;
            }

            var editWindow = _serviceProvider.GetRequiredService<Views.EditWindow>();

            await editWindow.LoadFriendAsync(friend.Id);

            editWindow.Show();
            editWindow.Activate();

            // 編集ウィンドウが閉じられたら直近の誕生日を再読み込み
            editWindow.Closed += async (sender, args) => await LoadUpcomingBirthdaysAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show edit window or load friend data: {FriendId}", friend.Id);
        }
    }

    private void ClearForm()
    {
        Name = string.Empty;
        BirthYear = null;
        BirthMonth = null;
        BirthDay = null;
        Memo = null;
        NotifyDaysBeforeIndex = 0;
        Aliases.Clear();
    }

    private async Task LoadUpcomingBirthdaysAsync()
    {
        try
        {
            var upcomingFriends = await _friendRepository.GetUpcomingBirthdaysAsync(DateTime.Now, int.MaxValue);

            UpcomingBirthdays.Clear();
            foreach (var friend in upcomingFriends)
            {
                var daysUntil = friend.CalculateDaysUntilBirthday(DateTime.Now);
                UpcomingBirthdays.Add(new UpcomingBirthdayItem
                {
                    Id = friend.Id,
                    Name = friend.Name,
                    BirthdayDisplay = friend.GetBirthdayDisplayString(),
                    DaysUntilDisplay = daysUntil.HasValue ? string.Format(_localizationService.GetString("DaysUntilFormat"), daysUntil.Value) : string.Empty
                });
            }

            _logger.LogInformation("Loaded {Count} upcoming birthdays", UpcomingBirthdays.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load upcoming birthdays");
        }
    }

    public async Task RefreshUpcomingBirthdaysAsync()
    {
        await LoadUpcomingBirthdaysAsync();
    }
}

/// <summary>
/// エイリアス入力用のアイテム
/// </summary>
public partial class AliasItem : ObservableObject
{
    [ObservableProperty]
    private string? _value;
}

/// <summary>
/// 直近の誕生日表示用のアイテム
/// </summary>
public partial class UpcomingBirthdayItem : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _birthdayDisplay = string.Empty;

    [ObservableProperty]
    private string _daysUntilDisplay = string.Empty;
}
