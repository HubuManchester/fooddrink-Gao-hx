using CommunityToolkit.Mvvm.ComponentModel;

namespace FoodieApp.ViewModels;

/// <summary>Base ViewModel providing busy state, navigation helpers and safe execution.</summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    public bool IsNotBusy => !IsBusy;

    protected static Task GoBackAsync() =>
        Shell.Current.GoToAsync("..");

    protected static Task GoToAsync(string route, Dictionary<string, object>? parameters = null) =>
        parameters == null
            ? Shell.Current.GoToAsync(route)
            : Shell.Current.GoToAsync(route, parameters);

    /// <summary>
    /// Runs an async action with IsBusy tracking.
    /// Shows an error dialog when an unhandled exception occurs.
    /// </summary>
    protected async Task ExecuteSafelyAsync(
        Func<Task> action,
        string errorTitle = "Error")
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] {errorTitle}: {ex.Message}");
            await Shell.Current.DisplayAlert(errorTitle, ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
