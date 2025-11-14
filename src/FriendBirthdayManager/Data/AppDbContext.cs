using FriendBirthdayManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FriendBirthdayManager.Data;

/// <summary>
/// アプリケーションのデータベースコンテキスト
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 友人テーブル
    /// </summary>
    public DbSet<Friend> Friends => Set<Friend>();

    /// <summary>
    /// エイリアステーブル
    /// </summary>
    public DbSet<Alias> Aliases => Set<Alias>();

    /// <summary>
    /// 設定テーブル
    /// </summary>
    public DbSet<Setting> Settings => Set<Setting>();

    /// <summary>
    /// 通知履歴テーブル
    /// </summary>
    public DbSet<NotificationHistory> NotificationHistories => Set<NotificationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Friends テーブル
        modelBuilder.Entity<Friend>(entity =>
        {
            entity.ToTable("friends");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.BirthYear).HasColumnName("birth_year");
            entity.Property(e => e.BirthMonth).HasColumnName("birth_month");
            entity.Property(e => e.BirthDay).HasColumnName("birth_day");
            entity.Property(e => e.Memo).HasColumnName("memo").HasMaxLength(5000);
            entity.Property(e => e.NotifyDaysBefore).HasColumnName("notify_days_before");
            entity.Property(e => e.NotifyEnabled).HasColumnName("notify_enabled").IsRequired().HasDefaultValue(true);
            entity.Property(e => e.NotifySoundEnabled).HasColumnName("notify_sound_enabled");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // インデックス
            entity.HasIndex(e => new { e.BirthMonth, e.BirthDay })
                .HasDatabaseName("idx_friends_birth_month_day")
                .HasFilter("birth_month IS NOT NULL AND birth_day IS NOT NULL");

            entity.HasIndex(e => e.Name).HasDatabaseName("idx_friends_name");

            entity.HasIndex(e => e.NotifyEnabled)
                .HasDatabaseName("idx_friends_notify_enabled")
                .HasFilter("notify_enabled = 1");

            // チェック制約
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_friends_birth_month", "birth_month IS NULL OR (birth_month BETWEEN 1 AND 12)");
                t.HasCheckConstraint("CK_friends_birth_day", "birth_day IS NULL OR (birth_day BETWEEN 1 AND 31)");
                t.HasCheckConstraint("CK_friends_notify_days_before", "notify_days_before IS NULL OR (notify_days_before BETWEEN 1 AND 30)");
            });
        });

        // Aliases テーブル
        modelBuilder.Entity<Alias>(entity =>
        {
            entity.ToTable("aliases");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FriendId).HasColumnName("friend_id").IsRequired();
            entity.Property(e => e.AliasName).HasColumnName("alias").IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            // 外部キー
            entity.HasOne(e => e.Friend)
                .WithMany(f => f.Aliases)
                .HasForeignKey(e => e.FriendId)
                .OnDelete(DeleteBehavior.Cascade);

            // ユニーク制約
            entity.HasIndex(e => new { e.FriendId, e.AliasName })
                .IsUnique()
                .HasDatabaseName("UQ_aliases_friend_id_alias");

            // インデックス
            entity.HasIndex(e => e.FriendId).HasDatabaseName("idx_aliases_friend_id");
            entity.HasIndex(e => e.AliasName).HasDatabaseName("idx_aliases_alias");
        });

        // Settings テーブル
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.ToTable("settings");

            entity.HasKey(e => e.Key);

            entity.Property(e => e.Key).HasColumnName("key").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).HasColumnName("value").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        // NotificationHistories テーブル
        modelBuilder.Entity<NotificationHistory>(entity =>
        {
            entity.ToTable("notification_history");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FriendId).HasColumnName("friend_id").IsRequired();
            entity.Property(e => e.NotificationDate).HasColumnName("notification_date").IsRequired().HasMaxLength(10);
            entity.Property(e => e.NotifiedAt).HasColumnName("notified_at").IsRequired();

            // 外部キー
            entity.HasOne(e => e.Friend)
                .WithMany()
                .HasForeignKey(e => e.FriendId)
                .OnDelete(DeleteBehavior.Cascade);

            // ユニーク制約
            entity.HasIndex(e => new { e.FriendId, e.NotificationDate })
                .IsUnique()
                .HasDatabaseName("UQ_notification_history_friend_id_date");

            // インデックス
            entity.HasIndex(e => e.NotificationDate).HasDatabaseName("idx_notification_history_date");
        });
    }

    /// <summary>
    /// データベースを初期化し、必要に応じてマイグレーションを実行
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        // データベースを作成（存在しない場合）
        await Database.EnsureCreatedAsync();

        // FTS5仮想テーブルの作成
        await CreateFts5TableAsync();

        // デフォルト設定を挿入
        await SeedDefaultSettingsAsync();
    }

    /// <summary>
    /// FTS5仮想テーブルを作成
    /// </summary>
    private async Task CreateFts5TableAsync()
    {
        // 既に存在するかチェック
        var tableExists = await Database.ExecuteSqlRawAsync(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='friends_fts'");

        if (tableExists > 0)
        {
            return; // 既に存在する
        }

        // FTS5仮想テーブルの作成
        await Database.ExecuteSqlRawAsync(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS friends_fts USING fts5(
                name,
                memo,
                content=friends,
                content_rowid=id
            );
        ");

        // トリガーの作成（INSERT）
        await Database.ExecuteSqlRawAsync(@"
            CREATE TRIGGER IF NOT EXISTS friends_ai AFTER INSERT ON friends BEGIN
                INSERT INTO friends_fts(rowid, name, memo)
                VALUES (new.id, new.name, COALESCE(new.memo, ''));
            END;
        ");

        // トリガーの作成（DELETE）
        await Database.ExecuteSqlRawAsync(@"
            CREATE TRIGGER IF NOT EXISTS friends_ad AFTER DELETE ON friends BEGIN
                DELETE FROM friends_fts WHERE rowid = old.id;
            END;
        ");

        // トリガーの作成（UPDATE）
        await Database.ExecuteSqlRawAsync(@"
            CREATE TRIGGER IF NOT EXISTS friends_au AFTER UPDATE ON friends BEGIN
                UPDATE friends_fts SET name = new.name, memo = COALESCE(new.memo, '')
                WHERE rowid = new.id;
            END;
        ");
    }

    /// <summary>
    /// デフォルト設定を挿入
    /// </summary>
    private async Task SeedDefaultSettingsAsync()
    {
        var schemaVersion = await Settings.FindAsync("schema_version");
        if (schemaVersion == null)
        {
            Settings.Add(new Setting
            {
                Key = "schema_version",
                Value = "1",
                UpdatedAt = DateTime.UtcNow
            });
        }

        var defaultNotifyDaysBefore = await Settings.FindAsync("default_notify_days_before");
        if (defaultNotifyDaysBefore == null)
        {
            Settings.Add(new Setting
            {
                Key = "default_notify_days_before",
                Value = "1",
                UpdatedAt = DateTime.UtcNow
            });
        }

        var defaultNotifySound = await Settings.FindAsync("default_notify_sound");
        if (defaultNotifySound == null)
        {
            Settings.Add(new Setting
            {
                Key = "default_notify_sound",
                Value = "true",
                UpdatedAt = DateTime.UtcNow
            });
        }

        var notificationTime = await Settings.FindAsync("notification_time");
        if (notificationTime == null)
        {
            Settings.Add(new Setting
            {
                Key = "notification_time",
                Value = "12:00",
                UpdatedAt = DateTime.UtcNow
            });
        }

        var startWithWindows = await Settings.FindAsync("start_with_windows");
        if (startWithWindows == null)
        {
            Settings.Add(new Setting
            {
                Key = "start_with_windows",
                Value = "false",
                UpdatedAt = DateTime.UtcNow
            });
        }

        var language = await Settings.FindAsync("language");
        if (language == null)
        {
            Settings.Add(new Setting
            {
                Key = "language",
                Value = "ja-JP",
                UpdatedAt = DateTime.UtcNow
            });
        }

        await SaveChangesAsync();
    }
}
