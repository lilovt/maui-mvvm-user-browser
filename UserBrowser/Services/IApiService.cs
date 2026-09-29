using UserBrowser.Models;

namespace UserBrowser.Services;

// Abstraction over the REST API. ViewModels depend on this interface,
// not on HttpClient, so unit tests can substitute a fake (Day 3).
public interface IApiService
{
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default);
}
