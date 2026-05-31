using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodieApp.Models;
using FoodieApp.Services;
using FoodieApp.Views;

namespace FoodieApp.ViewModels;

// ── Main / Home ───────────────────────────────────────────────────────────────

/// <summary>ViewModel for the home dashboard.</summary>
public partial class MainViewModel : BaseViewModel
{
    private readonly IRecipeService _recipes;
    private readonly IShakeService  _shake;

    [ObservableProperty] private ObservableCollection<Recipe> _featuredRecipes = new();
    [ObservableProperty] private string _greetingMessage = string.Empty;

    public MainViewModel(IRecipeService recipes, IShakeService shake)
    {
        _recipes = recipes;
        _shake   = shake;
        Title    = "FoodieApp";
        SetGreeting();
        // Shake listener registered here; hardware implementation comes in v3
        _shake.ShakeDetected += OnShakeDetected;
        _shake.Start();
    }

    [RelayCommand]
    private async Task LoadFeaturedRecipesAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var all = await _recipes.GetAllRecipesAsync();
            FeaturedRecipes.Clear();
            foreach (var r in all.OrderBy(_ => Random.Shared.Next()).Take(5))
                FeaturedRecipes.Add(r);
        });
    }

    // Navigation to recipe detail will be wired up in v2
    [RelayCommand]
    private Task NavigateToRecipeDetailAsync(Recipe recipe) => Task.CompletedTask;

    [RelayCommand]
    private async Task PickRandomRecipeAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var all = await _recipes.GetAllRecipesAsync();
            if (all.Count == 0) return;
            var pick = all[Random.Shared.Next(all.Count)];
            await Shell.Current.DisplayAlert("Random Recipe", $"How about: {pick.Name}?", "OK");
        });
    }

    // Nearby restaurants navigation will be added in v2
    [RelayCommand]
    private Task NavigateToNearbyAsync() => Task.CompletedTask;

    private void SetGreeting()
    {
        var h = DateTime.Now.Hour;
        GreetingMessage = h switch
        {
            >= 5  and < 12 => "Good Morning!",
            >= 12 and < 17 => "Good Afternoon!",
            >= 17 and < 21 => "Good Evening!",
            _              => "Good Night!"
        };
    }

    private async void OnShakeDetected(object? sender, EventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(PickRandomRecipeAsync);
    }

    public void Cleanup() => _shake.Stop();
}

// ── Settings ──────────────────────────────────────────────────────────────────

/// <summary>ViewModel for the settings page. Every toggle persists immediately via ISettingsService.</summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private bool _isTtsEnabled;
    [ObservableProperty] private int  _fontSizeIndex;

    public List<string> FontSizeLabels { get; } = new() { "Small", "Medium", "Large" };

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        Title     = "Settings";

        IsDarkMode    = settings.IsDarkMode;
        IsTtsEnabled  = settings.IsTextToSpeechEnabled;
        FontSizeIndex = settings.FontSizeIndex;
    }

    [RelayCommand]
    private async Task OpenHelpAsync() => await GoToAsync(nameof(HelpPage));

    [RelayCommand]
    private async Task ResetAsync()
    {
        bool ok = await Shell.Current.DisplayAlert(
            "Reset Settings", "Reset all settings to defaults?", "Reset", "Cancel");
        if (!ok) return;
        IsDarkMode    = false;
        IsTtsEnabled  = true;
        FontSizeIndex = 1;
        await Shell.Current.DisplayAlert("Done", "Settings have been reset.", "OK");
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        _settings.IsDarkMode = value;
        if (Application.Current is App app) app.ApplyTheme(value);
    }

    partial void OnIsTtsEnabledChanged(bool value) => _settings.IsTextToSpeechEnabled = value;

    partial void OnFontSizeIndexChanged(int value)
    {
        _settings.FontSizeIndex = value;
        if (Application.Current is App app) app.ApplyFontSize(value);
    }
}
