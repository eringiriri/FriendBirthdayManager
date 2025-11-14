using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.Services;

/// <summary>
/// Windowsスタートアップ登録サービスの実装
/// </summary>
public class StartupService : IStartupService
{
    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsRegisteredAsync()
    {
        try
        {
            _logger.LogInformation("Checking if registered in Windows startup...");
            // TODO: スタートアップ登録の確認（Phase 7で実装）
            await Task.CompletedTask;
            return false;
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
            // TODO: スタートアップへの登録（Phase 7で実装）
            await Task.CompletedTask;
            return true;
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
            // TODO: スタートアップからの登録解除（Phase 7で実装）
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister from startup");
            return false;
        }
    }
}
