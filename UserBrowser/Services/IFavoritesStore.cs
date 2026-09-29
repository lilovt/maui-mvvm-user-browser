namespace UserBrowser.Services;

// Where favorites are remembered between launches. An interface so tests use an in-memory fake.
public interface IFavoritesStore
{
    IReadOnlySet<int> Load();
    void Save(IEnumerable<int> userIds);
}
