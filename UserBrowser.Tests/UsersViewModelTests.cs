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

    private class FakeNavigationService : INavigationService
    {
        public User? NavigatedTo { get; private set; }

        public Task GoToUserDetailAsync(User user)
        {
            NavigatedTo = user;
            return Task.CompletedTask;
        }
    }

    private static User MakeUser(int id, string name = "Test User") =>
        new(id, name, "user" + id, $"user{id}@example.com", "555-0100", "example.com",
            new Company("Acme"), new Address("Greensboro"));

    private static (UsersViewModel vm, FakeApiService api, FakeNavigationService nav) Create()
    {
        var api = new FakeApiService();
        var nav = new FakeNavigationService();
        return (new UsersViewModel(api, nav), api, nav);
    }

    // ---- Tests ----

    [Fact]
    public async Task Load_PopulatesUsers()
    {
        var (vm, api, _) = Create();
        api.UsersToReturn = new() { MakeUser(1), MakeUser(2), MakeUser(3) };

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, vm.Users.Count);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task Load_ClearsIsBusyWhenFinished()
    {
        var (vm, _, _) = Create();

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Load_SetsIsBusyWhileRunning()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<User>>();
        var vm = new UsersViewModel(new SlowApi(tcs.Task), new FakeNavigationService());

        var running = vm.LoadCommand.ExecuteAsync(null);
        Assert.True(vm.IsBusy);                  // in flight

        tcs.SetResult(new List<User>());
        await running;
        Assert.False(vm.IsBusy);                 // done
    }

    private class SlowApi : IApiService
    {
        private readonly Task<IReadOnlyList<User>> _result;
        public SlowApi(Task<IReadOnlyList<User>> result) => _result = result;
        public Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default) => _result;
    }

    [Fact]
    public async Task Load_OnNetworkError_SetsErrorAndKeepsListEmpty()
    {
        var (vm, api, _) = Create();
        api.ExceptionToThrow = new HttpRequestException("offline");

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.Contains("reach the server", vm.ErrorMessage);
        Assert.Empty(vm.Users);
        Assert.False(vm.IsBusy);                 // finally-block still ran
    }

    [Fact]
    public async Task Load_OnUnexpectedError_ShowsMessage()
    {
        var (vm, api, _) = Create();
        api.ExceptionToThrow = new InvalidOperationException("boom");

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.Contains("boom", vm.ErrorMessage);
    }

    [Fact]
    public async Task Reload_ReplacesUsersInsteadOfDuplicating()
    {
        var (vm, api, _) = Create();
        api.UsersToReturn = new() { MakeUser(1), MakeUser(2) };

        await vm.LoadCommand.ExecuteAsync(null);
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Users.Count);
        Assert.Equal(2, api.CallCount);
    }

    [Fact]
    public async Task SuccessfulLoad_ClearsPreviousError()
    {
        var (vm, api, _) = Create();
        api.ExceptionToThrow = new HttpRequestException();
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);

        api.ExceptionToThrow = null;
        api.UsersToReturn = new() { MakeUser(1) };
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.HasError);
        Assert.Single(vm.Users);
    }

    [Fact]
    public void ErrorMessage_RaisesPropertyChangedForHasError()
    {
        var (vm, _, _) = Create();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.ErrorMessage = "oops";

        Assert.Contains(nameof(UsersViewModel.HasError), raised);
    }

    [Fact]
    public async Task SelectUser_NavigatesToDetailWithThatUser()
    {
        var (vm, _, nav) = Create();
        var user = MakeUser(7, "Grace Hopper");

        await vm.SelectUserCommand.ExecuteAsync(user);

        Assert.Equal(user, nav.NavigatedTo);
    }
}
