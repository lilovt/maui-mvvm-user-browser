using UserBrowser.ViewModels;

namespace UserBrowser.Views;

public partial class UserDetailPage : ContentPage
{
    public UserDetailPage(UserDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
