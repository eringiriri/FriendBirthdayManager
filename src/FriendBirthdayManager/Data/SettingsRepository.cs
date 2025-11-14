using FriendBirthdayManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Data;

/// <summary>
/// 設定情報のリポジトリ実装
/// </summary>
public class SettingsRepository : ISettingsRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<SettingsRepository> _logger;

    public SettingsRepository(AppDbContext context, ILogger<SettingsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            var setting = await _context.Settings.FindAsync(key);
            return setting?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get setting: {Key}", key);
            throw;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            var setting = await _context.Settings.FindAsync(key);
            if (setting == null)
            {
                _context.Settings.Add(new Setting
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated setting: {Key} = {Value}", key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set setting: {Key}", key);
            throw;
        }
    }

    public async Task<AppSettings> GetAppSettingsAsync()
    {
        try
        {
            var settings = new AppSettings();

            var defaultNotifyDaysBefore = await GetAsync("default_notify_days_before");
            if (int.TryParse(defaultNotifyDaysBefore, out var daysBefore))
            {
                settings.DefaultNotifyDaysBefore = daysBefore;
            }

            var defaultNotifySound = await GetAsync("default_notify_sound");
            if (bool.TryParse(defaultNotifySound, out var notifySound))
            {
                settings.DefaultNotifySound = notifySound;
            }

            var notificationTime = await GetAsync("notification_time");
            if (TimeSpan.TryParse(notificationTime, out var time))
            {
                settings.NotificationTime = time;
            }

            var startWithWindows = await GetAsync("start_with_windows");
            if (bool.TryParse(startWithWindows, out var startWith))
            {
                settings.StartWithWindows = startWith;
            }

            var language = await GetAsync("language");
            if (!string.IsNullOrEmpty(language))
            {
                settings.Language = language;
            }

            _logger.LogInformation("Retrieved application settings");
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get application settings");
            throw;
        }
    }

    public async Task SaveAppSettingsAsync(AppSettings settings)
    {
        try
        {
            await SetAsync("default_notify_days_before", settings.DefaultNotifyDaysBefore.ToString());
            await SetAsync("default_notify_sound", settings.DefaultNotifySound.ToString());
            await SetAsync("notification_time", settings.NotificationTime.ToString(@"hh\:mm"));
            await SetAsync("start_with_windows", settings.StartWithWindows.ToString());
            await SetAsync("language", settings.Language);

            _logger.LogInformation("Saved application settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save application settings");
            throw;
        }
    }

    public async Task<int> GetSchemaVersionAsync()
    {
        try
        {
            var version = await GetAsync("schema_version");
            return int.TryParse(version, out var v) ? v : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get schema version");
            throw;
        }
    }

    public async Task SetSchemaVersionAsync(int version)
    {
        try
        {
            await SetAsync("schema_version", version.ToString());
            _logger.LogInformation("Set schema version to {Version}", version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set schema version");
            throw;
        }
    }
}
