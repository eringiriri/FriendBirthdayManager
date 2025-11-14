using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace FriendBirthdayManager.ViewModels;

/// <summary>
/// クレジット画面（About画面）のViewModel
/// </summary>
public partial class AboutViewModel : ObservableObject
{
    private readonly ILogger<AboutViewModel> _logger;

    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _author = "えりんぎ";

    [ObservableProperty]
    private string _twitterUrl = "https://twitter.com/eringi_vrc";

    [ObservableProperty]
    private string _email = "eringi@eringi.me";

    [ObservableProperty]
    private string _githubUrl = "https://github.com/eringiriri/FriendBirthdayManager";

    [ObservableProperty]
    private string _license = "MIT License";

    public AboutViewModel(ILogger<AboutViewModel> logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    private void OpenTwitter()
    {
        try
        {
            _logger.LogInformation("Opening Twitter profile: {Url}", TwitterUrl);
            OpenUrl(TwitterUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Twitter profile");
        }
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            _logger.LogInformation("Opening GitHub repository: {Url}", GithubUrl);
            OpenUrl(GithubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open GitHub repository");
        }
    }

    private void OpenUrl(string url)
    {
        var psi = new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        };
        Process.Start(psi);
    }
}
