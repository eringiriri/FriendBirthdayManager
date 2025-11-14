# Friend Birthday Manager - 開発計画書

## 📋 プロジェクト概要

### 目的
友人の誕生日を管理し、タスクトレイに常駐して適切なタイミングで通知を行うWindowsデスクトップアプリケーション

### 主要機能
- タスクトレイ常駐
- 友人情報（名前、誕生日、エイリアス、メモ）の登録・編集・削除
- 柔軟な検索機能（エイリアス対応）
- 誕生日までの日数表示（アイコン上）
- カスタマイズ可能な通知設定（全体・個人）
- CSV エクスポート/インポート

---

## 🛠 技術スタック

### 推奨技術スタック: **C# + WPF**

| 項目 | 技術 | 理由 |
|------|------|------|
| 言語 | C# 12.0+ | Windows開発に最適、生産性が高い |
| フレームワーク | .NET 8.0 | 最新の長期サポート版 |
| GUI | WPF (Windows Presentation Foundation) | 洗練されたUI、MVVM対応 |
| データベース | SQLite | 軽量、ファイルベース、メンテナンス不要 |
| DB アクセス | Microsoft.Data.Sqlite / Entity Framework Core | 型安全、LINQ対応 |
| タスクトレイ | Hardcodet.NotifyIcon.Wpf | WPF用タスクトレイライブラリ |
| 通知 | Microsoft.Toolkit.Uwp.Notifications | Windowsトースト通知 |
| アイコン生成 | System.Drawing | 動的アイコン生成 |

### 代替案: Rust (非推奨)
- **課題**: GUI フレームワーク未成熟、開発時間2-3倍、タスクトレイ実装が複雑
- **判断**: このプロジェクトには C# が最適

---

## 📊 データベース設計

### テーブル構成

#### 1. `friends` テーブル
友人の基本情報を管理

| カラム名 | 型 | 制約 | 説明 |
|---------|-----|------|------|
| id | INTEGER | PRIMARY KEY AUTOINCREMENT | 固有ID |
| name | TEXT | NOT NULL | 友人の名前 |
| birthday | TEXT | NULL | 誕生日 (YYYY-MM-DD, MM-DD, YYYY形式、または NULL) |
| aliases | TEXT | NULL | エイリアス (カンマ区切り) |
| memo | TEXT | NULL | メモ |
| notify_days_before | INTEGER | NULL | 個人通知設定 (NULL=デフォルト使用) |
| notify_enabled | INTEGER | NOT NULL DEFAULT 1 | 通知有効フラグ (0/1) |
| notify_sound_enabled | INTEGER | NULL | 音声通知 (NULL=デフォルト, 0/1) |
| created_at | TEXT | NOT NULL | 作成日時 (ISO8601) |
| updated_at | TEXT | NOT NULL | 更新日時 (ISO8601) |

#### 複合ユニーク制約
```sql
UNIQUE(name, birthday, aliases, memo)
```
- 同姓同名同誕生日は、aliases または memo が異なる場合のみ登録可能

#### 2. `settings` テーブル
アプリケーション全体設定

| カラム名 | 型 | 制約 | 説明 |
|---------|-----|------|------|
| key | TEXT | PRIMARY KEY | 設定キー |
| value | TEXT | NOT NULL | 設定値 |

**初期設定項目**:
- `default_notify_days_before`: デフォルト通知日数 (1-9)
- `default_notify_sound`: デフォルト音声通知 (0/1)
- `notification_times`: 通知時刻 (カンマ区切り、例: "12:00" または "09:00,12:00,18:00")
- `start_with_windows`: スタートアップ登録 (0/1)

#### 3. `notification_history` テーブル (オプション)
通知履歴を記録（重複通知防止）

| カラム名 | 型 | 制約 | 説明 |
|---------|-----|------|------|
| id | INTEGER | PRIMARY KEY AUTOINCREMENT | 履歴ID |
| friend_id | INTEGER | FOREIGN KEY | 友人ID |
| notification_date | TEXT | NOT NULL | 通知日 (YYYY-MM-DD) |
| notified_at | TEXT | NOT NULL | 通知日時 (ISO8601) |

---

## 🎨 UI設計詳細

### 画面A: メイン画面（誕生日追加）
**トリガー**: タスクトレイアイコンをクリック、または右クリックメニュー「誕生日追加」

```
┌─────────────────────────────────────────┐
│ 友人の誕生日を追加                       │
├─────────────────────────────────────────┤
│ 名前:                                    │
│ [____________________________________]  │
│                                         │
│ 誕生日:                                  │
│ [____/__/____] (YYYY/MM/DD)            │
│                                         │
│ エイリアス (カンマ区切り):               │
│ [____________________________________]  │
│                                         │
│ メモ:                                    │
│ ┌──────────────────────────────────┐   │
│ │                                  │   │
│ │                                  │   │
│ └──────────────────────────────────┘   │
│                                         │
│ 通知設定: [▼ デフォルト設定を使用 ▼]     │
│           (1~9日前, または通知なし)      │
│                                         │
│        [登録]        [一覧表示]         │
├─────────────────────────────────────────┤
│ 📅 直近の誕生日                          │
│                                         │
│ 1. 山田太郎                              │
│    2025年11月20日 (あと 6日)            │
│                                         │
│ 2. 佐藤花子                              │
│    2025年11月25日 (あと 11日)           │
└─────────────────────────────────────────┘
```

**機能**:
- 入力バリデーション（**名前のみ必須**、誕生日・エイリアス・メモは任意）
- 誕生日形式: YYYY/MM/DD、MM/DD、YYYY、または空欄
  - **通知対象**: 月日が入力されている場合のみ（年は任意）
  - **通知対象外**: 年のみ、月のみ、日のみ、または未入力
- エイリアス複数登録可能（カンマ区切り）
- 直近2名の誕生日を常時表示（誕生日未設定の友人は表示されない）

---

### 画面B: 一覧表示画面
**トリガー**: 「一覧表示」ボタン、または右クリックメニュー「一覧表示」

```
┌──────────────────────────────────────────────────────────┐
│ 友人一覧                                                  │
├──────────────────────────────────────────────────────────┤
│ 検索: [_______________________] [🔽 並び替え: 近い順]    │
│                                                          │
│ ┌────────────────────────────────────────────────────┐  │
│ │☐ 名前      │ 誕生日    │ あと  │        │          │  │
│ ├────────────────────────────────────────────────────┤  │
│ │☐ 山田太郎  │11月20日   │ 6日   │ [編集] │ [削除]  │  │
│ │☐ 佐藤花子  │11月25日   │ 11日  │ [編集] │ [削除]  │  │
│ │☐ 鈴木一郎  │12月1日    │ 17日  │ [編集] │ [削除]  │  │
│ │☐ ...                                              │  │
│ └────────────────────────────────────────────────────┘  │
│                                                          │
│ 選択中: 0件          [チェック済みを削除]    [閉じる]    │
└──────────────────────────────────────────────────────────┘
```

**機能**:
- **即時検索**: 名前、エイリアス、誕生日、メモ欄を横断検索
- **並び替え**:
  - 近い順（今日から近い順、誕生日未設定は最後）
  - 日付順（1月1日→12月31日、誕生日未設定は最後）
  - 名前順（50音順）
- **名前クリック**: 編集画面（画面C）へ遷移
- **削除**: 確認ダイアログ表示後に削除
- **チェックボックス**: 複数選択して一括削除可能

---

### 画面C: 編集画面
**トリガー**: 一覧画面で名前をクリック

```
┌─────────────────────────────────────────┐
│ 友人情報の編集                           │
├─────────────────────────────────────────┤
│ 名前:                                    │
│ [山田太郎__________________________]    │
│                                         │
│ 誕生日:                                  │
│ [2000/05/15]                           │
│                                         │
│ エイリアス (カンマ区切り):               │
│ [tarou, taro, たろー_______________]   │
│                                         │
│ メモ:                                    │
│ ┌──────────────────────────────────┐   │
│ │高校時代の友人                     │   │
│ │好きなもの: ラーメン               │   │
│ └──────────────────────────────────┘   │
│                                         │
│ 通知設定:                                │
│ ┌─────────────────────────────────┐    │
│ │ [▼ 3日前 ▼]  [🔔 通知 ON ]      │    │
│ │ [☑ 音を鳴らす]                   │    │
│ └─────────────────────────────────┘    │
│                                         │
│          [保存]          [キャンセル]    │
└─────────────────────────────────────────┘
```

**機能**:
- 通知トグルボタン（ON/OFF）
- 個人通知設定（デフォルト、1~9日前）
- 音声通知の個別設定

---

### 画面D: 設定画面
**トリガー**: 右クリックメニュー「設定」

```
┌─────────────────────────────────────────┐
│ 設定                                     │
├─────────────────────────────────────────┤
│ 【通知設定】                             │
│                                         │
│ 通知時刻 (カンマ区切りで複数設定可能):   │
│   [12:00___________________________]    │
│   例: 09:00,12:00,18:00                 │
│                                         │
│ デフォルト通知タイミング:                │
│   [▼ 1日前から ▼] (1~9日前から選択)    │
│   ※ 選択した日数前から誕生日当日まで毎日 │
│      上記時刻に通知されます              │
│                                         │
│ デフォルト音声通知:                      │
│   [☑ 音を鳴らす]                        │
│                                         │
├─────────────────────────────────────────┤
│ 【起動設定】                             │
│                                         │
│   [☑] Windowsスタートアップに登録       │
│                                         │
├─────────────────────────────────────────┤
│ 【データ管理】                           │
│                                         │
│   [データベースをエクスポート (CSV)]     │
│   [データベースをインポート (CSV)]       │
│                                         │
│   ※ インポートは本ツール出力CSV限定      │
│                                         │
├─────────────────────────────────────────┤
│              [保存]        [閉じる]      │
└─────────────────────────────────────────┘
```

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
│                  [閉じる]                │
└─────────────────────────────────────────┘
```

---

### タスクトレイアイコン

#### 右クリックメニュー
```
┌────────────────────┐
│ 誕生日を追加       │ → 画面A
│ 一覧表示           │ → 画面B
│ ────────────────── │
│ 設定               │ → 画面D
│ クレジット         │ → 画面E
│ ────────────────── │
│ 終了               │ → アプリ終了
└────────────────────┘
```

#### アイコン表示仕様
- **1~9日以内に誕生日がある場合**: 日数を表示（例: `3`）
- **10日以上先の場合**: `誕` の文字を表示
- **アイコンサイズ**: 16x16ピクセル（標準）、32x32ピクセル（高DPI）
- **推奨形式**: ICO形式（マルチサイズ対応）
  - または PNG 形式（透過対応）
- **動的生成**: 日数は System.Drawing で動的に描画

---

## 🔔 通知機能の詳細

### 通知タイミング
- **設定で指定した時刻** にチェック（デフォルト: 12:00 正午）
- カンマ区切りで複数設定可能（例: "09:00,12:00,18:00" → 1日3回通知）
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

- **音声**: デフォルト Windows 通知音、または設定でOFF
- **クリックアクション**: メイン画面（画面A）または一覧画面を開く

### 通知ロジック（重要）

**「X日前から通知」= 「X日前の日から誕生日当日まで毎日、設定時刻に通知」**

```
例: 山田太郎さん (誕生日: 5月15日)
     個人設定=3日前から通知
     通知時刻=09:00,12:00,18:00

- 5月12日 09:00 → 「山田太郎さん (5/15) まであと3日」
- 5月12日 12:00 → 「山田太郎さん (5/15) まであと3日」
- 5月12日 18:00 → 「山田太郎さん (5/15) まであと3日」
- 5月13日 09:00 → 「山田太郎さん (5/15) まであと2日」
- 5月13日 12:00 → 「山田太郎さん (5/15) まであと2日」
- 5月13日 18:00 → 「山田太郎さん (5/15) まであと2日」
- 5月14日 09:00 → 「山田太郎さん (5/15) まであと1日」
- 5月14日 12:00 → 「山田太郎さん (5/15) まであと1日」
- 5月14日 18:00 → 「山田太郎さん (5/15) まであと1日」
- 5月15日 09:00 → 「今日は山田太郎さんの誕生日です！」
- 5月15日 12:00 → 「今日は山田太郎さんの誕生日です！」
- 5月15日 18:00 → 「今日は山田太郎さんの誕生日です！」
```

**通知対象の判定**:
- 月日が入力されている → 通知する（年の有無は問わない）
- 年のみ、月のみ、日のみ、または未入力 → 通知しない

重複防止のため notification_history テーブルに記録（friend_id + notification_date + time で判定）
```

---

## 🔍 検索機能の詳細

### エイリアス検索の例
```
登録データ:
  名前: 山田太郎
  エイリアス: tarou, taro, たろー

検索可能なキーワード:
  - "太郎" → ヒット（名前部分一致）
  - "tarou" → ヒット（エイリアス完全一致）
  - "taro" → ヒット（エイリアス完全一致）
  - "たろー" → ヒット（エイリアス完全一致）
  - "tar" → ヒット（エイリアス部分一致）
  - "山田" → ヒット（名前部分一致）
```

### 検索対象フィールド
- 名前（部分一致）
- エイリアス（完全一致 + 部分一致）
- 誕生日（フォーマット自由）
- メモ（部分一致）

### SQL クエリ例
```sql
SELECT * FROM friends
WHERE
    name LIKE '%{keyword}%'
    OR aliases LIKE '%{keyword}%'
    OR birthday LIKE '%{keyword}%'
    OR memo LIKE '%{keyword}%'
```

---

## 🧩 アーキテクチャ設計

### MVVM パターン
```
┌─────────────┐
│    View     │  (WPF XAML)
│  (UI Layer) │
└──────┬──────┘
       │ Data Binding
┌──────▼──────┐
│  ViewModel  │  (INotifyPropertyChanged)
│ (Logic)     │
└──────┬──────┘
       │ Commands
┌──────▼──────┐
│    Model    │  (Business Logic)
│  (Domain)   │
└──────┬──────┘
       │
┌──────▼──────┐
│ Repository  │  (Data Access)
│  (SQLite)   │
└─────────────┘
```

### 主要クラス設計

#### Model
```csharp
public class Friend
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Birthday { get; set; }  // NULL可能、形式: "YYYY-MM-DD", "MM-DD", "YYYY", または NULL
    public string? Aliases { get; set; }
    public string? Memo { get; set; }
    public int? NotifyDaysBefore { get; set; }
    public bool NotifyEnabled { get; set; }
    public bool? NotifySoundEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ヘルパーメソッド
    public bool HasValidBirthdayForNotification() { }  // 月日が入力されているか
    public int? DaysUntilBirthday(DateTime today) { }  // NULL可能（誕生日未設定の場合）
    public List<string> GetAliasesList() { }
    public (int? Year, int? Month, int? Day) ParseBirthday() { }
}

public class AppSettings
{
    public int DefaultNotifyDaysBefore { get; set; }
    public bool DefaultNotifySound { get; set; }
    public List<TimeSpan> NotificationTimes { get; set; }  // 複数の通知時刻
    public bool StartWithWindows { get; set; }
}
```

#### Repository
```csharp
public interface IFriendRepository
{
    Task<List<Friend>> GetAllAsync();
    Task<Friend?> GetByIdAsync(int id);
    Task<int> AddAsync(Friend friend);
    Task<bool> UpdateAsync(Friend friend);
    Task<bool> DeleteAsync(int id);
    Task<List<Friend>> SearchAsync(string keyword);
    Task<bool> ExistsDuplicateAsync(Friend friend);
}

public interface ISettingsRepository
{
    Task<string?> GetSettingAsync(string key);
    Task SetSettingAsync(string key, string value);
    Task<AppSettings> GetAppSettingsAsync();
    Task SaveAppSettingsAsync(AppSettings settings);
}
```

#### Service
```csharp
public class NotificationService
{
    public async Task CheckAndNotifyAsync();
    public void ShowNotification(Friend friend, int daysUntil);
}

public class TrayIconService
{
    public void UpdateIcon(int days); // or "誕"
    public void ShowTrayIcon();
    public void HideTrayIcon();
}

public class CsvService
{
    public async Task<bool> ExportToCsvAsync(string filePath);
    public async Task<List<Friend>> ImportFromCsvAsync(string filePath);
}
```

---

## 📁 プロジェクト構成

```
FriendBirthdayManager/
│
├── FriendBirthdayManager.sln          # ソリューションファイル
│
├── src/
│   └── FriendBirthdayManager/
│       ├── FriendBirthdayManager.csproj
│       │
│       ├── App.xaml                    # アプリケーションエントリポイント
│       ├── App.xaml.cs
│       │
│       ├── Models/
│       │   ├── Friend.cs
│       │   ├── AppSettings.cs
│       │   └── NotificationHistory.cs
│       │
│       ├── Views/
│       │   ├── MainWindow.xaml          # 画面A: メイン画面
│       │   ├── ListWindow.xaml          # 画面B: 一覧画面
│       │   ├── EditWindow.xaml          # 画面C: 編集画面
│       │   ├── SettingsWindow.xaml      # 画面D: 設定画面
│       │   └── AboutWindow.xaml         # 画面E: クレジット
│       │
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   ├── ListViewModel.cs
│       │   ├── EditViewModel.cs
│       │   ├── SettingsViewModel.cs
│       │   └── AboutViewModel.cs
│       │
│       ├── Services/
│       │   ├── NotificationService.cs
│       │   ├── TrayIconService.cs
│       │   ├── CsvService.cs
│       │   └── StartupService.cs        # スタートアップ登録
│       │
│       ├── Repositories/
│       │   ├── FriendRepository.cs
│       │   ├── SettingsRepository.cs
│       │   └── DatabaseContext.cs       # Entity Framework Context
│       │
│       ├── Helpers/
│       │   ├── DateHelper.cs            # 日付計算
│       │   ├── IconGenerator.cs         # 動的アイコン生成
│       │   └── ValidationHelper.cs
│       │
│       ├── Resources/
│       │   ├── Icons/
│       │   │   └── default.ico          # デフォルトアイコン
│       │   └── Sounds/
│       │       └── notification.wav     # 通知音（オプション）
│       │
│       └── Database/
│           └── friends.db                # SQLite データベース
│
├── tests/
│   └── FriendBirthdayManager.Tests/
│       ├── Models/
│       ├── Services/
│       └── Repositories/
│
├── docs/
│   ├── PLAN.md                          # 本ファイル
│   ├── API.md                           # API仕様
│   └── DATABASE.md                      # DB詳細仕様
│
├── .gitignore
├── README.md
└── LICENSE
```

---

## 📅 実装計画（フェーズ分け）

### Phase 1: プロジェクト基盤構築 ✅
- [ ] プロジェクト作成（.NET 8 WPF）
- [ ] NuGetパッケージ導入
  - Microsoft.Data.Sqlite
  - Hardcodet.NotifyIcon.Wpf
  - Microsoft.Toolkit.Uwp.Notifications
  - CommunityToolkit.Mvvm (MVVM Helper)
- [ ] フォルダ構成作成
- [ ] データベーススキーマ定義・初期化

### Phase 2: データ層実装 🔄
- [ ] Model クラス作成
- [ ] Repository 実装
  - FriendRepository
  - SettingsRepository
- [ ] データベース初期化処理
- [ ] マイグレーション機能（将来対応）

### Phase 3: 基本UI実装 📱
- [ ] 画面A: メイン画面（追加）
- [ ] 画面B: 一覧画面
- [ ] 画面C: 編集画面
- [ ] 入力バリデーション
- [ ] データバインディング

### Phase 4: タスクトレイ機能 🖥️
- [ ] タスクトレイアイコン表示
- [ ] 右クリックメニュー
- [ ] 動的アイコン生成（日数表示）
- [ ] アイコン更新ロジック

### Phase 5: 通知機能 🔔
- [ ] NotificationService 実装
- [ ] Windows トースト通知
- [ ] 通知タイミング制御（複数時刻対応、デフォルト12:00）
- [ ] 重複通知防止（同日・同時刻の重複チェック）
- [ ] 音声通知制御
- [ ] 通知対象判定（月日入力済みか確認）

### Phase 6: 検索・ソート機能 🔍
- [ ] エイリアス検索
- [ ] 即時検索（リアルタイムフィルター）
- [ ] 並び替え（近い順、日付順、名前順）
- [ ] 日本語50音順ソート

### Phase 7: 設定機能 ⚙️
- [ ] 画面D: 設定画面
- [ ] スタートアップ登録
- [ ] デフォルト通知設定
- [ ] CSV エクスポート/インポート
- [ ] 設定永続化

### Phase 8: 細部調整・UX改善 ✨
- [ ] 画面E: クレジット画面
- [ ] エラーハンドリング
- [ ] ローディング表示
- [ ] 確認ダイアログ
- [ ] ツールチップ・ヘルプ

### Phase 9: テスト 🧪
- [ ] 単体テスト
- [ ] UIテスト（手動）
- [ ] エッジケーステスト
  - うるう年対応
  - 同名重複登録
  - 大量データ（1000件以上）

### Phase 10: ビルド・配布 📦
- [ ] リリースビルド
- [ ] 単一実行ファイル化（Self-contained）
- [ ] インストーラー作成（オプション）
- [ ] README・ドキュメント整備

---

## 🎯 開発優先順位

### 高優先度（MVP: Minimum Viable Product）
1. データベース設計・実装
2. 友人登録・一覧表示
3. タスクトレイ常駐
4. 基本通知機能

### 中優先度
5. エイリアス検索
6. 編集機能
7. 設定画面
8. CSV エクスポート

### 低優先度（後で追加可能）
9. CSV インポート
10. 高度な並び替え
11. 通知履歴
12. 統計情報（友人数、次の誕生日まで、など）

---

## 🖼️ アイコン仕様

### 必要なアイコン

#### 1. タスクトレイアイコン（動的生成）
- **サイズ**: 16x16px、32x32px、48x48px（マルチサイズ）
- **形式**: ICO 形式（推奨）、または PNG（透過）
- **内容**:
  - ベース画像: ケーキ、カレンダー、ギフトボックスなどの誕生日関連アイコン
  - 数字オーバーレイ: 1~9（白文字、縁取り黒）
  - 「誕」文字（10日以上先の場合）

#### 2. 静的アイコン（オプション）
- アプリケーションアイコン（.exe用）
- ウィンドウアイコン

### 提供推奨形式
- **ICO形式**: `app_icon.ico`（マルチサイズ含む）
- **PNG形式**: `app_icon.png`（透過、256x256px以上）

### 動的生成仕様
```csharp
// 疑似コード
Icon GenerateTrayIcon(int daysUntil)
{
    Bitmap base = LoadBaseIcon();
    Graphics g = Graphics.FromImage(base);

    if (daysUntil <= 9)
        g.DrawString(daysUntil.ToString(), font, brush, point);
    else
        g.DrawString("誕", font, brush, point);

    return Icon.FromBitmap(base);
}
```

---

## ⚠️ 注意事項・制約

### 1. 重複登録制御
- 同じ「名前 + 誕生日 + エイリアス + メモ」の組み合わせは登録不可
- 誕生日が未入力（NULL）でも、他の項目が全く同じなら登録不可
- エラーメッセージ:
  > 「登録済みです。多重登録を行いたい場合はエイリアスかメモ欄に固有の情報を入れてください」

### 2. 誕生日入力パターンと通知可否

| パターン | 入力例 | 通知 | 備考 |
|---------|-------|------|------|
| 年月日 | 2000/05/15 | ✅ | 毎年5月15日に通知 |
| 月日のみ | 05/15 | ✅ | 毎年5月15日に通知 |
| 年のみ | 2000 | ❌ | 通知なし |
| 月のみ | 05 | ❌ | 通知なし |
| 日のみ | 15 | ❌ | 通知なし |
| 未入力 | (空欄) | ❌ | 通知なし、一覧では最後に表示 |

**重要**: 通知が行われるのは「月と日の両方が入力されている場合のみ」

### 3. 日付計算
- うるう年対応必須
- 例: 2月29日生まれ → 平年は2月28日に通知

### 4. 並び替え「今日から近い順」
```
例: 今日が 11月14日の場合

1. 11月15日 (あと1日)
2. 11月20日 (あと6日)
3. 12月1日 (あと17日)
4. 1月5日 (あと52日)
5. 10月1日 (あと321日) ← 来年の誕生日
6. 田中次郎 (未設定) --- ← 誕生日未設定は最後
```

### 5. CSV フォーマット
```csv
id,name,birthday,aliases,memo,notify_days_before,notify_enabled,notify_sound_enabled
1,山田太郎,2000-05-15,"tarou,taro,たろー",高校時代の友人,3,1,1
2,佐藤花子,05-25,"hanako,はなこ",大学の先輩,,1,
3,田中次郎,,,仕事関係,,,0
```
- UTF-8 BOM付き
- ヘッダー行必須
- NULL値は空欄

---

## 🚀 開発開始手順

### 1. 環境準備
```bash
# .NET 8 SDK インストール確認
dotnet --version

# プロジェクト作成
dotnet new wpf -n FriendBirthdayManager -f net8.0-windows
cd FriendBirthdayManager

# NuGetパッケージ追加
dotnet add package Microsoft.Data.Sqlite
dotnet add package Hardcodet.NotifyIcon.Wpf
dotnet add package Microsoft.Toolkit.Uwp.Notifications
dotnet add package CommunityToolkit.Mvvm
```

### 2. データベース初期化
```bash
# SQLite CLI で初期スキーマ作成
sqlite3 friends.db < schema.sql
```

### 3. Visual Studio / Cursor で開発開始
```bash
# ソリューションを開く
FriendBirthdayManager.sln
```

---

## 📚 参考リソース

### 公式ドキュメント
- [WPF Documentation](https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [Windows Notifications](https://learn.microsoft.com/ja-jp/windows/apps/design/shell/tiles-and-notifications/adaptive-interactive-toasts)

### サンプルコード
- [Hardcodet NotifyIcon WPF Examples](https://github.com/hardcodet/wpf-notifyicon)
- [Community Toolkit MVVM](https://learn.microsoft.com/ja-jp/dotnet/communitytoolkit/mvvm/)

---

## 📝 TODO リスト

### 即座に必要なもの
- [ ] アイコン画像の準備（ICO/PNG形式）
- [ ] 開発環境のセットアップ確認

### 決定済み仕様 ✅
- ✅ 通知時刻: 複数設定可能（カンマ区切り、デフォルト12:00）
- ✅ 通知の繰り返し: 同日に複数回通知可能
- ✅ 年齢表示機能: 不要
- ✅ 誕生日の年: 省略可能（月日のみで通知対象）
- ✅ 入力必須項目: 名前のみ（誕生日は任意）
- ✅ 通知対象: 月日が入力されている場合のみ

---

## ✅ 完成イメージ

### ゴール
- タスクトレイに常駐し、ユーザーが意識せずに使える
- 友人の誕生日を忘れない仕組み
- シンプルで直感的なUI
- 軽量で高速動作

### 成功指標
- [ ] 100人以上の友人を登録してもスムーズに動作
- [ ] 通知が確実に届く
- [ ] エイリアス検索が便利
- [ ] データのバックアップ・復元が簡単

---

**ドキュメント作成日**: 2025-11-14
**作成者**: Claude (Anthropic)
**プロジェクトオーナー**: えりんぎ (@eringi_vrc)

---

## 次のステップ

1. **このドキュメントをレビュー**して、追加・修正したい内容があればお知らせください
2. **アイコン画像を準備**してください（ICO/PNG形式）
3. **Phase 1（プロジェクト基盤構築）**を開始します

質問や懸念事項があれば、お気軽にお聞きください！
