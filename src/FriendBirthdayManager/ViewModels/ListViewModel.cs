using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Models;
using FriendBirthdayManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace FriendBirthdayManager.ViewModels;

/// <summary>
/// 一覧画面のViewModel
/// </summary>
public partial class ListViewModel : ObservableObject
{
    private readonly IFriendRepository _friendRepository;
    private readonly ICsvService _csvService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILocalizationService _localizationService;
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
        ICsvService csvService,
        IServiceProvider serviceProvider,
        ILocalizationService localizationService,
        ILogger<ListViewModel> logger)
    {
        _friendRepository = friendRepository;
        _csvService = csvService;
        _serviceProvider = serviceProvider;
        _localizationService = localizationService;
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
                    // UIスレッドで実行
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadFriendsAsync();
                    });
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
                var daysUntilDisplay = daysUntil.HasValue
                    ? string.Format(_localizationService.GetString("DaysFormat"), daysUntil.Value)
                    : _localizationService.GetString("DaysUntilNone");

                Friends.Add(new FriendListItem
                {
                    Id = friend.Id,
                    Name = friend.Name,
                    BirthdayDisplay = friend.GetBirthdayDisplayString(),
                    DaysUntil = daysUntil,
                    DaysUntilDisplay = daysUntilDisplay,
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
    private async Task DeleteFriend(FriendListItem friend)
    {
        try
        {
            // 削除確認ダイアログを表示
            var result = MessageBox.Show(
                $"「{friend.Name}」を削除してもよろしいですか？\n\nこの操作は取り消せません。",
                "削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                _logger.LogInformation("Friend deletion cancelled by user: {FriendId}", friend.Id);
                return;
            }

            // 削除実行
            _logger.LogInformation("Deleting friend: {FriendId} ({FriendName})", friend.Id, friend.Name);
            await _friendRepository.DeleteAsync(friend.Id);

            StatusMessage = $"「{friend.Name}」を削除しました";
            _logger.LogInformation("Friend deleted successfully: {FriendId}", friend.Id);

            // リストを再読み込み
            await LoadFriendsAsync();

            // タスクトレイアイコンを更新
            var trayIconService = _serviceProvider.GetService<ITrayIconService>();
            if (trayIconService != null)
            {
                await trayIconService.UpdateTrayIconFromRepositoryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete friend: {FriendId}", friend.Id);
            StatusMessage = "エラー: 友人の削除に失敗しました";
            MessageBox.Show(
                $"友人の削除に失敗しました: {ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchKeyword = string.Empty;
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        try
        {
            _logger.LogInformation("Import CSV requested");

            // ファイル選択ダイアログを表示
            var dialog = new OpenFileDialog
            {
                Filter = "CSV ファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*",
                DefaultExt = "csv",
                Title = "CSVファイルのインポート"
            };

            if (dialog.ShowDialog() != true)
            {
                _logger.LogInformation("CSV import cancelled by user");
                return;
            }

            StatusMessage = "インポート中...";

            // CSVインポート実行
            var result = await _csvService.ImportAsync(dialog.FileName);

            // 結果メッセージの作成
            var messageLines = new List<string>();
            messageLines.Add($"新規登録: {result.SuccessCount}件");
            messageLines.Add($"更新: {result.UpdateCount}件");
            messageLines.Add($"失敗: {result.FailureCount}件");

            if (result.Errors.Count > 0)
            {
                messageLines.Add("");
                messageLines.Add("エラー詳細:");
                // 最初の10件のエラーのみ表示
                foreach (var error in result.Errors.Take(10))
                {
                    messageLines.Add($"- {error}");
                }
                if (result.Errors.Count > 10)
                {
                    messageLines.Add($"... 他 {result.Errors.Count - 10} 件");
                }
            }

            StatusMessage = $"インポート完了: 新規 {result.SuccessCount}件, 更新 {result.UpdateCount}件, 失敗 {result.FailureCount}件";

            // 結果ダイアログを表示
            var messageType = result.FailureCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information;
            MessageBox.Show(
                string.Join("\n", messageLines),
                "インポート完了",
                MessageBoxButton.OK,
                messageType);

            _logger.LogInformation("CSV import completed: Success={SuccessCount}, Update={UpdateCount}, Failure={FailureCount}",
                result.SuccessCount, result.UpdateCount, result.FailureCount);

            // 成功または更新が1件以上ある場合はリストを再読み込み
            if (result.SuccessCount > 0 || result.UpdateCount > 0)
            {
                await LoadFriendsAsync();

                // タスクトレイアイコンを更新
                var trayIconService = _serviceProvider.GetService<ITrayIconService>();
                if (trayIconService != null)
                {
                    await trayIconService.UpdateTrayIconFromRepositoryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import CSV");
            StatusMessage = "エラー: CSVインポートに失敗しました";
            MessageBox.Show(
                $"CSVファイルのインポートに失敗しました: {ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        try
        {
            _logger.LogInformation("Export CSV requested");

            // ファイル保存ダイアログを表示
            var dialog = new SaveFileDialog
            {
                Filter = "CSV ファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"friends_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "CSVファイルのエクスポート"
            };

            if (dialog.ShowDialog() != true)
            {
                _logger.LogInformation("CSV export cancelled by user");
                return;
            }

            StatusMessage = "エクスポート中...";

            // CSVエクスポート実行
            var success = await _csvService.ExportAsync(dialog.FileName);

            if (success)
            {
                StatusMessage = $"CSVファイルをエクスポートしました: {Path.GetFileName(dialog.FileName)}";
                MessageBox.Show(
                    $"CSVファイルをエクスポートしました。\n\n{dialog.FileName}",
                    "エクスポート完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                _logger.LogInformation("CSV export completed: {FilePath}", dialog.FileName);
            }
            else
            {
                StatusMessage = "エラー: CSVエクスポートに失敗しました";
                MessageBox.Show(
                    "CSVファイルのエクスポートに失敗しました。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _logger.LogError("CSV export failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export CSV");
            StatusMessage = "エラー: CSVエクスポートに失敗しました";
            MessageBox.Show(
                $"CSVファイルのエクスポートに失敗しました: {ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
    private string _daysUntilDisplay = string.Empty;

    [ObservableProperty]
    private bool _notifyEnabled;

    public string NotifyIcon => NotifyEnabled ? "🔔" : "🔕";
}
