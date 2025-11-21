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

## バージョン履歴

### Version 1.0.0 (2025-11-20)
- 🎉 初版リリース

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
