# Friend Birthday Manager - 開発計画書（改訂版）

## 📋 プロジェクト概要

### 目的
友人の誕生日を管理し、タスクトレイに常駐して適切なタイミングで通知を行うWindowsデスクトップアプリケーション

### 主要機能
- タスクトレイ常駐
- 友人情報（名前、誕生日、エイリアス、メモ）の登録・編集・削除
- 柔軟な検索機能（エイリアス対応、FTS5フルテキスト検索）
- 誕生日までの日数表示（アイコン上）
- カスタマイズ可能な通知設定（全体・個人）
- CSV エクスポート/インポート
- 多言語対応基盤（将来的な拡張を考慮）

---

## 🛠 技術スタック

### 推奨技術スタック: **C# + WPF**

| 項目 | 技術 | バージョン | 理由 |
|------|------|-----------|------|
| 言語 | C# | 12.0+ | Windows開発に最適、nullable参照型、required修飾子 |
| フレームワーク | .NET | 8.0 LTS | 長期サポート版、2026年11月までサポート |
| GUI | WPF | .NET 8組み込み | MVVM対応、データバインディング |
| データベース | SQLite | 3.40+ | 軽量、ファイルベース、FTS5サポート |
| DB アクセス | Entity Framework Core | 8.0+ | 型安全、LINQ、マイグレーション |
| DI コンテナ | Microsoft.Extensions.DependencyInjection | 8.0+ | テスタビリティ向上 |
| タスクトレイ | Hardcodet.NotifyIcon.Wpf | 1.1.0+ | WPF用タスクトレイライブラリ |
| 通知 | Microsoft.Toolkit.Uwp.Notifications | 7.1.2+ | Windowsトースト通知 |
| ログ | Serilog | 3.1.0+ | 構造化ログ、ファイル出力 |
| テスト | xUnit + FluentAssertions + Moq | 最新 | 単体テスト、モック |

### ⚠️ ライブラリのメンテナンス状況について

**注意**: 開発開始前に以下のライブラリの最新状況を確認してください。

- **Hardcodet.NotifyIcon.Wpf**: 2018年以降更新が停止している可能性があります。代替として [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) の使用を検討してください。
- **Microsoft.Toolkit.Uwp.Notifications**: [CommunityToolkit.Notifications](https://www.nuget.org/packages/CommunityToolkit.WinUI.Notifications/) への移行が推奨されています。最新のパッケージ名とバージョンを確認してください。

**推奨手順**:
1. プロジェクト開始前にNuGetで各ライブラリの最終更新日を確認
2. メンテナンスが停止している場合は、代替ライブラリを検討
3. GitHub Issuesで既知の問題を確認

---

## 📊 データベース設計（改訂版）

### テーブル構成

#### 1. `friends` テーブル
友人の基本情報を管理

| カラム名 | 型 | 制約 | 説明 |
|---------|-----|------|------|
| id | INTEGER | PRIMARY KEY AUTOINCREMENT | 固有ID |
| name | TEXT | NOT NULL | 友人の名前 |
| birth_year | INTEGER | NULL | 誕生年（例: 2000） |
| birth_month | INTEGER | NULL CHECK(birth_month IS NULL OR (birth_month BETWEEN 1 AND 12)) | 誕生月（1-12） |
| birth_day | INTEGER | NULL CHECK(birth_day IS NULL OR (birth_day BETWEEN 1 AND 31)) | 誕生日（1-31） |
| memo | TEXT | NULL | メモ |
| notify_days_before | INTEGER | NULL CHECK(notify_days_before IS NULL OR (notify_days_before BETWEEN 1 AND 30)) | 個人通知設定（NULL=デフォルト使用） |
| notify_enabled | INTEGER | NOT NULL DEFAULT 1 CHECK(notify_enabled IN (0, 1)) | 通知有効フラグ |
| notify_sound_enabled | INTEGER | NULL CHECK(notify_sound_enabled IS NULL OR (notify_sound_enabled IN (0, 1))) | 音声通知（NULL=デフォルト） |
| created_at | TEXT | NOT NULL | 作成日時（ISO 8601） |
| updated_at | TEXT | NOT NULL | 更新日時（ISO 8601） |

**インデックス**:
```sql
CREATE INDEX idx_friends_birth_month_day ON friends(birth_month, birth_day)
    WHERE birth_month IS NOT NULL AND birth_day IS NOT NULL;
CREATE INDEX idx_friends_name ON friends(name);
CREATE INDEX idx_friends_notify_enabled ON friends(notify_enabled) WHERE notify_enabled = 1;
```

**通知対象判定**:
- `birth_month IS NOT NULL AND birth_day IS NOT NULL` の場合のみ通知対象
- 年のみ、月のみ、日のみの場合は通知されない

**重複登録制御**:
- アプリケーションロジックで制御（データベース制約ではなく）
- 同名同誕生日の場合、ユーザーに確認ダイアログを表示
- メモで区別することを推奨するが、強制はしない

#### 2. `aliases` テーブル（正規化）
エイリアスを別テーブルで管理

| カラム名 | 型 | 制約 | 説明 |
|---------|-----|------|------|
| id | INTEGER | PRIMARY KEY AUTOINCREMENT | エイリアスID |
| friend_id | INTEGER | NOT NULL | 友人ID |
| alias | TEXT | NOT NULL | エイリアス |
| created_at | TEXT | NOT NULL | 作成日時（ISO 8601） |

**制約**:
```sql
FOREIGN KEY (friend_id) REFERENCES friends(id) ON DELETE CASCADE,
UNIQUE(friend_id, alias)
```

**インデックス**:
```sql
CREATE INDEX idx_aliases_friend_id ON aliases(friend_id);
CREATE INDEX idx_aliases_alias ON aliases(alias);
```

#### 3. `settings` テーブル
アプリケーション全体設定

| カラム名 | 型 | 制約 | 説明 |
|---------|-----|------|------|
| key | TEXT | PRIMARY KEY | 設定キー |
| value | TEXT | NOT NULL | 設定値（JSON形式も可） |
| updated_at | TEXT | NOT NULL | 更新日時（ISO 8601） |

**初期設定項目**:
- `default_notify_days_before`: デフォルト通知日数（1-30）
- `default_notify_sound`: デフォルト音声通知（0/1）
- `notification_time`: 通知時刻（24時間形式、例: "12:00"）
- `start_with_windows`: スタートアップ登録（0/1）
- `language`: 言語設定（"ja-JP", "en-US" など）
- `schema_version`: データベーススキーマバージョン（マイグレーション用）

#### 4. `notification_history` テーブル
通知履歴を記録（重複通知防止）

| カラム名 | 型 | 制約 | 説明 |
|---------|-----|------|------|
| id | INTEGER | PRIMARY KEY AUTOINCREMENT | 履歴ID |
| friend_id | INTEGER | NOT NULL | 友人ID |
| notification_date | TEXT | NOT NULL | 通知対象日（YYYY-MM-DD） |
| notified_at | TEXT | NOT NULL | 通知実行日時（ISO 8601） |

**制約**:
```sql
FOREIGN KEY (friend_id) REFERENCES friends(id) ON DELETE CASCADE,
UNIQUE(friend_id, notification_date)
```

**インデックス**:
```sql
CREATE INDEX idx_notification_history_date ON notification_history(notification_date);
```

**自動削除**:
- 30日より古い履歴は自動削除（アプリ起動時にクリーンアップ）

**実装例**:
```csharp
public class NotificationHistoryCleanupService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationHistoryCleanupService> _logger;

    public async Task CleanupOldHistoryAsync()
    {
        var cutoffDate = DateTime.UtcNow.Date.AddDays(-30).ToString("yyyy-MM-dd");

        var deletedCount = await _context.Database
            .ExecuteSqlRawAsync(
                "DELETE FROM notification_history WHERE notification_date < {0}",
                cutoffDate
            );

        _logger.LogInformation("Deleted {DeletedCount} old notification history records", deletedCount);
    }
}

// アプリケーション起動時に実行
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var cleanupService = _serviceProvider.GetRequiredService<NotificationHistoryCleanupService>();
        await cleanupService.CleanupOldHistoryAsync();

        // ... (他の起動処理)
    }
}
```

#### 5. `friends_fts` テーブル（FTS5 仮想テーブル）
高速フルテキスト検索用

```sql
CREATE VIRTUAL TABLE friends_fts USING fts5(
    name,
    memo,
    content=friends,
    content_rowid=id
);

-- トリガーでfriends テーブルと同期
-- NOTE: memoはNULLable。FTS5ではNULLを空文字列に変換する必要がある
CREATE TRIGGER friends_ai AFTER INSERT ON friends BEGIN
    INSERT INTO friends_fts(rowid, name, memo)
    VALUES (new.id, new.name, COALESCE(new.memo, ''));
END;

CREATE TRIGGER friends_ad AFTER DELETE ON friends BEGIN
    DELETE FROM friends_fts WHERE rowid = old.id;
END;

CREATE TRIGGER friends_au AFTER UPDATE ON friends BEGIN
    UPDATE friends_fts SET name = new.name, memo = COALESCE(new.memo, '')
    WHERE rowid = new.id;
END;
```

### データベースマイグレーション戦略

**スキーマバージョン管理**:
- `settings.schema_version` でバージョン追跡
- 起動時にバージョンチェック、必要に応じてマイグレーション実行
- マイグレーション前に自動バックアップ

**マイグレーション実装例**:
```csharp
public interface IDatabaseMigration
{
    int TargetVersion { get; }
    Task MigrateAsync(SqliteConnection connection);
}

public class Migration_001_AddAliasesTable : IDatabaseMigration
{
    public int TargetVersion => 1;

    public async Task MigrateAsync(SqliteConnection connection)
    {
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // エイリアステーブルの作成
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS aliases (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    friend_id INTEGER NOT NULL,
                    alias TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY (friend_id) REFERENCES friends(id) ON DELETE CASCADE,
                    UNIQUE(friend_id, alias)
                );

                CREATE INDEX IF NOT EXISTS idx_aliases_friend_id ON aliases(friend_id);
                CREATE INDEX IF NOT EXISTS idx_aliases_alias ON aliases(alias);
            ";
            await cmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

public class MigrationRunner
{
    private readonly AppDbContext _context;
    private readonly ILogger<MigrationRunner> _logger;
    private readonly IEnumerable<IDatabaseMigration> _migrations;

    public MigrationRunner(
        AppDbContext context,
        ILogger<MigrationRunner> logger,
        IEnumerable<IDatabaseMigration> migrations)
    {
        _context = context;
        _logger = logger;
        _migrations = migrations.OrderBy(m => m.TargetVersion);
    }

    public async Task RunMigrationsAsync()
    {
        var currentVersion = await GetCurrentSchemaVersionAsync();
        _logger.LogInformation("Current schema version: {Version}", currentVersion);

        foreach (var migration in _migrations.Where(m => m.TargetVersion > currentVersion))
        {
            _logger.LogInformation("Running migration to version {Version}", migration.TargetVersion);

            // バックアップ作成
            await CreateBackupAsync();

            try
            {
                var connection = (SqliteConnection)_context.Database.GetDbConnection();
                await connection.OpenAsync();
                await migration.MigrateAsync(connection);
                await SetSchemaVersionAsync(migration.TargetVersion);

                _logger.LogInformation("Migration to version {Version} completed", migration.TargetVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration to version {Version} failed", migration.TargetVersion);
                throw;
            }
        }
    }

    private async Task<int> GetCurrentSchemaVersionAsync()
    {
        var version = await _context.Settings
            .Where(s => s.Key == "schema_version")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        return int.TryParse(version, out var v) ? v : 0;
    }

    private async Task SetSchemaVersionAsync(int version)
    {
        var setting = await _context.Settings.FindAsync("schema_version");
        if (setting == null)
        {
            _context.Settings.Add(new Setting
            {
                Key = "schema_version",
                Value = version.ToString(),
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            setting.Value = version.ToString();
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private async Task CreateBackupAsync()
    {
        var dbPath = _context.Database.GetDbConnection().DataSource;
        var backupPath = $"{dbPath}.backup_{DateTime.Now:yyyyMMddHHmmss}";

        await Task.Run(() => File.Copy(dbPath, backupPath, overwrite: false));
        _logger.LogInformation("Database backup created: {BackupPath}", backupPath);
    }
}
```

---

## 🎨 UI設計詳細

### 画面A: メイン画面（誕生日追加）
**トリガー**: タスクトレイアイコンをクリック、または右クリックメニュー「誕生日追加」

```
┌─────────────────────────────────────────┐
│ 友人の誕生日を追加                       │
├─────────────────────────────────────────┤
│ 名前: *必須                              │
│ [____________________________________]  │
│                                         │
│ 誕生日:                                  │
│ [YYYY] / [MM] / [DD]                   │
│ (DatePickerコンポーネント使用)           │
│ [ ] 月日のみ登録（年を省略）             │
│                                         │
│ エイリアス:                              │
│ [____________________________________]  │
│ [+ エイリアスを追加]                     │
│                                         │
│ メモ:                                    │
│ ┌──────────────────────────────────┐   │
│ │                                  │   │
│ │                                  │   │
│ └──────────────────────────────────┘   │
│                                         │
│ 通知設定: [▼ デフォルト設定を使用 ▼]     │
│           (1~30日前, または通知なし)     │
│                                         │
│        [登録]        [一覧表示]         │
├─────────────────────────────────────────┤
│ 📅 直近の誕生日（最大5件）                │
│                                         │
│ 1. 山田太郎                              │
│    2025年11月20日（あと 6日）            │
│                                         │
│ 2. 佐藤花子                              │
│    2025年11月25日（あと 11日）           │
└─────────────────────────────────────────┘
```

**機能**:
- 入力バリデーション（**名前のみ必須**、その他任意）
- 誕生日入力: 3つの独立したフィールド
  - 年: [____] (例: 2000) ← 任意
  - 月: [__] (1-12) ← 任意
  - 日: [__] (1-31) ← 任意
  - 💡 月と日の両方を入力すると通知対象になる
- エイリアス: 動的に追加可能（個別の入力フィールド）
- 直近5件の誕生日を表示（**BirthMonth と BirthDay が登録されている友人のみ**）
- キーボードショートカット対応（Ctrl+N: 新規登録、Ctrl+L: 一覧表示）

**バリデーション**:
- 名前: 1文字以上、200文字以下
- 年: 1900-2100の範囲（任意）
- 月: 1-12の範囲（任意）
- 日: 1-31の範囲（任意）、うるう年考慮
- エイリアス: 各50文字以下
- メモ: 5000文字以下

**データベース保存**:
- 年・月・日は個別のINTEGERカラムに保存
- これにより「5月生まれ」と「5日生まれ」が明確に区別される

---

### 画面B: 一覧表示画面
**トリガー**: 「一覧表示」ボタン、または右クリックメニュー「一覧表示」

```
┌──────────────────────────────────────────────────────────┐
│ 友人一覧                                      [×] [□] [_] │
├──────────────────────────────────────────────────────────┤
│ 🔍 [_______________________]   [🔽 近い順]   [エクスポート]│
│     即時検索（名前/エイリアス/メモ）                      │
│                                                          │
│ ┌────────────────────────────────────────────────────┐  │
│ │    名前      │ 誕生日    │ あと  │ 通知 │          │  │
│ ├────────────────────────────────────────────────────┤  │
│ │ 山田太郎     │11月20日   │ 6日   │ 🔔  │ [編集]  │  │
│ │ 佐藤花子     │11月25日   │ 11日  │ 🔔  │ [編集]  │  │
│ │ 鈴木一郎     │12月1日    │ 17日  │ 🔕  │ [編集]  │  │
│ │ 田中次郎     │（未設定）  │ －   │ －  │ [編集]  │  │
│ └────────────────────────────────────────────────────┘  │
│                                                          │
│ 総件数: 42件  表示: 4件                          [閉じる] │
└──────────────────────────────────────────────────────────┘
```

**機能**:
- **即時検索**: FTS5による高速フルテキスト検索
  - 名前、エイリアス、メモを横断検索
  - 入力中にリアルタイムでフィルタリング
- **並び替え**:
  - 近い順: 今日から誕生日までの日数（昇順）、誕生日未設定は最後
    - 同日の場合は名前順（Unicode順でソート、アプリケーション層で実装）
  - 日付順: 1月1日→12月31日（月日のみで判定）、同日は名前順
  - 名前順: Unicode順（C#のstring.Compare使用）
- **名前クリック**: 編集画面（画面C）へ遷移
- **削除**: 編集画面から実行（誤操作防止のため一覧から直接削除不可）
- **通知アイコン**: 🔔（有効）、🔕（無効）
- **ページネーション**: 100件以上の場合、仮想化リスト使用（パフォーマンス対策）
- **アクセシビリティ**: スクリーンリーダー対応、キーボードナビゲーション

---

### 画面C: 編集画面
**トリガー**: 一覧画面で名前をクリック

```
┌─────────────────────────────────────────┐
│ 友人情報の編集                           │
├─────────────────────────────────────────┤
│ 名前: *必須                              │
│ [山田太郎__________________________]    │
│                                         │
│ 誕生日:                                  │
│ [2000] / [05] / [15]                   │
│ [ ] 月日のみ登録                         │
│                                         │
│ エイリアス:                              │
│ ┌─────────────────────────────────┐    │
│ │ tarou                     [削除] │    │
│ │ taro                      [削除] │    │
│ │ たろー                     [削除] │    │
│ │ [新しいエイリアス...] [+ 追加]  │    │
│ └─────────────────────────────────┘    │
│                                         │
│ メモ:                                    │
│ ┌──────────────────────────────────┐   │
│ │高校時代の友人                     │   │
│ │好きなもの: ラーメン               │   │
│ └──────────────────────────────────┘   │
│                                         │
│ 通知設定:                                │
│ ┌─────────────────────────────────┐    │
│ │ [▼ 3日前から ▼]  [🔔 通知 ON ]  │    │
│ │ [☑ 音を鳴らす]                   │    │
│ └─────────────────────────────────┘    │
│                                         │
│    [保存]    [削除]    [キャンセル]      │
└─────────────────────────────────────────┘
```

**機能**:
- 通知トグルボタン（ON/OFF）
- 個人通知設定（デフォルト、1~30日前）
- 音声通知の個別設定
- エイリアス個別追加・削除
- 削除ボタン: 確認ダイアログ表示（「本当に削除しますか？この操作は取り消せません。」）
- 変更検知: 未保存の変更がある場合、閉じる時に確認ダイアログ

---

### 画面D: 設定画面
**トリガー**: 右クリックメニュー「設定」

```
┌─────────────────────────────────────────┐
│ 設定                        [タブUI実装] │
├─────────────────────────────────────────┤
│ [通知] [起動] [データ] [詳細]            │
│                                         │
│ 【通知設定】                             │
│                                         │
│ 通知時刻:                                │
│   [🕐 12] : [00]  (TimePicker使用)      │
│                                         │
│ デフォルト通知タイミング:                │
│   [▼ 1日前から ▼] (1~30日前から選択)    │
│   ※ 選択した日数前から誕生日当日まで     │
│      毎日1回、上記時刻に通知             │
│                                         │
│ デフォルト音声通知:                      │
│   [☑] 音を鳴らす                        │
│                                         │
├─────────────────────────────────────────┤
│ 【起動設定】                             │
│                                         │
│   [☑] Windowsスタートアップに登録       │
│   （タスクスケジューラ使用、UAC不要）    │
│                                         │
├─────────────────────────────────────────┤
│ 【データ管理】                           │
│                                         │
│   [CSVをエクスポート]                    │
│   [CSVをインポート]                      │
│   [データベースをバックアップ]           │
│                                         │
│   ※ インポートは本ツール出力CSV限定      │
│   ※ 大容量ファイル（10MB以上）は警告     │
│                                         │
├─────────────────────────────────────────┤
│ 【詳細設定】                             │
│                                         │
│   言語: [▼ 日本語 ▼]                    │
│   ログレベル: [▼ Information ▼]        │
│   データベースパス: C:\Users\...\friends.db │
│   ログファイルパス: C:\Users\...\logs\   │
│                                         │
├─────────────────────────────────────────┤
│              [保存]        [閉じる]      │
└─────────────────────────────────────────┘
```

**変更点**:
- TimePicker UIコンポーネント使用（カンマ区切りテキスト入力を廃止）
- 通知は**1日1回のみ**に変更（ユーザー負担軽減）
- タブUIでカテゴリ分け（視認性向上）
- 詳細設定タブ追加（言語、ログ、パス表示）

---

### 画面E: クレジット
**トリガー**: 右クリックメニュー「クレジット」

```
┌─────────────────────────────────────────┐
│ Friend Birthday Manager について         │
├─────────────────────────────────────────┤
│                                         │
│        Friend Birthday Manager          │
│               Version 1.0.0             │
│                                         │
│ ────────────────────────────────────── │
│                                         │
│ 制作者: えりんぎ                         │
│                                         │
│ Twitter: @eringi_vrc                    │
│   (クリックでプロフィールを開く)          │
│                                         │
│ 連絡先: eringi@eringi.me                │
│                                         │
│ ソースコード: [GitHub アイコン]          │
│   (クリックでリポジトリを開く)            │
│                                         │
│ ライセンス: MIT License                  │
│                                         │
│                  [閉じる]                │
└─────────────────────────────────────────┘
```

---

### タスクトレイアイコン

#### 右クリックメニュー
```
┌────────────────────┐
│ 誕生日を追加 (Ctrl+N) │ → 画面A
│ 一覧表示 (Ctrl+L)     │ → 画面B
│ ────────────────── │
│ 設定               │ → 画面D
│ クレジット         │ → 画面E
│ ────────────────── │
│ 終了               │ → アプリ終了
└────────────────────┘
```

#### アイコン表示仕様
- **1~9日以内に誕生日がある場合**: 対応する日数のアイコンを表示（例: `1.ico`, `2.ico`, ..., `9.ico`）
- **10日以上先、または誕生日当日の場合**: ケーキアイコンを表示（`birthday.ico`）
- **アイコンサイズ**: 16x16px（標準）、32x32px（高DPI）、48x48px（超高DPI）
- **形式**: ICO形式（マルチサイズ対応）
- **準備が必要なアイコンファイル**:
  - `Resources/Icons/birthday.ico` - 誕生日ケーキ（デフォルト）
  - `Resources/Icons/1.ico` - 数字1
  - `Resources/Icons/2.ico` - 数字2
  - `Resources/Icons/3.ico` - 数字3
  - `Resources/Icons/4.ico` - 数字4
  - `Resources/Icons/5.ico` - 数字5
  - `Resources/Icons/6.ico` - 数字6
  - `Resources/Icons/7.ico` - 数字7
  - `Resources/Icons/8.ico` - 数字8
  - `Resources/Icons/9.ico` - 数字9

---

## 🔔 通知機能の詳細（改訂版）

### 通知タイミング
- **設定で指定した時刻に1日1回チェック**（デフォルト: 12:00 正午）
- 各友人について「今日が通知日か」を判定
- **通知対象**: 月日が登録されている友人のみ（年は任意）

### 通知内容
```
┌────────────────────────────────────┐
│ 🎂 友人の誕生日が近づいています     │
├────────────────────────────────────┤
│ 山田太郎さんの誕生日まで あと3日    │
│ 誕生日: 11月20日                   │
│                                    │
│ [詳細を見る]                        │
└────────────────────────────────────┘
```

- **音声**: Windows 標準通知音、または設定でOFF
- **クリックアクション**: 編集画面（画面C）を開く

### 通知ロジック（重要）

**「X日前から通知」= 「X日前の日から誕生日当日まで毎日1回、設定時刻に通知」**

```
例: 山田太郎さん (誕生日: 5月15日)
     個人設定=3日前から通知
     通知時刻=12:00

- 5月12日 12:00 → 「山田太郎さん (5/15) まであと3日」
- 5月13日 12:00 → 「山田太郎さん (5/15) まであと2日」
- 5月14日 12:00 → 「山田太郎さん (5/15) まであと1日」
- 5月15日 12:00 → 「今日は山田太郎さんの誕生日です！🎉」
```

**重複防止**:
- `notification_history` テーブルに記録
- `(friend_id, notification_date)` の組み合わせで判定
- 同日に複数回通知されることはない

**通知対象の判定**:
```csharp
public bool ShouldNotifyToday(Friend friend, DateTime today, int daysBefore)
{
    if (!friend.NotifyEnabled) return false;
    if (!friend.BirthMonth.HasValue || !friend.BirthDay.HasValue) return false; // 月日が必要

    var nextBirthday = CalculateNextBirthday(today, friend.BirthMonth.Value, friend.BirthDay.Value);
    var daysUntil = (nextBirthday.Date - today.Date).Days; // 時刻部分を除外して計算

    return daysUntil >= 0 && daysUntil <= daysBefore;
}
```

**エラーハンドリング**:
- 通知API呼び出し失敗時はログに記録、次回リトライ
- 3回連続失敗した場合はタスクトレイにエラーアイコン表示

---

## 🔍 検索機能の詳細（改訂版）

### SQLite FTS5による高速検索

**FTS5の利点**:
- 部分一致検索が高速
- トークン化による柔軟な検索
- ランキング機能（関連度順）

**検索クエリ例**:
```csharp
// 名前・メモのフルテキスト検索
var friendsFromFts = await dbContext.Database
    .SqlQuery<int>($"SELECT rowid FROM friends_fts WHERE friends_fts MATCH {keyword}")
    .ToListAsync();

// エイリアステーブルから検索
var friendsFromAlias = await dbContext.Aliases
    .Where(a => a.Alias.Contains(keyword))
    .Select(a => a.FriendId)
    .ToListAsync();

// 結果をマージ
var friendIds = friendsFromFts.Union(friendsFromAlias).Distinct();
var friends = await dbContext.Friends
    .Where(f => friendIds.Contains(f.Id))
    .ToListAsync();
```

### 検索対象フィールド
- 名前（FTS5）
- エイリアス（正規化されたテーブルから検索）
- メモ（FTS5）

### パフォーマンス目標
- 1,000件のデータで検索応答時間 < 50ms
- 10,000件のデータで検索応答時間 < 200ms

---

## 🧩 アーキテクチャ設計（改訂版）

### MVVM + DI パターン

```
┌─────────────┐
│    View     │  (XAML)
│  (UI Layer) │  - キーボードショートカット
└──────┬──────┘  - アクセシビリティ属性
       │ Data Binding, Commands
┌──────▼──────┐
│  ViewModel  │  (INotifyPropertyChanged)
│             │  - CommunityToolkit.Mvvm使用
└──────┬──────┘  - [ObservableProperty], [RelayCommand]
       │ DI注入
┌──────▼──────┐
│  Services   │  (Business Logic)
│             │  - NotificationService
└──────┬──────┘  - TrayIconService
       │         - CsvService
┌──────▼──────┐
│ Repository  │  (Data Access Layer)
│             │  - IFriendRepository
└──────┬──────┘  - ISettingsRepository
       │
┌──────▼──────┐
│   EF Core   │  (ORM)
│   SQLite    │
└─────────────┘
```

### Dependency Injection設定

```csharp
// App.xaml.cs
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite("Data Source=friends.db"));

        // Repositories
        services.AddScoped<IFriendRepository, FriendRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();

        // Services
        // NOTE: NotificationServiceとTrayIconServiceはSingletonだが、
        // DbContextを使用する場合はIServiceProviderを注入してスコープを作成する
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddScoped<ICsvService, CsvService>();
        services.AddScoped<IStartupService, StartupService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ListViewModel>();
        services.AddTransient<EditViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog(new LoggerConfiguration()
                .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
                .CreateLogger());
        });
    }
}
```

### 主要クラス設計（改訂版）

#### Model

```csharp
public class Friend
{
    public int Id { get; set; }

    public required string Name { get; set; }  // C# 11+ required修飾子

    public int? BirthYear { get; set; }   // 誕生年（例: 2000）
    public int? BirthMonth { get; set; }  // 誕生月（1-12）
    public int? BirthDay { get; set; }    // 誕生日（1-31）

    public string? Memo { get; set; }

    public int? NotifyDaysBefore { get; set; }  // NULL = use default

    public bool NotifyEnabled { get; set; } = true;

    public bool? NotifySoundEnabled { get; set; }  // NULL = use default

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Alias> Aliases { get; set; } = new List<Alias>();

    // ヘルパーメソッド
    public bool HasValidBirthdayForNotification()
    {
        // 月と日の両方が入力されている場合のみ通知対象
        return BirthMonth.HasValue && BirthDay.HasValue;
    }

    public int? CalculateDaysUntilBirthday(DateTime referenceDate)
    {
        if (!HasValidBirthdayForNotification()) return null;

        int year = referenceDate.Year;
        int month = BirthMonth!.Value;
        int day = BirthDay!.Value;

        // うるう年処理: 2月29日生まれで平年の場合は2月28日にフォールバック
        if (month == 2 && day == 29 && !DateTime.IsLeapYear(year))
        {
            day = 28;
        }

        var nextBirthday = new DateTime(year, month, day);

        if (nextBirthday < referenceDate)
        {
            // 来年の誕生日を計算（うるう年処理を再適用）
            year++;
            if (month == 2 && BirthDay == 29 && !DateTime.IsLeapYear(year))
            {
                day = 28;
            }
            else
            {
                day = BirthDay!.Value;
            }
            nextBirthday = new DateTime(year, month, day);
        }

        return (nextBirthday - referenceDate).Days;
    }

    // 表示用の誕生日文字列を生成
    public string GetBirthdayDisplayString()
    {
        if (BirthYear.HasValue && BirthMonth.HasValue && BirthDay.HasValue)
            return $"{BirthYear:0000}-{BirthMonth:00}-{BirthDay:00}";
        if (BirthMonth.HasValue && BirthDay.HasValue)
            return $"{BirthMonth:00}-{BirthDay:00}";
        if (BirthYear.HasValue)
            return $"{BirthYear}年";
        if (BirthMonth.HasValue)
            return $"{BirthMonth}月";
        if (BirthDay.HasValue)
            return $"{BirthDay}日";
        return "未設定";
    }
}

public class Alias
{
    public int Id { get; set; }
    public int FriendId { get; set; }
    public required string Alias { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public Friend Friend { get; set; } = null!;
}

public class AppSettings
{
    public int DefaultNotifyDaysBefore { get; set; } = 1;
    public bool DefaultNotifySound { get; set; } = true;
    public TimeSpan NotificationTime { get; set; } = new TimeSpan(12, 0, 0);
    public bool StartWithWindows { get; set; } = false;
    public string Language { get; set; } = "ja-JP";
}
```

#### Repository

```csharp
public interface IFriendRepository
{
    Task<List<Friend>> GetAllAsync();
    Task<Friend?> GetByIdAsync(int id);
    Task<int> AddAsync(Friend friend);
    Task UpdateAsync(Friend friend);
    Task DeleteAsync(int id);
    Task<List<Friend>> SearchAsync(string keyword);
    Task<List<Friend>> GetUpcomingBirthdaysAsync(DateTime referenceDate, int count);
}

public interface ISettingsRepository
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<AppSettings> GetAppSettingsAsync();
    Task SaveAppSettingsAsync(AppSettings settings);
    Task<int> GetSchemaVersionAsync();
    Task SetSchemaVersionAsync(int version);
}
```

#### Service

```csharp
public interface INotificationService
{
    Task CheckAndNotifyAsync(CancellationToken cancellationToken = default);
    Task<bool> ShowNotificationAsync(Friend friend, int daysUntil);
}

// 実装例: SingletonサービスでScopedなDbContextを使用する方法
public class NotificationService : INotificationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IServiceProvider serviceProvider, ILogger<NotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task CheckAndNotifyAsync(CancellationToken cancellationToken = default)
    {
        // スコープを作成してDbContextを取得
        using var scope = _serviceProvider.CreateScope();
        var friendRepository = scope.ServiceProvider.GetRequiredService<IFriendRepository>();

        var friends = await friendRepository.GetAllAsync();
        // 通知処理...
    }
}

public interface ITrayIconService
{
    void Initialize();
    void UpdateIcon(int? daysUntilNextBirthday);
    void ShowBalloonTip(string title, string message);
    void Dispose();
}

public interface ICsvService
{
    Task<bool> ExportAsync(string filePath);
    Task<ImportResult> ImportAsync(string filePath);
}

public record ImportResult(
    int SuccessCount,
    int SkippedCount,
    List<string> Errors
);
```

---

## 🛡️ セキュリティ考慮事項

### 1. SQLインジェクション対策
- **Entity Framework Core使用**により、パラメータ化クエリが自動生成される
- 生SQLを使用する場合は必ず `SqlParameter` を使用

### 2. CSVインポート時の検証
```csharp
public async Task<ImportResult> ImportAsync(string filePath)
{
    // ファイルサイズチェック（10MB制限）
    var fileInfo = new FileInfo(filePath);
    if (fileInfo.Length > 10 * 1024 * 1024)
        throw new InvalidOperationException("ファイルサイズが大きすぎます（10MB以下）");

    // 行数チェック（DoS対策）
    var lineCount = File.ReadLines(filePath).Count();
    if (lineCount > 100_000)
        throw new InvalidOperationException("データ件数が多すぎます（10万件以下）");

    // 各フィールドの長さ検証
    // Excel数式インジェクション対策の実装は下記CsvServiceを参照
}

// Excel数式インジェクション対策の実装例
public string SanitizeForCsv(string value)
{
    if (string.IsNullOrEmpty(value))
        return value;

    // =, +, -, @で始まるセルに'を付加して無害化
    if (value.StartsWith("=") || value.StartsWith("+") ||
        value.StartsWith("-") || value.StartsWith("@"))
    {
        return "'" + value;
    }

    return value;
}

public async Task<bool> ExportAsync(string filePath)
{
    var friends = await _friendRepository.GetAllAsync();

    using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true)); // BOM付き
    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

    // ヘッダー書き込み
    await csv.WriteFieldAsync("name");
    await csv.WriteFieldAsync("birth_year");
    // ... (他のヘッダー)
    await csv.NextRecordAsync();

    // データ書き込み（Excel数式インジェクション対策適用）
    foreach (var friend in friends)
    {
        await csv.WriteFieldAsync(SanitizeForCsv(friend.Name));
        await csv.WriteFieldAsync(friend.BirthYear?.ToString() ?? "");
        await csv.WriteFieldAsync(friend.BirthMonth?.ToString() ?? "");
        await csv.WriteFieldAsync(friend.BirthDay?.ToString() ?? "");

        var aliases = string.Join(",", friend.Aliases.Select(a => SanitizeForCsv(a.Alias)));
        await csv.WriteFieldAsync(aliases);

        await csv.WriteFieldAsync(SanitizeForCsv(friend.Memo ?? ""));
        // ... (他のフィールド)
        await csv.NextRecordAsync();
    }

    return true;
}
```

### 3. パス操作の安全性
- データベースパス、ログパスは相対パスまたは絶対パスで指定可能
- パストラバーサル攻撃を防ぐため、外部入力を受け付ける場合は `Path.GetFullPath()` で正規化

---

## ⚡ パフォーマンス最適化

### 1. データベースインデックス
- すでに設計セクションで定義済み
- 定期的に `ANALYZE` コマンド実行（統計情報更新）

### 2. UI仮想化
- 100件以上のリスト表示時は `VirtualizingStackPanel` 使用

### 3. 非同期処理
- すべてのDB操作は非同期（async/await）
- UI スレッドをブロックしない

### 4. キャッシュ戦略
- 設定情報はメモリキャッシュ（起動時に読み込み、変更時のみDB書き込み）
- 友人リストは変更時に無効化

### 5. パフォーマンステスト計画
- 100件、1,000件、10,000件のデータセットでベンチマーク
- 起動時間 < 3秒
- 検索応答時間: 1,000件 < 50ms、10,000件 < 200ms

---

## 🧪 テスト戦略（拡充版）

### 1. 単体テスト（xUnit）

**カバレッジ目標**: 80%以上

```csharp
public class FriendTests
{
    [Theory]
    [InlineData(2000, 5, 15, true)]    // 年月日 → 通知可能
    [InlineData(null, 5, 15, true)]    // 月日のみ → 通知可能
    [InlineData(2000, null, null, false)]  // 年のみ → 通知不可
    [InlineData(null, 5, null, false)]     // 月のみ → 通知不可
    [InlineData(null, null, 15, false)]    // 日のみ → 通知不可
    [InlineData(null, null, null, false)]  // 未入力 → 通知不可
    public void HasValidBirthdayForNotification_ReturnsExpected(
        int? year, int? month, int? day, bool expected)
    {
        var friend = new Friend
        {
            Name = "Test",
            BirthYear = year,
            BirthMonth = month,
            BirthDay = day
        };
        friend.HasValidBirthdayForNotification().Should().Be(expected);
    }

    [Theory]
    [InlineData(2000, 5, 15, "2000-05-15")]   // 完全形式
    [InlineData(null, 5, 15, "05-15")]        // 月日のみ
    [InlineData(2000, null, null, "2000年")]  // 年のみ
    [InlineData(null, 5, null, "5月")]        // 月のみ
    [InlineData(null, null, 15, "15日")]      // 日のみ
    [InlineData(null, null, null, "未設定")]  // 未入力
    public void GetBirthdayDisplayString_ReturnsExpected(
        int? year, int? month, int? day, string expected)
    {
        var friend = new Friend
        {
            Name = "Test",
            BirthYear = year,
            BirthMonth = month,
            BirthDay = day
        };
        friend.GetBirthdayDisplayString().Should().Be(expected);
    }

    [Fact]
    public void CalculateDaysUntilBirthday_LeapYear_ReturnsCorrectDays()
    {
        var friend = new Friend
        {
            Name = "Test",
            BirthMonth = 2,
            BirthDay = 29
        };
        var referenceDate = new DateTime(2024, 2, 28); // うるう年
        friend.CalculateDaysUntilBirthday(referenceDate).Should().Be(1);

        referenceDate = new DateTime(2023, 2, 28); // 平年
        friend.CalculateDaysUntilBirthday(referenceDate).Should().Be(0); // 2/28に通知
    }
}

public class NotificationServiceTests
{
    [Fact]
    public async Task CheckAndNotifyAsync_NotifiesCorrectly()
    {
        // Arrange
        var mockRepo = new Mock<IFriendRepository>();
        var mockNotification = new Mock<INotificationService>();
        // ...

        // Act
        await service.CheckAndNotifyAsync();

        // Assert
        mockNotification.Verify(x => x.ShowNotificationAsync(It.IsAny<Friend>(), 3), Times.Once);
    }
}
```

### 2. 統合テスト

```csharp
public class DatabaseIntegrationTests : IDisposable
{
    private readonly AppDbContext _context;

    [Fact]
    public async Task AddFriend_WithAliases_PersistsCorrectly()
    {
        // Arrange
        var friend = new Friend
        {
            Name = "Test User",
            BirthMonth = 5,
            BirthDay = 15,
            Aliases = new List<Alias>
            {
                new() { Alias = "test1" },
                new() { Alias = "test2" }
            }
        };

        // Act
        _context.Friends.Add(friend);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.Friends
            .Include(f => f.Aliases)
            .FirstAsync(f => f.Name == "Test User");
        retrieved.Aliases.Should().HaveCount(2);
    }
}
```

### 3. UIテスト（手動 + 自動化検討）

**手動テストケース**:
- [ ] 友人登録→一覧表示→編集→削除の一連フロー
- [ ] 検索機能（部分一致、エイリアス検索）
- [ ] 並び替え（近い順、日付順、名前順）
- [ ] CSV エクスポート→インポート→データ整合性確認
- [ ] 通知表示（タイミング、内容、音声）
- [ ] タスクトレイアイコン更新
- [ ] スタートアップ登録→PC再起動→自動起動確認

**自動UIテスト（将来実装）**:
- Appium for WPF / FlaUI 使用を検討

### 4. エッジケーステスト

- [ ] うるう年（2月29日生まれ、平年の場合）
- [ ] 同名同誕生日の友人を複数登録
- [ ] 1,000件以上の友人登録
- [ ] タイムゾーン変更時の挙動
- [ ] システム時刻を過去に変更
- [ ] データベースファイル破損
- [ ] ディスク容量不足
- [ ] アプリケーション多重起動
- [ ] 通知API呼び出し失敗
- [ ] 特殊文字を含む名前（絵文字、制御文字）
- [ ] 超長文メモ（5,000文字）
- [ ] 部分入力（年のみ、月のみ、日のみ）の動作確認
- [ ] 1-12の曖昧な数値（月として扱われるか確認）

---

## 📄 CSV仕様（明確化版）

### フォーマット

```csv
name,birth_year,birth_month,birth_day,aliases,memo,notify_days_before,notify_enabled,notify_sound_enabled
山田太郎,2000,5,15,"tarou,taro,たろー",高校時代の友人,3,1,1
佐藤花子,,5,25,"hanako,はなこ","大学の先輩
2行目のメモ",,1,
鈴木一郎,2000,,,ichiro,2000年生まれ（月日不明）,,,1
田中花子,,5,,hanako,5月生まれ（日不明）,,,1
田中次郎,,,,,,,,,0
```

**重要な変更点**:
- `birthday` カラムを `birth_year`, `birth_month`, `birth_day` の3カラムに分割
- これにより「5月生まれ」(birth_month=5) と「5日生まれ」(birth_day=5) が明確に区別できる

### 仕様詳細

- **エンコーディング**: UTF-8 BOM付き
- **改行コード**: CRLF (Windows標準)
- **ヘッダー行**: 必須
- **NULL値**: 空欄（連続カンマ）
- **エスケープルール**（RFC 4180準拠）:
  - カンマを含む値: ダブルクォートで囲む
  - ダブルクォートを含む値: `""` でエスケープ
  - 改行を含む値: ダブルクォートで囲む
- **バリデーション**:
  - 必須カラム: `name`
  - 各フィールドの最大長チェック
  - 誕生日検証: birth_year (1900-2100), birth_month (1-12), birth_day (1-31)

### エクスポート時の注意
- Excel数式インジェクション対策: `=`, `+`, `-`, `@` で始まるセルの先頭に `'`（シングルクォート）を付加して無害化

### インポート時の検証

```csharp
public class CsvValidator
{
    public ValidationResult Validate(string[] row, int lineNumber)
    {
        // 必須チェック: name（row[0]）
        if (string.IsNullOrWhiteSpace(row[0]))
            return ValidationResult.Error($"Line {lineNumber}: Name is required");

        // 長さチェック
        if (row[0].Length > 200)
            return ValidationResult.Error($"Line {lineNumber}: Name too long (max 200 chars)");

        // birth_year（row[1]）検証
        if (!string.IsNullOrEmpty(row[1]))
        {
            if (!int.TryParse(row[1], out int year) || year < 1900 || year > 2100)
                return ValidationResult.Error($"Line {lineNumber}: Invalid birth_year (1900-2100)");
        }

        // birth_month（row[2]）検証
        if (!string.IsNullOrEmpty(row[2]))
        {
            if (!int.TryParse(row[2], out int month) || month < 1 || month > 12)
                return ValidationResult.Error($"Line {lineNumber}: Invalid birth_month (1-12)");
        }

        // birth_day（row[3]）検証
        if (!string.IsNullOrEmpty(row[3]))
        {
            if (!int.TryParse(row[3], out int day) || day < 1 || day > 31)
                return ValidationResult.Error($"Line {lineNumber}: Invalid birth_day (1-31)");
        }

        return ValidationResult.Success();
    }
}

public class ValidationResult
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }

    private ValidationResult(bool isSuccess, string errorMessage = "")
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new ValidationResult(true);
    public static ValidationResult Error(string message) => new ValidationResult(false, message);
}
```

---

## 📁 プロジェクト構成（改訂版）

```
FriendBirthdayManager/
│
├── FriendBirthdayManager.sln
│
├── src/
│   └── FriendBirthdayManager/
│       ├── FriendBirthdayManager.csproj
│       │
│       ├── App.xaml
│       ├── App.xaml.cs                    # DI設定
│       │
│       ├── Models/
│       │   ├── Friend.cs
│       │   ├── Alias.cs                   # 正規化
│       │   ├── AppSettings.cs
│       │   ├── NotificationHistory.cs
│       │   └── BirthdayComponents.cs      # 誕生日パース用
│       │
│       ├── Views/
│       │   ├── MainWindow.xaml
│       │   ├── ListWindow.xaml
│       │   ├── EditWindow.xaml
│       │   ├── SettingsWindow.xaml
│       │   └── AboutWindow.xaml
│       │
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   ├── ListViewModel.cs
│       │   ├── EditViewModel.cs
│       │   ├── SettingsViewModel.cs
│       │   └── AboutViewModel.cs
│       │
│       ├── Services/
│       │   ├── INotificationService.cs
│       │   ├── NotificationService.cs
│       │   ├── ITrayIconService.cs
│       │   ├── TrayIconService.cs
│       │   ├── ICsvService.cs
│       │   ├── CsvService.cs
│       │   ├── IStartupService.cs
│       │   └── StartupService.cs
│       │
│       ├── Repositories/
│       │   ├── IFriendRepository.cs
│       │   ├── FriendRepository.cs
│       │   ├── ISettingsRepository.cs
│       │   ├── SettingsRepository.cs
│       │   └── AppDbContext.cs            # EF Core DbContext
│       │
│       ├── Migrations/                     # EF Core マイグレーション
│       │   ├── IDatabaseMigration.cs
│       │   ├── Migration_001_Initial.cs
│       │   └── MigrationRunner.cs
│       │
│       ├── Helpers/
│       │   ├── DateHelper.cs
│       │   ├── IconSelector.cs            # 静的アイコンファイル選択
│       │   ├── ValidationHelper.cs
│       │   └── CsvValidator.cs
│       │
│       ├── Resources/
│       │   ├── Icons/
│       │   │   ├── birthday.ico          # 誕生日ケーキ（デフォルト）
│       │   │   ├── 1.ico                 # 数字1~9
│       │   │   ├── 2.ico
│       │   │   ├── 3.ico
│       │   │   ├── 4.ico
│       │   │   ├── 5.ico
│       │   │   ├── 6.ico
│       │   │   ├── 7.ico
│       │   │   ├── 8.ico
│       │   │   └── 9.ico
│       │   ├── Sounds/
│       │   │   └── notification.wav
│       │   └── Strings/                   # i18n対応
│       │       ├── Resources.ja-JP.resx
│       │       └── Resources.en-US.resx
│       │
│       └── Data/
│           └── friends.db                  # SQLite DB（実行時生成）
│
├── tests/
│   └── FriendBirthdayManager.Tests/
│       ├── Models/
│       │   └── FriendTests.cs
│       ├── Services/
│       │   ├── NotificationServiceTests.cs
│       │   └── CsvServiceTests.cs
│       ├── Repositories/
│       │   └── FriendRepositoryTests.cs
│       └── Integration/
│           └── DatabaseIntegrationTests.cs
│
├── docs/
│   ├── PLAN.md                             # 本ファイル
│   ├── ARCHITECTURE.md                     # アーキテクチャ詳細
│   ├── DATABASE.md                         # DB詳細仕様
│   └── API.md                              # 内部API仕様
│
├── .gitignore
├── README.md
└── LICENSE                                 # MIT License
```

---

## 🗓️ 実装計画（フェーズ分け・改訂版）

### Phase 1: プロジェクト基盤構築（期間: 2-3日）
- [ ] プロジェクト作成（.NET 8 WPF）
- [ ] NuGetパッケージ導入
  - Microsoft.EntityFrameworkCore.Sqlite
  - Hardcodet.NotifyIcon.Wpf
  - Microsoft.Toolkit.Uwp.Notifications
  - CommunityToolkit.Mvvm
  - Microsoft.Extensions.DependencyInjection
  - Serilog
  - xUnit, FluentAssertions, Moq
- [ ] フォルダ構成作成
- [ ] DI設定（App.xaml.cs）
- [ ] ログ設定（Serilog）

**マイルストーン**: 空のアプリが起動し、ログが出力される

---

### Phase 2: データ層実装（期間: 3-4日）
- [ ] Model クラス作成（Friend, Alias, AppSettings）
- [ ] AppDbContext 実装
- [ ] Repository 実装（IFriendRepository, ISettingsRepository）
- [ ] データベース初期化処理
- [ ] マイグレーション機能実装
- [ ] 単体テスト作成（Model, Repository）

**依存関係**: なし
**マイルストーン**: データベースにCRUD操作が可能

---

### Phase 3: 基本UI実装（期間: 4-5日）
- [ ] 画面A: メイン画面（追加）
- [ ] 画面B: 一覧画面
- [ ] 画面C: 編集画面
- [ ] ViewModel実装（MVVM）
- [ ] 入力バリデーション
- [ ] データバインディング
- [ ] キーボードショートカット

**依存関係**: Phase 2完了
**マイルストーン**: GUIで友人の登録・編集・削除が可能

---

### Phase 4: タスクトレイ機能（期間: 2-3日）
- [ ] タスクトレイアイコン表示
- [ ] 右クリックメニュー
- [ ] 動的アイコン生成（SkiaSharp）
- [ ] アイコン更新ロジック

**依存関係**: Phase 3完了
**マイルストーン**: タスクトレイ常駐、アイコンに日数表示

---

### Phase 5: 通知機能（期間: 3-4日）
- [ ] NotificationService 実装
- [ ] Windows トースト通知
- [ ] 通知タイミング制御（バックグラウンドタイマー）
- [ ] 重複通知防止（notification_history）
- [ ] 音声通知制御
- [ ] 通知対象判定ロジック
- [ ] エラーハンドリング（通知失敗時）

**依存関係**: Phase 2, 4完了
**マイルストーン**: 設定時刻に通知が表示される

---

### Phase 6: 検索・ソート機能（期間: 2-3日）
- [ ] FTS5仮想テーブル作成
- [ ] エイリアス検索実装
- [ ] 即時検索（リアルタイムフィルター）
- [ ] 並び替え（近い順、日付順、名前順）
- [ ] Unicode順ソート（アプリケーション層で実装）

**依存関係**: Phase 3完了
**マイルストーン**: 検索が高速（1,000件で<100ms）

---

### Phase 7: 設定機能（期間: 2-3日）
- [ ] 画面D: 設定画面（タブUI）
- [ ] スタートアップ登録（タスクスケジューラ）
- [ ] デフォルト通知設定
- [ ] CSV エクスポート/インポート
- [ ] 設定永続化
- [ ] CSVバリデーション

**依存関係**: Phase 2完了
**マイルストーン**: CSV I/O、スタートアップ登録が動作

---

### Phase 8: 細部調整・UX改善（期間: 2-3日）
- [ ] 画面E: クレジット画面
- [ ] エラーハンドリング強化
- [ ] ローディング表示
- [ ] 確認ダイアログ
- [ ] ツールチップ・ヘルプ
- [ ] アクセシビリティ対応
- [ ] i18n基盤（リソースファイル）

**依存関係**: Phase 1-7完了
**マイルストーン**: ユーザー体験が向上

---

### Phase 9: テスト（期間: 3-4日）
- [ ] 単体テストカバレッジ80%達成
- [ ] 統合テスト
- [ ] 手動UIテスト（全機能）
- [ ] パフォーマンステスト（100/1,000/10,000件）
- [ ] エッジケーステスト（うるう年、重複、etc.）
- [ ] セキュリティテスト（CSV悪意ファイル）

**依存関係**: Phase 1-8完了
**マイルストーン**: バグ0件、パフォーマンス目標達成（1,000件 < 50ms、10,000件 < 200ms）

---

### Phase 10: ビルド・配布（期間: 1-2日）
- [ ] リリースビルド
- [ ] 単一実行ファイル化（PublishSingleFile）
- [ ] インストーラー作成（WiX or Inno Setup、オプション）
- [ ] README・ドキュメント整備
- [ ] GitHub Releases公開

**依存関係**: Phase 9完了
**マイルストーン**: v1.0.0リリース

---

**合計期間見積もり**: 24-34日（約1ヶ月）

---

## 🎯 開発優先順位（MVP定義）

### 高優先度（MVP: Minimum Viable Product）
1. ✅ データベース設計・実装（Phase 2）
2. ✅ 友人登録・一覧表示（Phase 3）
3. ✅ タスクトレイ常駐（Phase 4）
4. ✅ 基本通知機能（Phase 5）

**MVPゴール**: 友人の誕生日を登録し、通知を受け取れる

---

### 中優先度
5. エイリアス検索（Phase 6）
6. 編集機能（Phase 3）
7. 設定画面（Phase 7）
8. CSV エクスポート（Phase 7）

---

### 低優先度（後で追加可能）
9. CSV インポート（Phase 7）
10. 高度な並び替え（Phase 6）
11. i18n対応（Phase 8）
12. 統計情報（将来バージョン）

---

## 🖼️ アイコン仕様（改訂版）

### タスクトレイアイコン（静的ファイル使用）

**サイズ**: 16x16px、32x32px、48x48px（マルチサイズICO）
**形式**: ICO形式（推奨）
**配置場所**: `Resources/Icons/` ディレクトリ

**必要なアイコンファイル**:
- `birthday.ico` - 誕生日ケーキ（デフォルト、10日以上先または誕生日当日）
- `1.ico` - 数字1（1日前）
- `2.ico` - 数字2（2日前）
- `3.ico` - 数字3（3日前）
- `4.ico` - 数字4（4日前）
- `5.ico` - 数字5（5日前）
- `6.ico` - 数字6（6日前）
- `7.ico` - 数字7（7日前）
- `8.ico` - 数字8（8日前）
- `9.ico` - 数字9（9日前）

**アイコン選択ロジック**:
```csharp
public class IconSelector
{
    private readonly Dictionary<int, Icon> _iconCache = new();
    private readonly Icon _defaultIcon;

    public IconSelector()
    {
        // アイコンファイルを事前読み込み
        _defaultIcon = LoadIcon("Resources/Icons/birthday.ico");

        for (int i = 1; i <= 9; i++)
        {
            _iconCache[i] = LoadIcon($"Resources/Icons/{i}.ico");
        }
    }

    public Icon GetTrayIcon(int? daysUntil)
    {
        if (daysUntil.HasValue && daysUntil.Value >= 1 && daysUntil.Value <= 9)
        {
            return _iconCache[daysUntil.Value];
        }

        return _defaultIcon; // 10日以上先、または誕生日当日、またはnull
    }

    private Icon LoadIcon(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Icon file not found: {path}");
        }

        return new Icon(path);
    }
}
```

**注意事項**:
- アイコンファイルは事前に用意する必要があります
- アイコンはマルチサイズ（16x16, 32x32, 48x48）を含むICO形式を推奨
- SkiaSharpライブラリは不要になります（依存関係を削減）

---

## 🌐 国際化（i18n）対応

### リソースファイル構成

```
Resources/
  Strings/
    Resources.resx           (デフォルト: 日本語)
    Resources.ja-JP.resx
    Resources.en-US.resx
```

### 使用方法

```csharp
// ViewModelで使用
public string WelcomeMessage => Resources.WelcomeMessage;

// XAMLで使用
<TextBlock Text="{x:Static resources:Resources.WelcomeMessage}" />
```

### 対応言語（将来）
- 日本語（ja-JP）: v1.0で対応
- 英語（en-US）: v1.1で対応予定

---

## 📝 ログ戦略

### Serilog設定

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(
        "logs/app.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
```

### ログレベル
- **Verbose**: 詳細なデバッグ情報
- **Debug**: 開発時のデバッグ情報
- **Information**: 通常の動作ログ（デフォルト）
- **Warning**: 警告（通知失敗など）
- **Error**: エラー（例外発生）
- **Fatal**: 致命的エラー（アプリクラッシュ）

### ログ出力例

```csharp
_logger.LogInformation("Friend added: {FriendName}, Birthday: {Birthday}", friend.Name, friend.Birthday);
_logger.LogWarning("Notification failed for friend {FriendId}, retry count: {RetryCount}", friendId, retryCount);
_logger.LogError(ex, "Database operation failed");
```

---

## ⚠️ 注意事項・制約（改訂版）

### 1. 重複登録制御

**基本方針**:
- データベース制約ではなく、**アプリケーションロジックで制御**
- 同名同誕生日の友人が既に存在する場合、確認ダイアログを表示
- ユーザーが許可すれば重複登録可能（メモやエイリアスで区別することを推奨）

#### 新規登録時の重複チェック

```csharp
public async Task<bool> AddAsync(Friend friend)
{
    // 同名同誕生日のレコードを検索
    // NOTE: SQLではNULL == NULLはfalseになるため、NULL値の比較は個別に処理する必要がある
    var duplicate = await _context.Friends
        .Where(f => f.Name == friend.Name
                 && (f.BirthYear == friend.BirthYear || (f.BirthYear == null && friend.BirthYear == null))
                 && (f.BirthMonth == friend.BirthMonth || (f.BirthMonth == null && friend.BirthMonth == null))
                 && (f.BirthDay == friend.BirthDay || (f.BirthDay == null && friend.BirthDay == null)))
        .FirstOrDefaultAsync();

    if (duplicate != null)
    {
        var birthdayStr = friend.GetBirthdayDisplayString();
        var result = await ShowConfirmationDialog(
            $"{friend.Name}（{birthdayStr}）が既に登録されています。\n" +
            "同名同誕生日の別人として登録する場合は、メモやエイリアスで区別してください。\n" +
            "登録しますか？"
        );

        if (!result) return false;  // ユーザーがキャンセル
    }

    // 登録処理
    _context.Friends.Add(friend);
    await _context.SaveChangesAsync();
    return true;
}
```

#### 更新時の重複チェック

**重要**: 更新時は**自分自身（同じID）を除外**してチェックする必要があります。

**問題となるケース**:
```
初期状態:
  ID=1: 山田太郎, BirthYear=2000, BirthMonth=5, BirthDay=5
  ID=2: 山田太郎, BirthYear=2000, BirthMonth=5, BirthDay=4

ID=2を編集してBirthDay=5に変更した場合:
  → ID=1と同名同誕生日になってしまう
```

**実装例**:
```csharp
public async Task<bool> UpdateAsync(Friend friend)
{
    // 自分自身を除外して同名同誕生日のレコードを検索
    // NOTE: SQLではNULL == NULLはfalseになるため、NULL値の比較は個別に処理する必要がある
    var duplicate = await _context.Friends
        .Where(f => f.Id != friend.Id)  // 👈 自分自身を除外
        .Where(f => f.Name == friend.Name
                 && (f.BirthYear == friend.BirthYear || (f.BirthYear == null && friend.BirthYear == null))
                 && (f.BirthMonth == friend.BirthMonth || (f.BirthMonth == null && friend.BirthMonth == null))
                 && (f.BirthDay == friend.BirthDay || (f.BirthDay == null && friend.BirthDay == null)))
        .FirstOrDefaultAsync();

    if (duplicate != null)
    {
        var birthdayStr = friend.GetBirthdayDisplayString();
        var result = await ShowConfirmationDialog(
            $"{friend.Name}（{birthdayStr}）が既に登録されています（ID={duplicate.Id}）。\n" +
            "同名同誕生日になりますが、更新しますか？"
        );

        if (!result) return false;  // ユーザーがキャンセル
    }

    // 更新処理
    _context.Friends.Update(friend);
    await _context.SaveChangesAsync();
    return true;
}
```

#### テストケース

```csharp
[Fact]
public async Task UpdateAsync_WhenCreatingDuplicate_ShowsConfirmation()
{
    // Arrange
    var friend1 = new Friend
    {
        Id = 1,
        Name = "山田太郎",
        BirthYear = 2000,
        BirthMonth = 5,
        BirthDay = 5
    };
    var friend2 = new Friend
    {
        Id = 2,
        Name = "山田太郎",
        BirthYear = 2000,
        BirthMonth = 5,
        BirthDay = 4
    };
    await _repository.AddAsync(friend1);
    await _repository.AddAsync(friend2);

    // Act: friend2の誕生日をfriend1と同じにする
    friend2.BirthDay = 5;
    var result = await _repository.UpdateAsync(friend2);

    // Assert: 確認ダイアログが表示されるべき
    _mockDialog.Verify(x => x.ShowConfirmationDialog(It.IsAny<string>()), Times.Once);
}

[Fact]
public async Task UpdateAsync_WhenNotCreatingDuplicate_NoConfirmation()
{
    // Arrange
    var friend = new Friend
    {
        Id = 1,
        Name = "山田太郎",
        BirthYear = 2000,
        BirthMonth = 5,
        BirthDay = 5
    };
    await _repository.AddAsync(friend);

    // Act: 自分自身の誕生日を変更（重複にならない）
    friend.BirthDay = 6;
    var result = await _repository.UpdateAsync(friend);

    // Assert: 確認ダイアログは表示されない
    _mockDialog.Verify(x => x.ShowConfirmationDialog(It.IsAny<string>()), Times.Never);
}
```

### 2. 誕生日入力パターンと通知可否

| パターン | DB保存例 | 通知 | 備考 |
|---------|---------|------|------|
| 年月日 | BirthYear=2000, BirthMonth=5, BirthDay=15 | ✅ | 毎年5月15日に通知 |
| 月日のみ | BirthYear=NULL, BirthMonth=5, BirthDay=15 | ✅ | 毎年5月15日に通知 |
| 年のみ | BirthYear=2000, BirthMonth=NULL, BirthDay=NULL | ❌ | 通知なし、「2000年生まれ」を記録可能 |
| 月のみ | BirthYear=NULL, BirthMonth=5, BirthDay=NULL | ❌ | 通知なし、「5月生まれ」を記録可能 |
| 日のみ | BirthYear=NULL, BirthMonth=NULL, BirthDay=15 | ❌ | 通知なし、「15日生まれ」を記録可能 |
| 未入力 | 全てNULL | ❌ | 通知なし、一覧では最後に表示 |

**重要**: 通知が行われるのは「BirthMonth と BirthDay の両方が入力されている場合のみ」

**解決された問題**:
- ✅ 「5月生まれ」(BirthMonth=5, BirthDay=NULL) と「5日生まれ」(BirthMonth=NULL, BirthDay=5) が明確に区別できる
- ✅ データベースレベルで曖昧性が排除された
- ✅ CHECK制約により不正な値（month=13など）を防止

#### 部分入力機能の背景と必要性

**なぜ部分入力が必要か**:

現実の人間関係では、完全な誕生日情報を得られないケースが多く存在します。この機能は、そうした**実際の社交状況に対応**するために設計されました。

**典型的なシナリオ**:

1. **プライバシーへの配慮**
   - 「誕生日いつ？」→「5月だけど、日は秘密」
   - 完全な日付を教えたくない友人も、部分的な情報なら共有してくれる

2. **情報の段階的な取得**
   - 初対面: 「何年生まれ？」→「2000年です」（年のみ）
   - 後日: 「誕生月は？」→「5月」（月を追加、年月が揃う）
   - さらに後: 「何日？」→「15日」（完全な日付になり、通知可能に）
   - **段階的に情報を更新できることで、自然なコミュニケーションが可能**

3. **記憶が曖昧な場合**
   - 「確か5月生まれだったと思うけど、日は覚えてない」
   - 部分的な情報でも記録しておけば、後で正確な情報を得たときに更新可能

4. **年齢だけを知りたい場合**
   - オンラインゲームのフレンド: 「何歳？」→「24歳（2000年生まれ）」
   - 誕生日通知は不要だが、年齢・世代の情報として記録したい

5. **誕生日非公開文化への対応**
   - 一部のコミュニティでは完全な誕生日を公開しない文化がある
   - それでも「同じ月生まれだね！」などの会話のために月だけ共有する

**設計哲学**:

- **完璧主義を避ける**: 「完全な情報がないと登録できない」という制約は、実用性を損なう
- **柔軟性の提供**: ユーザーが得られた情報の範囲で登録でき、後から追加・更新可能
- **プライバシーの尊重**: 友人が全情報を共有したくない場合にも対応
- **段階的な情報収集**: 時間をかけて情報を充実させていくスタイルをサポート

**技術的な実装ポイント**:

- 部分入力は通知の対象外（月日が必要）だが、一覧画面では表示される
- 後から情報を追加して「月日」が揃えば、自動的に通知対象になる
- データベースには文字列として柔軟に保存（"2000", "05", "15", "05-15", "2000-05-15"）

**ユースケース例**:

| シチュエーション | 取得できた情報 | 登録内容 | 後の展開 |
|-----------------|---------------|---------|---------|
| オンラインゲームで知り合った | 「2000年生まれです」 | 年のみ登録 | 親しくなったら月日を聞く |
| 飲み会で軽く聞いた | 「5月生まれです」 | 月のみ登録 | 次に会ったとき日を確認 |
| プライバシー重視の友人 | 「15日だけど年は秘密」 | 日のみ登録 | 本人が望まない限り年月は聞かない |
| SNSのプロフィールから | 「5月15日」 | 月日登録（通知可能） | 誕生日通知が届く |

**結論**: この機能は、完璧な情報を強制せず、**現実の人間関係の多様性に対応する**ためのものです。

### 3. 日付計算
- **うるう年対応必須**
  - 2月29日生まれ → 平年は2月28日に通知
  - `DateTime.IsLeapYear()` 使用

### 4. 並び替え「今日から近い順」の詳細

```
例: 今日が 11月14日の場合

1. 11月15日（あと1日）
2. 11月20日（あと6日）
3. 12月1日（あと17日）
4. 1月5日（あと52日）
5. 10月1日（あと321日） ← 来年の誕生日
6. 佐藤花子（5月のみ）--- ← 誕生日未設定・部分入力は最後（名前順でソート）
7. 鈴木一郎（2000年のみ）---
8. 田中次郎（未設定）---
```

**並び替えルール**:
- **近い順**: 誕生日までの日数（昇順）→ 同日の場合は名前順（Unicode順）
- **日付順**: 月日（1月1日→12月31日）→ 同日の場合は名前順
- **名前順**: Unicode順（C#のstring.Compare使用）
- **誕生日未設定・部分入力**: 常に最後に表示し、名前順でソート

### 5. CSV フォーマット

詳細は「## 📄 CSV仕様（明確化版）」セクションを参照してください。

---

## 🚀 開発開始手順

### 1. 環境準備

```bash
# .NET 8 SDK インストール確認
dotnet --version  # 8.0.x以上

# プロジェクト作成
dotnet new wpf -n FriendBirthdayManager -f net8.0-windows
cd FriendBirthdayManager

# NuGetパッケージ追加
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Hardcodet.NotifyIcon.Wpf
dotnet add package Microsoft.Toolkit.Uwp.Notifications
dotnet add package CommunityToolkit.Mvvm
dotnet add package Serilog.Sinks.File

# テストプロジェクト作成
dotnet new xunit -n FriendBirthdayManager.Tests
dotnet add FriendBirthdayManager.Tests package FluentAssertions
dotnet add FriendBirthdayManager.Tests package Moq
```

### 2. データベース初期化

```bash
# EF Core ツールインストール
dotnet tool install --global dotnet-ef

# 初期マイグレーション作成
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. 開発環境

- **推奨IDE**: Visual Studio 2022 / JetBrains Rider / Visual Studio Code
- **拡張機能**:
  - C# Dev Kit (VS Code)
  - ReSharper (Visual Studio)

---

## 📚 参考リソース

### 公式ドキュメント
- [WPF Documentation](https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/)
- [Entity Framework Core](https://learn.microsoft.com/ja-jp/ef/core/)
- [SQLite FTS5](https://www.sqlite.org/fts5.html)
- [Windows Notifications](https://learn.microsoft.com/ja-jp/windows/apps/design/shell/tiles-and-notifications/adaptive-interactive-toasts)

### ライブラリ
- [Hardcodet NotifyIcon WPF](https://github.com/hardcodet/wpf-notifyicon)
- [CommunityToolkit MVVM](https://learn.microsoft.com/ja-jp/dotnet/communitytoolkit/mvvm/)
- [Serilog](https://serilog.net/)

---

## ✅ 完成イメージ

### ゴール
- ✅ タスクトレイに常駐し、ユーザーが意識せずに使える
- ✅ 友人の誕生日を忘れない仕組み
- ✅ シンプルで直感的なUI
- ✅ 軽量で高速動作（起動3秒以内、10,000件で検索 < 200ms）
- ✅ テストカバレッジ80%以上
- ✅ セキュアで安全（SQLインジェクション対策、CSV検証）

### 成功指標
- [x] 10,000件の友人を登録してもスムーズに動作（検索 < 200ms）
- [x] 通知が確実に届く（失敗時はリトライ＆ログ記録）
- [x] エイリアス検索が高速（FTS5使用）
- [x] データのバックアップ・復元が簡単（CSV I/O）
- [x] 単体テストカバレッジ80%以上

---

**ドキュメント作成日**: 2025-11-14
**改訂日**: 2025-11-14
**バージョン**: 3.0（DB構造改訂版）
**作成者**: Claude (Anthropic)
**プロジェクトオーナー**: えりんぎ (@eringi_vrc)
**ライセンス**: MIT License

---

## 改訂履歴

| バージョン | 日付 | 変更内容 |
|----------|------|---------|
| 1.0 | 2025-11-14 | 初版作成 |
| 2.0 | 2025-11-14 | 全面改訂（DB正規化、FTS5、DI、セキュリティ、テスト強化） |
| 2.1 | 2025-11-14 | 部分入力対応（年のみ、月のみ、日のみの登録が可能に） |
| 2.2 | 2025-11-14 | 重複登録制御の詳細化（新規登録時・更新時の処理を明確化、テストケース追加） |
| 3.0 | 2025-11-14 | **DB構造改訂**: birthday TEXT → birth_year, birth_month, birth_day INTEGER に分割（曖昧性排除） |

---

## 次のステップ

1. ✅ **このドキュメントをレビュー完了**
2. **アイコン画像を準備**（10個のICOファイル: birthday.ico, 1.ico～9.ico）
3. **Phase 1（プロジェクト基盤構築）を開始**
4. GitHub リポジトリ作成、ブランチ運用ルール決定

**質問や懸念事項があれば、お気軽にお聞きください！**
