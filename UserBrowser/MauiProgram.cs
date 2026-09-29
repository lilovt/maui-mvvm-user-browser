using Microsoft.Extensions.Logging;
using UserBrowser.Services;
using UserBrowser.ViewModels;
using UserBrowser.Views;

namespace UserBrowser;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Dependency injection: register once here, receive via constructors.
		builder.Services.AddSingleton(new HttpClient
		{
			BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"),
			Timeout = TimeSpan.FromSeconds(15)
		});
		builder.Services.AddSingleton<IApiService, ApiService>();
		builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
		builder.Services.AddTransient<UsersViewModel>();
		builder.Services.AddTransient<UsersPage>();
		builder.Services.AddTransient<UserDetailViewModel>();
		builder.Services.AddTransient<UserDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
