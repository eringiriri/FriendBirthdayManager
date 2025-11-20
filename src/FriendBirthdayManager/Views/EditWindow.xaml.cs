using System.Windows;
using FriendBirthdayManager.ViewModels;

namespace FriendBirthdayManager.Views;

/// <summary>
/// Interaction logic for EditWindow.xaml
/// </summary>
public partial class EditWindow : BaseWindow
{
    private readonly EditViewModel _viewModel;

    public EditWindow(EditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public int? FriendId { get; private set; }

    public async Task LoadFriendAsync(int friendId)
    {
        FriendId = friendId;
        await _viewModel.LoadFriendAsync(friendId);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
