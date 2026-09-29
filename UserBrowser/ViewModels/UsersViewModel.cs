using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UserBrowser.Models;
using UserBrowser.Services;

namespace UserBrowser.ViewModels;

// VIEWMODEL: state + commands for the page. No UI types, so it is unit-testable.
// ObservableObject implements INotifyPropertyChanged; the toolkit's source
// generators write the boilerplate for the [ObservableProperty]/[RelayCommand] members.
public partial class UsersViewModel : ObservableObject
{
    private readonly IApiService _api;
    private readonly INavigationService _navigation;

    public UsersViewModel(IApiService api, INavigationService navigation)
    {
        _api = api;
        _navigation = navigation;
    }

    // ObservableCollection raises CollectionChanged, so the CollectionView updates on add/clear.
    public ObservableCollection<User> Users { get; } = new();

    // Partial property: the toolkit's source generator writes the getter/setter
    // body with change notification (the AOT-safe form for WinUI).
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    // Also notifies HasError whenever it changes.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // Generates SelectUserCommand; the tapped User arrives as the command parameter.
    [RelayCommand]
    private Task SelectUserAsync(User user) => _navigation.GoToUserDetailAsync(user);

    // Generates LoadCommand (IAsyncRelayCommand). The toolkit prevents overlapping runs by default.
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var users = await _api.GetUsersAsync();

            Users.Clear();
            foreach (var user in users)
                Users.Add(user);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Couldn't reach the server. Check your connection and pull down to retry.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Something went wrong: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
