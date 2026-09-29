using UserBrowser.Models;
using UserBrowser.Services;
using UserBrowser.ViewModels;

namespace UserBrowser.Tests;

public class UsersViewModelTests
{
    // ---- Hand-written fakes (no mocking library needed) ----

    private class FakeApiService : IApiService
    {
        public List<User> UsersToReturn { get; set; } = new();
        public Exception? ExceptionToThrow { get; set; }
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
            return Task.FromResult<IReadOnlyList<User>>(UsersToReturn);
        }
    }

    private class SlowApi : IApiService
    {
        private readonly Task<IReadOnlyList<User>> _result;
        public SlowApi(Task<IReadOnlyList<User>> result) => _result = result;
        public Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default) => _result;
    }

    private class FakeNavigationService : INavigationService
    {
        public UserItem? NavigatedTo { get; private set; }

        public Task GoToUserDetailAsync(UserItem item)
        {
            NavigatedTo = item;
            return Task.CompletedTask;
        }
    }

    private class FakeFavoritesStore : IFavoritesStore
    {
        public HashSet<int> Stored { get; set; } = new();
        public int SaveCount { get; private set; }

        public IReadOnlySet<int> Load() => Stored;

        public void Save(IEnumerable<int> userIds)
        {
            Stored = userIds.ToHashSet();
            SaveCount++;
        }
    }

    private static User MakeUser(int id, string name = "Test User", string company = "Acme", string city = "Greensboro") =>
        new(id, name, "user" + id, $"user{id}@example.com", "555-0100", "example.com",
            new Company(company), new Address(city));

    private class Context
    {
        public FakeApiService Api { get; } = new();
        public FakeNavigationService Nav { get; } = new();
        public FakeFavoritesStore Favorites { get; } = new();
        public UsersViewModel Vm { get; }

        public Context() => Vm = new UsersViewModel(Api, Nav, Favorites);
    }

    // ---- Loading ----

    [Fact]
    public async Task Load_PopulatesUsers()
    {
        var c = new Context();
        c.Api.UsersToReturn = new() { MakeUser(1), MakeUser(2), MakeUser(3) };

        await c.Vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, c.Vm.Users.Count);
        Assert.Equal("3 people", c.Vm.SummaryText);
        Assert.False(c.Vm.HasError);
    }

    [Fact]
    public async Task Load_ClearsIsBusyWhenFinished()
    {
        var c = new Context();

        await c.Vm.LoadCommand.ExecuteAsync(null);

        Assert.False(c.Vm.IsBusy);
    }

    [Fact]
    public async Task Load_SetsIsBusyWhileRunning()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<User>>();
        var vm = new UsersViewModel(new SlowApi(tcs.Task), new FakeNavigationService(), new FakeFavoritesStore());

        var running = vm.LoadCommand.ExecuteAsync(null);
        Assert.True(vm.IsBusy);                       // in flight
        Assert.Equal("Loading people…", vm.EmptyTitle);

        tcs.SetResult(new List<User>());
        await running;
        Assert.False(vm.IsBusy);                      // done
    }

    [Fact]
    public async Task Load_OnNetworkError_SetsErrorAndKeepsListEmpty()
    {
        var c = new Context();
        c.Api.ExceptionToThrow = new HttpRequestException("offline");

        await c.Vm.LoadCommand.ExecuteAsync(null);

        Assert.True(c.Vm.HasError);
        Assert.Contains("reach the server", c.Vm.ErrorMessage);
        Assert.Empty(c.Vm.Users);
        Assert.False(c.Vm.IsBusy);                    // finally-block still ran
    }

    [Fact]
    public async Task Load_OnUnexpectedError_ShowsMessage()
    {
        var c = new Context();
        c.Api.ExceptionToThrow = new InvalidOperationException("boom");

        await c.Vm.LoadCommand.ExecuteAsync(null);

        Assert.True(c.Vm.HasError);
        Assert.Contains("boom", c.Vm.ErrorMessage);
    }

    [Fact]
    public async Task Reload_ReplacesUsersInsteadOfDuplicating()
    {
        var c = new Context();
        c.Api.UsersToReturn = new() { MakeUser(1), MakeUser(2) };

        await c.Vm.LoadCommand.ExecuteAsync(null);
        await c.Vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, c.Vm.Users.Count);
        Assert.Equal(2, c.Api.CallCount);
    }

    [Fact]
    public async Task SuccessfulLoad_ClearsPreviousError()
    {
        var c = new Context();
        c.Api.ExceptionToThrow = new HttpRequestException();
        await c.Vm.LoadCommand.ExecuteAsync(null);
        Assert.True(c.Vm.HasError);

        c.Api.ExceptionToThrow = null;
        c.Api.UsersToReturn = new() { MakeUser(1) };
        await c.Vm.LoadCommand.ExecuteAsync(null);

        Assert.False(c.Vm.HasError);
        Assert.Single(c.Vm.Users);
    }

    [Fact]
    public void ErrorMessage_RaisesPropertyChangedForHasError()
    {
        var c = new Context();
        var raised = new List<string?>();
        c.Vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        c.Vm.ErrorMessage = "oops";

        Assert.Contains(nameof(UsersViewModel.HasError), raised);
    }

    // ---- Grouping and search ----

    [Fact]
    public async Task Groups_AreAlphabeticalByFirstLetterIgnoringHonorifics()
    {
        var c = new Context();
        c.Api.UsersToReturn = new()
        {
            MakeUser(1, "Leanne Graham"),
            MakeUser(2, "Ervin Howell"),
            MakeUser(3, "Mrs. Dennis Schulist"),   // sorts under D, not M
            MakeUser(4, "Clementine Bauch"),
            MakeUser(5, "Chelsey Dietrich"),
        };

        await c.Vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "C", "D", "E", "L" }, c.Vm.Groups.Select(g => g.Key));
        Assert.Equal(new[] { "Chelsey Dietrich", "Clementine Bauch" },
                     c.Vm.Groups[0].Select(i => i.User.Name));
    }

    [Fact]
    public async Task Search_FiltersByNameCompanyOrCity_AndClearingRestoresAll()
    {
        var c = new Context();
        c.Api.UsersToReturn = new()
        {
            MakeUser(1, "Leanne Graham", "Romaguera-Crona", "Gwenborough"),
            MakeUser(2, "Ervin Howell", "Deckow-Crist", "Wisokyburgh"),
            MakeUser(3, "Clementine Bauch", "Romaguera-Jacobson", "McKenziehaven"),
        };
        await c.Vm.LoadCommand.ExecuteAsync(null);

        c.Vm.SearchText = "romaguera";                       // company, case-insensitive
        Assert.Equal(2, c.Vm.Groups.Sum(g => g.Count));
        Assert.Equal("2 of 3 people", c.Vm.SummaryText);

        c.Vm.SearchText = "wisoky";                          // city
        Assert.Equal("Ervin Howell", c.Vm.Groups.Single().Single().User.Name);

        c.Vm.SearchText = "";
        Assert.Equal(3, c.Vm.Groups.Sum(g => g.Count));
    }

    [Fact]
    public async Task Search_WithNoMatches_ExplainsWhatToDo()
    {
        var c = new Context();
        c.Api.UsersToReturn = new() { MakeUser(1, "Leanne Graham") };
        await c.Vm.LoadCommand.ExecuteAsync(null);

        c.Vm.SearchText = "zzz";

        Assert.Empty(c.Vm.Groups);
        Assert.Contains("zzz", c.Vm.EmptyTitle);
        Assert.Contains("name, company, or city", c.Vm.EmptyHint);
    }

    // ---- Favorites ----

    [Fact]
    public async Task ToggleFavorite_PersistsIdsAndUpdatesLabel()
    {
        var c = new Context();
        c.Api.UsersToReturn = new() { MakeUser(1), MakeUser(2) };
        await c.Vm.LoadCommand.ExecuteAsync(null);
        Assert.Equal("Favorites", c.Vm.FavoritesLabel);

        c.Vm.ToggleFavoriteCommand.Execute(c.Vm.Users[1]);

        Assert.True(c.Vm.Users[1].IsFavorite);
        Assert.Equal("★", c.Vm.Users[1].StarGlyph);
        Assert.Equal(new HashSet<int> { 2 }, c.Favorites.Stored);
        Assert.Equal("Favorites (1)", c.Vm.FavoritesLabel);
    }

    [Fact]
    public async Task Favorites_AreRestoredFromStore()
    {
        var c = new Context();
        c.Favorites.Stored = new() { 2 };
        c.Api.UsersToReturn = new() { MakeUser(1), MakeUser(2) };

        await c.Vm.LoadCommand.ExecuteAsync(null);

        Assert.False(c.Vm.Users[0].IsFavorite);
        Assert.True(c.Vm.Users[1].IsFavorite);
        Assert.Equal(0, c.Favorites.SaveCount);           // restoring must not re-save
    }

    [Fact]
    public async Task FavoritesFilter_ShowsOnlyFavorites_AndRemovesRowWhenUnstarred()
    {
        var c = new Context();
        c.Favorites.Stored = new() { 1 };
        c.Api.UsersToReturn = new() { MakeUser(1, "Leanne Graham"), MakeUser(2, "Ervin Howell") };
        await c.Vm.LoadCommand.ExecuteAsync(null);

        c.Vm.ShowFavoritesCommand.Execute(null);
        Assert.Equal("Leanne Graham", c.Vm.Groups.Single().Single().User.Name);

        c.Vm.ToggleFavoriteCommand.Execute(c.Vm.Users[0]);   // unstar the only favorite
        Assert.Empty(c.Vm.Groups);
        Assert.Equal("No favorites yet", c.Vm.EmptyTitle);

        c.Vm.ShowAllCommand.Execute(null);
        Assert.Equal(2, c.Vm.Groups.Sum(g => g.Count));
    }

    [Fact]
    public async Task ChangingFavoriteOnItem_IsPersisted_EvenWhenChangedElsewhere()
    {
        // The detail page toggles the same UserItem directly; the list must still save it.
        var c = new Context();
        c.Api.UsersToReturn = new() { MakeUser(1) };
        await c.Vm.LoadCommand.ExecuteAsync(null);

        c.Vm.Users[0].IsFavorite = true;

        Assert.Equal(new HashSet<int> { 1 }, c.Favorites.Stored);
    }

    // ---- Navigation ----

    [Fact]
    public async Task SelectUser_NavigatesToDetailWithThatItem()
    {
        var c = new Context();
        c.Api.UsersToReturn = new() { MakeUser(7, "Grace Hopper") };
        await c.Vm.LoadCommand.ExecuteAsync(null);
        var item = c.Vm.Users[0];

        await c.Vm.SelectUserCommand.ExecuteAsync(item);

        Assert.Same(item, c.Nav.NavigatedTo);
    }
}

public class UserItemTests
{
    private static UserItem Item(int id, string name) =>
        new(new User(id, name, "u", "e@x.com", "1", "x.com", new Company("Co"), new Address("City")));

    [Theory]
    [InlineData("Leanne Graham", "LG")]
    [InlineData("Mrs. Dennis Schulist", "DS")]           // honorific skipped
    [InlineData("Nicholas Runolfsdottir V", "NR")]       // suffix skipped
    [InlineData("Clementina DuBuque", "CD")]
    [InlineData("Prince", "P")]                          // single name
    public void Initials_UseFirstAndLastMeaningfulWord(string name, string expected) =>
        Assert.Equal(expected, Item(1, name).Initials);

    [Fact]
    public void SortName_DropsHonorific_SoGroupKeyIsTheRealFirstName()
    {
        var item = Item(1, "Mrs. Dennis Schulist");

        Assert.Equal("Dennis Schulist", item.SortName);
        Assert.Equal("D", item.GroupKey);
    }

    [Fact]
    public void AvatarColor_IsStablePerUser_AndDiffersBetweenNeighbours()
    {
        Assert.Equal(Item(3, "A B").AvatarColor, Item(3, "C D").AvatarColor);
        Assert.NotEqual(Item(1, "A B").AvatarColor, Item(2, "A B").AvatarColor);
    }

    [Fact]
    public void Matches_IsCaseInsensitiveAcrossFields()
    {
        var item = Item(1, "Leanne Graham");

        Assert.True(item.Matches("LEANNE"));
        Assert.True(item.Matches("city"));
        Assert.False(item.Matches("nobody"));
    }
}
