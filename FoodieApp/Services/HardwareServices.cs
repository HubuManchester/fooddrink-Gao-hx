using FoodieApp.Models;

namespace FoodieApp.Services;

// ── Camera service (stub) ─────────────────────────────────────────────────────

public interface ICameraService
{
    Task<Stream?> TakePhotoAsync();
    Task<Stream?> PickPhotoAsync();
}

/// <summary>Placeholder camera service. Camera features will be added in v3.</summary>
public class CameraService : ICameraService
{
    public Task<Stream?> TakePhotoAsync()
    {
        Shell.Current.DisplayAlert("Coming Soon", "Camera support will be added in a future update.", "OK");
        return Task.FromResult<Stream?>(null);
    }

    public Task<Stream?> PickPhotoAsync()
    {
        Shell.Current.DisplayAlert("Coming Soon", "Photo picking will be added in a future update.", "OK");
        return Task.FromResult<Stream?>(null);
    }
}

// ── Barcode scanner (stub) ────────────────────────────────────────────────────

public interface IBarcodeScannerService
{
    Task<BarcodeResult> ScanBarcodeAsync();
    Task<BarcodeResult> ManualEntryAsync();
}

/// <summary>Placeholder barcode service. Barcode scanning will be added in v3.</summary>
public class BarcodeScannerService : IBarcodeScannerService
{
    public Task<BarcodeResult> ScanBarcodeAsync()
        => Task.FromResult(new BarcodeResult { IsSuccess = false, ErrorMessage = "Scanner not yet implemented." });

    public Task<BarcodeResult> ManualEntryAsync()
        => Task.FromResult(new BarcodeResult { IsSuccess = false, ErrorMessage = "Scanner not yet implemented." });
}

// ── Location service (stub) ───────────────────────────────────────────────────

public interface ILocationService
{
    Task<Location?> GetCurrentLocationAsync();
    Task<string> GetAddressAsync(double lat, double lng);
}

/// <summary>Placeholder location service. GPS will be added in v3.</summary>
public class LocationService : ILocationService
{
    public Task<Location?> GetCurrentLocationAsync()
        => Task.FromResult<Location?>(null);

    public Task<string> GetAddressAsync(double lat, double lng)
        => Task.FromResult($"{lat:F4}, {lng:F4}");
}

// ── Text-to-speech (stub) ─────────────────────────────────────────────────────

public interface ITextToSpeechService
{
    Task SpeakAsync(string text, CancellationToken ct = default);
    void Stop();
    bool IsSpeaking { get; }
}

/// <summary>Placeholder TTS service. Text-to-speech will be added in v3.</summary>
public class TextToSpeechService : ITextToSpeechService
{
    public bool IsSpeaking => false;
    public Task SpeakAsync(string text, CancellationToken ct = default) => Task.CompletedTask;
    public void Stop() { }
}

// ── Shake / accelerometer (stub) ──────────────────────────────────────────────

public interface IShakeService
{
    event EventHandler? ShakeDetected;
    void Start();
    void Stop();
    bool IsMonitoring { get; }
}

/// <summary>Placeholder shake service. Accelerometer shake will be added in v3.</summary>
public class ShakeService : IShakeService
{
    public event EventHandler? ShakeDetected;
    public bool IsMonitoring => false;
    public void Start() { }
    public void Stop() { }
}

// ── Settings service ──────────────────────────────────────────────────────────

public interface ISettingsService
{
    bool   IsDarkMode            { get; set; }
    bool   IsTextToSpeechEnabled { get; set; }
    int    FontSizeIndex         { get; set; }
    string PreferredCuisine      { get; set; }
}

/// <summary>Persists user preferences using MAUI Preferences (key-value store).</summary>
public class SettingsService : ISettingsService
{
    public bool   IsDarkMode            { get => Preferences.Get("dark_mode", false); set => Preferences.Set("dark_mode", value); }
    public bool   IsTextToSpeechEnabled { get => Preferences.Get("tts", true);        set => Preferences.Set("tts", value); }
    public int    FontSizeIndex         { get => Preferences.Get("font_size", 1);     set => Preferences.Set("font_size", value); }
    public string PreferredCuisine      { get => Preferences.Get("cuisine", "All");   set => Preferences.Set("cuisine", value); }
}

// ── Nutrition service (stub) ──────────────────────────────────────────────────

public interface INutritionService
{
    Task<NutritionInfo?> GetNutritionByBarcodeAsync(string barcode);
}

/// <summary>Placeholder nutrition service. Barcode lookup will be added in v3.</summary>
public class NutritionService : INutritionService
{
    public Task<NutritionInfo?> GetNutritionByBarcodeAsync(string barcode)
        => Task.FromResult<NutritionInfo?>(null);
}
