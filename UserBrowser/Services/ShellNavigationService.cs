using UserBrowser.ViewModels;

namespace UserBrowser.Services;

// The real implementation, backed by MAUI Shell. Not included in the unit test project.
public class ShellNavigationService : INavigationService
{
    public const string DetailRoute = "userdetail";

    public Task GoToUserDetailAsync(UserItem item) =>
        // Shell hands the object to the target ViewModel through its [QueryProperty].
        // Passing the same UserItem keeps the favorite star in sync between both pages.
        Shell.Current.GoToAsync(DetailRoute, new Dictionary<string, object> { ["Item"] = item });
}
