using FoodieApp.ViewModels;

namespace FoodieApp.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadFeaturedRecipesCommand.ExecuteAsync(null);
    }
}
