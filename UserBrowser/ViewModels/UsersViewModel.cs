using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UserBrowser.Services;

namespace UserBrowser.ViewModels;

// VIEWMODEL: state + commands for the list page. No UI types, so it is unit-testable.
// ObservableObject implements INotifyPropertyChanged; the toolkit's source generators
// write the boilerplate for the [ObservableProperty]/[RelayCommand] members.
public partial class UsersViewModel : ObservableObject
{
    private readonly IApiService _api;
    private readonly INavigationService _navigation;
    private readonly IFavoritesStore _favorites;

    public UsersViewModel(IApiService api, INavigationService navigation, IFavoritesStore favorites)
    {
        _api = api;
        _navigation = navigation;
        _favorites = favorites;
    }

    // Everyone the API returned.
    public ObservableCollection<UserItem> Users { get; } = new();

    // What the list actually shows: Users after search/filter, sorted and grouped by letter.
    public ObservableCollection<UserGroup> Groups { get; } = new();

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    // Bound two-way to the search box; every keystroke re-filters the list.
    [ObservableProperty]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    public partial bool ShowFavoritesOnly { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // ---- Text derived from state (recomputed by UpdateDerived) ----

    private int VisibleCount => Groups.Sum(g => g.Count);
    private bool HasQuery => !string.IsNullOrWhiteSpace(SearchText);
    private int FavoriteCount => Users.Count(u => u.IsFavorite);

    public string SummaryText =>
        Users.Count == 0 ? string.Empty
        : VisibleCount == Users.Count ? $"{Users.Count} people"
        : $"{VisibleCount} of {Users.Count} people";

    public string FavoritesLabel => FavoriteCount > 0 ? $"Favorites ({FavoriteCount})" : "Favorites";

    public string EmptyTitle =>
        IsBusy ? "Loading people…"
        : HasError ? string.Empty
        : Users.Count == 0 ? "No one here yet"
        : HasQuery ? $"No one matches “{SearchText!.Trim()}”"
        : "No favorites yet";

    public string EmptyHint =>
        IsBusy || HasError ? string.Empty
        : Users.Count == 0 ? "Pull down or use Refresh to load the list."
        : HasQuery ? "Try a name, company, or city."
        : "Tap the star next to a name to keep them here.";

    // ---- Commands ----

    // Generates SelectUserCommand; the tapped row's item arrives as the command parameter.
    [RelayCommand]
    private Task SelectUserAsync(UserItem item) => _navigation.GoToUserDetailAsync(item);

    [RelayCommand]
    private void ToggleFavorite(UserItem item) => item.IsFavorite = !item.IsFavorite;

    [RelayCommand]
    private void ShowAll() => ShowFavoritesOnly = false;

    [RelayCommand]
    private void ShowFavorites() => ShowFavoritesOnly = true;

    // Generates LoadCommand (IAsyncRelayCommand). The toolkit prevents overlapping runs by default.
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var users = await _api.GetUsersAsync();
            var favoriteIds = _favorites.Load();

            Users.Clear();
            foreach (var user in users)
            {
                var item = new UserItem(user, favoriteIds.Contains(user.Id));
                item.PropertyChanged += OnItemPropertyChanged;   // subscribe AFTER setting the initial state
                Users.Add(item);
            }

            ApplyFilter();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Couldn't reach the server. Check your connection and try again.";
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

    // ---- Reacting to state changes ----

    // Generated hooks: the toolkit calls these whenever the property changes.
    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnShowFavoritesOnlyChanged(bool value) => ApplyFilter();
    partial void OnIsBusyChanged(bool value) => UpdateDerived();
    partial void OnErrorMessageChanged(string? value) => UpdateDerived();

    // Fires when ANY item changes, whether the star was tapped in the list or on the detail page.
    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UserItem.IsFavorite)) return;

        _favorites.Save(Users.Where(u => u.IsFavorite).Select(u => u.User.Id));

        // Rebuilding the list on every star tap would jump the scroll position, so only
        // do it when the favorites filter is on (the row has to appear or disappear).
        if (ShowFavoritesOnly) ApplyFilter();
        else UpdateDerived();
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim();

        var matches = Users
            .Where(u => !ShowFavoritesOnly || u.IsFavorite)
            .Where(u => string.IsNullOrEmpty(query) || u.Matches(query))
            .OrderBy(u => u.SortName, StringComparer.CurrentCultureIgnoreCase)
            .GroupBy(u => u.GroupKey);

        Groups.Clear();
        foreach (var group in matches)
            Groups.Add(new UserGroup(group.Key, group));

        UpdateDerived();
    }

    private void UpdateDerived()
    {
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(FavoritesLabel));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyHint));
    }
}
