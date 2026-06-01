// Explicit aliases to resolve the ambiguity between
// Microsoft.Maui.Media.TextToSpeech and Android.Speech.Tts.TextToSpeech
using AndroidTts = Android.Speech.Tts.TextToSpeech;
using AndroidTtsResult = Android.Speech.Tts.OperationResult;
using Android.Content;

namespace FoodieApp.Platforms.Android;

/// <summary>
/// Holds a dedicated Android TTS engine instance used only for stop().
/// A separate engine is required because calling stop() on MAUI's shared
/// engine from the outside is not reliable.
/// </summary>
public class AndroidTtsStopService : Java.Lang.Object, AndroidTts.IOnInitListener
{
    private static AndroidTtsStopService? _instance;
    private AndroidTts? _engine;

    private AndroidTtsStopService() { }

    public static void Initialise()
    {
        if (_instance != null) return;
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                _instance = new AndroidTtsStopService();
                var ctx = global::Android.App.Application.Context;
                _instance._engine = new AndroidTts(ctx, _instance);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"AndroidTtsStopService init error: {ex.Message}");
            }
        });
    }

    public static void Stop()
    {
        try { _instance?._engine?.Stop(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"AndroidTtsStopService stop error: {ex.Message}");
        }
    }

    // IOnInitListener implementation
    void AndroidTts.IOnInitListener.OnInit(AndroidTtsResult status)
    {
        System.Diagnostics.Debug.WriteLine(
            $"AndroidTtsStopService engine ready: {status}");
    }
}
