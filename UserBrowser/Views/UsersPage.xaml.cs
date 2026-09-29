using System.ComponentModel;
using UserBrowser.ViewModels;

namespace UserBrowser.Views;

public partial class UsersPage : ContentPage
{
    private readonly UsersViewModel _viewModel;
    private bool _introPlayed;
    private DateTime _lastStarTap = DateTime.MinValue;

    // The ViewModel is constructor-injected; the page just sets it as BindingContext.
    public UsersPage(UsersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        FooterBar.SizeChanged += (_, _) => MoveTabPill(animated: false);   // keep the pill aligned on resize
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load once on first show; pull-to-refresh handles reloads after that.
        if (_viewModel.Users.Count == 0 && _viewModel.LoadCommand.CanExecute(null))
            _viewModel.LoadCommand.Execute(null);

        // The bars settle into place once, the first time the page appears.
        if (!_introPlayed)
        {
            _introPlayed = true;
            await PlayIntroAsync();
        }
    }

    // ---- Transitions (pure visuals, so they live in code-behind) ----

    private async Task PlayIntroAsync()
    {
        HeaderBar.TranslationY = -28;  HeaderBar.Opacity = 0;
        FooterBar.TranslationY = 36;   FooterBar.Opacity = 0;
        CircleLarge.Scale = 0.6;       CircleSmall.Scale = 0.6;

        await Task.WhenAll(
            HeaderBar.TranslateToAsync(0, 0, 450, Easing.CubicOut),
            HeaderBar.FadeToAsync(1, 350),
            FooterBar.TranslateToAsync(0, 0, 450, Easing.CubicOut),
            FooterBar.FadeToAsync(1, 350),
            CircleLarge.ScaleToAsync(1, 900, Easing.CubicOut),
            CircleSmall.ScaleToAsync(1, 1100, Easing.CubicOut));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UsersViewModel.ShowFavoritesOnly))
            MoveTabPill(animated: true);
    }

    // Slide the highlight under the active tab: the People tab is the left half of the bar, Favorites the right half.
    private void MoveTabPill(bool animated)
    {
        if (FooterBar.Width <= 0) return;

        var target = _viewModel.ShowFavoritesOnly ? FooterBar.Width / 2 : 0;
        TabPill.AbortAnimation("TranslateTo");

        if (animated) _ = TabPill.TranslateToAsync(target, 0, 320, Easing.CubicInOut);
        else TabPill.TranslationX = target;
    }

    // ---- Row taps: forward to the ViewModel's commands ----
    // Each row's BindingContext is its UserItem.

    private void OnRowTapped(object? sender, TappedEventArgs e)
    {
        // The star sits inside the row; if the same touch also reaches the row, don't open the detail page.
        if (DateTime.UtcNow - _lastStarTap < TimeSpan.FromMilliseconds(400)) return;

        if ((sender as BindableObject)?.BindingContext is UserItem item)
            _viewModel.SelectUserCommand.Execute(item);
    }

    private async void OnStarTapped(object? sender, TappedEventArgs e)
    {
        _lastStarTap = DateTime.UtcNow;

        if (sender is not VisualElement star) return;

        if (star.BindingContext is UserItem item)
            _viewModel.ToggleFavoriteCommand.Execute(item);

        // Bounce: pure visual feedback, so it stays in code-behind.
        await star.ScaleToAsync(1.4, 90, Easing.CubicOut);
        await star.ScaleToAsync(1.0, 140, Easing.SpringOut);
    }
}
