using System.Windows;
using FriendBirthdayManager.ViewModels;

namespace FriendBirthdayManager.Views;

/// <summary>
/// Interaction logic for ListWindow.xaml
/// </summary>
public partial class ListWindow : Window
{
    public ListWindow(ListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (s, e) => await viewModel.LoadFriendsAsync();
    }
}
