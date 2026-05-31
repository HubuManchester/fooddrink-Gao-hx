using FoodieApp.ViewModels;

namespace FoodieApp.Views;

public partial class MealPlannerPage : ContentPage
{
    private readonly MealPlannerViewModel _vm;

    public MealPlannerPage(MealPlannerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }
}
