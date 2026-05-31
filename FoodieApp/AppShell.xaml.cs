using FoodieApp.Views;

namespace FoodieApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Routes for pages not in the tab bar will be registered in later versions
        Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));
    }
}
