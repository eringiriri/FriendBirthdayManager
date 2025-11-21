using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FriendBirthdayManager.Data;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Services;

/// <summary>
/// タスクトレイアイコンサービスの実装
/// </summary>
public class TrayIconService : ITrayIconService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<TrayIconService> _logger;
    private TaskbarIcon? _taskbarIcon;
    private Icon? _currentIcon;
    private bool _disposed;
    private bool _isIconInitialized;
    private int? _currentDaysUntil;

    public TrayIconService(IServiceProvider serviceProvider, ILocalizationService localizationService, ILogger<TrayIconService> logger)
    {
        _serviceProvider = serviceProvider;
        _localizationService = localizationService;
        _logger = logger;
    }

    public void Initialize()
    {
        try
        {
            _logger.LogInformation("Initializing tray icon service...");

            Application.Current.Dispatcher.Invoke(() =>
            {
                _taskbarIcon = new TaskbarIcon
                {
                    ToolTipText = "Friend Birthday Manager",
                    ContextMenu = CreateContextMenu()
                };

                // ダブルクリックでメインウィンドウを表示
                _taskbarIcon.TrayMouseDoubleClick += (s, e) =>
                {
                    ShowMainWindow();
                };

                // タスクトレイアイコンを強制的に作成
                _taskbarIcon.ForceCreate();
                _logger.LogInformation("TaskbarIcon ForceCreate called");

                // アイコンを作成後に初期アイコンを設定
                UpdateIcon(null);
            });

            _logger.LogInformation("Tray icon initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tray icon");
            throw;
        }
    }

    public void UpdateIcon(int? daysUntilNextBirthday)
    {
        try
        {
            if (_taskbarIcon == null)
            {
                _logger.LogWarning("TaskbarIcon is not initialized");
                return;
            }

            // 初回ではない、かつ既に同じ状態の場合は更新しない
            if (_isIconInitialized && _currentDaysUntil == daysUntilNextBirthday)
            {
                return;
            }

            _isIconInitialized = true;
            _currentDaysUntil = daysUntilNextBirthday;

            Application.Current.Dispatcher.Invoke(() =>
            {
                string iconFileName = GetIconFileName(daysUntilNextBirthday);
                // 埋め込みリソースからアイコンを読み込む
                string resourceName = $"FriendBirthdayManager.Resources.Icons.{iconFileName}";

                try
                {
                    var assembly = Assembly.GetExecutingAssembly();

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            // 古いアイコンをDispose
                            _currentIcon?.Dispose();

                            // 新しいアイコンを作成して設定
                            _currentIcon = new Icon(stream);
                            _taskbarIcon.Icon = _currentIcon;

                            string tooltip = daysUntilNextBirthday.HasValue
                                ? string.Format(_localizationService.GetString("TrayTooltipDaysUntil"), daysUntilNextBirthday.Value)
                                : _localizationService.GetString("TrayTooltipDefault");
                            _taskbarIcon.ToolTipText = tooltip;

                            _logger.LogInformation("Tray icon updated: {IconFileName}, Days: {Days}", iconFileName, daysUntilNextBirthday);
                        }
                        else
                        {
                            _logger.LogWarning("Icon resource not found: {ResourceName}", resourceName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load icon resource: {ResourceName}", resourceName);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tray icon");
        }
    }

    public async Task UpdateTrayIconFromRepositoryAsync()
    {
        try
        {
            _logger.LogInformation("Updating tray icon from repository...");

            using var scope = _serviceProvider.CreateScope();
            var friendRepository = scope.ServiceProvider.GetRequiredService<IFriendRepository>();

            // 直近の誕生日を取得
            var upcomingBirthdays = await friendRepository.GetUpcomingBirthdaysAsync(DateTime.Now, 1);
            if (upcomingBirthdays.Count > 0)
            {
                var nextFriend = upcomingBirthdays[0];
                var daysUntil = nextFriend.CalculateDaysUntilBirthday(DateTime.Now);
                UpdateIcon(daysUntil);
                _logger.LogInformation("Tray icon updated from repository: Next birthday in {Days} days", daysUntil);
            }
            else
            {
                UpdateIcon(null);
                _logger.LogInformation("Tray icon updated from repository: No upcoming birthdays");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tray icon from repository");
        }
    }

    public void ShowBalloonTip(string title, string message)
    {
        try
        {
            if (_taskbarIcon == null)
            {
                _logger.LogWarning("TaskbarIcon is not initialized");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _taskbarIcon.ShowNotification(title, message);
            });

            _logger.LogInformation("Balloon tip shown: {Title} - {Message}", title, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show balloon tip");
        }
    }

    public void UpdateMenu()
    {
        try
        {
            if (_taskbarIcon == null)
            {
                _logger.LogWarning("TaskbarIcon is not initialized");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                // メニューを再作成
                _taskbarIcon.ContextMenu = CreateContextMenu();

                // ツールチップも更新
                string tooltip = _currentDaysUntil.HasValue
                    ? string.Format(_localizationService.GetString("TrayTooltipDaysUntil"), _currentDaysUntil.Value)
                    : _localizationService.GetString("TrayTooltipDefault");
                _taskbarIcon.ToolTipText = tooltip;
            });

            _logger.LogInformation("Tray icon menu and tooltip updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update menu");
        }
    }

    private ContextMenu CreateContextMenu()
    {
        var contextMenu = new ContextMenu();

        // 誕生日を追加
        var addMenuItem = new MenuItem { Header = _localizationService.GetString("TrayMenuAddBirthday") };
        addMenuItem.Click += (s, e) => ShowMainWindow();
        contextMenu.Items.Add(addMenuItem);

        // 一覧表示
        var listMenuItem = new MenuItem { Header = _localizationService.GetString("TrayMenuShowList") };
        listMenuItem.Click += (s, e) => ShowListWindow();
        contextMenu.Items.Add(listMenuItem);

        contextMenu.Items.Add(new Separator());

        // 設定
        var settingsMenuItem = new MenuItem { Header = _localizationService.GetString("TrayMenuSettings") };
        settingsMenuItem.Click += (s, e) => ShowSettingsWindow();
        contextMenu.Items.Add(settingsMenuItem);

        // クレジット
        var aboutMenuItem = new MenuItem { Header = _localizationService.GetString("TrayMenuCredits") };
        aboutMenuItem.Click += (s, e) => ShowAboutDialog();
        contextMenu.Items.Add(aboutMenuItem);

        contextMenu.Items.Add(new Separator());

        // 終了
        var exitMenuItem = new MenuItem { Header = _localizationService.GetString("TrayMenuExit") };
        exitMenuItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitMenuItem);

        return contextMenu;
    }

    private string GetIconFileName(int? daysUntil)
    {
        if (daysUntil.HasValue && daysUntil.Value >= 1 && daysUntil.Value <= 9)
        {
            return $"number_{daysUntil.Value}.ico";
        }

        return "birthday_cake.ico";
    }

    private void ShowMainWindow()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault();
                if (mainWindow == null)
                {
                    mainWindow = _serviceProvider.GetRequiredService<Views.MainWindow>();
                    mainWindow.Show();
                }
                else
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show main window");
        }
    }

    private void ShowListWindow()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var listWindow = Application.Current.Windows.OfType<Views.ListWindow>().FirstOrDefault();
                if (listWindow == null)
                {
                    listWindow = _serviceProvider.GetRequiredService<Views.ListWindow>();
                    listWindow.Show();
                }
                else
                {
                    listWindow.Show();
                    listWindow.WindowState = WindowState.Normal;
                    listWindow.Activate();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show list window");
        }
    }

    private void ShowSettingsWindow()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var settingsWindow = Application.Current.Windows.OfType<Views.SettingsWindow>().FirstOrDefault();
                if (settingsWindow == null)
                {
                    settingsWindow = _serviceProvider.GetRequiredService<Views.SettingsWindow>();
                    settingsWindow.Show();
                }
                else
                {
                    settingsWindow.Show();
                    settingsWindow.WindowState = WindowState.Normal;
                    settingsWindow.Activate();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show settings window");
        }
    }

    private void ShowAboutDialog()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var aboutWindow = Application.Current.Windows.OfType<Views.AboutWindow>().FirstOrDefault();
                if (aboutWindow == null)
                {
                    aboutWindow = _serviceProvider.GetRequiredService<Views.AboutWindow>();
                    aboutWindow.Show();
                }
                else
                {
                    aboutWindow.Show();
                    aboutWindow.WindowState = WindowState.Normal;
                    aboutWindow.Activate();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show about dialog");
        }
    }

    private void ExitApplication()
    {
        try
        {
            _logger.LogInformation("Exiting application from tray icon...");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exit application");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing tray icon service...");

        Application.Current.Dispatcher.Invoke(() =>
        {
            _taskbarIcon?.Dispose();
            _currentIcon?.Dispose();
        });

        _disposed = true;
    }
}
