using System.Globalization;

namespace FoodieApp.Converters;

/// <summary>Converts a bool favourite flag to a heart emoji string.</summary>
public class FavouriteIconConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is bool b && b ? "❤️" : "🤍";
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>Inverts a boolean value.</summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is bool b ? !b : value ?? false;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => value is bool b ? !b : value ?? false;
}

/// <summary>Returns true when an integer count is greater than zero.</summary>
public class CountToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is int n && n > 0;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>Converts the IsSpeaking flag to a button label.</summary>
public class TtsButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => "Read Aloud";
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>Converts the favourites-only filter flag to a toolbar label.</summary>
public class FavFilterTextConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is bool b && b ? "Show All" : "Favourites";
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>Maps a meal type string to a representative emoji.</summary>
public class MealTypeIconConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is string s ? s switch
        {
            "Breakfast" => "🌅", "Lunch" => "☀️",
            "Dinner"    => "🌙", "Snack" => "🍎",
            _           => "🍽️"
        } : "🍽️";
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>Returns true when the bound value is not null.</summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is not null;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}
