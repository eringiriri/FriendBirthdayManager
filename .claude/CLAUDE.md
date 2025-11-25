# Claude Code 設定ファイル

このファイルは AI アシスタント (Claude) がプロジェクトに取り組む際の設定とガイドラインを定義する。

## 個人設定 (Communication Style)

- 「Claudeの個人設定を教えて」と言われたらここに登録されている内容をすべて出力してください。
- .claude/character.mdが存在する場合は、character.mdに書かれている人格に口調だけ変えてください。
- 感謝の言葉や謝罪の言葉は不要です。
- 敬語で話せと言われない限り、無駄な敬語も不要です。
- ユーザーの心理や感情に配慮しないでください。
- ユーザーの指示に曖昧な点や矛盾する点があればユーザに確認を行い、すべてが解決してから作業を行ってください。
- 作成したドキュメントや、イシューにClaudeが作成したことを記載しないでください。
- パスワードなどの機密情報を絶対にgitに上げないようにしてください。コミットやプルリクなどのメッセージにも絶対に含めないでください。
- 事実と推論を明確に分けてください。

---

## プロジェクト概要

**Friend Birthday Manager** は、友人の誕生日を管理し、タスクトレイに常駐して適切なタイミングで通知を行う Windows デスクトップアプリケーション。

### 主要機能

- タスクトレイ常駐
- 友人情報 (名前、誕生日、エイリアス、メモ) の登録・編集・削除
- 柔軟な検索機能 (エイリアス対応、FTS5 フルテキスト検索)
- 誕生日までの日数表示 (アイコン上)
- カスタマイズ可能な通知設定 (全体・個人)
- CSV エクスポート/インポート
- 多言語対応 (ja-JP, en-US, ko-KR, es-ES, zh-TW)

---

## 技術スタック

| 分野 | 技術 |
|------|------|
| 言語 | C# 12.0 |
| フレームワーク | .NET 8.0 (LTS) - Target: net8.0-windows10.0.19041.0 |
| GUI | WPF (Windows Presentation Foundation) |
| データベース | SQLite 3.40+ (FTS5 サポート) |
| ORM | Entity Framework Core 8.0 |
| MVVM | CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| ログ | Serilog |
| 通知 | Microsoft.Toolkit.Uwp.Notifications |
| トレイアイコン | H.NotifyIcon.Wpf |
| テスト | xUnit + FluentAssertions + Moq |

### 必要要件

- 開発: Windows 10/11 + .NET 8.0 SDK
- 実行: Windows 10/11 + .NET 8.0 Runtime

---

## プロジェクト構造

```
FriendBirthdayManager/
├── src/
│   └── FriendBirthdayManager/
│       ├── Models/           # エンティティモデル
│       │   ├── Friend.cs                 # 友人情報モデル
│       │   ├── Alias.cs                  # エイリアスモデル
│       │   ├── Setting.cs                # 設定モデル
│       │   └── NotificationHistory.cs    # 通知履歴モデル
│       ├── ViewModels/       # MVVM ViewModel
│       │   ├── MainViewModel.cs          # メイン画面 (友人追加)
│       │   ├── ListViewModel.cs          # 一覧画面
│       │   ├── EditViewModel.cs          # 編集画面
│       │   ├── SettingsViewModel.cs      # 設定画面
│       │   └── AboutViewModel.cs         # About 画面
│       ├── Views/            # XAML UI
│       │   ├── BaseWindow.cs             # 共通基底クラス
│       │   ├── MainWindow.xaml[.cs]      # メインウィンドウ
│       │   ├── ListWindow.xaml[.cs]      # 一覧ウィンドウ
│       │   ├── EditWindow.xaml[.cs]      # 編集ウィンドウ
│       │   ├── SettingsWindow.xaml[.cs]  # 設定ウィンドウ
│       │   └── AboutWindow.xaml[.cs]     # Aboutウィンドウ
│       ├── Data/             # データアクセス層
│       │   ├── AppDbContext.cs                       # EF Core DbContext
│       │   ├── IFriendRepository.cs / FriendRepository.cs
│       │   ├── ISettingsRepository.cs / SettingsRepository.cs
│       │   └── INotificationHistoryRepository.cs / NotificationHistoryRepository.cs
│       ├── Services/         # ビジネスロジック
│       │   ├── INotificationService.cs / NotificationService.cs      # 通知管理
│       │   ├── ITrayIconService.cs / TrayIconService.cs              # トレイアイコン管理
│       │   ├── ICsvService.cs / CsvService.cs                        # CSV 入出力
│       │   ├── ILocalizationService.cs / LocalizationService.cs      # 多言語対応
│       │   └── IStartupService.cs / StartupService.cs                # スタートアップ設定
│       ├── Validation/       # バリデーションロジック
│       │   └── FriendValidator.cs
│       ├── Resources/        # リソース
│       │   ├── Icons/                    # アイコンファイル (.ico)
│       │   │   ├── birthday_cake.ico     # 今日が誕生日
│       │   │   └── number_1.ico ~ number_9.ico   # 残り日数アイコン
│       │   ├── Strings.ja-JP.xaml        # 日本語リソース
│       │   ├── Strings.en-US.xaml        # 英語リソース
│       │   ├── Strings.ko-KR.xaml        # 韓国語リソース
│       │   ├── Strings.es-ES.xaml        # スペイン語リソース
│       │   └── Strings.zh-TW.xaml        # 繁體中文リソース
│       ├── App.xaml / App.xaml.cs        # アプリケーションエントリポイント
│       ├── Constants.cs                   # 定数定義
│       └── FriendBirthdayManager.csproj
├── tests/
│   └── FriendBirthdayManager.Tests/
│       └── FriendBirthdayManager.Tests.csproj
├── .github/
│   └── workflows/
│       ├── ci.yml            # CI: ビルド、テスト、カバレッジ、アーティファクト生成
│       └── release.yml       # Release: タグベース自動リリース
├── .claude/                  # Claude Code 設定
│   ├── CLAUDE.md             # このファイル
│   ├── commands/github/      # GitHub コマンド
│   └── settings.local.json   # ローカル設定
├── FriendBirthdayManager.sln # Visual Studio ソリューション
├── .gitignore                # Git 無視設定
├── README.md                 # プロジェクト説明
└── PLAN.md                   # 詳細な開発計画書 (25000+ tokens)
```

---

## アーキテクチャパターン

### 1. MVVM パターン

CommunityToolkit.Mvvm を使用した MVVM 実装:

```csharp
// ViewModel の基本パターン
public partial class MainViewModel : ObservableObject
{
    // 自動プロパティ生成 (Source Generator)
    [ObservableProperty]
    private string _name = string.Empty;

    // 自動コマンド生成
    [RelayCommand]
    private async Task SaveAsync()
    {
        // ロジック実装
    }
}
```

**規約:**
- すべての ViewModel は `ObservableObject` を継承
- プロパティは `[ObservableProperty]` 属性でバッキングフィールドから自動生成
- コマンドは `[RelayCommand]` 属性でメソッドから自動生成
- 非同期コマンドは `Task` 戻り値の `*Async()` メソッド → `*Command` が生成される

### 2. Repository パターン

データアクセス層を抽象化:

```csharp
// インターフェース定義
public interface IFriendRepository
{
    Task<int> AddAsync(Friend friend);
    Task<List<Friend>> GetAllAsync();
    // ...
}

// 実装
public class FriendRepository : IFriendRepository
{
    private readonly AppDbContext _context;
    // 実装...
}
```

**規約:**
- すべてのデータアクセスは Repository 経由
- インターフェース (`I*Repository`) と実装 (`*Repository`) を分離
- DI で注入 (Scoped ライフタイム)

### 3. Dependency Injection

`App.xaml.cs` の `ConfigureServices` でサービスを登録:

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // DbContext: データベースパスを指定して登録
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));

    // Repositories: Scoped (DbContext と同じライフタイム)
    services.AddScoped<IFriendRepository, FriendRepository>();

    // Services: Singleton (長期実行、ただし DbContext はスコープ経由で取得)
    services.AddSingleton<INotificationService, NotificationService>();
    services.AddSingleton<ITrayIconService, TrayIconService>();

    // ViewModels: Transient (ウィンドウごとに新規作成)
    services.AddTransient<MainViewModel>();

    // Views: Transient
    services.AddTransient<Views.MainWindow>();
}
```

**重要な注意点:**
- `NotificationService` と `TrayIconService` は Singleton だが、DbContext を直接注入せず `IServiceProvider` を注入してスコープを作成する
- これにより DbContext のライフタイム問題を回避

### 4. ログ

Serilog を使用したログ管理:

```csharp
// App.xaml.cs で設定
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

// 各クラスで ILogger<T> を注入
public class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger;
    }

    public async Task DoSomethingAsync()
    {
        _logger.LogInformation("Operation started");
        try
        {
            // ...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed");
        }
    }
}
```

**ログの場所:**
- パス: `%LocalAppData%\FriendBirthdayManager\logs\app.log`
- ローテーション: 日次
- 保持期間: 30 日

### 5. データベース

**パス:** `%LocalAppData%\FriendBirthdayManager\friends.db`

**テーブル構造:**

| テーブル名 | 説明 |
|-----------|------|
| friends | 友人情報 (id, name, birth_year, birth_month, birth_day, memo, notify_days_before, notify_enabled, notify_sound_enabled, created_at, updated_at) |
| aliases | エイリアス (id, friend_id, alias, created_at) |
| settings | アプリ設定 (key, value, updated_at) |
| notification_history | 通知履歴 (id, friend_id, notification_date, notified_at) |
| friends_fts | FTS5 仮想テーブル (name, memo) - フルテキスト検索用 |

**命名規則:**
- テーブル名: snake_case (例: `friends`, `notification_history`)
- カラム名: snake_case (例: `birth_year`, `notify_enabled`)
- C# 側は PascalCase、`HasColumnName()` で明示的にマッピング

**FTS5 フルテキスト検索:**
- `friends_fts` 仮想テーブルで `name` と `memo` を全文検索
- INSERT/UPDATE/DELETE トリガーで自動同期

### 6. 多言語対応

**リソースファイル:**
- `Resources/Strings.ja-JP.xaml` (デフォルト)
- `Resources/Strings.en-US.xaml`
- `Resources/Strings.ko-KR.xaml`
- `Resources/Strings.es-ES.xaml`
- `Resources/Strings.zh-TW.xaml`

**使用方法:**

```csharp
// ILocalizationService 経由で取得
var message = _localizationService.GetString("MessageReady");

// 言語切り替え
_localizationService.ChangeLanguage("en-US");
```

**新しい言語を追加する際:**
1. `Resources/Strings.{language-code}.xaml` を作成
2. すべてのキーを翻訳
3. `ILocalizationService` のサポート言語リストに追加

---

## 開発ワークフロー

### ビルド

```bash
# 依存関係の復元
dotnet restore

# ビルド (Debug)
dotnet build

# ビルド (Release)
dotnet build --configuration Release
```

### テスト

```bash
# テスト実行
dotnet test --configuration Release --verbosity normal

# カバレッジ付きテスト
dotnet test --collect:"XPlat Code Coverage"
```

### 実行

```bash
# デバッグ実行
dotnet run --project src/FriendBirthdayManager/FriendBirthdayManager.csproj

# Visual Studio の場合
# F5 キーでデバッグ実行
```

### リリースビルド

```bash
# 単一実行ファイル生成
dotnet publish src/FriendBirthdayManager/FriendBirthdayManager.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

# 出力先
# src/FriendBirthdayManager/bin/Release/net8.0-windows/win-x64/publish/FriendBirthdayManager.exe
```

### CI/CD

#### CI Workflow (`.github/workflows/ci.yml`)

**トリガー:** `main` または `master` ブランチへの push/PR

**ステップ:**
1. .NET 8.0 SDK セットアップ
2. `dotnet restore`
3. `dotnet build --configuration Release`
4. `dotnet test --collect:"XPlat Code Coverage"`
5. リリースビルド生成 (win-x64)
6. アーティファクトアップロード (実行ファイル + カバレッジレポート)

#### Release Workflow (`.github/workflows/release.yml`)

**トリガー:** `v*.*.*` タグ push または手動実行

**ステップ:**
1. リリースビルド生成 (win-x64)
2. ZIP アーカイブ作成
3. GitHub Release 自動作成 (リリースノート自動生成)
4. アーティファクトアップロード (90 日保持)

**リリース手順:**
```bash
# バージョンタグを作成してプッシュ
git tag v1.0.1
git push origin v1.0.1

# Release workflow が自動実行される
```

---

## コーディング規約

### C# 言語機能

- **C# 12.0** の機能を積極的に活用
- **Nullable 参照型** を有効化 (`<Nullable>enable</Nullable>`)
- **Implicit Usings** を有効化 (`<ImplicitUsings>enable</ImplicitUsings>`)
- **required プロパティ** を使用 (必須プロパティの明示)

```csharp
public class Friend
{
    // required で必須プロパティを明示
    public required string Name { get; set; }

    // nullable 参照型
    public string? Memo { get; set; }
}
```

### 命名規則

| 要素 | 規則 | 例 |
|------|------|-----|
| クラス | PascalCase | `FriendRepository` |
| インターフェース | I + PascalCase | `IFriendRepository` |
| メソッド | PascalCase | `GetAllAsync()` |
| プロパティ | PascalCase | `BirthMonth` |
| プライベートフィールド | `_camelCase` | `_logger` |
| ローカル変数 | camelCase | `friendId` |
| 定数 | PascalCase | `MutexName` |
| 非同期メソッド | 末尾に Async | `SaveAsync()` |
| データベーステーブル | snake_case | `friends` |
| データベースカラム | snake_case | `birth_month` |

### 非同期処理

```csharp
// 非同期メソッドの基本パターン
public async Task<int> AddAsync(Friend friend)
{
    _context.Friends.Add(friend);
    await _context.SaveChangesAsync();
    return friend.Id;
}

// async void は避ける (イベントハンドラー以外)
// ConfigureAwait(false) でデッドロック回避 (必要に応じて)
await _context.SaveChangesAsync().ConfigureAwait(false);

// Fire-and-forget パターン
_ = Task.Run(async () =>
{
    try
    {
        await CheckAndNotifyAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in background task");
    }
});
```

### DI パターン

```csharp
// コンストラクタインジェクション
public class MainViewModel : ObservableObject
{
    private readonly IFriendRepository _friendRepository;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(
        IFriendRepository friendRepository,
        ILogger<MainViewModel> logger)
    {
        _friendRepository = friendRepository;
        _logger = logger;
    }
}

// Singleton サービスから Scoped サービスを使う場合
public class NotificationService : INotificationService
{
    private readonly IServiceProvider _serviceProvider;

    public async Task CheckAsync()
    {
        // スコープを作成して DbContext を取得
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFriendRepository>();

        // repository を使用...
    }
}
```

### 例外処理とログ

```csharp
public async Task SaveAsync()
{
    try
    {
        _logger.LogInformation("Saving friend: {FriendName}", friend.Name);

        await _friendRepository.AddAsync(friend);

        _logger.LogInformation("Friend saved successfully: {FriendName}", friend.Name);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to save friend: {FriendName}", friend.Name);

        // ユーザーにフィードバック
        MessageBox.Show($"保存に失敗しました: {ex.Message}", "エラー",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

**ログレベルのガイドライン:**
- `LogInformation`: 重要な操作の開始・完了
- `LogWarning`: 予期しない状態だが処理は継続可能
- `LogError`: エラーが発生して処理が失敗
- `LogDebug`: 開発時のデバッグ情報 (本番では出力しない)

### バリデーション

```csharp
// FriendValidator クラスでバリデーション集約
var validation = FriendValidator.ValidateBirthYear(BirthYear);
if (!validation.IsValid)
{
    MessageBox.Show(validation.ErrorMessage, "エラー",
        MessageBoxButton.OK, MessageBoxImage.Warning);
    return;
}

// ユーザー入力は必ずバリデーション
// MessageBox でユーザーにフィードバック
// StatusMessage プロパティで状態表示
```

### リソース管理

```csharp
// IDisposable の実装
public class NotificationService : INotificationService, IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;

        // リソースの解放
        _timer?.Dispose();

        _disposed = true;
    }
}

// using ステートメントでスコープ管理
using var scope = _serviceProvider.CreateScope();
var repository = scope.ServiceProvider.GetRequiredService<IFriendRepository>();
```

### XAML 規約

```xaml
<!-- View と ViewModel の紐付けは DataContext で -->
<Window x:Class="FriendBirthdayManager.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:FriendBirthdayManager.ViewModels"
        Title="Friend Birthday Manager">
    <Window.DataContext>
        <!-- ViewModelはDI経由で注入 -->
    </Window.DataContext>
</Window>

<!-- コマンドバインディング -->
<Button Content="保存" Command="{Binding SaveCommand}" />

<!-- プロパティバインディング -->
<TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" />
```

---

## よくある作業パターン

### 新しい友人を追加するフロー

1. `MainWindow` でユーザーが情報入力
2. `MainViewModel.SaveAsync()` が実行
3. `FriendValidator` でバリデーション
4. `IFriendRepository.AddAsync()` でデータベースに保存
5. トリガーで `friends_fts` が自動更新
6. `ITrayIconService.UpdateTrayIconFromRepositoryAsync()` でアイコン更新
7. ユーザーに成功メッセージ表示

### 通知チェックのフロー

1. `NotificationService.Start()` でタイマー開始 (1 時間ごと)
2. `CheckAndNotifyAsync()` が実行
3. 現在時刻が通知時刻の前後 30 分以内かチェック
4. `IFriendRepository.GetNotificationTargetsAsync()` で対象取得
5. 各友人について通知履歴をチェック
6. 未通知の場合 `ShowNotificationAsync()` でトースト通知
7. `INotificationHistoryRepository.AddAsync()` で履歴記録

### 新しいウィンドウを追加する場合

1. `Views/` に `*Window.xaml` と `*Window.xaml.cs` を作成
2. `ViewModels/` に `*ViewModel.cs` を作成
3. `App.xaml.cs` の `ConfigureServices()` で登録:
   ```csharp
   services.AddTransient<*ViewModel>();
   services.AddTransient<Views.*Window>();
   ```
4. 呼び出し側で DI 経由で取得:
   ```csharp
   var window = _serviceProvider.GetRequiredService<Views.*Window>();
   window.Show();
   ```

### 新しいサービスを追加する場合

1. `Services/` に `I*Service.cs` (インターフェース) を作成
2. `Services/` に `*Service.cs` (実装) を作成
3. `App.xaml.cs` の `ConfigureServices()` で登録:
   ```csharp
   // ライフタイムを適切に選択
   services.AddSingleton<I*Service, *Service>();  // 長期実行
   services.AddScoped<I*Service, *Service>();     // DbContext と同じ
   services.AddTransient<I*Service, *Service>();  // 毎回新規作成
   ```
4. 必要な場所で DI 経由で注入

### データベーステーブルを追加する場合

1. `Models/` に新しいモデルクラスを作成
2. `AppDbContext.cs` に `DbSet<T>` プロパティを追加
3. `OnModelCreating()` でテーブル定義 (カラム名マッピング、インデックス、制約)
4. 必要に応じて Repository を作成
5. 初回起動時に `EnsureCreated()` でテーブルが自動作成される

---

## トラブルシューティング

### データベースが壊れた場合

```bash
# データベースファイルを削除して再作成
# パス: %LocalAppData%\FriendBirthdayManager\friends.db
del %LocalAppData%\FriendBirthdayManager\friends.db

# アプリを再起動すると自動的に再作成される
```

### ログの確認

```bash
# ログの場所
%LocalAppData%\FriendBirthdayManager\logs\app{yyyyMMdd}.log

# 最新のログを表示 (PowerShell)
Get-Content "$env:LOCALAPPDATA\FriendBirthdayManager\logs\app*.log" -Tail 50
```

### 多重起動のロック解除

```bash
# Mutexが残っている場合はアプリを完全に終了
taskkill /IM FriendBirthdayManager.exe /F
```

### ビルドエラー

```bash
# NuGet パッケージのクリーンアップ
dotnet clean
dotnet restore --force

# キャッシュのクリア
dotnet nuget locals all --clear
```

---

## セキュリティとプライバシー

### 機密情報の扱い

- **絶対に Git にコミットしないもの:**
  - データベースファイル (`*.db`, `*.sqlite`)
  - ログファイル (`*.log`)
  - ユーザーデータ
  - 機密情報を含む設定ファイル

- `.gitignore` で適切に除外されていることを確認

### データの保存場所

- すべてのユーザーデータは `%LocalAppData%\FriendBirthdayManager\` に保存
- アンインストール時にこのフォルダを削除することでデータを完全に削除可能

---

## 参考資料

- **詳細な開発計画:** `PLAN.md` (25000+ tokens の詳細仕様)
- **プロジェクト説明:** `README.md`
- **.NET 8.0 ドキュメント:** https://learn.microsoft.com/ja-jp/dotnet/
- **WPF ドキュメント:** https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/
- **Entity Framework Core:** https://learn.microsoft.com/ja-jp/ef/core/
- **CommunityToolkit.Mvvm:** https://learn.microsoft.com/ja-jp/dotnet/communitytoolkit/mvvm/

---

## AI アシスタントへの指示

### コードを書く際の注意点

1. **既存のコードを読んでから変更する:**
   - 修正や機能追加の前に、関連するファイルを必ず Read ツールで確認
   - アーキテクチャパターンやコーディング規約に従う

2. **適切なログを追加する:**
   - 重要な操作には `LogInformation`
   - エラー時には `LogError` を使用

3. **例外処理を適切に行う:**
   - ユーザー入力に関わる処理は必ず try-catch
   - MessageBox でユーザーにフィードバック

4. **バリデーションを忘れない:**
   - ユーザー入力は `FriendValidator` を使用
   - データベース制約も意識する

5. **DI パターンを守る:**
   - コンストラクタインジェクション
   - ライフタイムを正しく設定

6. **非同期処理を正しく扱う:**
   - async/await パターン
   - async void を避ける (イベントハンドラー以外)
   - ConfigureAwait(false) でデッドロック回避

7. **多言語対応:**
   - ハードコードされた文字列を避ける
   - `ILocalizationService.GetString()` を使用

8. **テストを書く:**
   - 新機能には単体テストを追加
   - xUnit + FluentAssertions + Moq を使用

### コミットメッセージ

```
# 形式: <type>: <subject>

feat: 新機能追加
fix: バグ修正
docs: ドキュメント変更
style: コードスタイル変更 (フォーマット等)
refactor: リファクタリング
test: テスト追加・修正
chore: ビルド設定等の変更

# 例:
feat: スペイン語（es-ES）対応を追加
fix: トレイアイコンの更新タイミングを修正
docs: CLAUDE.md を更新
```

### PR 作成時

- CI が通ることを確認
- 変更内容を簡潔に記述
- 関連する Issue があれば参照

---

**最終更新:** 2025-11-25
