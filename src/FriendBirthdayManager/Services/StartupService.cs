using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace FriendBirthdayManager.Services;

/// <summary>
/// Windowsスタートアップ登録サービスの実装
/// レジストリを使用してスタートアップ登録を行う（UAC不要）
/// </summary>
public class StartupService : IStartupService
{
    private readonly ILogger<StartupService> _logger;
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryValueName = "FriendBirthdayManager";

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsRegisteredAsync()
    {
        try
        {
            _logger.LogInformation("Checking if registered in Windows startup...");

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            if (key == null)
            {
                _logger.LogInformation("Startup registration status: false (registry key not found)");
                return Task.FromResult(false);
            }

            var value = key.GetValue(RegistryValueName);
            var isRegistered = value != null;

            if (isRegistered)
            {
                _logger.LogInformation("Startup registration status: true (value: {Value})", value);
            }
            else
            {
                _logger.LogInformation("Startup registration status: false");
            }

            return Task.FromResult(isRegistered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check startup registration");
            return Task.FromResult(false);
        }
    }

    public Task<bool> RegisterAsync()
    {
        try
        {
            _logger.LogInformation("Registering in Windows startup...");

            // 実行ファイルのパスを取得
            var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

            // .dllの場合は.exeに変換（念のため）
            if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                exePath = exePath.Replace(".dll", ".exe");
            }

            // セキュリティチェック: exePathのバリデーション
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                _logger.LogError("Invalid executable path: {ExePath}", exePath);
                return Task.FromResult(false);
            }

            _logger.LogInformation("Executable path: {ExePath}", exePath);

            // レジストリに書き込み
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                _logger.LogError("Failed to open registry key: {KeyPath}", RegistryKeyPath);
                return Task.FromResult(false);
            }

            // パスにスペースが含まれる場合はダブルクォートで囲む
            var registryValue = exePath.Contains(" ") ? $"\"{exePath}\"" : exePath;
            key.SetValue(RegistryValueName, registryValue, RegistryValueKind.String);

            _logger.LogInformation("Successfully registered in startup: {Value}", registryValue);
            return Task.FromResult(true);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when registering in startup");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register in startup");
            return Task.FromResult(false);
        }
    }

    public Task<bool> UnregisterAsync()
    {
        try
        {
            _logger.LogInformation("Unregistering from Windows startup...");

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                _logger.LogWarning("Registry key not found, already unregistered");
                return Task.FromResult(true); // キーが存在しない = 既に登録解除済み
            }

            var value = key.GetValue(RegistryValueName);
            if (value == null)
            {
                _logger.LogInformation("Registry value not found, already unregistered");
                return Task.FromResult(true); // 値が存在しない = 既に登録解除済み
            }

            // レジストリから値を削除
            key.DeleteValue(RegistryValueName, false);

            _logger.LogInformation("Successfully unregistered from startup");
            return Task.FromResult(true);
        }
        catch (ArgumentException)
        {
            // 値が存在しない場合
            _logger.LogInformation("Registry value not found, already unregistered");
            return Task.FromResult(true); // 既に登録解除済み
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when unregistering from startup");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister from startup");
            return Task.FromResult(false);
        }
    }
}
