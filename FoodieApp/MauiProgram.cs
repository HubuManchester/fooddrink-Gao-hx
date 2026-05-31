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

        // Services
        builder.Services.AddSingleton<IRecipeService,         RecipeService>();
        builder.Services.AddSingleton<ISettingsService,       SettingsService>();
        // Hardware services registered as stubs - real implementations added in v3
        builder.Services.AddSingleton<IShakeService,          ShakeService>();
        builder.Services.AddSingleton<IBarcodeScannerService, BarcodeScannerService>();
        builder.Services.AddSingleton<ILocationService,       LocationService>();
        builder.Services.AddSingleton<ITextToSpeechService,   TextToSpeechService>();
        builder.Services.AddSingleton<INutritionService,      NutritionService>();
        builder.Services.AddSingleton<ICameraService,         CameraService>();

        // ViewModels - only those needed for v1 pages
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Pages - only Home, Settings and Help for v1
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<HelpPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
