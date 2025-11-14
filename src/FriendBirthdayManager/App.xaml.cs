using System.IO;
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

    public App()
    {
        // Serilogの設定
        ConfigureLogging();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // DIコンテナの設定
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            Log.Information("Application starting...");

            // データベースの初期化
            InitializeDatabaseAsync().GetAwaiter().GetResult();

            // タスクトレイアイコンを初期化
            _trayIconService = _serviceProvider.GetRequiredService<ITrayIconService>();
            _trayIconService.Initialize();

            // アイコンを更新（直近の誕生日を取得）
            UpdateTrayIconAsync().GetAwaiter().GetResult();

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

        // タスクトレイサービスのクリーンアップ
        _trayIconService?.Dispose();

        // リソースのクリーンアップ
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

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

        // Views
        services.AddTransient<Views.MainWindow>();
        services.AddTransient<Views.ListWindow>();
        services.AddTransient<Views.EditWindow>();
        services.AddTransient<Views.SettingsWindow>();

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
}
