# DietSentry4Windows

DietSentry4Windows is a .NET MAUI rewrite of the DietSentry Android app (as available in the https://github.com/namor5772/DietSentry4Android repo) . It provides a cross-platform UI for browsing foods, logging eaten items, and managing weight records. The UI and behavior are intentionally aligned with the original Android app (see `reference-android/`).

EVERYTHING was done using OpenAI codex. No manual coding was performed, ever!

## Project Layout

- `DietSentry4Windows/DietSentry4Windows.slnx`: solution file.
- `DietSentry4Windows/DietSentry/`: MAUI app project (XAML + C# code-behind).
- `DietSentry4Windows/DietSentry/Platforms/`: platform-specific entry points/manifests.
- `DietSentry4Windows/DietSentry/Resources/`: icons, splash, fonts, and styles.
- `DietSentry4Windows/DietSentry/Resources/Raw/foods.db`: packaged seed database.
- `reference-android/`: source for the original Android app; used for parity.

## Features

- Foods table with filtering and nutrition display modes.
- Eaten table with daily totals and edit/delete workflows.
- Weight table with add/edit/delete workflows and comments.
- Multi-screen navigation via `Shell`.

## Platform-Specific UI Notes (Recent)

- Android: top-row buttons on main screens are narrower and wrap across multiple rows to use full width.
- Android: Foods Table lower selection buttons use a tighter width (`WidthRequest` 57) for compact layout.
- Android: dialog overlays are rendered above scrollable content with explicit scrims to avoid black-screen backdrops.
- Android: Weight Table item template allows taps to reach the `CollectionView` selection handler.

## Not Yet Implemented

The following actions currently show a "Not implemented" dialog:

- Foods: Edit, Delete, Convert
- Utilities: Export db, Import db, Export csv

## Build and Run

From the repo root:

```bash
dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj
```

Windows target:

```bash
dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-windows10.0.19041.0
dotnet run --project DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-windows10.0.19041.0
```

Android target:

```bash
dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-android
```

## Prerequisites

- .NET 10 SDK (preview) with MAUI workloads installed.
- Platform-specific tooling (Android SDK, Windows SDK) depending on target.

If MAUI workloads are missing:

```bash
dotnet workload install maui
```

## Database Notes

- On first run the app copies `Resources/Raw/foods.db` into the platform app data directory.
- `DatabaseInitializer` is responsible for this copy and exposes the runtime database path.
- If you need a clean start, uninstall the app or delete the app data directory so the seed database is recopied.

## Development Notes

- XAML uses 4-space indentation and keeps attributes on separate lines.
- Nullable reference types are enabled.
- Tests are not set up yet; `dotnet test` does nothing until a test project is added.

## Reference App

The `reference-android/` directory contains the original Android implementation. It is the source of truth for screen structure and behavior when porting features to MAUI.
