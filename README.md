# UserBrowser: .NET MAUI + MVVM sample

A small .NET MAUI app that loads users from a public REST API (JSONPlaceholder), shows them in a
pull-to-refresh list, and opens a detail page on tap. Built to practice MVVM, data binding,
dependency injection, Shell navigation and unit testing.

## Stack
- .NET 10, .NET MAUI (built and run on the Windows target)
- CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`)
- xUnit for ViewModel tests

## Architecture
```
Views (XAML)  <-- binding -->  ViewModels  -->  Services (interfaces)  -->  REST API
                                    |
                                  Models
```
| Folder | Contents |
|---|---|
| `Models/` | `User`, `Company`, `Address` records |
| `Services/` | `IApiService`/`ApiService` (HttpClient), `INavigationService`/`ShellNavigationService` |
| `ViewModels/` | `UsersViewModel` (list, loading, error state), `UserDetailViewModel` |
| `Views/` | `UsersPage`, `UserDetailPage` (XAML, minimal code-behind) |

Key points:
- ViewModels depend on **interfaces** and get them by **constructor injection** (`MauiProgram.cs`).
- Navigation is behind `INavigationService`, so ViewModels contain no UI types and are testable.
- Compiled bindings (`x:DataType`) on every page.
- Loading, error and empty states are driven by ViewModel properties (`IsBusy`, `ErrorMessage`, `HasError`).

## Run
```
cd UserBrowser
dotnet run -f net10.0-windows10.0.19041.0
```
(Or open `UserBrowser.csproj` in Visual Studio, choose **Windows Machine**, press F5.)
Requires the .NET 10 SDK and the MAUI workload (`dotnet workload install maui`).

## Test
```
cd UserBrowser.Tests
dotnet test
```
9 tests cover loading, busy state, error handling, reload behavior, property-change
notification and navigation. They use hand-written fakes rather than a mocking library.

The test project links the UI-free source files instead of referencing the MAUI project
(which multi-targets Windows/Android/iOS). Moving them into a shared class library would be
the cleaner next step.

## Ideas for next steps
- Search/filter box, and a `Posts` list per user
- Retry with `Polly` / resilient `HttpClient`
- Android emulator run, plus `OnPlatform` layout tweaks
- CI build with GitHub Actions
