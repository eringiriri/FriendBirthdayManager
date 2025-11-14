using System.Windows;
using FriendBirthdayManager.ViewModels;

namespace FriendBirthdayManager.Views;

/// <summary>
/// Interaction logic for EditWindow.xaml
/// </summary>
public partial class EditWindow : Window
{
    private readonly EditViewModel _viewModel;

    public EditWindow(EditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public async Task LoadFriendAsync(int friendId)
    {
        await _viewModel.LoadFriendAsync(friendId);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
