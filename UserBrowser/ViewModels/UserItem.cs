using CommunityToolkit.Mvvm.ComponentModel;
using UserBrowser.Models;

namespace UserBrowser.ViewModels;

// A User plus the display state the UI needs (initials, color, favorite).
// Kept UI-free: the color is a hex string, which XAML converts to a Color.
public partial class UserItem : ObservableObject
{
    // Muted colors chosen to keep white initials readable.
    private static readonly string[] Palette =
    {
        "#3D5A80", "#2A9D8F", "#8E5572", "#A05C6E",
        "#5E8C3A", "#B0661F", "#5F4B8B", "#457B9D"
    };

    private static readonly HashSet<string> Honorifics =
        new(StringComparer.OrdinalIgnoreCase) { "Mr.", "Mrs.", "Ms.", "Miss", "Dr.", "Prof." };

    private static readonly HashSet<string> Suffixes =
        new(StringComparer.OrdinalIgnoreCase) { "Jr", "Jr.", "Sr", "Sr.", "II", "III", "IV", "V" };

    public UserItem(User user, bool isFavorite = false)
    {
        User = user;
        IsFavorite = isFavorite;

        var words = MeaningfulWords(user.Name);
        SortName = words.Count > 0 ? string.Join(' ', words) : user.Name;
        Initials = InitialsFrom(words);
        GroupKey = SortName.Length > 0 ? char.ToUpperInvariant(SortName[0]).ToString() : "#";
        AvatarColor = Palette[((user.Id - 1) % Palette.Length + Palette.Length) % Palette.Length];
    }

    public User User { get; }

    public string SortName { get; }
    public string GroupKey { get; }
    public string Initials { get; }
    public string AvatarColor { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StarGlyph))]
    public partial bool IsFavorite { get; set; }

    public string StarGlyph => IsFavorite ? "★" : "☆";

    public bool Matches(string query) =>
        Contains(User.Name, query) || Contains(User.Email, query) ||
        Contains(User.CompanyName, query) || Contains(User.City, query);

    private static bool Contains(string? text, string query) =>
        text?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;

    // "Mrs. Dennis Schulist" -> ["Dennis", "Schulist"];  "Nicholas Runolfsdottir V" -> ["Nicholas", "Runolfsdottir"]
    private static List<string> MeaningfulWords(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => !Honorifics.Contains(w))
                        .ToList();
        if (words.Count > 2 && Suffixes.Contains(words[^1]))
            words.RemoveAt(words.Count - 1);
        return words;
    }

    private static string InitialsFrom(List<string> words) => words.Count switch
    {
        0 => "?",
        1 => words[0][..1].ToUpperInvariant(),
        _ => (words[0][..1] + words[^1][..1]).ToUpperInvariant()
    };
}
