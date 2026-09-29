using System.Collections.ObjectModel;

namespace UserBrowser.ViewModels;

// One lettered section of the list. A grouped CollectionView needs each group to be a
// collection that also exposes its header value (Key).
public class UserGroup : ObservableCollection<UserItem>
{
    public UserGroup(string key, IEnumerable<UserItem> items) : base(items) => Key = key;

    public string Key { get; }
}
