using UserBrowser.ViewModels;

namespace UserBrowser.Views;

public partial class UsersPage : ContentPage
{
    private readonly UsersViewModel _viewModel;

    // The ViewModel is constructor-injected; the page just sets it as BindingContext.
    public UsersPage(UsersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load once on first show; pull-to-refresh handles reloads after that.
        if (_viewModel.Users.Count == 0 && _viewModel.LoadCommand.CanExecute(null))
            _viewModel.LoadCommand.Execute(null);
    }
}
