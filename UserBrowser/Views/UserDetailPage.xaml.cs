using UserBrowser.ViewModels;

namespace UserBrowser.Views;

public partial class UserDetailPage : ContentPage
{
    public UserDetailPage(UserDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Entrance, in one short sequence: the avatar springs in, the details fade up,
    // and the action bar rises from the bottom edge.
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AvatarTile.Scale = 0.7;      AvatarTile.Opacity = 0;
        DetailScroll.Opacity = 0;    DetailScroll.TranslationY = 16;
        ActionBar.TranslationY = 64;

        await Task.WhenAll(
            AvatarTile.ScaleToAsync(1, 420, Easing.SpringOut),
            AvatarTile.FadeToAsync(1, 220),
            DetailScroll.FadeToAsync(1, 380),
            DetailScroll.TranslateToAsync(0, 0, 380, Easing.CubicOut),
            ActionBar.TranslateToAsync(0, 0, 420, Easing.CubicOut));
    }
}
