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
    private int _friendId;

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
            _logger.LogInformation("Saving friend: {FriendId}", _friendId);
            // TODO: 編集の保存実装（Phase 3で実装）
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save friend: {FriendId}", _friendId);
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
