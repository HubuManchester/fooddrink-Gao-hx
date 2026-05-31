using FoodieApp.Models;
using Newtonsoft.Json;

namespace FoodieApp.Services;

public interface IRecipeService
{
    Task<List<Recipe>> GetAllRecipesAsync();
    Task<List<Recipe>> SearchRecipesAsync(string query);
    Task<Recipe?> GetRecipeByIdAsync(string id);
    Task SaveRecipeAsync(Recipe recipe);
    Task DeleteRecipeAsync(string id);
    Task ToggleFavouriteAsync(string id);
}

/// <summary>
/// Persists recipes as a JSON file in the app's local data directory.
/// Seeds 15 example recipes on first launch.
/// </summary>
public class RecipeService : IRecipeService
{
    private readonly string _filePath =
        Path.Combine(FileSystem.AppDataDirectory, "recipes.json");

    private List<Recipe>? _cache;

    public async Task<List<Recipe>> GetAllRecipesAsync()
    {
        await EnsureLoadedAsync();
        return _cache!;
    }

    public async Task<List<Recipe>> SearchRecipesAsync(string query)
    {
        await EnsureLoadedAsync();
        if (string.IsNullOrWhiteSpace(query)) return _cache!;
        return _cache!.Where(r =>
            (r.Name        ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (r.Category    ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (r.Cuisine     ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (r.Description ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<Recipe?> GetRecipeByIdAsync(string id)
    {
        await EnsureLoadedAsync();
        return _cache!.FirstOrDefault(r => r.Id == id);
    }

    public async Task SaveRecipeAsync(Recipe recipe)
    {
        await EnsureLoadedAsync();
        var existing = _cache!.FirstOrDefault(r => r.Id == recipe.Id);
        if (existing != null) _cache!.Remove(existing);
        _cache!.Add(recipe);
        await PersistAsync();
    }

    public async Task DeleteRecipeAsync(string id)
    {
        await EnsureLoadedAsync();
        var r = _cache!.FirstOrDefault(r => r.Id == id);
        if (r != null) { _cache!.Remove(r); await PersistAsync(); }
    }

    public async Task ToggleFavouriteAsync(string id)
    {
        await EnsureLoadedAsync();
        var r = _cache!.FirstOrDefault(r => r.Id == id);
        if (r != null) { r.IsFavourite = !r.IsFavourite; await PersistAsync(); }
    }

    // Increment this constant whenever new seed recipes are added.
    // If the stored count of seed recipes is less than this the seeds are merged in.
    private const int SeedCount = 15;

    private async Task EnsureLoadedAsync()
    {
        if (_cache != null) return;
        try
        {
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath);
                _cache = JsonConvert.DeserializeObject<List<Recipe>>(json) ?? new List<Recipe>();

                // Merge any seeds that are not yet in the stored data
                var seeds = BuildSeedRecipes();
                var existingIds = _cache.Select(r => r.Id).ToHashSet();
                bool added = false;
                foreach (var seed in seeds)
                {
                    if (!existingIds.Contains(seed.Id))
                    {
                        _cache.Add(seed);
                        added = true;
                    }
                }
                if (added) await PersistAsync();
            }
            else
            {
                _cache = BuildSeedRecipes();
                await PersistAsync();
            }
        }
        catch
        {
            _cache = BuildSeedRecipes();
            await PersistAsync();
        }
    }

    private async Task PersistAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RecipeService persist error: {ex.Message}");
        }
    }

    private static List<Recipe> BuildSeedRecipes() => new()
    {
        // ── Original 5 ──────────────────────────────────────────────────────
        new Recipe
        {
            Id = "seed-1", Name = "Classic Spaghetti Carbonara",
            Description = "A rich, creamy Italian pasta dish with eggs, cheese and pancetta.",
            Category = "Dinner", Cuisine = "Italian",
            PrepTimeMinutes = 10, CookTimeMinutes = 20, Servings = 4, Difficulty = "Medium",
            EmojiThumbnail = "🍝", CardColor = "#E8B89A",
            CaloriesPerServing = 560, ProteinGrams = 22, CarbohydratesGrams = 65, FatGrams = 24,
            Ingredients = new List<string>
            {
                "400g spaghetti", "200g pancetta or guanciale",
                "4 large eggs", "100g Pecorino Romano, grated",
                "50g Parmesan, grated", "Freshly ground black pepper", "Salt"
            },
            Steps = new List<string>
            {
                "Boil a large pot of salted water and cook spaghetti until al dente. Reserve 200ml pasta water before draining.",
                "Fry the pancetta in a large pan over medium heat until golden and the fat has rendered.",
                "Whisk eggs with the grated cheeses and a generous amount of black pepper.",
                "Remove the pancetta pan from the heat. Add the drained pasta and toss to coat in the fat.",
                "Pour the egg mixture over the pasta and toss quickly, adding pasta water splash by splash until creamy.",
                "Serve immediately with extra cheese and black pepper."
            },
            Notes = "Work quickly off the heat so the eggs do not scramble.",
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1612874742237-6526221588e3?w=600",
                "https://images.unsplash.com/photo-1588013273468-315fd88ea34c?w=600",
                "https://images.unsplash.com/photo-1625937286074-9ca519d5d9df?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-2", Name = "Avocado Toast with Poached Egg",
            Description = "A nutritious and satisfying breakfast ready in under 15 minutes.",
            Category = "Breakfast", Cuisine = "Modern",
            PrepTimeMinutes = 5, CookTimeMinutes = 8, Servings = 1, Difficulty = "Easy",
            EmojiThumbnail = "🥑", CardColor = "#A8D5A2",
            CaloriesPerServing = 320, ProteinGrams = 14, CarbohydratesGrams = 28, FatGrams = 18,
            Ingredients = new List<string>
            {
                "2 slices sourdough bread", "1 ripe avocado",
                "2 eggs", "1 tbsp white wine vinegar",
                "Chilli flakes", "Salt and pepper", "Lemon juice"
            },
            Steps = new List<string>
            {
                "Toast the sourdough until golden and crisp.",
                "Halve and stone the avocado. Mash the flesh with lemon juice, salt and pepper.",
                "Bring a small pan of water to a gentle simmer and add the vinegar.",
                "Crack each egg into a cup and slide gently into the simmering water. Poach for 3 minutes for a runny yolk.",
                "Spread avocado onto toast, top with a poached egg and sprinkle with chilli flakes."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1541519227354-08fa5d50c820?w=600",
                "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=600",
                "https://images.unsplash.com/photo-1547592180-85f173990554?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-3", Name = "Chicken Tikka Masala",
            Description = "Tender chicken in a smoky, aromatic tomato cream sauce.",
            Category = "Dinner", Cuisine = "Indian",
            PrepTimeMinutes = 20, CookTimeMinutes = 40, Servings = 4, Difficulty = "Medium",
            EmojiThumbnail = "🍛", CardColor = "#F4A460",
            CaloriesPerServing = 490, ProteinGrams = 35, CarbohydratesGrams = 22, FatGrams = 28,
            Ingredients = new List<string>
            {
                "700g chicken breast, cubed", "200ml plain yoghurt",
                "2 tsp garam masala", "2 tsp cumin", "1 tsp turmeric", "2 tsp paprika",
                "400g can chopped tomatoes", "200ml double cream",
                "2 onions, diced", "4 cloves garlic, minced", "2 tsp fresh ginger, grated"
            },
            Steps = new List<string>
            {
                "Marinate chicken in yoghurt, 1 tsp garam masala, 1 tsp cumin and turmeric for at least 1 hour.",
                "Grill or pan-fry marinated chicken until charred. Set aside.",
                "Saute onions in oil until golden. Add garlic and ginger; cook 2 minutes.",
                "Stir in remaining spices and cook 1 minute until fragrant.",
                "Add chopped tomatoes and simmer 15 minutes until the sauce thickens.",
                "Stir in cream, add the cooked chicken, simmer 5 more minutes.",
                "Serve with basmati rice or naan bread."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1565557623262-b51c2513a641?w=600",
                "https://images.unsplash.com/photo-1588166524941-3bf61a9c41db?w=600",
                "https://images.unsplash.com/photo-1567188040759-fb8a883dc6d8?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-4", Name = "Berry Smoothie Bowl",
            Description = "A vibrant, nutritious smoothie bowl packed with antioxidants.",
            Category = "Breakfast", Cuisine = "Modern",
            PrepTimeMinutes = 10, CookTimeMinutes = 0, Servings = 1, Difficulty = "Easy",
            EmojiThumbnail = "🍓", CardColor = "#C78FC0",
            CaloriesPerServing = 280, ProteinGrams = 8, CarbohydratesGrams = 52, FatGrams = 6,
            Ingredients = new List<string>
            {
                "150g frozen mixed berries", "1 frozen banana",
                "80ml oat milk", "2 tbsp granola",
                "1 tbsp chia seeds", "Fresh berries to top", "1 tbsp honey"
            },
            Steps = new List<string>
            {
                "Blend frozen berries, banana and oat milk until thick and smooth. Add more milk only if needed.",
                "Pour into a bowl — it must be thick enough for toppings to sit on top.",
                "Arrange granola, fresh berries and chia seeds on top.",
                "Drizzle with honey and serve immediately."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1511690743698-d9d85f2fbf38?w=600",
                "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600",
                "https://images.unsplash.com/photo-1490474504059-bf2db5ab2348?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-5", Name = "Classic Caesar Salad",
            Description = "Crisp romaine, crunchy croutons and rich Caesar dressing.",
            Category = "Lunch", Cuisine = "American",
            PrepTimeMinutes = 15, CookTimeMinutes = 10, Servings = 2, Difficulty = "Easy",
            EmojiThumbnail = "🥗", CardColor = "#8FBC8F",
            CaloriesPerServing = 380, ProteinGrams = 12, CarbohydratesGrams = 24, FatGrams = 28,
            Ingredients = new List<string>
            {
                "1 large romaine lettuce, torn", "50g Parmesan shavings",
                "100g sourdough croutons", "3 tbsp Caesar dressing",
                "Lemon juice", "Black pepper"
            },
            Steps = new List<string>
            {
                "Toss cubed bread in olive oil and garlic, bake at 200C for 10 minutes to make croutons.",
                "Toss romaine with Caesar dressing until every leaf is coated.",
                "Add croutons and half the Parmesan; toss gently.",
                "Plate and top with remaining Parmesan, a squeeze of lemon and black pepper."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1512852939750-1305098529bf?w=600",
                "https://images.unsplash.com/photo-1550304943-4f24f54ddde9?w=600",
                "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=600"
            }
        },

        // ── 10 New Recipes ───────────────────────────────────────────────────
        new Recipe
        {
            Id = "seed-6", Name = "Japanese Ramen",
            Description = "Rich tonkotsu-style broth with soft-boiled egg and chashu pork.",
            Category = "Dinner", Cuisine = "Japanese",
            PrepTimeMinutes = 30, CookTimeMinutes = 60, Servings = 2, Difficulty = "Hard",
            EmojiThumbnail = "🍜", CardColor = "#DEB887",
            CaloriesPerServing = 620, ProteinGrams = 38, CarbohydratesGrams = 72, FatGrams = 20,
            Ingredients = new List<string>
            {
                "2 portions ramen noodles", "1 litre pork or chicken broth",
                "2 tbsp soy sauce", "1 tbsp mirin", "1 tbsp sesame oil",
                "200g pork belly, rolled and tied", "2 eggs",
                "4 slices bamboo shoots (menma)", "2 spring onions, sliced",
                "1 sheet nori, halved", "1 tsp white sesame seeds"
            },
            Steps = new List<string>
            {
                "Roll pork belly tightly, tie with string, sear on all sides then braise in soy, mirin and water for 45 minutes. Slice when cool.",
                "Soft-boil eggs for 6.5 minutes, cool in ice water, peel and marinate in soy and mirin for at least 1 hour.",
                "Bring broth to a simmer; season with soy sauce, mirin and sesame oil.",
                "Cook noodles according to packet; drain and place in bowls.",
                "Ladle hot broth over noodles. Top with sliced pork, halved egg, bamboo shoots, spring onions and nori.",
                "Sprinkle sesame seeds and serve at once."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=600",
                "https://images.unsplash.com/photo-1591814468924-caf88d1232e1?w=600",
                "https://images.unsplash.com/photo-1617093727343-374698b1b08d?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-7", Name = "Beef Tacos",
            Description = "Crispy corn tortillas loaded with seasoned beef, salsa and guacamole.",
            Category = "Dinner", Cuisine = "Mexican",
            PrepTimeMinutes = 15, CookTimeMinutes = 20, Servings = 4, Difficulty = "Easy",
            EmojiThumbnail = "🌮", CardColor = "#F0A500",
            CaloriesPerServing = 430, ProteinGrams = 28, CarbohydratesGrams = 38, FatGrams = 18,
            Ingredients = new List<string>
            {
                "500g lean beef mince", "8 corn tortillas",
                "1 tsp cumin", "1 tsp smoked paprika", "1 tsp garlic powder",
                "1 tsp chilli powder", "Salt and pepper",
                "1 avocado", "1 lime", "200g tomato salsa",
                "50g cheddar, grated", "Sour cream to serve", "Fresh coriander"
            },
            Steps = new List<string>
            {
                "Brown beef mince in a hot pan, breaking it up as it cooks.",
                "Add all spices and a splash of water; stir and cook for 5 minutes until fragrant.",
                "Mash avocado with lime juice and a pinch of salt to make quick guacamole.",
                "Warm tortillas in a dry pan for 30 seconds each side.",
                "Fill each tortilla with beef, salsa, guacamole, grated cheese, sour cream and coriander."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=600",
                "https://images.unsplash.com/photo-1551504734-5ee1c4a1479b?w=600",
                "https://images.unsplash.com/photo-1624300629298-e9de39c13be5?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-8", Name = "Margherita Pizza",
            Description = "Classic Neapolitan pizza with san marzano tomato and fresh mozzarella.",
            Category = "Dinner", Cuisine = "Italian",
            PrepTimeMinutes = 20, CookTimeMinutes = 15, Servings = 2, Difficulty = "Medium",
            EmojiThumbnail = "🍕", CardColor = "#FF8C69",
            CaloriesPerServing = 480, ProteinGrams = 18, CarbohydratesGrams = 62, FatGrams = 16,
            Ingredients = new List<string>
            {
                "300g pizza dough", "150ml tomato passata",
                "200g fresh mozzarella, torn", "Fresh basil leaves",
                "2 tbsp olive oil", "Salt", "Semolina for dusting"
            },
            Steps = new List<string>
            {
                "Place a pizza stone or heavy baking sheet in the oven and preheat to 250C (or as hot as it goes) for 30 minutes.",
                "Stretch dough on a semolina-dusted surface into a thin round. Do not use a rolling pin.",
                "Spread passata lightly over the base leaving a 2cm border. Season with salt and drizzle with oil.",
                "Scatter torn mozzarella evenly.",
                "Slide onto the hot stone and bake 10-12 minutes until the crust is charred at the edges and cheese is bubbling.",
                "Top with fresh basil and a final drizzle of olive oil before serving."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=600",
                "https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=600",
                "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-9", Name = "Banana Pancakes",
            Description = "Fluffy, naturally sweet pancakes made with ripe bananas.",
            Category = "Breakfast", Cuisine = "American",
            PrepTimeMinutes = 10, CookTimeMinutes = 15, Servings = 2, Difficulty = "Easy",
            EmojiThumbnail = "🥞", CardColor = "#FFD580",
            CaloriesPerServing = 310, ProteinGrams = 10, CarbohydratesGrams = 54, FatGrams = 7,
            Ingredients = new List<string>
            {
                "2 ripe bananas", "2 large eggs",
                "100g plain flour", "1 tsp baking powder",
                "120ml milk", "1 tbsp butter (plus extra for frying)",
                "Maple syrup to serve", "Pinch of salt"
            },
            Steps = new List<string>
            {
                "Mash bananas thoroughly in a large bowl until smooth.",
                "Whisk in eggs, then milk and melted butter.",
                "Sift in flour, baking powder and salt; stir until just combined. Do not over-mix.",
                "Heat a non-stick pan over medium heat and melt a small knob of butter.",
                "Pour 3-4 tablespoons of batter per pancake. Cook until bubbles appear on the surface (about 2 minutes), then flip and cook 1 more minute.",
                "Serve stacked with maple syrup and sliced banana."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1506084868230-bb9d95c24759?w=600",
                "https://images.unsplash.com/photo-1528207776546-365bb710ee93?w=600",
                "https://images.unsplash.com/photo-1551024601-bec78aea704b?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-10", Name = "Greek Salad",
            Description = "A refreshing summer salad with olives, feta and crisp vegetables.",
            Category = "Lunch", Cuisine = "Greek",
            PrepTimeMinutes = 15, CookTimeMinutes = 0, Servings = 4, Difficulty = "Easy",
            EmojiThumbnail = "🥗", CardColor = "#87CEEB",
            CaloriesPerServing = 220, ProteinGrams = 8, CarbohydratesGrams = 12, FatGrams = 16,
            Ingredients = new List<string>
            {
                "4 ripe tomatoes, cut into chunks", "1 large cucumber, thickly sliced",
                "1 red onion, thinly sliced", "200g kalamata olives",
                "200g feta cheese, cubed", "1 green pepper, sliced",
                "4 tbsp extra-virgin olive oil", "1 tbsp red wine vinegar",
                "1 tsp dried oregano", "Salt and black pepper"
            },
            Steps = new List<string>
            {
                "Combine tomatoes, cucumber, red onion, green pepper and olives in a large bowl.",
                "Whisk olive oil, red wine vinegar, oregano, salt and pepper to make the dressing.",
                "Pour dressing over the vegetables and toss gently.",
                "Place feta cubes on top — do not mix them in so they stay intact.",
                "Finish with an extra pinch of oregano and serve immediately with crusty bread."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1572449043416-55f4685c9bb7?w=600",
                "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600",
                "https://images.unsplash.com/photo-1490474504059-bf2db5ab2348?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-11", Name = "Chocolate Lava Cake",
            Description = "Decadent warm chocolate cake with a gooey molten centre.",
            Category = "Dessert", Cuisine = "French",
            PrepTimeMinutes = 15, CookTimeMinutes = 12, Servings = 4, Difficulty = "Medium",
            EmojiThumbnail = "🍰", CardColor = "#8B4513",
            CaloriesPerServing = 420, ProteinGrams = 8, CarbohydratesGrams = 45, FatGrams = 24,
            Ingredients = new List<string>
            {
                "200g dark chocolate (70% cocoa), chopped", "150g unsalted butter",
                "4 large eggs", "4 egg yolks",
                "150g caster sugar", "60g plain flour",
                "Butter and cocoa powder for greasing ramekins",
                "Vanilla ice cream to serve"
            },
            Steps = new List<string>
            {
                "Preheat oven to 200C. Butter 4 ramekins and dust with cocoa powder.",
                "Melt chocolate and butter together in a heatproof bowl over simmering water. Stir until smooth and leave to cool slightly.",
                "Whisk eggs, yolks and sugar with an electric mixer until pale and thick (about 3 minutes).",
                "Fold the chocolate mixture into the egg mixture, then fold in the flour.",
                "Divide the batter among the prepared ramekins. Chill for up to 24 hours or bake immediately.",
                "Bake for 10-12 minutes until the edges are set but the centre still wobbles.",
                "Run a knife around the edge, invert onto plates and serve immediately with vanilla ice cream."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1624353365286-3f8d62daad51?w=600",
                "https://images.unsplash.com/photo-1578985545062-69928b1d9587?w=600",
                "https://images.unsplash.com/photo-1606313564200-e75d5e30476c?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-12", Name = "Hummus with Pita",
            Description = "Smooth, creamy homemade hummus served with warm pita bread.",
            Category = "Snack", Cuisine = "Middle Eastern",
            PrepTimeMinutes = 10, CookTimeMinutes = 5, Servings = 6, Difficulty = "Easy",
            EmojiThumbnail = "🧆", CardColor = "#D2B48C",
            CaloriesPerServing = 210, ProteinGrams = 7, CarbohydratesGrams = 28, FatGrams = 9,
            Ingredients = new List<string>
            {
                "400g can chickpeas, drained (reserve liquid)", "60ml tahini",
                "2 cloves garlic", "3 tbsp lemon juice",
                "2 tbsp olive oil", "1/2 tsp cumin",
                "Salt", "Paprika and parsley to garnish",
                "4 pita breads"
            },
            Steps = new List<string>
            {
                "Blend chickpeas, tahini, garlic, lemon juice and cumin in a food processor for 1 minute.",
                "With the motor running, add 3-4 tablespoons of reserved chickpea liquid until the hummus is very smooth and creamy.",
                "Season with salt and taste; adjust lemon and garlic as needed.",
                "Spoon into a bowl, drizzle with olive oil and sprinkle with paprika and chopped parsley.",
                "Warm pita breads in a dry pan or under the grill and serve alongside."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1541014741259-de529411b96a?w=600",
                "https://images.unsplash.com/photo-1615361200141-f45040f367be?w=600",
                "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-13", Name = "Salmon Teriyaki",
            Description = "Glazed salmon fillets with a sweet soy teriyaki sauce and steamed rice.",
            Category = "Dinner", Cuisine = "Japanese",
            PrepTimeMinutes = 10, CookTimeMinutes = 15, Servings = 2, Difficulty = "Easy",
            EmojiThumbnail = "🐟", CardColor = "#FA8072",
            CaloriesPerServing = 460, ProteinGrams = 38, CarbohydratesGrams = 42, FatGrams = 14,
            Ingredients = new List<string>
            {
                "2 salmon fillets (150g each)", "3 tbsp soy sauce",
                "2 tbsp mirin", "2 tbsp sake or dry sherry",
                "1 tbsp honey", "1 tsp sesame oil",
                "200g jasmine rice", "1 tbsp vegetable oil",
                "Spring onions and sesame seeds to garnish"
            },
            Steps = new List<string>
            {
                "Cook rice according to packet instructions.",
                "Mix soy sauce, mirin, sake, honey and sesame oil in a small bowl to make the teriyaki sauce.",
                "Pat salmon dry and season lightly with salt.",
                "Heat vegetable oil in a non-stick pan over medium-high heat. Cook salmon skin-side up for 3 minutes until golden.",
                "Flip and pour the teriyaki sauce into the pan. Cook 3-4 more minutes, spooning sauce over the fish, until the glaze is thick.",
                "Serve over rice, garnished with sliced spring onions and sesame seeds."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1467003909585-2f8a72700288?w=600",
                "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=600",
                "https://images.unsplash.com/photo-1559742811-822873691df8?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-14", Name = "Vegetable Stir Fry",
            Description = "A quick and colourful wok dish packed with crisp vegetables and savory sauce.",
            Category = "Dinner", Cuisine = "Chinese",
            PrepTimeMinutes = 15, CookTimeMinutes = 10, Servings = 3, Difficulty = "Easy",
            EmojiThumbnail = "🥘", CardColor = "#90EE90",
            CaloriesPerServing = 230, ProteinGrams = 9, CarbohydratesGrams = 32, FatGrams = 8,
            Ingredients = new List<string>
            {
                "1 red pepper, sliced", "1 yellow pepper, sliced",
                "200g broccoli florets", "150g snap peas",
                "2 carrots, julienned", "3 cloves garlic, minced",
                "1 tbsp fresh ginger, grated", "3 tbsp soy sauce",
                "1 tbsp oyster sauce", "1 tsp sesame oil",
                "1 tbsp cornflour mixed with 2 tbsp water",
                "2 tbsp vegetable oil", "Cooked noodles or rice to serve"
            },
            Steps = new List<string>
            {
                "Mix soy sauce, oyster sauce, sesame oil and cornflour mixture in a small bowl to make the sauce.",
                "Heat a wok over the highest heat until smoking. Add vegetable oil.",
                "Add garlic and ginger; stir-fry 30 seconds until fragrant.",
                "Add carrots and broccoli; stir-fry 2 minutes.",
                "Add peppers and snap peas; stir-fry 2 more minutes. Vegetables should remain crisp.",
                "Pour the sauce over and toss everything to coat. Cook 1 minute until the sauce thickens.",
                "Serve immediately over noodles or rice."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1512058564366-18510be2db19?w=600",
                "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=600",
                "https://images.unsplash.com/photo-1585032226651-759b368d7246?w=600"
            }
        },
        new Recipe
        {
            Id = "seed-15", Name = "French Onion Soup",
            Description = "Slow-cooked caramelised onion soup topped with gruyere croutons.",
            Category = "Lunch", Cuisine = "French",
            PrepTimeMinutes = 15, CookTimeMinutes = 60, Servings = 4, Difficulty = "Medium",
            EmojiThumbnail = "🥣", CardColor = "#DAA520",
            CaloriesPerServing = 380, ProteinGrams = 14, CarbohydratesGrams = 38, FatGrams = 18,
            Ingredients = new List<string>
            {
                "1kg onions, thinly sliced", "50g unsalted butter",
                "2 tbsp olive oil", "1 tsp sugar",
                "2 cloves garlic, minced", "200ml dry white wine",
                "1.2 litres good beef stock", "1 bouquet garni",
                "Salt and black pepper",
                "8 thick slices baguette", "150g Gruyere, grated"
            },
            Steps = new List<string>
            {
                "Melt butter with oil in a large heavy pan over low heat. Add onions and sugar; cook stirring occasionally for 40-45 minutes until deeply caramelised and golden.",
                "Add garlic and cook 2 minutes. Increase heat to medium, add wine and stir to deglaze the pan.",
                "Add stock and bouquet garni. Simmer for 20 minutes. Season well and remove bouquet garni.",
                "Toast baguette slices until crisp.",
                "Ladle soup into ovenproof bowls. Float croutons on top and pile on grated Gruyere.",
                "Place under a hot grill until the cheese is bubbling and golden. Serve immediately and carefully."
            },
            FoodImageUrls = new List<string>
            {
                "https://images.unsplash.com/photo-1547592180-85f173990554?w=600",
                "https://images.unsplash.com/photo-1603105037880-880cd4edfb0d?w=600",
                "https://images.unsplash.com/photo-1476718406336-bb5a9690ee2a?w=600"
            }
        }
    };
}
