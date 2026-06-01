using FoodieApp.Models;

namespace FoodieApp.Services;

// ── Hardware Feature 1: Camera ────────────────────────────────────────────────

public interface ICameraService
{
    Task<Stream?> TakePhotoAsync();
    Task<Stream?> PickPhotoAsync();
}

/// <summary>Camera access via MAUI MediaPicker. Handles emulator gracefully.</summary>
public class CameraService : ICameraService
{
    public async Task<Stream?> TakePhotoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await Shell.Current.DisplayAlert("Camera", "Camera capture is not supported on this device.", "OK");
                return null;
            }
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted) return null;
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            return photo != null ? await photo.OpenReadAsync() : null;
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Camera", "Camera is not available on this device.", "OK");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CameraService: {ex.Message}");
            return null;
        }
    }

    public async Task<Stream?> PickPhotoAsync()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            return photo != null ? await photo.OpenReadAsync() : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CameraService.Pick: {ex.Message}");
            return null;
        }
    }
}

// ── Hardware Feature 2: Barcode Scanner ──────────────────────────────────────

public interface IBarcodeScannerService
{
    /// <summary>Scans a barcode using the device camera. Falls back to manual entry if camera unavailable.</summary>
    Task<BarcodeResult> ScanBarcodeAsync();

    /// <summary>Opens a text prompt for manual barcode entry (used as fallback).</summary>
    Task<BarcodeResult> ManualEntryAsync();
}

/// <summary>
/// Barcode scanner using the device camera (MediaPicker) and ZXing.Net for decoding.
/// On Android the photo is decoded via AndroidBarcodeDecoder which uses BitmapFactory
/// to correctly load the JPEG before passing pixels to ZXing.
/// Falls back to a text-input prompt when the camera is unavailable (emulator without camera).
/// </summary>
public class BarcodeScannerService : IBarcodeScannerService
{
    public async Task<BarcodeResult> ScanBarcodeAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert(
                    "Camera Permission",
                    "Camera access is required to scan barcodes. Please grant permission in Settings.",
                    "OK");
                return new BarcodeResult { IsSuccess = false, ErrorMessage = "Camera permission denied." };
            }

            if (!MediaPicker.Default.IsCaptureSupported)
                return await ManualEntryAsync();

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null)
                return new BarcodeResult { IsSuccess = false, ErrorMessage = "Scan cancelled." };

            string? code = null;
            using (var stream = await photo.OpenReadAsync())
            {
                code = await DecodeBarcodeAsync(stream);
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                bool retry = await Shell.Current.DisplayAlert(
                    "No Barcode Detected",
                    "Could not find a barcode in the photo.\nTip: hold the camera steady, ensure good lighting, and keep the barcode centred.\n\nWould you like to enter the barcode manually?",
                    "Enter Manually", "Cancel");

                return retry
                    ? await ManualEntryAsync()
                    : new BarcodeResult { IsSuccess = false, ErrorMessage = "No barcode detected." };
            }

            return new BarcodeResult { Value = code.Trim(), IsSuccess = true };
        }
        catch (FeatureNotSupportedException)
        {
            return await ManualEntryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BarcodeScannerService: {ex.Message}");
            return await ManualEntryAsync();
        }
    }

    public async Task<BarcodeResult> ManualEntryAsync()
    {
        var code = await Shell.Current.DisplayPromptAsync(
            "Enter Barcode",
            "Type or paste the product barcode number:",
            "Look Up",
            "Cancel",
            "e.g. 3017620422003",
            maxLength: 30,
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(code))
            return new BarcodeResult { IsSuccess = false, ErrorMessage = "Cancelled." };

        return new BarcodeResult { Value = code.Trim(), IsSuccess = true };
    }

    /// <summary>
    /// Dispatches barcode decoding to the platform-specific implementation.
    /// Android uses BitmapFactory for reliable JPEG loading before ZXing processing.
    /// </summary>
    private static Task<string?> DecodeBarcodeAsync(Stream imageStream)
    {
#if ANDROID
        return FoodieApp.Platforms.Android.AndroidBarcodeDecoder.DecodeAsync(imageStream);
#else
        return Task.FromResult<string?>(null);
#endif
    }
}

// ── Hardware Feature 3: GPS / Location ───────────────────────────────────────

public interface ILocationService
{
    Task<Location?> GetCurrentLocationAsync();
    Task<string> GetAddressAsync(double lat, double lng);
}

/// <summary>GPS location and reverse geocoding via MAUI Geolocation.</summary>
public class LocationService : ILocationService
{
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Location", "Location permission is required.", "OK");
                return null;
            }
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            return await Geolocation.Default.GetLocationAsync(request);
        }
        catch (FeatureNotEnabledException)
        {
            await Shell.Current.DisplayAlert("Location", "Please enable GPS on your device.", "OK");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocationService: {ex.Message}");
            return null;
        }
    }

    public async Task<string> GetAddressAsync(double lat, double lng)
    {
        if (IsEmulator(lat, lng))
            return $"{lat:F4}, {lng:F4}";

        try
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat:F6}&lon={lng:F6}&accept-language=zh,en";
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FoodieApp/1.0");
            client.Timeout = TimeSpan.FromSeconds(8);

            var json = await client.GetStringAsync(url);
            var root = System.Text.Json.JsonDocument.Parse(json).RootElement;

            if (root.TryGetProperty("address", out var addr))
            {
                var parts = new List<string>();
                foreach (var key in new[] { "road", "suburb", "city_district", "city", "town", "village", "state", "country" })
                {
                    if (addr.TryGetProperty(key, out var val))
                    {
                        var s = val.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) parts.Add(s);
                        if (key == "city" || key == "town" || key == "village") break;
                    }
                }
                if (parts.Count > 0)
                    return string.Join(", ", parts.Take(4));
            }

            if (root.TryGetProperty("display_name", out var display))
            {
                var name = display.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var segments = name.Split(',');
                    return string.Join(", ", segments.Take(3).Select(s => s.Trim()));
                }
            }
        }
        catch { }

        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(lat, lng);
            var p = placemarks?.FirstOrDefault();
            if (p != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(p.Thoroughfare))    parts.Add(p.Thoroughfare);
                if (!string.IsNullOrWhiteSpace(p.SubLocality))     parts.Add(p.SubLocality);
                if (!string.IsNullOrWhiteSpace(p.Locality))        parts.Add(p.Locality);
                if (!string.IsNullOrWhiteSpace(p.AdminArea))       parts.Add(p.AdminArea);
                if (parts.Count > 0)
                    return string.Join(", ", parts.Take(3));
            }
        }
        catch { }

        return $"{lat:F4}, {lng:F4}";
    }

    private static bool IsEmulator(double lat, double lng)
    {
#if ANDROID
        try
        {
            var fingerprint  = Android.OS.Build.Fingerprint ?? "";
            var model        = Android.OS.Build.Model        ?? "";
            var manufacturer = Android.OS.Build.Manufacturer ?? "";
            if (fingerprint.Contains("generic") || fingerprint.Contains("emulator") ||
                model.Contains("Emulator") || model.Contains("Android SDK") ||
                manufacturer.Contains("Genymotion"))
                return true;
        }
        catch { }
        if (lat == 0 && lng == 0) return true;
        if (Math.Abs(lat - 37.422) < 0.01 && Math.Abs(lng + 122.084) < 0.01) return true;
#endif
        return false;
    }
}

// ── Hardware Feature 4: Text-to-Speech ───────────────────────────────────────

public interface ITextToSpeechService
{
    Task SpeakAsync(string text, CancellationToken ct = default);
    void Stop();
    bool IsSpeaking { get; }
}

/// <summary>
/// Text-to-speech service.
/// Uses a volatile bool (_speaking) as a fast-path stop flag checked between sentences.
/// Stop() cancels the CTS and calls the Android platform TTS stop() directly.
/// NativeStop() is awaited on the main thread to ensure the current utterance is
/// interrupted before any new speech begins.
/// </summary>
public class TextToSpeechService : ITextToSpeechService
{
    private volatile bool            _speaking;
    private CancellationTokenSource? _currentCts;
    private readonly object          _lock = new();
    private Locale?                  _englishLocale;

    public bool IsSpeaking => _speaking;

    public async Task SpeakAsync(string text, CancellationToken externalCt = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        StopInternal();
        await NativeStopAsync();

        if (_englishLocale == null)
            _englishLocale = await GetEnglishLocaleAsync();

        CancellationTokenSource cts;
        lock (_lock)
        {
            cts         = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            _currentCts = cts;
            _speaking   = true;
        }

        try
        {
            var sentences = text
                .Split(new[] { ". ", ".\n", "! ", "? " },
                       StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            if (sentences.Count == 0) sentences.Add(text);

            foreach (var sentence in sentences)
            {
                if (!_speaking || cts.IsCancellationRequested) break;

                try
                {
                    var opts = new SpeechOptions { Volume = 1.0f, Pitch = 1.0f };
                    if (_englishLocale != null) opts.Locale = _englishLocale;

                    await TextToSpeech.Default.SpeakAsync(sentence, opts, cts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch { break; }

                if (!_speaking || cts.IsCancellationRequested) break;

                try { await Task.Delay(80, cts.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS SpeakAsync: {ex.Message}");
        }
        finally
        {
            lock (_lock)
            {
                _speaking = false;
                if (ReferenceEquals(_currentCts, cts))
                    _currentCts = null;
            }
            cts.Dispose();
        }
    }

    private static async Task<Locale?> GetEnglishLocaleAsync()
    {
        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            return locales?.FirstOrDefault(l =>
                l.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }

    public void Stop()
    {
        StopInternal();
        _ = NativeStopAsync();
    }

    private void StopInternal()
    {
        CancellationTokenSource? ctsToCancel;
        lock (_lock)
        {
            _speaking   = false;
            ctsToCancel = _currentCts;
            _currentCts = null;
        }
        ctsToCancel?.Cancel();
        ctsToCancel?.Dispose();
    }

    private static async Task NativeStopAsync()
    {
#if ANDROID
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                var ttsDefault = TextToSpeech.Default;
                var ttsType    = ttsDefault.GetType();

                foreach (var fname in new[] { "tts", "_tts", "textToSpeech", "_textToSpeech" })
                {
                    var f = ttsType.GetField(fname,
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                    if (f?.GetValue(ttsDefault) is Android.Speech.Tts.TextToSpeech native)
                    {
                        native.Stop();
                        return;
                    }
                }

                using var cts = new CancellationTokenSource();
                cts.Cancel();
                _ = TextToSpeech.Default.SpeakAsync(" ", null, cts.Token);
            }
            catch { }
        });
#else
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                using var cts = new CancellationTokenSource();
                cts.Cancel();
                _ = TextToSpeech.Default.SpeakAsync(" ", null, cts.Token);
            }
            catch { }
        });
#endif
    }
}

// ── Hardware Feature 5: Accelerometer / Shake ────────────────────────────────

public interface IShakeService
{
    event EventHandler? ShakeDetected;
    void Start();
    void Stop();
    bool IsMonitoring { get; }
}

/// <summary>Shake detection using the device accelerometer.</summary>
public class ShakeService : IShakeService
{
    public event EventHandler? ShakeDetected;
    public bool IsMonitoring => Accelerometer.Default.IsMonitoring;

    public void Start()
    {
        try
        {
            if (!Accelerometer.Default.IsSupported || Accelerometer.Default.IsMonitoring) return;
            Accelerometer.Default.ShakeDetected += OnShake;
            Accelerometer.Default.Start(SensorSpeed.Game);
        }
        catch (FeatureNotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine("Accelerometer not supported.");
        }
    }

    public void Stop()
    {
        try
        {
            if (!Accelerometer.Default.IsMonitoring) return;
            Accelerometer.Default.ShakeDetected -= OnShake;
            Accelerometer.Default.Stop();
        }
        catch { }
    }

    private void OnShake(object? s, EventArgs e) => ShakeDetected?.Invoke(this, EventArgs.Empty);
}

// ── Settings Service ──────────────────────────────────────────────────────────

public interface ISettingsService
{
    bool   IsDarkMode              { get; set; }
    bool   IsTextToSpeechEnabled   { get; set; }
    int    FontSizeIndex           { get; set; }
    string PreferredCuisine        { get; set; }
}

/// <summary>Persists user preferences using MAUI Preferences (key-value store).</summary>
public class SettingsService : ISettingsService
{
    public bool   IsDarkMode            { get => Preferences.Get("dark_mode", false);  set => Preferences.Set("dark_mode", value); }
    public bool   IsTextToSpeechEnabled { get => Preferences.Get("tts", true);         set => Preferences.Set("tts", value); }
    public int    FontSizeIndex         { get => Preferences.Get("font_size", 1);      set => Preferences.Set("font_size", value); }
    public string PreferredCuisine      { get => Preferences.Get("cuisine", "All");    set => Preferences.Set("cuisine", value); }
}

// ── Nutrition Service (Open Food Facts API) ───────────────────────────────────

public interface INutritionService
{
    Task<NutritionInfo?> GetNutritionByBarcodeAsync(string barcode);
}

/// <summary>
/// Queries Open Food Facts REST API.
/// Returns null when the product is not found (status != 1).
/// Returns null when the network is unavailable so the caller can show an error.
/// </summary>
public class NutritionService : INutritionService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(12) };

    private static readonly Dictionary<string, NutritionInfo> _localDb =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // Nongfu Spring Mineral Water 550ml
        ["6921168593569"] = new NutritionInfo
        {
            Barcode = "6921168593569", ProductName = "Nongfu Spring Mineral Water",
            Brand = "Nongfu Spring", ServingSize = "550ml",
            Calories = 0, ProteinGrams = 0, CarbohydratesGrams = 0,
            SugarsGrams = 0, FatGrams = 0, SaturatedFatGrams = 0,
            SaltGrams = 0, FibreGrams = 0, NutriScore = "A"
        },
        // Coca-Cola Classic 330ml can (China)
        ["6928804011388"] = new NutritionInfo
        {
            Barcode = "6928804011388", ProductName = "Coca-Cola Classic",
            Brand = "Coca-Cola", ServingSize = "330ml",
            Calories = 139, ProteinGrams = 0, CarbohydratesGrams = 35.6,
            SugarsGrams = 35.6, FatGrams = 0, SaturatedFatGrams = 0,
            SaltGrams = 0.03, FibreGrams = 0, NutriScore = "E"
        },
        // Lay's Classic Salted Potato Chips 75g
        ["6924743900086"] = new NutritionInfo
        {
            Barcode = "6924743900086", ProductName = "Lay's Classic Salted Potato Chips",
            Brand = "Lay's", ServingSize = "75g",
            Calories = 397, ProteinGrams = 5.0, CarbohydratesGrams = 54.0,
            SugarsGrams = 0.5, FatGrams = 18.0, SaturatedFatGrams = 1.8,
            SaltGrams = 0.7, FibreGrams = 2.5, NutriScore = "D"
        },
        // Sanquan Quick-frozen Bread Rolls 400g
        ["6925303730155"] = new NutritionInfo
        {
            Barcode = "6925303730155", ProductName = "Sanquan Soft Bread Rolls",
            Brand = "Sanquan", ServingSize = "100g",
            Calories = 278, ProteinGrams = 8.5, CarbohydratesGrams = 52.0,
            SugarsGrams = 8.0, FatGrams = 4.5, SaturatedFatGrams = 1.2,
            SaltGrams = 0.5, FibreGrams = 1.8, NutriScore = "C"
        },
        // Master Kong Braised Beef Instant Noodles
        ["6920202888139"] = new NutritionInfo
        {
            Barcode = "6920202888139", ProductName = "Master Kong Braised Beef Instant Noodles",
            Brand = "Master Kong", ServingSize = "104g",
            Calories = 476, ProteinGrams = 10.2, CarbohydratesGrams = 64.8,
            SugarsGrams = 2.1, FatGrams = 19.0, SaturatedFatGrams = 8.5,
            SaltGrams = 2.8, FibreGrams = 1.2, NutriScore = "D"
        },
        // Want Want Rice Crackers Original
        ["4710094018322"] = new NutritionInfo
        {
            Barcode = "4710094018322", ProductName = "Want Want Original Rice Crackers",
            Brand = "Want Want", ServingSize = "56g",
            Calories = 262, ProteinGrams = 3.8, CarbohydratesGrams = 42.0,
            SugarsGrams = 6.0, FatGrams = 9.0, SaturatedFatGrams = 4.0,
            SaltGrams = 0.6, FibreGrams = 0.5, NutriScore = "C"
        },
        // Mengniu Pure Milk 250ml
        ["6921733301119"] = new NutritionInfo
        {
            Barcode = "6921733301119", ProductName = "Mengniu Pure Fresh Milk",
            Brand = "Mengniu", ServingSize = "250ml",
            Calories = 162, ProteinGrams = 8.0, CarbohydratesGrams = 12.5,
            SugarsGrams = 12.5, FatGrams = 8.0, SaturatedFatGrams = 5.0,
            SaltGrams = 0.28, FibreGrams = 0, NutriScore = "B"
        },
        // Yili Mango Yogurt Drink 250ml
        ["6921028321107"] = new NutritionInfo
        {
            Barcode = "6921028321107", ProductName = "Yili Mango Yogurt Drink",
            Brand = "Yili", ServingSize = "250ml",
            Calories = 175, ProteinGrams = 5.0, CarbohydratesGrams = 30.0,
            SugarsGrams = 28.0, FatGrams = 3.5, SaturatedFatGrams = 2.2,
            SaltGrams = 0.15, FibreGrams = 0, NutriScore = "C"
        },
        // Orion Choco Pie Box 12pcs
        ["6925303730704"] = new NutritionInfo
        {
            Barcode = "6925303730704", ProductName = "Orion Choco Pie",
            Brand = "Orion", ServingSize = "30g (1 piece)",
            Calories = 126, ProteinGrams = 1.6, CarbohydratesGrams = 18.5,
            SugarsGrams = 12.0, FatGrams = 5.2, SaturatedFatGrams = 2.5,
            SaltGrams = 0.12, FibreGrams = 0.3, NutriScore = "D"
        },
        // Wahaha Nutritional Express Milk Drink Strawberry 500ml
        ["6920808001088"] = new NutritionInfo
        {
            Barcode = "6920808001088", ProductName = "Wahaha Nutritional Express Strawberry Milk Drink",
            Brand = "Wahaha", ServingSize = "500ml",
            Calories = 310, ProteinGrams = 10.0, CarbohydratesGrams = 60.0,
            SugarsGrams = 48.0, FatGrams = 3.5, SaturatedFatGrams = 2.0,
            SaltGrams = 0.4, FibreGrams = 0, NutriScore = "D"
        },
        // Original Flavor Waffle
        ["6942506205173"] = new NutritionInfo
        {
            Barcode = "6942506205173", ProductName = "Original Flavor Waffle",
            Brand = "Unknown", ServingSize = "100g",
            Calories = 412, ProteinGrams = 7.2, CarbohydratesGrams = 58.0,
            SugarsGrams = 18.0, FatGrams = 16.5, SaturatedFatGrams = 7.0,
            SaltGrams = 0.4, FibreGrams = 1.2, NutriScore = "D"
        },
        // Nongfu Spring Vitamin Water DFML-FV-2412 500ml
        ["6921168558049"] = new NutritionInfo
        {
            Barcode = "6921168558049", ProductName = "Nongfu Spring Vitamin Water",
            Brand = "Nongfu Spring", ServingSize = "500ml",
            Calories = 45, ProteinGrams = 0, CarbohydratesGrams = 11.0,
            SugarsGrams = 11.0, FatGrams = 0, SaturatedFatGrams = 0,
            SaltGrams = 0.05, FibreGrams = 0, NutriScore = "C"
        },
    };

    public async Task<NutritionInfo?> GetNutritionByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        if (_localDb.TryGetValue(barcode.Trim(), out var local))
            return local;

        try
        {
            var json = await _http.GetStringAsync(
                $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json");

            var root = Newtonsoft.Json.Linq.JObject.Parse(json);
            if (root == null) return null;

            var status = root["status"]?.ToObject<int>() ?? 0;
            if (status != 1) return null;

            var product    = root["product"] as Newtonsoft.Json.Linq.JObject;
            var nutriments = product?["nutriments"] as Newtonsoft.Json.Linq.JObject;

            if (product == null) return null;

            return new NutritionInfo
            {
                Barcode            = barcode,
                ProductName        = product["product_name"]?.ToString()  ?? "Unknown Product",
                Brand              = product["brands"]?.ToString()         ?? "Unknown Brand",
                ServingSize        = product["serving_size"]?.ToString()   ?? "100g",
                Calories           = GetCalories(nutriments),
                ProteinGrams       = GetNutrient(nutriments, "proteins",       "proteins_100g"),
                CarbohydratesGrams = GetNutrient(nutriments, "carbohydrates",  "carbohydrates_100g"),
                SugarsGrams        = GetNutrient(nutriments, "sugars",         "sugars_100g"),
                FatGrams           = GetNutrient(nutriments, "fat",            "fat_100g"),
                SaturatedFatGrams  = GetNutrient(nutriments, "saturated-fat",  "saturated-fat_100g"),
                SaltGrams          = GetNutrient(nutriments, "salt",           "salt_100g"),
                FibreGrams         = GetNutrient(nutriments, "fiber",          "fiber_100g", "fibers", "fibers_100g"),
                NutriScore         = (product["nutriscore_grade"]?.ToString() ?? "").ToUpper()
            };
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NutritionService error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Tries multiple field name variants used by Open Food Facts.
    /// The API is inconsistent: some products use _100g suffix, others don't,
    /// and some only have _serving values.
    /// </summary>
    private static double GetNutrient(
        Newtonsoft.Json.Linq.JObject? nutriments, params string[] keys)
    {
        if (nutriments == null) return 0;
        foreach (var key in keys)
        {
            foreach (var candidate in new[] { key, key + "_100g", key + "_serving" })
            {
                var token = nutriments[candidate];
                if (token != null)
                {
                    try
                    {
                        var val = token.ToObject<double>();
                        if (val != 0) return val;
                    }
                    catch { }
                }
            }
        }
        return 0;
    }

    /// <summary>
    /// Converts energy from kJ to kcal when the kcal field is missing.
    /// 1 kcal = 4.184 kJ.
    /// </summary>
    private static double GetCalories(Newtonsoft.Json.Linq.JObject? nutriments)
    {
        if (nutriments == null) return 0;

        var kcal = GetNutrient(nutriments, "energy-kcal", "energy-kcal_100g");
        if (kcal > 0) return kcal;

        var kj = GetNutrient(nutriments, "energy-kj", "energy-kj_100g", "energy_100g", "energy");
        if (kj > 0) return Math.Round(kj / 4.184, 1);

        return 0;
    }
}
