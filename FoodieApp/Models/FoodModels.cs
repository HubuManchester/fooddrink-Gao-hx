namespace FoodieApp.Models;

/// <summary>One slot in the weekly meal plan (day + meal type + optional recipe).</summary>
public class MealPlanEntry
{
    public string   Id       { get; set; } = Guid.NewGuid().ToString();
    public int      DayIndex { get; set; }
    public string   MealType { get; set; } = string.Empty;
    public Recipe?  Recipe   { get; set; }

    public string DayName => DayIndex switch
    {
        0 => "Monday", 1 => "Tuesday", 2 => "Wednesday", 3 => "Thursday",
        4 => "Friday",  5 => "Saturday", 6 => "Sunday",   _ => "Unknown"
    };
}

/// <summary>Nutritional data returned by a barcode product lookup.</summary>
public class NutritionInfo
{
    public string ProductName         { get; set; } = string.Empty;
    public string Barcode             { get; set; } = string.Empty;
    public string Brand               { get; set; } = string.Empty;
    public string ServingSize         { get; set; } = "100g";
    public double Calories            { get; set; }
    public double ProteinGrams        { get; set; }
    public double CarbohydratesGrams  { get; set; }
    public double SugarsGrams         { get; set; }
    public double FatGrams            { get; set; }
    public double SaturatedFatGrams   { get; set; }
    public double SaltGrams           { get; set; }
    public double FibreGrams          { get; set; }
    public string NutriScore          { get; set; } = "C";
    public List<string> Allergens     { get; set; } = new();

    public string NutriScoreColor => NutriScore switch
    {
        "A" => "#038141", "B" => "#85BB2F", "C" => "#FECB02",
        "D" => "#EE8100", "E" => "#E63E11", _   => "#888888"
    };
}

/// <summary>Result from the barcode scanner service.</summary>
public class BarcodeResult
{
    public string  Value        { get; set; } = string.Empty;
    public bool    IsSuccess    { get; set; }
    public string? ErrorMessage { get; set; }
}
