namespace FoodieApp.Models;

/// <summary>Core recipe domain model stored as JSON on device.</summary>
public class Recipe
{
    public string Id              { get; set; } = Guid.NewGuid().ToString();
    public string Name            { get; set; } = string.Empty;
    public string Description     { get; set; } = string.Empty;
    public string Category        { get; set; } = string.Empty;
    public string Cuisine         { get; set; } = string.Empty;
    public int    PrepTimeMinutes { get; set; }
    public int    CookTimeMinutes { get; set; }
    public int    Servings        { get; set; } = 2;
    public string Difficulty      { get; set; } = "Easy";
    public List<string> Ingredients { get; set; } = new();
    public List<string> Steps       { get; set; } = new();
    public string EmojiThumbnail  { get; set; } = "🍽️";
    public string CardColor       { get; set; } = "#FF6B35";
    public bool   IsFavourite     { get; set; }
    public int    CaloriesPerServing { get; set; }
    public double ProteinGrams    { get; set; }
    public double CarbohydratesGrams { get; set; }
    public double FatGrams        { get; set; }
    public string Notes           { get; set; } = string.Empty;
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// URLs of food photos shown in the recipe detail gallery.
    /// Uses Unsplash source URLs so photos load on any device with internet.
    /// </summary>
    public List<string> FoodImageUrls { get; set; } = new();

    public int    TotalTimeMinutes => PrepTimeMinutes + CookTimeMinutes;
    public string TotalTimeDisplay => TotalTimeMinutes >= 60
        ? $"{TotalTimeMinutes / 60}h {TotalTimeMinutes % 60}m"
        : $"{TotalTimeMinutes}m";
}
