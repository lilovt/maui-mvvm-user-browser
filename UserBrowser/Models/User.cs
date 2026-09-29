namespace UserBrowser.Models;

// MODEL: plain data shapes. No UI, no logic.
// Property names match the JSON from jsonplaceholder.typicode.com/users;
// System.Text.Json (via ReadFromJsonAsync) matches them case-insensitively.
public record User(
    int Id,
    string Name,
    string Username,
    string Email,
    string Phone,
    string Website,
    Company Company,
    Address Address)
{
    // Convenience properties for binding (keeps XAML simple).
    public string CompanyName => Company.Name;
    public string City => Address.City;
}

public record Company(string Name);

public record Address(string City);
