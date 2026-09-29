using UserBrowser.Models;

namespace UserBrowser.Services;

// Navigation behind an interface: ViewModels never touch Shell directly,
// so they stay free of UI types and can be unit tested.
public interface INavigationService
{
    Task GoToUserDetailAsync(User user);
}
