using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace UserBrowser.ViewModels;

// Receives the selected UserItem from Shell navigation.
// [QueryProperty] maps the "Item" key in the GoToAsync dictionary to the Item property.
[QueryProperty(nameof(Item), "Item")]
public partial class UserDetailViewModel : ObservableObject
{
    [ObservableProperty]
    public partial UserItem? Item { get; set; }

    // Short confirmation shown after copying a value ("Copied ...").
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    public partial string? Message { get; set; }

    public bool HasMessage => !string.IsNullOrEmpty(Message);

    [RelayCommand]
    private Task GoBackAsync() => Shell.Current.GoToAsync("..");

    // The list page listens for IsFavorite changes on the same object, so this is all it takes.
    [RelayCommand]
    private void ToggleFavorite()
    {
        if (Item is not null) Item.IsFavorite = !Item.IsFavorite;
    }

    [RelayCommand]
    private async Task CopyAsync(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        await Clipboard.Default.SetTextAsync(value);
        await ShowMessageAsync($"Copied {value}");
    }

    [RelayCommand]
    private async Task EmailAsync()
    {
        if (Item is null) return;
        try { await Email.Default.ComposeAsync(new EmailMessage { To = new List<string> { Item.User.Email } }); }
        catch (FeatureNotSupportedException) { await ShowMessageAsync("No email app is set up on this device."); }
    }

    [RelayCommand]
    private async Task CallAsync()
    {
        if (Item is null) return;
        try { PhoneDialer.Default.Open(Item.User.Phone); }
        catch (Exception ex) when (ex is FeatureNotSupportedException or ArgumentException)
        { await ShowMessageAsync("Calling isn't available on this device."); }
    }

    [RelayCommand]
    private async Task OpenWebsiteAsync()
    {
        if (Item is null) return;
        await Launcher.Default.OpenAsync($"https://{Item.User.Website}");
    }

    private async Task ShowMessageAsync(string text)
    {
        Message = text;
        await Task.Delay(2000);
        if (Message == text) Message = null;   // don't clear a newer message
    }
}
