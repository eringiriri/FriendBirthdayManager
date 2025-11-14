using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Services;

/// <summary>
/// Windowsスタートアップ登録サービスの実装
/// タスクスケジューラを使用してスタートアップ登録を行う（UAC不要）
/// </summary>
public class StartupService : IStartupService
{
    private readonly ILogger<StartupService> _logger;
    private const string TaskName = "FriendBirthdayManager_Startup";

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsRegisteredAsync()
    {
        try
        {
            _logger.LogInformation("Checking if registered in Windows startup...");

            // タスクスケジューラでタスクの存在を確認
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Query /TN \"{TaskName}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start schtasks process");
                return false;
            }

            await process.WaitForExitAsync();

            // ExitCode 0 = タスクが存在する
            var isRegistered = process.ExitCode == 0;
            _logger.LogInformation("Startup registration status: {IsRegistered}", isRegistered);

            return isRegistered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check startup registration");
            return false;
        }
    }

    public async Task<bool> RegisterAsync()
    {
        try
        {
            _logger.LogInformation("Registering in Windows startup...");

            // 実行ファイルのパスを取得
            var exePath = Assembly.GetExecutingAssembly().Location;

            // .dllの場合は.exeに変換（dotnet publishで単一実行ファイルの場合）
            if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                exePath = exePath.Replace(".dll", ".exe");
            }

            // 実行ファイルが存在しない場合はProcessのパスを使用
            if (!File.Exists(exePath))
            {
                exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? exePath;
            }

            _logger.LogInformation("Executable path: {ExePath}", exePath);

            // 既存のタスクを削除（存在する場合）
            await UnregisterAsync();

            // タスクスケジューラにタスクを作成
            // /SC ONLOGON: ログオン時に実行
            // /TN: タスク名
            // /TR: 実行するプログラム
            // /RL HIGHEST: 最高の権限で実行（UAC不要）
            // /F: 既存のタスクを上書き
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Create /SC ONLOGON /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\"\" /RL HIGHEST /F",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start schtasks process");
                return false;
            }

            await process.WaitForExitAsync();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Successfully registered in startup: {Output}", output);
                return true;
            }
            else
            {
                _logger.LogError("Failed to register in startup. Exit code: {ExitCode}, Error: {Error}",
                    process.ExitCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register in startup");
            return false;
        }
    }

    public async Task<bool> UnregisterAsync()
    {
        try
        {
            _logger.LogInformation("Unregistering from Windows startup...");

            // タスクスケジューラからタスクを削除
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Delete /TN \"{TaskName}\" /F",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start schtasks process");
                return false;
            }

            await process.WaitForExitAsync();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            // ExitCode 0 = 成功
            // タスクが存在しない場合もエラーになるが、結果的には削除されているので成功とみなす
            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Successfully unregistered from startup: {Output}", output);
                return true;
            }
            else
            {
                // タスクが存在しない場合はエラーだが、目的は達成されているので警告レベル
                _logger.LogWarning("Unregister result - Exit code: {ExitCode}, Output: {Output}, Error: {Error}",
                    process.ExitCode, output, error);
                return true; // タスクが存在しない = 登録されていない = 目的達成
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister from startup");
            return false;
        }
    }
}
