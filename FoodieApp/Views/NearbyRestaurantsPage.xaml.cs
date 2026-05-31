using FoodieApp.ViewModels;

namespace FoodieApp.Views;

public partial class NearbyRestaurantsPage : ContentPage
{
    public NearbyRestaurantsPage(NearbyRestaurantsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
