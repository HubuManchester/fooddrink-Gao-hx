using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodieApp.Models;
using FoodieApp.Services;
using FoodieApp.Views;

namespace FoodieApp.ViewModels;

// ── Main / Home ───────────────────────────────────────────────────────────────

/// <summary>ViewModel for the home dashboard. Shake-to-random-recipe uses the accelerometer.</summary>
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

    [RelayCommand]
    private async Task NavigateToRecipeDetailAsync(Recipe recipe)
    {
        await GoToAsync(nameof(RecipeDetailPage),
            new Dictionary<string, object> { { "Recipe", recipe } });
    }

    [RelayCommand]
    private async Task PickRandomRecipeAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var all = await _recipes.GetAllRecipesAsync();
            if (all.Count == 0) return;
            var pick = all[Random.Shared.Next(all.Count)];
            bool go = await Shell.Current.DisplayAlert(
                "Random Recipe", $"How about: {pick.Name}?", "Cook it!", "Try again");
            if (go) await NavigateToRecipeDetailAsync(pick);
        });
    }

    [RelayCommand]
    private async Task NavigateToNearbyAsync() =>
        await GoToAsync(nameof(NearbyRestaurantsPage));

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

// ── Recipe List ───────────────────────────────────────────────────────────────

/// <summary>ViewModel for the recipe browse/search page with category filtering and favourites.</summary>
public partial class RecipeListViewModel : BaseViewModel
{
    private readonly IRecipeService _recipes;

    [ObservableProperty] private ObservableCollection<Recipe> _allRecipes      = new();
    [ObservableProperty] private ObservableCollection<Recipe> _filteredRecipes = new();
    [ObservableProperty] private string _searchQuery        = string.Empty;
    [ObservableProperty] private bool   _showFavouritesOnly;
    [ObservableProperty] private string _selectedCategory   = "All";

    public List<string> Categories { get; } =
        new() { "All", "Breakfast", "Lunch", "Dinner", "Snack", "Dessert" };

    public RecipeListViewModel(IRecipeService recipes)
    {
        _recipes = recipes;
        Title    = "Recipes";
    }

    [RelayCommand]
    private async Task LoadRecipesAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var all = await _recipes.GetAllRecipesAsync();
            AllRecipes.Clear();
            foreach (var r in all) AllRecipes.Add(r);
            ApplyFilter();
        });
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadRecipesAsync();
            return;
        }
        await ExecuteSafelyAsync(async () =>
        {
            var results = await _recipes.SearchRecipesAsync(SearchQuery);
            AllRecipes.Clear();
            foreach (var r in results) AllRecipes.Add(r);
            ApplyFilter();

            if (results.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "No Results",
                    $"No recipes found for \"{SearchQuery}\". Try a different name, category, or cuisine.",
                    "OK");
            }
        });
    }

    [RelayCommand]
    private void FilterByCategory(string category)
    {
        SelectedCategory = category;
        ApplyFilter();
    }

    [RelayCommand]
    private void ToggleFavouritesFilter()
    {
        ShowFavouritesOnly = !ShowFavouritesOnly;
        ApplyFilter();
    }

    [RelayCommand]
    private async Task OpenRecipeAsync(Recipe recipe)
    {
        await GoToAsync(nameof(RecipeDetailPage),
            new Dictionary<string, object> { { "Recipe", recipe } });
    }

    [RelayCommand]
    private async Task AddRecipeAsync() =>
        await GoToAsync(nameof(AddRecipePage));

    /// <summary>Navigates to the edit page pre-populated with the given recipe's data.</summary>
    [RelayCommand]
    private async Task EditRecipeAsync(Recipe recipe)
    {
        await GoToAsync(nameof(AddRecipePage),
            new Dictionary<string, object> { { "EditRecipe", recipe } });
    }

    /// <summary>Shows a confirmation dialog before permanently deleting the recipe.</summary>
    [RelayCommand]
    private async Task DeleteRecipeAsync(Recipe recipe)
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Delete Recipe",
            $"Are you sure you want to delete '{recipe.Name}'?\nThis cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        await _recipes.DeleteRecipeAsync(recipe.Id);
        await LoadRecipesAsync();
    }

    /// <summary>Toggles the favourite flag and refreshes the list.</summary>
    [RelayCommand]
    private async Task ToggleFavouriteAsync(Recipe recipe)
    {
        await _recipes.ToggleFavouriteAsync(recipe.Id);
        var cached = AllRecipes.FirstOrDefault(r => r.Id == recipe.Id);
        if (cached != null) cached.IsFavourite = !cached.IsFavourite;
        await LoadRecipesAsync();
    }

    private void ApplyFilter()
    {
        var src = AllRecipes.AsEnumerable();
        if (ShowFavouritesOnly) src = src.Where(r => r.IsFavourite);
        if (SelectedCategory != "All") src = src.Where(r => r.Category == SelectedCategory);
        FilteredRecipes.Clear();
        foreach (var r in src) FilteredRecipes.Add(r);
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            _ = LoadRecipesAsync();
    }
}

// ── Recipe Detail ─────────────────────────────────────────────────────────────

/// <summary>
/// ViewModel for recipe detail with step-by-step cooking mode and TTS.
/// Uses two independent IsSpeaking flags so the "Read Aloud" button at the
/// top (reads all steps) and the one inside Cooking Mode (reads current step)
/// never affect each other's icon state.
/// When Stop is called the IsReading flags reset synchronously so the UI
/// button text updates immediately without waiting for SpeakAsync to finish.
/// </summary>
[QueryProperty(nameof(Recipe), "Recipe")]
public partial class RecipeDetailViewModel : BaseViewModel
{
    private readonly ITextToSpeechService _tts;
    private readonly IRecipeService       _recipes;
    private readonly ISettingsService     _settings;

    [ObservableProperty] private Recipe? _recipe;
    [ObservableProperty] private int     _currentStepIndex;
    [ObservableProperty] private bool    _isCookingMode;

    // Separate speaking flags so each Read Aloud button has independent state
    [ObservableProperty] private bool _isReadingIngredients;
    [ObservableProperty] private bool _isReadingAll;
    [ObservableProperty] private bool _isReadingStep;

    [ObservableProperty] private bool    _isFavourite;
    [ObservableProperty] private int     _servingMultiplier = 1;

    // Full-screen photo overlay
    [ObservableProperty] private string  _selectedPhotoUrl = string.Empty;
    [ObservableProperty] private bool    _isPhotoFullScreen;

    public bool   IsTtsEnabled  => _settings.IsTextToSpeechEnabled;

    public void RefreshTtsEnabled() => OnPropertyChanged(nameof(IsTtsEnabled));

    public bool   HasFoodImages => Recipe?.FoodImageUrls?.Count > 0;
    public bool   CanGoPrev    => CurrentStepIndex > 0;
    public bool   CanGoNext    => Recipe != null && CurrentStepIndex < Recipe.Steps.Count - 1;
    public string CurrentStep  => Recipe?.Steps.ElementAtOrDefault(CurrentStepIndex) ?? string.Empty;
    public string StepProgress => Recipe != null
        ? $"Step {CurrentStepIndex + 1} of {Recipe.Steps.Count}" : string.Empty;

    public RecipeDetailViewModel(
        ITextToSpeechService tts, IRecipeService recipes, ISettingsService settings)
    {
        _tts      = tts;
        _recipes  = recipes;
        _settings = settings;
        Title     = "Recipe";
    }

    partial void OnRecipeChanged(Recipe? value)
    {
        if (value == null) return;
        Title       = value.Name;
        IsFavourite = value.IsFavourite;
        OnPropertyChanged(nameof(HasFoodImages));
    }

    // ── Read Ingredients aloud ────────────────────────────────────────────────

    /// <summary>
    /// Reads the ingredients list aloud.
    /// Tapping while reading immediately stops speech and resets the button state.
    /// </summary>
    [RelayCommand]
    private async Task ReadIngredientsAsync()
    {
        if (Recipe == null || !_settings.IsTextToSpeechEnabled) return;

        if (IsReadingIngredients) { IsReadingIngredients = false; _tts.Stop(); }

        if (IsReadingAll)  { IsReadingAll  = false; _tts.Stop(); }
        if (IsReadingStep) { IsReadingStep = false; _tts.Stop(); }

        IsReadingIngredients = true;
        try
        {
            var text = $"Ingredients for {Recipe.Name}. " +
                       string.Join(". ", Recipe.Ingredients);
            await _tts.SpeakAsync(text);
        }
        finally
        {
            IsReadingIngredients = false;
        }
    }

    // ── Read all Instructions aloud ───────────────────────────────────────────

    /// <summary>
    /// Reads every cooking step aloud from beginning to end.
    /// Tapping while reading immediately stops speech and resets the button state.
    /// </summary>
    [RelayCommand]
    private async Task ReadAllAsync()
    {
        if (Recipe == null || !_settings.IsTextToSpeechEnabled) return;

        if (IsReadingAll) { IsReadingAll = false; _tts.Stop(); }

        if (IsReadingIngredients) { IsReadingIngredients = false; _tts.Stop(); }
        if (IsReadingStep)        { IsReadingStep        = false; _tts.Stop(); }

        IsReadingAll = true;
        try
        {
            var fullText = $"Instructions for {Recipe.Name}. " +
                           string.Join(". Next step: ", Recipe.Steps);
            await _tts.SpeakAsync(fullText);
        }
        finally
        {
            IsReadingAll = false;
        }
    }

    // ── Read current cooking step aloud ──────────────────────────────────────

    /// <summary>
    /// Reads only the currently displayed cooking-mode step aloud.
    /// Tapping while reading immediately stops speech and resets the button state.
    /// </summary>
    [RelayCommand]
    private async Task ReadAloudAsync()
    {
        if (!_settings.IsTextToSpeechEnabled) return;

        if (IsReadingStep) { IsReadingStep = false; _tts.Stop(); }

        if (IsReadingIngredients) { IsReadingIngredients = false; _tts.Stop(); }
        if (IsReadingAll)         { IsReadingAll         = false; _tts.Stop(); }

        IsReadingStep = true;
        try
        {
            await _tts.SpeakAsync(CurrentStep);
        }
        finally
        {
            IsReadingStep = false;
        }
    }

    /// <summary>Stops all active speech from any external trigger.</summary>
    [RelayCommand]
    private void StopSpeaking()
    {
        IsReadingIngredients = false;
        IsReadingAll         = false;
        IsReadingStep        = false;
        _tts.Stop();
    }

    [RelayCommand]
    private void NextStep()
    {
        if (!CanGoNext) return;
        CurrentStepIndex++;
        NotifyStep();
    }

    [RelayCommand]
    private void PrevStep()
    {
        if (!CanGoPrev) return;
        CurrentStepIndex--;
        NotifyStep();
    }

    [RelayCommand]
    private void ToggleCookingMode()
    {
        IsCookingMode    = !IsCookingMode;
        CurrentStepIndex = 0;
        NotifyStep();
    }

    /// <summary>
    /// Toggles the favourite flag both in the service and the local IsFavourite property
    /// so the toolbar icon updates without needing a full reload.
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavouriteAsync()
    {
        if (Recipe == null) return;
        await _recipes.ToggleFavouriteAsync(Recipe.Id);
        IsFavourite        = !IsFavourite;
        Recipe.IsFavourite = IsFavourite;
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (Recipe == null) return;
        var text = $"{Recipe.Name}\n\nIngredients:\n" +
            string.Join("\n", Recipe.Ingredients.Select(i => $"- {i}")) +
            "\n\nSteps:\n" +
            string.Join("\n", Recipe.Steps.Select((s, i) => $"{i + 1}. {s}")) +
            "\n\nShared from FoodieApp";
        await Share.Default.RequestAsync(new ShareTextRequest { Text = text, Title = Recipe.Name });
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (Recipe == null) return;
        await GoToAsync(nameof(AddRecipePage),
            new Dictionary<string, object> { { "EditRecipe", Recipe } });
    }

    /// <summary>Opens the full-screen photo overlay for the tapped image URL.</summary>
    [RelayCommand]
    private void ViewPhoto(string url)
    {
        SelectedPhotoUrl  = url;
        IsPhotoFullScreen = true;
    }

    /// <summary>Closes the full-screen photo overlay.</summary>
    [RelayCommand]
    private void ClosePhoto()
    {
        IsPhotoFullScreen = false;
        SelectedPhotoUrl  = string.Empty;
    }

    [RelayCommand]
    private void IncrementServings()
    {
        if (ServingMultiplier < 10) ServingMultiplier++;
    }

    [RelayCommand]
    private void DecrementServings()
    {
        if (ServingMultiplier > 1) ServingMultiplier--;
    }

    private void NotifyStep()
    {
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(StepProgress));
    }
}
