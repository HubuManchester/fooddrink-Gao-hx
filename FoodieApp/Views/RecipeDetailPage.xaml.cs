using FoodieApp.ViewModels;

namespace FoodieApp.Views;

public partial class RecipeDetailPage : ContentPage
{
    private readonly RecipeDetailViewModel _vm;

    public RecipeDetailPage(RecipeDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshTtsEnabled();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.StopSpeakingCommand.Execute(null);
    }
}
