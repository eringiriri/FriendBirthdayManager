using System.Collections.ObjectModel;
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
/// 編集画面のViewModel
/// </summary>
public partial class EditViewModel : ObservableObject
{
    private readonly IFriendRepository _friendRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IServiceProvider _serviceProvider;
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

    public EditViewModel(
        IFriendRepository friendRepository,
        ILocalizationService localizationService,
        IServiceProvider serviceProvider,
        ILogger<EditViewModel> logger)
    {
        _friendRepository = friendRepository;
        _localizationService = localizationService;
        _serviceProvider = serviceProvider;
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
            NotifyDaysBeforeIndex = FriendValidator.ConvertNotifyDaysToIndex(friend.NotifyDaysBefore);

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
                StatusMessage = _localizationService.GetString("MessageNameRequired");
                _logger.LogWarning("Validation failed: Name is required");
                System.Windows.MessageBox.Show(
                    "名前を入力してください。",
                    "入力エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 名前の重複チェック（自分以外）
            var trimmedName = Name.Trim();
            var existingFriends = await _friendRepository.GetAllAsync();
            if (existingFriends.Any(f => f.Id != _friendId && f.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = string.Format(_localizationService.GetString("MessageErrorNameExists"), trimmedName);
                _logger.LogWarning("Duplicate friend name rejected in edit: {FriendName}", trimmedName);
                System.Windows.MessageBox.Show(
                    $"「{trimmedName}」は既に登録されています。\n別の名前を入力してください。",
                    "入力エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 誕生日のバリデーション
            int? birthYear = null;
            int? birthMonth = null;
            int? birthDay = null;

            var yearValidation = FriendValidator.ValidateBirthYear(BirthYear);
            if (!yearValidation.IsValid)
            {
                StatusMessage = _localizationService.GetString("MessageErrorBirthYearRange");
                _logger.LogWarning("Validation failed: Invalid birth year: {BirthYear}", BirthYear);
                System.Windows.MessageBox.Show(
                    yearValidation.ErrorMessage,
                    "入力エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (!string.IsNullOrWhiteSpace(BirthYear))
            {
                birthYear = int.Parse(BirthYear);
            }

            var monthValidation = FriendValidator.ValidateBirthMonth(BirthMonth);
            if (!monthValidation.IsValid)
            {
                StatusMessage = _localizationService.GetString("MessageErrorBirthMonthRange");
                _logger.LogWarning("Validation failed: Invalid birth month: {BirthMonth}", BirthMonth);
                System.Windows.MessageBox.Show(
                    monthValidation.ErrorMessage,
                    "入力エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (!string.IsNullOrWhiteSpace(BirthMonth))
            {
                birthMonth = int.Parse(BirthMonth);
            }

            var dayValidation = FriendValidator.ValidateBirthDay(BirthDay);
            if (!dayValidation.IsValid)
            {
                StatusMessage = _localizationService.GetString("MessageErrorBirthDayRange");
                _logger.LogWarning("Validation failed: Invalid birth day: {BirthDay}", BirthDay);
                System.Windows.MessageBox.Show(
                    dayValidation.ErrorMessage,
                    "入力エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (!string.IsNullOrWhiteSpace(BirthDay))
            {
                birthDay = int.Parse(BirthDay);
            }

            // 既存の友人情報を取得
            var friend = await _friendRepository.GetByIdAsync(_friendId.Value);
            if (friend == null)
            {
                StatusMessage = _localizationService.GetString("MessageErrorSaveFriend");
                _logger.LogError("Friend not found for saving: {FriendId}", _friendId);
                System.Windows.MessageBox.Show(
                    "友人情報が見つかりませんでした。",
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            // 日付の組み合わせの妥当性チェック
            var combinationValidation = FriendValidator.ValidateBirthdateCombination(birthYear, birthMonth, birthDay);
            if (!combinationValidation.IsValid)
            {
                StatusMessage = _localizationService.GetString("MessageErrorInvalidDate");
                _logger.LogWarning("Validation failed: Invalid date combination");
                System.Windows.MessageBox.Show(
                    combinationValidation.ErrorMessage,
                    "入力エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 友人情報を更新
            friend.Name = trimmedName;
            friend.BirthYear = birthYear;
            friend.BirthMonth = birthMonth;
            friend.BirthDay = birthDay;
            friend.Memo = string.IsNullOrWhiteSpace(Memo) ? null : Memo.Trim();
            friend.NotifyEnabled = NotifyEnabled;
            friend.NotifySoundEnabled = NotifySoundEnabled;

            // NotifyDaysBeforeIndexをNotifyDaysBefore（日数）に変換
            if (NotifyDaysBeforeIndex == 1)
            {
                friend.NotifyEnabled = false;
                friend.NotifyDaysBefore = null;
            }
            else if (NotifyDaysBeforeIndex > 1)
            {
                friend.NotifyDaysBefore = FriendValidator.ConvertNotifyIndexToDays(NotifyDaysBeforeIndex);
            }
            else
            {
                friend.NotifyDaysBefore = null;
            }

            // エイリアスを更新
            // EF Coreのトラッキング機能により、既存のエイリアスは自動的に削除される
            friend.Aliases.Clear();
            foreach (var aliasItem in Aliases.Where(a => !string.IsNullOrWhiteSpace(a.Value)))
            {
                friend.Aliases.Add(new Alias
                {
                    AliasName = aliasItem.Value!.Trim(),
                    FriendId = friend.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 保存
            await _friendRepository.UpdateAsync(friend);

            StatusMessage = _localizationService.GetString("MessageFriendSaved");
            _logger.LogInformation("Friend saved successfully: {FriendName} (ID: {FriendId})", friend.Name, friend.Id);

            // タスクトレイアイコンを更新
            var trayIconService = _serviceProvider.GetService<ITrayIconService>();
            if (trayIconService != null)
            {
                await trayIconService.UpdateTrayIconFromRepositoryAsync();
            }

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
            StatusMessage = _localizationService.GetString("MessageErrorSaveFriend");
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
            StatusMessage = _localizationService.GetString("MessageFriendDeleted");

            // タスクトレイアイコンを更新
            var trayIconService = _serviceProvider.GetService<ITrayIconService>();
            if (trayIconService != null)
            {
                await trayIconService.UpdateTrayIconFromRepositoryAsync();
            }

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
            StatusMessage = _localizationService.GetString("MessageErrorDeleteFriend");
            System.Windows.MessageBox.Show(
                $"削除に失敗しました:\n{ex.Message}",
                "エラー",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
