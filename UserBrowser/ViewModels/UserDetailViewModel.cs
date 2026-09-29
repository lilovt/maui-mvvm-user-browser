using CommunityToolkit.Mvvm.ComponentModel;
using UserBrowser.Models;

namespace UserBrowser.ViewModels;

// Receives the selected User from Shell navigation.
// [QueryProperty] maps the "User" key in the GoToAsync dictionary to the User property.
[QueryProperty(nameof(User), "User")]
public partial class UserDetailViewModel : ObservableObject
{
    [ObservableProperty]
    public partial User? User { get; set; }
}
