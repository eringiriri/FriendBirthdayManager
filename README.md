# Friend Birthday Manager

友人の誕生日を管理し、タスクトレイに常駐して適切なタイミングで通知を行うWindowsデスクトップアプリケーションです。

## 主要機能

- ✅ タスクトレイ常駐
- ✅ 友人情報（名前、誕生日、エイリアス、メモ）の登録・編集・削除
- ✅ 柔軟な検索機能（エイリアス対応、FTS5フルテキスト検索）
- ✅ 誕生日までの日数表示（アイコン上）
- ✅ カスタマイズ可能な通知設定（全体・個人）
- ✅ CSV エクスポート/インポート
- ✅ 多言語対応基盤（将来的な拡張を考慮）

## 技術スタック

- **言語**: C# 12.0+
- **フレームワーク**: .NET 8.0 (LTS)
- **GUI**: WPF (Windows Presentation Foundation)
- **データベース**: SQLite 3.40+ (FTS5サポート)
- **ORM**: Entity Framework Core 8.0+
- **MVVM**: CommunityToolkit.Mvvm
- **ログ**: Serilog
- **テスト**: xUnit + FluentAssertions + Moq

## 必要要件

- Windows 10 / 11
- .NET 8.0 SDK（開発時）
- .NET 8.0 Runtime（実行時）

## ビルド方法

### 開発環境のセットアップ

1. .NET 8.0 SDKをインストール
   ```
   https://dotnet.microsoft.com/download/dotnet/8.0
   ```

2. リポジトリをクローン
   ```bash
   git clone https://github.com/eringiriri/FriendBirthdayManager.git
   cd FriendBirthdayManager
   ```

3. 依存パッケージの復元
   ```bash
   dotnet restore
   ```

4. ビルド
   ```bash
   dotnet build
   ```

5. 実行
   ```bash
   dotnet run --project src/FriendBirthdayManager/FriendBirthdayManager.csproj
   ```

### リリースビルド

単一実行ファイルとしてビルド:
```bash
dotnet publish src/FriendBirthdayManager/FriendBirthdayManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

出力ファイル: `src/FriendBirthdayManager/bin/Release/net8.0-windows/win-x64/publish/FriendBirthdayManager.exe`

## Visual Studioでの開発

1. Visual Studio 2022以降を開く
2. `FriendBirthdayManager.sln` を開く
3. ビルド → ソリューションのビルド
4. デバッグ → デバッグの開始 (F5)

## プロジェクト構造

```
FriendBirthdayManager/
├── src/
│   └── FriendBirthdayManager/
│       ├── Models/           # エンティティモデル
│       ├── ViewModels/       # MVVMのViewModel
│       ├── Views/            # XAML UI
│       ├── Data/             # データアクセス層
│       ├── Services/         # ビジネスロジック
│       └── Resources/        # アイコン、文字列リソース
├── tests/
│   └── FriendBirthdayManager.Tests/  # 単体テスト
├── PLAN.md                   # 詳細な開発計画書
└── README.md                 # このファイル
```

## 開発状況

現在の実装状況:

### Phase 1: プロジェクト基盤構築 ✅ 完了
- ✅ .NET 8 WPFプロジェクト作成
- ✅ NuGetパッケージ導入
- ✅ フォルダ構成作成
- ✅ DI設定（App.xaml.cs）
- ✅ ログ設定（Serilog）

### Phase 2: データ層実装 ✅ 完了
- ✅ Modelクラス作成（Friend, Alias, Setting, NotificationHistory）
- ✅ AppDbContext実装
- ✅ Repository実装（IFriendRepository, ISettingsRepository）
- ✅ データベース初期化処理
- ✅ FTS5フルテキスト検索テーブル

### Phase 3: 基本UI実装 ✅ 部分完了
- ✅ MainWindow（友人追加画面）
- ✅ ListWindow（一覧画面）
- ✅ EditWindow（編集画面）
- ✅ SettingsWindow（設定画面）
- ✅ ViewModel実装（MVVM）

### Phase 4: タスクトレイ機能 ✅ 完了
- ✅ タスクトレイアイコン表示
- ✅ 右クリックメニュー
- ✅ 動的アイコン更新（誕生日までの日数に応じて）
- ✅ ダブルクリックでメインウィンドウ表示
- ✅ バルーンチップ通知機能

### Phase 5: 通知機能 ✅ 完了
- ✅ Windowsトースト通知
- ✅ 通知タイミング制御（バックグラウンドタイマー、1時間ごとチェック）
- ✅ 重複通知防止（通知履歴管理）
- ✅ 設定時刻（±30分）に通知送信
- ✅ 30日以上前の履歴自動クリーンアップ

### Phase 6: 検索・ソート機能 🚧 未実装
- ✅ FTS5仮想テーブル作成
- ❌ 即時検索（リアルタイムフィルター）
- ❌ 並び替え（近い順、日付順、名前順）

### Phase 7: 設定機能 🚧 未実装
- ❌ スタートアップ登録
- ❌ CSV エクスポート/インポート

## データベース

データベースとログは以下の場所に保存されます:

- データベース: `%LocalAppData%\FriendBirthdayManager\friends.db`
- ログファイル: `%LocalAppData%\FriendBirthdayManager\logs\`

例:
```
C:\Users\YourName\AppData\Local\FriendBirthdayManager\friends.db
C:\Users\YourName\AppData\Local\FriendBirthdayManager\logs\app20250114.log
```

## 使用方法

1. アプリケーションを起動
2. メイン画面で友人の名前と誕生日を入力
3. 「登録」ボタンをクリック
4. 「一覧表示」から登録した友人を確認
5. タスクトレイに常駐し、設定した時刻に通知

詳細な使用方法は `PLAN.md` の「UI設計詳細」セクションを参照してください。

## ライセンス

MIT License

## 制作者

- 制作者: えりんぎ
- Twitter: [@eringi_vrc](https://twitter.com/eringi_vrc)
- 連絡先: eringi@eringi.me

## 参考

詳細な開発計画とアーキテクチャについては [PLAN.md](PLAN.md) を参照してください。
