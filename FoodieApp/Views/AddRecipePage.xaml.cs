using FoodieApp.ViewModels;

namespace FoodieApp.Views;

public partial class AddRecipePage : ContentPage
{
    public AddRecipePage(AddRecipeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
