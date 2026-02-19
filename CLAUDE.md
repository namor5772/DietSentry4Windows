# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

All commands run from the repo root:

```bash
# Build all targets
dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj

# Build + run Windows
dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-windows10.0.19041.0
dotnet run --project DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-windows10.0.19041.0

# Build Android
dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-android

# Install MAUI workloads (if missing)
dotnet workload install maui
```

No test project exists yet (`dotnet test` is a no-op).

## Architecture

**.NET 10 MAUI app** porting an existing Kotlin Android app. Targets Windows, Android, iOS, and macOS Catalyst. Primary development is on **Windows and Android**.

**Code-behind pattern (not MVVM):** Each XAML `ContentPage` sets `BindingContext = this` and raises `OnPropertyChanged()` directly. There is no ViewModel layer or DI-based service registration.

**Key layers:**
- **Pages** (`MainPage`, `EatenLogPage`, `WeightTablePage`, `AddSolidFoodPage`, `AddLiquidFoodPage`, `AddRecipePage`, `EditFoodPage`, `EditRecipePage`, `CopyFoodPage`, `CopyRecipePage`, `AddFoodByJsonPage`) — XAML + code-behind files that own UI logic and data binding
- **Data service** (`FoodDatabaseService.cs`) — all SQLite access via raw `Microsoft.Data.Sqlite` ADO.NET queries (no ORM). Hand-written SQL mapped to model classes
- **Models** (`Food.cs`, `EatenFood.cs`, `WeightEntry.cs`, `RecipeItem.cs`, `DailyTotals.cs`) — sealed classes with `init`-only properties
- **Database init** (`DatabaseInitializer.cs`) — copies `Resources/Raw/foods.db` to `FileSystem.AppDataDirectory` on first run

**Navigation:** MAUI Shell with registered routes in `AppShell.xaml.cs`. Pages receive parameters via `[QueryProperty]` attributes and `Shell.Current.GoToAsync()`.

**Platform divergence:** `#if ANDROID` / `#if WINDOWS` preprocessor directives in code-behind files handle platform-specific behavior (file pickers, layout tweaks). Android uses narrower buttons, explicit dialog scrims, and `DocumentsContract` for folder picking. Windows uses `Windows.Storage.Pickers.FolderPicker`.

**Custom controls:** `MarkdownView` wraps a `WebView` with Markdig-based markdown-to-HTML conversion, including dark/light theme CSS injection.

## Do Not Edit

- `MAUIcodexall.txt` — read-only AI context file
- `reference-android/` — original Kotlin source, used as behavioral reference only

## Coding Conventions

- 4-space indentation, braces on new lines (C# and XAML)
- `PascalCase` for public types/methods, `camelCase` for locals/fields
- XAML attributes on separate lines, matching the style in `MainPage.xaml`
- Nullable reference types enabled; do not toggle `#nullable`
- Commit messages: short, capitalized, past-tense (e.g., "Fixed Min/NIP/All mode wiring regression")
- PRs should note which target frameworks were built/tested (Windows, Android, etc.)

## NuGet Dependencies

`Microsoft.Data.Sqlite` (8.0.8), `Markdig` (0.37.0), `Microsoft.Extensions.Logging.Debug` (10.0.0)
