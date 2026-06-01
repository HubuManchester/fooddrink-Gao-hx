using CommunityToolkit.Maui;
using FoodieApp.Services;
using FoodieApp.ViewModels;
using FoodieApp.Views;
using Microsoft.Extensions.Logging;

namespace FoodieApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",  "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services - singleton lifetime so hardware listeners persist across pages
        builder.Services.AddSingleton<IRecipeService,         RecipeService>();
        builder.Services.AddSingleton<ISettingsService,       SettingsService>();
        builder.Services.AddSingleton<IShakeService,          ShakeService>();
        builder.Services.AddSingleton<IBarcodeScannerService, BarcodeScannerService>();
        builder.Services.AddSingleton<ILocationService,       LocationService>();
        builder.Services.AddSingleton<ITextToSpeechService,   TextToSpeechService>();
        builder.Services.AddSingleton<INutritionService,      NutritionService>();
        builder.Services.AddSingleton<ICameraService,         CameraService>();

        // ViewModels - transient so each navigation gets a fresh instance
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<RecipeListViewModel>();
        builder.Services.AddTransient<RecipeDetailViewModel>();
        builder.Services.AddTransient<BarcodeScannerViewModel>();
        builder.Services.AddTransient<MealPlannerViewModel>();
        builder.Services.AddTransient<NearbyRestaurantsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<AddRecipeViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<RecipeListPage>();
        builder.Services.AddTransient<RecipeDetailPage>();
        builder.Services.AddTransient<BarcodeScannerPage>();
        builder.Services.AddTransient<MealPlannerPage>();
        builder.Services.AddTransient<NearbyRestaurantsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AddRecipePage>();
        builder.Services.AddTransient<HelpPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
