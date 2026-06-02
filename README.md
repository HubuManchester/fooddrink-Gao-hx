# FoodieApp

This is a cross-platform mobile application built with **.NET MAUI** (Multi-platform App UI).

The app is themed around **Food & Drink**, combining recipe management, barcode-based nutrition lookup, meal planning, and nearby restaurant discovery in a single, accessible interface.

---

## Features

| Feature | Description |
|---|---|
| **Recipe Manager** | Browse, search, add, and view detailed recipes with ingredients and step-by-step instructions |
| **Barcode Scanner** | Scan food product barcodes with the device camera to fetch nutritional information via the Open Food Facts API; falls back to manual barcode entry when a camera is unavailable |
| **Nutrition Info** | Displays calories, macronutrients (protein, carbs, fat), sugar, fibre, salt, and NutriScore grade for scanned products |
| **Meal Planner** | Plan meals across the week |
| **Nearby Restaurants** | Uses GPS to display the user's current location and find food venues nearby |
| **Text-to-Speech** | Reads recipe steps aloud, with play control on each recipe detail page |
| **Settings** | Toggle dark mode, adjust font size (Small / Medium / Large), enable/disable TTS |
| **Help Page** | In-app guidance accessible via the `Help & FAQ` button on the home screen |

---

## Hardware Features Used

| # | Hardware | Usage |
|---|---|---|
| 1 | **Camera** | Take a photo or scan a barcode |
| 2 | **Barcode decoding (Camera + ZXing)** | Decodes EAN/UPC barcodes from camera images using ZXing.Net on Android |
| 3 | **GPS / Geolocation** | Fetches the user's current coordinates for the Nearby Restaurants page |
| 4 | **Text-to-Speech** | Reads recipe instructions aloud using the device TTS engine |
| 5 | **Accelerometer / Shake** | Detects a shake gesture on the Meal Planner page to randomise the meal plan |

---

## Project Structure

```
FoodieApp/
├── Models/           # Data models (Recipe, NutritionInfo, BarcodeResult, etc.)
├── ViewModels/       # MVVM ViewModels for each page
├── Views/            # XAML pages (MainPage, RecipeListPage, BarcodeScannerPage, etc.)
├── Services/         # Hardware & data services (Camera, GPS, TTS, Shake, Nutrition)
├── Converters/       # XAML value converters
├── Platforms/
│   └── Android/      # Android-specific implementations (BitmapFactory barcode decoder)
└── Resources/
    └── Styles/       # Shared colours and styles (light & dark theme)
```

---

## Getting Started

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/) (v17.8 or later) with the **.NET MAUI** workload installed
- .NET 9 SDK
- Android SDK (API 21+ / Android 5.0 or higher) for Android deployment
- A physical Android device **or** an Android emulator (API 21+)

### Running the App

1. Clone or download this repository.
2. Open `FoodieApp.sln` in Visual Studio 2022.
3. Restore NuGet packages (this happens automatically on first build).
4. Select a deployment target (Android emulator or physical device) from the toolbar.
5. Press **F5** (or click **Run**) to build and deploy.

---

## Accessibility

The app follows [WCAG 2.1](https://www.w3.org/WAI/WCAG21/quickref/) accessibility principles:

- **Dark mode** toggle in Settings (changes the entire app theme)
- **Adjustable font sizes** (Small / Medium / Large) applied app-wide via dynamic styles
- **Text-to-Speech** for recipe instructions (supports users with reading difficulties)

---

## Author

**Huixin Gao**  
21906370
