using System.IO;
using System.Threading;
using System.Windows;
using FriendBirthdayManager.Data;
using FriendBirthdayManager.Services;
using FriendBirthdayManager.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace FriendBirthdayManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private ITrayIconService? _trayIconService;
    private INotificationService? _notificationService;
    private static Mutex? _mutex;
    private System.Threading.Timer? _hourlyUpdateTimer;
    private const string MutexName = "FriendBirthdayManager_SingleInstance";

    /// <summary>
    /// サービスプロバイダー
    /// </summary>
    public IServiceProvider? Services => _serviceProvider;

    public App()
    {
        // Serilogの設定
        ConfigureLogging();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 多重起動チェック
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            // 既に起動している場合は終了
            MessageBox.Show(
                "Friend Birthday Manager は既に起動しています。\nタスクトレイアイコンからアクセスしてください。",
                "多重起動",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown(1);
            return;
        }

        try
        {
            // DIコンテナの設定
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            Log.Information("Application starting...");

            // データベースの初期化
            // ConfigureAwait(false)を使用してデッドロックを防止
            InitializeDatabaseAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            // 言語設定の初期化
            InitializeLanguageAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            // タスクトレイアイコンを初期化
            _trayIconService = _serviceProvider.GetRequiredService<ITrayIconService>();
            _trayIconService.Initialize();

            // アイコンを更新（直近の誕生日を取得）
            // ConfigureAwait(false)を使用してデッドロックを防止
            UpdateTrayIconAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            // 通知サービスを開始
            _notificationService = _serviceProvider.GetRequiredService<INotificationService>();
            _notificationService.Start();

            // 毎時00分にアイコン更新するタイマーを開始
            StartHourlyUpdateTimer();

            // メインウィンドウは表示せず、タスクトレイのみ常駐
            // ※ ユーザーがタスクトレイから「誕生日を追加」を選択したときに表示される

            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show($"アプリケーションの起動に失敗しました: {ex.Message}",
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down...");

        // 通知サービスの停止
        _notificationService?.Stop();

        // 毎時更新タイマーの停止
        _hourlyUpdateTimer?.Dispose();

        // タスクトレイサービスのクリーンアップ
        _trayIconService?.Dispose();

        // リソースのクリーンアップ
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Mutexを解放
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void ConfigureLogging()
    {
        // ログディレクトリの作成
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FriendBirthdayManager",
            "logs");

        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, "app.log");

        // Serilogの設定
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // データベースパスの設定
        var dbDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FriendBirthdayManager");

        Directory.CreateDirectory(dbDirectory);

        var dbPath = Path.Combine(dbDirectory, "friends.db");

        // DbContext
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
            options.EnableSensitiveDataLogging(false);
        });

        // Repositories
        services.AddScoped<IFriendRepository, FriendRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<INotificationHistoryRepository, NotificationHistoryRepository>();

        // Services
        // NOTE: NotificationServiceとTrayIconServiceはSingletonだが、
        // DbContextを使用する場合はIServiceProviderを注入してスコープを作成する
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddScoped<ICsvService, CsvService>();
        services.AddScoped<IStartupService, StartupService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ListViewModel>();
        services.AddTransient<EditViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();

        // Views
        services.AddTransient<Views.MainWindow>();
        services.AddTransient<Views.ListWindow>();
        services.AddTransient<Views.EditWindow>();
        services.AddTransient<Views.SettingsWindow>();
        services.AddTransient<Views.AboutWindow>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            Log.Information("Initializing database...");

            using var scope = _serviceProvider!.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.InitializeDatabaseAsync();

            Log.Information("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize database");
            throw;
        }
    }

    private async Task InitializeLanguageAsync()
    {
        try
        {
            Log.Information("Initializing language settings...");

            var localizationService = _serviceProvider!.GetRequiredService<ILocalizationService>();

            using var scope = _serviceProvider!.CreateScope();
            var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

            // 保存されている言語設定を取得
            var settings = await settingsRepository.GetAppSettingsAsync();
            var languageCode = settings.Language;

            // 言語設定が空の場合はシステムのデフォルト言語を使用
            if (string.IsNullOrEmpty(languageCode))
            {
                languageCode = localizationService.GetSystemLanguage();
                Log.Information("No saved language setting, using system language: {Language}", languageCode);

                // デフォルト言語をデータベースに保存
                settings.Language = languageCode;
                await settingsRepository.SaveAppSettingsAsync(settings);
            }

            // 言語を設定
            localizationService.ChangeLanguage(languageCode);

            Log.Information("Language initialized successfully: {Language}", languageCode);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize language settings");
            // エラーが発生してもアプリケーションは継続
        }
    }

    private async Task UpdateTrayIconAsync()
    {
        try
        {
            Log.Information("Updating tray icon...");

            using var scope = _serviceProvider!.CreateScope();
            var friendRepository = scope.ServiceProvider.GetRequiredService<IFriendRepository>();

            // 直近の誕生日を取得
            var upcomingBirthdays = await friendRepository.GetUpcomingBirthdaysAsync(DateTime.Now, 1);
            if (upcomingBirthdays.Count > 0)
            {
                var nextFriend = upcomingBirthdays[0];
                var daysUntil = nextFriend.CalculateDaysUntilBirthday(DateTime.Now);
                _trayIconService?.UpdateIcon(daysUntil);
                Log.Information("Tray icon updated: Next birthday in {Days} days", daysUntil);
            }
            else
            {
                _trayIconService?.UpdateIcon(null);
                Log.Information("Tray icon updated: No upcoming birthdays");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update tray icon");
        }
    }

    private void StartHourlyUpdateTimer()
    {
        try
        {
            var now = DateTime.Now;

            // 次の00分までの時間を計算
            var nextHour = now.Date.AddHours(now.Hour + 1);
            var dueTime = nextHour - now;

            Log.Information("Hourly update timer starting. First update at: {NextUpdate}", nextHour);

            // 毎時00分に実行するタイマーを設定
            _hourlyUpdateTimer = new System.Threading.Timer(
                async _ =>
                {
                    Log.Information("Hourly tray icon update triggered");
                    await UpdateTrayIconAsync();
                },
                null,
                dueTime,
                TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start hourly update timer");
        }
    }
}
