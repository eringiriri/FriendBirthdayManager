using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
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
    private string _statusMessage = "準備完了";

    public MainViewModel(
        IFriendRepository friendRepository,
        IServiceProvider serviceProvider,
        ILogger<MainViewModel> logger)
    {
        _friendRepository = friendRepository;
        _serviceProvider = serviceProvider;
        _logger = logger;

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
                StatusMessage = "エラー: 名前は必須です";
                MessageBox.Show("名前を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 友人オブジェクトの作成
            var friend = new Friend
            {
                Name = Name.Trim()
            };

            // 誕生年のパース
            if (!string.IsNullOrWhiteSpace(BirthYear))
            {
                if (int.TryParse(BirthYear, out var year) && year >= 1900 && year <= 2100)
                {
                    friend.BirthYear = year;
                }
                else
                {
                    StatusMessage = "エラー: 誕生年は1900-2100の範囲で入力してください";
                    MessageBox.Show("誕生年は1900-2100の範囲で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // 誕生月のパース
            if (!string.IsNullOrWhiteSpace(BirthMonth))
            {
                if (int.TryParse(BirthMonth, out var month) && month >= 1 && month <= 12)
                {
                    friend.BirthMonth = month;
                }
                else
                {
                    StatusMessage = "エラー: 誕生月は1-12の範囲で入力してください";
                    MessageBox.Show("誕生月は1-12の範囲で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // 誕生日のパース
            if (!string.IsNullOrWhiteSpace(BirthDay))
            {
                if (int.TryParse(BirthDay, out var day) && day >= 1 && day <= 31)
                {
                    friend.BirthDay = day;
                }
                else
                {
                    StatusMessage = "エラー: 誕生日は1-31の範囲で入力してください";
                    MessageBox.Show("誕生日は1-31の範囲で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // 日付の妥当性チェック
            if (friend.BirthMonth.HasValue && friend.BirthDay.HasValue)
            {
                try
                {
                    var testYear = friend.BirthYear ?? 2000;
                    _ = new DateTime(testYear, friend.BirthMonth.Value, friend.BirthDay.Value);
                }
                catch (ArgumentOutOfRangeException)
                {
                    StatusMessage = "エラー: 無効な日付です";
                    MessageBox.Show("指定された誕生月と誕生日の組み合わせは無効です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
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
                var notifyDaysMapping = new[] { 0, 0, 1, 2, 3, 5, 7, 14, 30 };
                friend.NotifyDaysBefore = notifyDaysMapping[NotifyDaysBeforeIndex];
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
            StatusMessage = $"友人 '{friend.Name}' を追加しました";

            MessageBox.Show($"友人 '{friend.Name}' を追加しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

            // フォームをクリア
            ClearForm();

            // 直近の誕生日を再読み込み
            await LoadUpcomingBirthdaysAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save friend");
            StatusMessage = "エラー: 友人の追加に失敗しました";
            MessageBox.Show($"友人の追加に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ShowList()
    {
        try
        {
            _logger.LogInformation("ShowList command executed");
            var listWindow = _serviceProvider.GetRequiredService<Views.ListWindow>();
            listWindow.Show();
            listWindow.Activate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show list window");
            MessageBox.Show("一覧画面の表示に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var upcomingFriends = await _friendRepository.GetUpcomingBirthdaysAsync(DateTime.Now, 5);

            UpcomingBirthdays.Clear();
            foreach (var friend in upcomingFriends)
            {
                var daysUntil = friend.CalculateDaysUntilBirthday(DateTime.Now);
                UpcomingBirthdays.Add(new UpcomingBirthdayItem
                {
                    Name = friend.Name,
                    BirthdayDisplay = friend.GetBirthdayDisplayString(),
                    DaysUntilDisplay = daysUntil.HasValue ? $"（あと {daysUntil.Value}日）" : string.Empty
                });
            }

            _logger.LogInformation("Loaded {Count} upcoming birthdays", UpcomingBirthdays.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load upcoming birthdays");
        }
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
    private string _name = string.Empty;

    [ObservableProperty]
    private string _birthdayDisplay = string.Empty;

    [ObservableProperty]
    private string _daysUntilDisplay = string.Empty;
}
