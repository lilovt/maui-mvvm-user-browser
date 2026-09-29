namespace UserBrowser;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Routes not declared in AppShell.xaml (pushed pages) are registered in code.
		Routing.RegisterRoute(Services.ShellNavigationService.DetailRoute, typeof(Views.UserDetailPage));
	}
}
