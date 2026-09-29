namespace UserBrowser.Services;

// Stores favorite ids as "1,4,7" in MAUI Preferences (per-user app settings on every platform).
public class PreferencesFavoritesStore : IFavoritesStore
{
    private const string Key = "favorite-user-ids";

    public IReadOnlySet<int> Load() =>
        Preferences.Default.Get(Key, string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

    public void Save(IEnumerable<int> userIds) =>
        Preferences.Default.Set(Key, string.Join(',', userIds));
}
