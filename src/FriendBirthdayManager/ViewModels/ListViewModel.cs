using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.ViewModels;

/// <summary>
/// 一覧画面のViewModel
/// </summary>
public partial class ListViewModel : ObservableObject
{
    private readonly IFriendRepository _friendRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ListViewModel> _logger;
    private List<Friend> _allFriends = new();
    private CancellationTokenSource? _searchCancellationTokenSource;

    [ObservableProperty]
    private ObservableCollection<FriendListItem> _friends = new();

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private int _sortIndex = 0; // 0 = 近い順, 1 = 日付順, 2 = 名前順

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ListViewModel(
        IFriendRepository friendRepository,
        IServiceProvider serviceProvider,
        ILogger<ListViewModel> logger)
    {
        _friendRepository = friendRepository;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    partial void OnSearchKeywordChanged(string value)
    {
        // 即時検索: 検索キーワードが変更されたら自動的に検索を実行
        // 前の検索をキャンセル
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource = new CancellationTokenSource();

        var cancellationToken = _searchCancellationTokenSource.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                // 少し遅延を入れてデバウンス効果を持たせる（300ms）
                await Task.Delay(300, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await LoadFriendsAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合は何もしない
                _logger.LogDebug("Search cancelled due to new keyword input");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load friends on search keyword change");
            }
        }, cancellationToken);
    }

    partial void OnSortIndexChanged(int value)
    {
        // ソート順が変更されたら並び替えを実行
        ApplySort();
    }

    [RelayCommand]
    public async Task LoadFriendsAsync()
    {
        try
        {
            _logger.LogInformation("Loading friends list...");
            StatusMessage = "読み込み中...";

            // 検索キーワードに応じてデータを取得
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                _allFriends = await _friendRepository.GetAllAsync();
            }
            else
            {
                _allFriends = await _friendRepository.SearchAsync(SearchKeyword);
            }

            // 並び替えを適用してリストに表示
            ApplySort();

            StatusMessage = $"総件数: {Friends.Count}件";
            _logger.LogInformation("Loaded {Count} friends", Friends.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load friends");
            StatusMessage = "エラー: 友人一覧の読み込みに失敗しました";
        }
    }

    private void ApplySort()
    {
        try
        {
            IEnumerable<Friend> sortedFriends = SortIndex switch
            {
                0 => SortByNearestBirthday(_allFriends), // 近い順
                1 => SortByBirthdayDate(_allFriends),    // 日付順（1月1日→12月31日）
                2 => SortByName(_allFriends),            // 名前順
                _ => _allFriends
            };

            Friends.Clear();
            foreach (var friend in sortedFriends)
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

            _logger.LogInformation("Applied sort: {SortIndex}", SortIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply sort");
        }
    }

    /// <summary>
    /// 近い順にソート
    /// </summary>
    private IEnumerable<Friend> SortByNearestBirthday(List<Friend> friends)
    {
        var today = DateTime.Now;

        return friends
            .Select(f => new
            {
                Friend = f,
                DaysUntil = f.CalculateDaysUntilBirthday(today)
            })
            .OrderBy(x => x.DaysUntil.HasValue ? 0 : 1) // 誕生日設定済みを優先
            .ThenBy(x => x.DaysUntil ?? int.MaxValue)   // 日数の昇順
            .ThenBy(x => x.Friend.Name)                  // 同じ日数なら名前順
            .Select(x => x.Friend);
    }

    /// <summary>
    /// 日付順にソート（1月1日→12月31日）
    /// </summary>
    private IEnumerable<Friend> SortByBirthdayDate(List<Friend> friends)
    {
        return friends
            .OrderBy(f => f.BirthMonth.HasValue && f.BirthDay.HasValue ? 0 : 1) // 誕生日設定済みを優先
            .ThenBy(f => f.BirthMonth ?? 13)   // 月順
            .ThenBy(f => f.BirthDay ?? 32)     // 日順
            .ThenBy(f => f.Name);               // 同じ日付なら名前順
    }

    /// <summary>
    /// 名前順にソート（Unicode順）
    /// </summary>
    private IEnumerable<Friend> SortByName(List<Friend> friends)
    {
        return friends.OrderBy(f => f.Name, StringComparer.CurrentCulture);
    }

    [RelayCommand]
    private async Task EditFriend(FriendListItem friend)
    {
        try
        {
            _logger.LogInformation("Edit friend: {FriendId}", friend.Id);
            var editWindow = _serviceProvider.GetRequiredService<Views.EditWindow>();

            // ウィンドウ表示前にデータをロード（awaitで完了を待つ）
            await editWindow.LoadFriendAsync(friend.Id);

            editWindow.Show();
            editWindow.Activate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show edit window or load friend data: {FriendId}", friend.Id);
        }
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
