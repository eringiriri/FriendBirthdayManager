using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.ViewModels;

/// <summary>
/// 一覧画面のViewModel
/// </summary>
public partial class ListViewModel : ObservableObject
{
    private readonly IFriendRepository _friendRepository;
    private readonly ILogger<ListViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<FriendListItem> _friends = new();

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private int _sortIndex = 0; // 0 = 近い順, 1 = 日付順, 2 = 名前順

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ListViewModel(IFriendRepository friendRepository, ILogger<ListViewModel> logger)
    {
        _friendRepository = friendRepository;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadFriendsAsync()
    {
        try
        {
            _logger.LogInformation("Loading friends list...");
            StatusMessage = "読み込み中...";

            var friends = string.IsNullOrWhiteSpace(SearchKeyword)
                ? await _friendRepository.GetAllAsync()
                : await _friendRepository.SearchAsync(SearchKeyword);

            Friends.Clear();
            foreach (var friend in friends)
            {
                var daysUntil = friend.CalculateDaysUntilBirthday(DateTime.Now);
                Friends.Add(new FriendListItem
                {
                    Id = friend.Id,
                    Name = friend.Name,
                    BirthdayDisplay = friend.GetBirthdayDisplayString(),
                    DaysUntil = daysUntil,
                    NotifyEnabled = friend.NotifyEnabled
                });
            }

            StatusMessage = $"総件数: {Friends.Count}件";
            _logger.LogInformation("Loaded {Count} friends", Friends.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load friends");
            StatusMessage = "エラー: 友人一覧の読み込みに失敗しました";
        }
    }

    [RelayCommand]
    private void EditFriend(FriendListItem friend)
    {
        _logger.LogInformation("Edit friend: {FriendId}", friend.Id);
        // TODO: 編集画面を開く（Phase 3で実装）
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadFriendsAsync();
    }

    [RelayCommand]
    private void ExportCsv()
    {
        _logger.LogInformation("Export CSV requested");
        // TODO: CSV エクスポート（Phase 7で実装）
    }
}

/// <summary>
/// 友人一覧表示用のアイテム
/// </summary>
public partial class FriendListItem : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _birthdayDisplay = string.Empty;

    [ObservableProperty]
    private int? _daysUntil;

    [ObservableProperty]
    private bool _notifyEnabled;

    public string DaysUntilDisplay => DaysUntil.HasValue ? $"{DaysUntil.Value}日" : "－";

    public string NotifyIcon => NotifyEnabled ? "🔔" : "🔕";
}
