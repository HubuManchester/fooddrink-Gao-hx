using FoodieApp.ViewModels;

namespace FoodieApp.Views;

public partial class BarcodeScannerPage : ContentPage
{
    public BarcodeScannerPage(BarcodeScannerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
