using FoodieApp.ViewModels;

namespace FoodieApp.Views;

public partial class RecipeListPage : ContentPage
{
    private readonly RecipeListViewModel _vm;

    public RecipeListPage(RecipeListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadRecipesCommand.ExecuteAsync(null);
    }
}
