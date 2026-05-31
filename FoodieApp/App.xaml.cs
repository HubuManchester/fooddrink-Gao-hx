using FoodieApp.Services;

namespace FoodieApp;

public partial class App : Application
{
    private readonly ISettingsService _settings;

    public App(ISettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        ApplyTheme(_settings.IsDarkMode);
        ApplyFontSize(_settings.FontSizeIndex);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    /// <summary>Switches the app between light and dark theme at runtime.</summary>
    public void ApplyTheme(bool isDark)
    {
        if (Current != null)
            Current.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
    }

    /// <summary>
    /// Applies a global font size scale by updating named resource values
    /// that are referenced throughout the app's styles.
    /// 0 = Small (13), 1 = Medium (15, default), 2 = Large (18).
    /// </summary>
    public void ApplyFontSize(int index)
    {
        // Base body font size for each setting
        double bodySize = index switch
        {
            0 => 13.0,
            2 => 18.0,
            _ => 15.0   // Medium is the default
        };

        double captionSize  = bodySize - 2.0;
        double headingSize  = bodySize + 7.0;
        double subheadSize  = bodySize + 2.0;

        if (Current?.Resources == null) return;

        Current.Resources["GlobalBodyFontSize"]      = bodySize;
        Current.Resources["GlobalCaptionFontSize"]   = captionSize;
        Current.Resources["GlobalHeadingFontSize"]   = headingSize;
        Current.Resources["GlobalSubheadFontSize"]   = subheadSize;
    }
}
