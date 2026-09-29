using UserBrowser.Models;

namespace UserBrowser.Services;

// The real implementation, backed by MAUI Shell. Not included in the unit test project.
public class ShellNavigationService : INavigationService
{
    public const string DetailRoute = "userdetail";

    public Task GoToUserDetailAsync(User user) =>
        // Shell passes the object to the target ViewModel via its [QueryProperty].
        Shell.Current.GoToAsync(DetailRoute, new Dictionary<string, object> { ["User"] = user });
}
