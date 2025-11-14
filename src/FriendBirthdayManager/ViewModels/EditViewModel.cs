using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.ViewModels;

/// <summary>
/// 編集画面のViewModel
/// </summary>
public partial class EditViewModel : ObservableObject
{
    private readonly IFriendRepository _friendRepository;
    private readonly ILogger<EditViewModel> _logger;
    private int? _friendId;

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
    private int _notifyDaysBeforeIndex = 0;

    [ObservableProperty]
    private bool _notifyEnabled = true;

    [ObservableProperty]
    private bool _notifySoundEnabled = true;

    [ObservableProperty]
    private ObservableCollection<AliasItem> _aliases = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public EditViewModel(IFriendRepository friendRepository, ILogger<EditViewModel> logger)
    {
        _friendRepository = friendRepository;
        _logger = logger;
    }

    public async Task LoadFriendAsync(int friendId)
    {
        try
        {
            _friendId = friendId;
            var friend = await _friendRepository.GetByIdAsync(friendId);

            if (friend == null)
            {
                _logger.LogWarning("Friend not found: {FriendId}", friendId);
                return;
            }

            Name = friend.Name;
            BirthYear = friend.BirthYear?.ToString();
            BirthMonth = friend.BirthMonth?.ToString();
            BirthDay = friend.BirthDay?.ToString();
            Memo = friend.Memo;
            NotifyEnabled = friend.NotifyEnabled;
            NotifySoundEnabled = friend.NotifySoundEnabled ?? true;

            // NotifyDaysBeforeを NotifyDaysBeforeIndex に変換
            if (friend.NotifyDaysBefore.HasValue)
            {
                var notifyDaysMapping = new[] { 0, 0, 1, 2, 3, 5, 7, 14, 30 };
                var index = Array.IndexOf(notifyDaysMapping, friend.NotifyDaysBefore.Value);
                NotifyDaysBeforeIndex = index >= 0 ? index : 0;
            }
            else
            {
                NotifyDaysBeforeIndex = 0; // デフォルト
            }

            // エイリアスを読み込み
            Aliases.Clear();
            foreach (var alias in friend.Aliases)
            {
                Aliases.Add(new AliasItem { Value = alias.AliasName });
            }

            _logger.LogInformation("Loaded friend for editing: {FriendName} (ID: {FriendId})", friend.Name, friendId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load friend for editing: {FriendId}", friendId);
        }
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
            if (_friendId == null)
            {
                _logger.LogWarning("Cannot save: FriendId is null");
                return;
            }

            _logger.LogInformation("Saving friend: {FriendId}", _friendId.Value);

            // バリデーション
            if (string.IsNullOrWhiteSpace(Name))
            {
                StatusMessage = "エラー: 名前を入力してください";
                _logger.LogWarning("Validation failed: Name is required");
                System.Windows.MessageBox.Show(
                    "名前を入力してください。",
                    "入力エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 誕生日のパース
            int? birthYear = null;
            int? birthMonth = null;
            int? birthDay = null;

            if (!string.IsNullOrWhiteSpace(BirthYear))
            {
                if (!int.TryParse(BirthYear, out var year) || year < 1900 || year > 2100)
                {
                    StatusMessage = "エラー: 誕生年が無効です（1900-2100）";
                    _logger.LogWarning("Validation failed: Invalid birth year: {BirthYear}", BirthYear);
                    System.Windows.MessageBox.Show(
                        "誕生年は1900～2100の範囲で入力してください。",
                        "入力エラー",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                birthYear = year;
            }

            if (!string.IsNullOrWhiteSpace(BirthMonth))
            {
                if (!int.TryParse(BirthMonth, out var month) || month < 1 || month > 12)
                {
                    StatusMessage = "エラー: 誕生月が無効です（1-12）";
                    _logger.LogWarning("Validation failed: Invalid birth month: {BirthMonth}", BirthMonth);
                    System.Windows.MessageBox.Show(
                        "誕生月は1～12の範囲で入力してください。",
                        "入力エラー",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                birthMonth = month;
            }

            if (!string.IsNullOrWhiteSpace(BirthDay))
            {
                if (!int.TryParse(BirthDay, out var day) || day < 1 || day > 31)
                {
                    StatusMessage = "エラー: 誕生日が無効です（1-31）";
                    _logger.LogWarning("Validation failed: Invalid birth day: {BirthDay}", BirthDay);
                    System.Windows.MessageBox.Show(
                        "誕生日は1～31の範囲で入力してください。",
                        "入力エラー",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                birthDay = day;
            }

            // 既存の友人情報を取得
            var friend = await _friendRepository.GetByIdAsync(_friendId.Value);
            if (friend == null)
            {
                StatusMessage = "エラー: 友人情報が見つかりません";
                _logger.LogError("Friend not found for saving: {FriendId}", _friendId);
                System.Windows.MessageBox.Show(
                    "友人情報が見つかりませんでした。",
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            // 友人情報を更新
            friend.Name = Name.Trim();
            friend.BirthYear = birthYear;
            friend.BirthMonth = birthMonth;
            friend.BirthDay = birthDay;
            friend.Memo = string.IsNullOrWhiteSpace(Memo) ? null : Memo.Trim();
            friend.NotifyEnabled = NotifyEnabled;
            friend.NotifySoundEnabled = NotifySoundEnabled;

            // NotifyDaysBeforeIndexをNotifyDaysBefore（日数）に変換
            // 0=デフォルト, 1=通知無効, 2以降は具体的な日数
            if (NotifyDaysBeforeIndex == 1)
            {
                friend.NotifyEnabled = false;
                friend.NotifyDaysBefore = null; // デフォルト使用
            }
            else if (NotifyDaysBeforeIndex > 1)
            {
                var notifyDaysMapping = new[] { 0, 0, 1, 2, 3, 5, 7, 14, 30 };
                friend.NotifyDaysBefore = notifyDaysMapping[NotifyDaysBeforeIndex];
            }
            else
            {
                friend.NotifyDaysBefore = null; // デフォルト使用
            }

            // エイリアスを更新
            // EF Coreのトラッキング機能により、既存のエイリアスは自動的に削除される
            friend.Aliases.Clear();
            foreach (var aliasItem in Aliases.Where(a => !string.IsNullOrWhiteSpace(a.Value)))
            {
                friend.Aliases.Add(new Alias
                {
                    AliasName = aliasItem.Value.Trim(),
                    FriendId = friend.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 保存
            await _friendRepository.UpdateAsync(friend);

            StatusMessage = "保存しました";
            _logger.LogInformation("Friend saved successfully: {FriendName} (ID: {FriendId})", friend.Name, friend.Id);

            System.Windows.MessageBox.Show(
                "保存しました。",
                "保存完了",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            // ウィンドウを閉じる
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var window = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                window?.Close();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save friend: {FriendId}", _friendId);
            StatusMessage = "エラー: 保存に失敗しました";
            System.Windows.MessageBox.Show(
                $"保存に失敗しました:\n{ex.Message}",
                "エラー",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        try
        {
            if (_friendId == null)
            {
                _logger.LogWarning("Cannot delete: FriendId is null");
                return;
            }

            _logger.LogInformation("Deleting friend: {FriendId}", _friendId);

            // 削除確認ダイアログ
            var result = System.Windows.MessageBox.Show(
                $"「{Name}」を削除してもよろしいですか？\n\nこの操作は元に戻せません。",
                "削除の確認",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                _logger.LogInformation("Delete operation cancelled by user");
                return;
            }

            // 削除実行
            await _friendRepository.DeleteAsync(_friendId.Value);

            _logger.LogInformation("Friend deleted successfully: {FriendId}", _friendId);
            StatusMessage = "削除しました";

            // ウィンドウを閉じる
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var window = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                window?.Close();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete friend: {FriendId}", _friendId);
            StatusMessage = "エラー: 削除に失敗しました";
            System.Windows.MessageBox.Show(
                $"削除に失敗しました:\n{ex.Message}",
                "エラー",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
