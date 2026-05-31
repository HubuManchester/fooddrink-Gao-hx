using FoodieApp.Views;

namespace FoodieApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register push-navigation routes not in the tab bar
        Routing.RegisterRoute(nameof(RecipeDetailPage),      typeof(RecipeDetailPage));
        Routing.RegisterRoute(nameof(AddRecipePage),         typeof(AddRecipePage));
        Routing.RegisterRoute(nameof(NearbyRestaurantsPage), typeof(NearbyRestaurantsPage));
        Routing.RegisterRoute(nameof(HelpPage),              typeof(HelpPage));
    }
}
