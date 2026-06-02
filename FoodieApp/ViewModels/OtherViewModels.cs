using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodieApp.Models;
using FoodieApp.Services;
using FoodieApp.Views;

namespace FoodieApp.ViewModels;

// ── Barcode Scanner ───────────────────────────────────────────────────────────

/// <summary>ViewModel for the barcode scanner page. Supports both camera scan and manual entry.</summary>
public partial class BarcodeScannerViewModel : BaseViewModel
{
    private readonly IBarcodeScannerService _scanner;
    private readonly INutritionService _nutrition;

    [ObservableProperty] private NutritionInfo? _nutritionInfo;
    [ObservableProperty] private bool _hasResult;
    [ObservableProperty] private string _statusMessage = "Scan a product barcode to get nutrition info";
    [ObservableProperty] private ObservableCollection<NutritionInfo> _history = new();

    public BarcodeScannerViewModel(IBarcodeScannerService scanner, INutritionService nutrition)
    {
        _scanner = scanner;
        _nutrition = nutrition;
        Title = "Barcode Scanner";
    }

    /// <summary>Opens the device camera to capture a photo and decode the barcode from it.</summary>
    [RelayCommand]
    private async Task ScanWithCameraAsync()
    {
        await PerformScanAsync(useCamera: true);
    }

    /// <summary>Opens a text prompt for manual barcode entry.</summary>
    [RelayCommand]
    private async Task ScanManualAsync()
    {
        await PerformScanAsync(useCamera: false);
    }

    private async Task PerformScanAsync(bool useCamera)
    {
        await ExecuteSafelyAsync(async () =>
        {
            StatusMessage = useCamera ? "Opening camera..." : "Waiting for barcode input...";
            HasResult = false;

            BarcodeResult result = useCamera
                ? await _scanner.ScanBarcodeAsync()
                : await _scanner.ManualEntryAsync();

            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage ?? "Scan cancelled.";
                return;
            }

            StatusMessage = $"Looking up barcode {result.Value}...";

            NutritionInfo? info = null;
            try { info = await _nutrition.GetNutritionByBarcodeAsync(result.Value); }
            catch { }

            if (info == null)
            {
                StatusMessage = $"Product not found for barcode: {result.Value}";
                return;
            }

            NutritionInfo = info;
            HasResult = true;
            StatusMessage = "Product found!";
            History.Insert(0, NutritionInfo);
            if (History.Count > 20) History.RemoveAt(History.Count - 1);
        }, "Scan Error");
    }

    [RelayCommand]
    private void ClearResult()
    {
        NutritionInfo = null;
        HasResult = false;
        StatusMessage = "Scan a product barcode to get nutrition info";
    }

    [RelayCommand] private void ClearHistory() => History.Clear();

    [RelayCommand]
    private void SelectHistory(NutritionInfo item)
    {
        NutritionInfo = item;
        HasResult = true;
    }
}

// ── Meal Planner ──────────────────────────────────────────────────────────────

/// <summary>ViewModel for the weekly meal planner with per-day nutrition totals.</summary>
public partial class MealPlannerViewModel : BaseViewModel
{
    private readonly IRecipeService _recipes;

    private static readonly string[] DayNames =
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    private static readonly string[] MealTypes = { "Breakfast", "Lunch", "Dinner", "Snack" };

    [ObservableProperty] private ObservableCollection<MealPlanEntry> _weeklyPlan = new();
    [ObservableProperty] private ObservableCollection<MealPlanEntry> _dayEntries = new();
    [ObservableProperty] private int _selectedDayIndex;
    [ObservableProperty] private string _selectedDayName = "Monday";
    [ObservableProperty] private ObservableCollection<Recipe> _availableRecipes = new();
    [ObservableProperty] private double _dailyCalories;
    [ObservableProperty] private double _dailyProtein;
    [ObservableProperty] private double _dailyCarbs;
    [ObservableProperty] private double _dailyFat;

    public MealPlannerViewModel(IRecipeService recipes)
    {
        _recipes = recipes;
        Title = "Meal Planner";
        BuildEmptyPlan();
        UpdateDayView();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var all = await _recipes.GetAllRecipesAsync();
            AvailableRecipes.Clear();
            foreach (var r in all) AvailableRecipes.Add(r);
            RecalcNutrition();
        });
    }

    [RelayCommand]
    private async Task AssignRecipeAsync(MealPlanEntry entry)
    {
        if (AvailableRecipes.Count == 0)
        {
            await Shell.Current.DisplayAlert("No Recipes", "Add some recipes first.", "OK");
            return;
        }
        var names = AvailableRecipes.Select(r => r.Name).ToArray();
        var chosen = await Shell.Current.DisplayActionSheet(
            $"{entry.DayName} - {entry.MealType}", "Cancel", null, names);
        if (string.IsNullOrEmpty(chosen) || chosen == "Cancel") return;

        entry.Recipe = AvailableRecipes.First(r => r.Name == chosen);
        RefreshEntry(entry);
        RecalcNutrition();
    }

    [RelayCommand]
    private void RemoveEntry(MealPlanEntry entry)
    {
        entry.Recipe = null;
        RefreshEntry(entry);
        RecalcNutrition();
    }

    /// <summary>Called from XAML day-selector buttons; CommandParameter is the day index string.</summary>
    [RelayCommand]
    private void SelectDay(string dayIndexStr)
    {
        if (int.TryParse(dayIndexStr, out var idx))
        {
            SelectedDayIndex = idx;
            SelectedDayName = DayNames[idx];
        }
        UpdateDayView();
        RecalcNutrition();
    }

    private void BuildEmptyPlan()
    {
        for (var d = 0; d < 7; d++)
            foreach (var m in MealTypes)
                WeeklyPlan.Add(new MealPlanEntry { DayIndex = d, MealType = m });
    }

    /// <summary>Refreshes DayEntries to show only the currently selected day.</summary>
    private void UpdateDayView()
    {
        DayEntries.Clear();
        foreach (var e in WeeklyPlan.Where(e => e.DayIndex == SelectedDayIndex))
            DayEntries.Add(e);
    }

    private void RecalcNutrition()
    {
        var day = WeeklyPlan
            .Where(e => e.DayIndex == SelectedDayIndex && e.Recipe != null)
            .Select(e => e.Recipe!);
        DailyCalories = day.Sum(r => r.CaloriesPerServing);
        DailyProtein = day.Sum(r => r.ProteinGrams);
        DailyCarbs = day.Sum(r => r.CarbohydratesGrams);
        DailyFat = day.Sum(r => r.FatGrams);
    }

    private void RefreshEntry(MealPlanEntry entry)
    {
        var i = WeeklyPlan.IndexOf(entry);
        if (i >= 0) { WeeklyPlan.RemoveAt(i); WeeklyPlan.Insert(i, entry); }
        var j = DayEntries.IndexOf(entry);
        if (j >= 0) { DayEntries.RemoveAt(j); DayEntries.Insert(j, entry); }
    }
}

// ── Nearby Restaurants ────────────────────────────────────────────────────────

/// <summary>ViewModel for GPS-based nearby restaurant discovery.</summary>
public partial class NearbyRestaurantsViewModel : BaseViewModel
{
    private readonly ILocationService _location;

    [ObservableProperty] private string _currentAddress = "Tap 'Get My Location' to start";
    [ObservableProperty] private bool _hasLocation;
    [ObservableProperty] private ObservableCollection<RestaurantItem> _restaurants = new();

    public NearbyRestaurantsViewModel(ILocationService location)
    {
        _location = location;
        Title = "Nearby Restaurants";
    }

    [RelayCommand]
    private async Task GetLocationAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            CurrentAddress = "Getting your location...";
            var loc = await _location.GetCurrentLocationAsync();
            if (loc == null) { CurrentAddress = "Could not get location."; return; }

            HasLocation = true;
            CurrentAddress = await _location.GetAddressAsync(loc.Latitude, loc.Longitude);
            ShowSimulatedRestaurants(loc.Latitude, loc.Longitude);
        }, "Location Error");
    }

    [RelayCommand]
    private async Task OpenMapsAsync(RestaurantItem r)
    {
        try
        {
            await Map.Default.OpenAsync(
                new Location(r.Latitude, r.Longitude),
                new MapLaunchOptions { Name = r.Name });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Maps", ex.Message, "OK");
        }
    }

    private void ShowSimulatedRestaurants(double lat, double lng)
    {
        var rng = Random.Shared;
        var data = new (string Name, string Cuisine, string Emoji)[]
        {
            ("The Golden Wok",   "Chinese",  "🥟"), ("Pasta Bella",   "Italian",  "🍝"),
            ("Spice Garden",     "Indian",   "🍛"), ("Burger Barn",   "American", "🍔"),
            ("Sakura Sushi",     "Japanese", "🍣"), ("Le Petit Cafe", "French",   "☕"),
            ("Taco Fiesta",      "Mexican",  "🌮"), ("The Greenhouse", "Vegan",   "🥗"),
        };
        Restaurants.Clear();
        foreach (var (name, cuisine, emoji) in data)
            Restaurants.Add(new RestaurantItem
            {
                Name = name,
                Cuisine = cuisine,
                Emoji = emoji,
                Rating = Math.Round(3.5 + rng.NextDouble() * 1.5, 1),
                Distance = $"{(rng.NextDouble() * 1.8 + 0.1):F1} km",
                Latitude = lat + (rng.NextDouble() - 0.5) * 0.02,
                Longitude = lng + (rng.NextDouble() - 0.5) * 0.02
            });
    }
}

/// <summary>Display model for a nearby restaurant card.</summary>
public class RestaurantItem
{
    public string Name { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string Distance { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string RatingDisplay => $"{Rating:F1} stars";
}

// ── Settings ──────────────────────────────────────────────────────────────────

/// <summary>ViewModel for the settings page. Every toggle persists immediately via ISettingsService.</summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private bool _isTtsEnabled;
    [ObservableProperty] private int _fontSizeIndex;

    public List<string> FontSizeLabels { get; } = new() { "Small", "Medium", "Large" };

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        Title = "Settings";

        IsDarkMode = settings.IsDarkMode;
        IsTtsEnabled = settings.IsTextToSpeechEnabled;
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
        IsDarkMode = false; IsTtsEnabled = true;
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

// ── Add Recipe ────────────────────────────────────────────────────────────────

/// <summary>
/// ViewModel for the Add / Edit Recipe form.
/// When an EditRecipe query property is supplied the form pre-populates
/// with the existing recipe's data and SaveAsync overwrites it in place.
/// </summary>
[QueryProperty(nameof(EditRecipe), "EditRecipe")]
public partial class AddRecipeViewModel : BaseViewModel
{
    private readonly IRecipeService _recipes;
    private readonly ICameraService _camera;

    // When non-null this is an edit operation; the original Id is preserved on save
    private string? _editingId;

    // Up to 5 captured photo paths stored locally on the device
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddPhoto))]
    [NotifyPropertyChangedFor(nameof(PhotoCountText))]
    private ObservableCollection<string> _capturedPhotoPaths = new();

    /// <summary>True when fewer than 5 photos have been captured.</summary>
    public bool CanAddPhoto => CapturedPhotoPaths.Count < 5;

    /// <summary>Photo count label shown on the button.</summary>
    public string PhotoCountText => $"Add Photo ({CapturedPhotoPaths.Count}/5)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameValid), nameof(NameError))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionValid), nameof(DescriptionError))]
    private string _description = string.Empty;

    [ObservableProperty] private string _category = "Dinner";
    [ObservableProperty] private string _cuisine = "Other";
    [ObservableProperty] private string _difficulty = "Easy";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPrepValid), nameof(PrepTimeError))]
    private string _prepTimeText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCookValid), nameof(CookTimeError))]
    private string _cookTimeText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsServingsValid), nameof(ServingsError))]
    private string _servingsText = string.Empty;

    [ObservableProperty] private string _ingredientsText = string.Empty;
    [ObservableProperty] private string _stepsText = string.Empty;
    [ObservableProperty] private string _caloriesText = string.Empty;
    [ObservableProperty] private string _proteinText = string.Empty;
    [ObservableProperty] private string _carbsText = string.Empty;
    [ObservableProperty] private string _fatText = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _selectedEmoji = "🍽️";
    [ObservableProperty] private bool _isEditMode;

    // Populated via Shell QueryProperty when navigating from the edit button
    public Recipe? EditRecipe
    {
        set
        {
            if (value == null) return;
            _editingId = value.Id;
            IsEditMode = true;
            Title = "Edit Recipe";
            Name = value.Name;
            Description = value.Description;
            Category = value.Category;
            Cuisine = value.Cuisine;
            Difficulty = value.Difficulty;
            PrepTimeText = value.PrepTimeMinutes.ToString();
            CookTimeText = value.CookTimeMinutes.ToString();
            ServingsText = value.Servings.ToString();
            IngredientsText = string.Join("\n", value.Ingredients);
            StepsText = string.Join("\n", value.Steps);
            CaloriesText = value.CaloriesPerServing > 0 ? value.CaloriesPerServing.ToString() : "";
            ProteinText = value.ProteinGrams > 0 ? value.ProteinGrams.ToString("F1") : "";
            CarbsText = value.CarbohydratesGrams > 0 ? value.CarbohydratesGrams.ToString("F1") : "";
            FatText = value.FatGrams > 0 ? value.FatGrams.ToString("F1") : "";
            Notes = value.Notes;
            SelectedEmoji = value.EmojiThumbnail;

            // Populate photo thumbnails from existing recipe image URLs
            CapturedPhotoPaths.Clear();
            if (value.FoodImageUrls?.Count > 0)
            {
                foreach (var url in value.FoodImageUrls.Take(5))
                    CapturedPhotoPaths.Add(url);
            }
            OnPropertyChanged(nameof(CanAddPhoto));
            OnPropertyChanged(nameof(PhotoCountText));
        }
    }

    public bool IsNameValid => Name.Length >= 3;
    public bool IsDescriptionValid => !string.IsNullOrWhiteSpace(Description);
    public bool IsPrepValid => int.TryParse(PrepTimeText, out var v) && v >= 0;
    public bool IsCookValid => int.TryParse(CookTimeText, out var v) && v >= 0;
    public bool IsServingsValid => int.TryParse(ServingsText, out var v) && v >= 1;

    public string NameError => IsNameValid ? "" : "At least 3 characters required.";
    public string DescriptionError => IsDescriptionValid ? "" : "Description is required.";
    public string PrepTimeError => IsPrepValid ? "" : "Enter a valid number (minutes).";
    public string CookTimeError => IsCookValid ? "" : "Enter a valid number (minutes).";
    public string ServingsError => IsServingsValid ? "" : "Must be at least 1.";

    public List<string> Categories { get; } = new() { "Breakfast", "Lunch", "Dinner", "Snack", "Dessert" };
    public List<string> Cuisines { get; } = new() { "Italian", "Indian", "Japanese", "Mexican", "American", "French", "Chinese", "Other" };
    public List<string> Difficulties { get; } = new() { "Easy", "Medium", "Hard" };
    public List<string> EmojiOptions { get; } = new()
        { "🍽️","🍝","🍛","🍣","🥗","🥘","🍕","🍔","🌮","🍜","🥞","🍰","🍓","🥑","🥩","🐟" };

    public AddRecipeViewModel(IRecipeService recipes, ICameraService camera)
    {
        _recipes = recipes;
        _camera = camera;
        Title = "Add Recipe";
    }

    [RelayCommand]
    private void SelectEmoji(string emoji) { SelectedEmoji = emoji; }

    /// <summary>
    /// Opens the device camera to capture a photo and adds it to the local photo list.
    /// Limited to 5 photos per recipe.
    /// </summary>
    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        if (!CanAddPhoto)
        {
            await Shell.Current.DisplayAlert(
                "Photo Limit Reached",
                "You can add a maximum of 5 photos per recipe. Remove a photo first to add another.",
                "OK");
            return;
        }

        try
        {
            var stream = await _camera.TakePhotoAsync();
            if (stream == null) return;

            var fileName = $"recipe_photo_{Guid.NewGuid():N}.jpg";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            using (var fileStream = File.OpenWrite(filePath))
            {
                await stream.CopyToAsync(fileStream);
            }

            CapturedPhotoPaths.Add(filePath);
            OnPropertyChanged(nameof(CanAddPhoto));
            OnPropertyChanged(nameof(PhotoCountText));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Camera Error",
                $"Could not capture photo: {ex.Message}",
                "OK");
        }
    }

    /// <summary>Opens the device gallery to pick an existing photo.</summary>
    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        if (!CanAddPhoto)
        {
            await Shell.Current.DisplayAlert(
                "Photo Limit Reached",
                "You can add a maximum of 5 photos per recipe. Remove a photo first to add another.",
                "OK");
            return;
        }

        try
        {
            var stream = await _camera.PickPhotoAsync();
            if (stream == null) return;

            var fileName = $"recipe_photo_{Guid.NewGuid():N}.jpg";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            using (var fileStream = File.OpenWrite(filePath))
            {
                await stream.CopyToAsync(fileStream);
            }

            CapturedPhotoPaths.Add(filePath);
            OnPropertyChanged(nameof(CanAddPhoto));
            OnPropertyChanged(nameof(PhotoCountText));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Gallery Error",
                $"Could not pick photo: {ex.Message}",
                "OK");
        }
    }

    /// <summary>Removes a photo from the captured list by its file path.</summary>
    [RelayCommand]
    private void RemovePhoto(string filePath)
    {
        CapturedPhotoPaths.Remove(filePath);
        OnPropertyChanged(nameof(CanAddPhoto));
        OnPropertyChanged(nameof(PhotoCountText));
        try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
    }

    /// <summary>Shows a confirmation dialog before removing the photo.</summary>
    [RelayCommand]
    private async Task RemovePhotoWithConfirmationAsync(string filePath)
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Remove Photo",
            "Do you want to remove this photo?",
            "Remove",
            "Cancel");

        if (!confirmed) return;

        CapturedPhotoPaths.Remove(filePath);
        OnPropertyChanged(nameof(CanAddPhoto));
        OnPropertyChanged(nameof(PhotoCountText));
        try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!IsNameValid || !IsDescriptionValid || !IsPrepValid || !IsCookValid || !IsServingsValid)
        {
            var missing = new List<string>();
            if (!IsNameValid) missing.Add("• Recipe name must be at least 3 characters");
            if (!IsDescriptionValid) missing.Add("• Description cannot be empty");
            if (!IsPrepValid) missing.Add("• Prep time must be a valid number (minutes)");
            if (!IsCookValid) missing.Add("• Cook time must be a valid number (minutes)");
            if (!IsServingsValid) missing.Add("• Servings must be at least 1");

            await Shell.Current.DisplayAlert(
                "Please fill in the required fields",
                string.Join("\n", missing),
                "OK");
            return;
        }

        await ExecuteSafelyAsync(async () =>
        {
            var ingredients = IngredientsText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

            var steps = StepsText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

            if (ingredients.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "Ingredients missing",
                    "Please add at least one ingredient. Enter each ingredient on a separate line.",
                    "OK");
                return;
            }

            if (steps.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    "Cooking steps missing",
                    "Please add at least one cooking step. Enter each step on a separate line.",
                    "OK");
                return;
            }

            if (!string.IsNullOrWhiteSpace(CaloriesText) &&
                (!int.TryParse(CaloriesText, out var calCheck) || calCheck < 0))
            {
                await Shell.Current.DisplayAlert(
                    "Invalid calorie value",
                    "Calories must be a whole number, e.g. 450. Leave it blank if you don't know.",
                    "OK");
                return;
            }

            var existing = _editingId != null
                ? await _recipes.GetRecipeByIdAsync(_editingId)
                : null;

            // Build the final photo URL list from CapturedPhotoPaths.
            // CapturedPhotoPaths can hold three kinds of entries:
            //
            //   1. Raw local file path — e.g. /data/.../cache/recipe_photo_xxx.jpg
            //      Written by TakePhotoAsync / PickPhotoAsync in the current session.
            //      → Verify the file exists, then emit as "file:///path".
            //
            //   2. file:// URL — e.g. file:///data/.../recipe_photo_xxx.jpg
            //      Pre-populated from the saved recipe when entering edit mode
            //      (FoodImageUrls that were previously local photos).
            //      → Strip the "file://" prefix to get a bare path, verify the
            //        file exists, then re-emit as "file:///path".
            //
            //   3. Remote URL — e.g. https://images.unsplash.com/...
            //      Pre-populated from seed recipes or externally sourced images.
            //      These are not on disk, so File.Exists must NOT be called on them.
            //      → Keep the URL unchanged.
            //
            // Passing a URL string to File.Exists always returns false, which was
            // causing all remote-URL photos to be silently dropped on every save.
            // The intentional user deletion of a photo is correctly handled because
            // RemovePhotoWithConfirmationAsync already removed it from
            // CapturedPhotoPaths before SaveAsync runs, so the removed entry never
            // reaches this loop. The old fallback that restored existing.FoodImageUrls
            // when photoUrls was empty has been removed; if the user deleted all
            // photos the list should genuinely be empty.
            var photoUrls = new List<string>();
            foreach (var entry in CapturedPhotoPaths)
            {
                if (entry.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    entry.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    // Remote URL — keep as-is, no filesystem check needed.
                    photoUrls.Add(entry);
                }
                else if (entry.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    // Local file:// URL — extract the bare path and confirm the
                    // file still exists before including it.
                    var barePath = entry.Substring("file://".Length);
                    if (File.Exists(barePath))
                        photoUrls.Add(entry);
                }
                else
                {
                    // Raw local path written by TakePhotoAsync / PickPhotoAsync.
                    if (File.Exists(entry))
                        photoUrls.Add($"file://{entry}");
                }
            }

            var recipe = new Recipe
            {
                Id = existing?.Id ?? Guid.NewGuid().ToString(),
                Name = Name.Trim(),
                Description = Description.Trim(),
                Category = Category,
                Cuisine = Cuisine,
                Difficulty = Difficulty,
                PrepTimeMinutes = int.Parse(PrepTimeText),
                CookTimeMinutes = int.Parse(CookTimeText),
                Servings = int.Parse(ServingsText),
                Ingredients = ingredients,
                Steps = steps,
                EmojiThumbnail = SelectedEmoji,
                Notes = Notes.Trim(),
                CaloriesPerServing = int.TryParse(CaloriesText, out var cal) ? cal : 0,
                ProteinGrams = double.TryParse(ProteinText, out var pro) ? pro : 0,
                CarbohydratesGrams = double.TryParse(CarbsText, out var carb) ? carb : 0,
                FatGrams = double.TryParse(FatText, out var fat) ? fat : 0,
                FoodImageUrls = photoUrls,
                IsFavourite = existing?.IsFavourite ?? false,
                CardColor = existing?.CardColor ?? RandomColor(),
                CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow
            };

            await _recipes.SaveRecipeAsync(recipe);

            var msg = IsEditMode
                ? $"'{recipe.Name}' has been updated successfully."
                : $"'{recipe.Name}' has been added to your recipe collection!";
            await Shell.Current.DisplayAlert("Saved!", msg, "OK");
            await GoBackAsync();
        }, "Something went wrong");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (!string.IsNullOrWhiteSpace(Name) || CapturedPhotoPaths.Count > 0)
        {
            bool discard = await Shell.Current.DisplayAlert(
                "Discard Changes?",
                "You have unsaved changes. Are you sure you want to go back without saving?",
                "Discard",
                "Keep Editing");

            if (!discard) return;
        }
        await GoBackAsync();
    }

    private static string RandomColor()
    {
        var colors = new[] { "#E8B89A", "#A8D5A2", "#C78FC0", "#F4A460", "#87CEEB", "#DDA0DD" };
        return colors[Random.Shared.Next(colors.Length)];
    }
}