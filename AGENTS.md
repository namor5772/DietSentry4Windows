# Repository Guidelines

## Project Structure & Module Organization
- `DietSentry4Windows/DietSentry4Windows.slnx` is the solution entry point.
- `DietSentry4Windows/DietSentry/` contains the MAUI app (XAML + C# code-behind, `MauiProgram.cs`, `AppShell.xaml`).
- `DietSentry4Windows/DietSentry/Platforms/` holds platform entry points/manifests for Windows, Android, iOS, and MacCatalyst.
- `DietSentry4Windows/DietSentry/Resources/` stores assets; `Resources/Raw/foods.db` is the packaged seed database.
- `reference-android/` is the Kotlin reference app used to keep UI/behavior parity; treat it as read-only reference.
- Build outputs land under `DietSentry4Windows/DietSentry/bin/` and `DietSentry4Windows/DietSentry/obj/`.

## Build, Test, and Development Commands
- `dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj` builds all configured targets.
- `dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-windows10.0.19041.0` builds the Windows target.
- `dotnet run --project DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-windows10.0.19041.0` runs the Windows app.
- `dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-android` builds the Android target.
- `dotnet workload install maui` installs MAUI workloads (required with the .NET 10 preview SDK).

## Coding Style & Naming Conventions
- Use 4-space indentation with braces on new lines in C# and XAML.
- Follow `PascalCase` for public types/methods and `camelCase` for locals/fields.
- Keep XAML attributes on separate lines; mirror the layout in `DietSentry4Windows/DietSentry/MainPage.xaml`.
- Nullable reference types are enabled; avoid toggling `#nullable` in new files.

## Testing Guidelines
- No test project exists yet, so `dotnet test` is a no-op.
- If adding tests, create a separate project (e.g., `DietSentry.Tests`) and name files `*Tests.cs`.
- Prefer unit tests colocated by feature area and run them from the repo root.

## Commit & Pull Request Guidelines
- Commit history uses short, capitalized, past-tense sentences (example: `Fully Wired the Copy button`); keep that style.
- PRs should include a concise summary, linked issues when applicable, and screenshots for UI changes.
- Call out which target frameworks you built or ran (Windows, Android, etc.).

## Data, Configuration, and Reference Notes
- App identifiers and versions live in `DietSentry4Windows/DietSentry/DietSentry.csproj`.
- The app copies `Resources/Raw/foods.db` into the platform app data directory on first run; clear app data to reset.
- Do not commit secrets or environment-specific configuration values.

## Do Not Edit
- Never modify `MAUIcodexall.txt` or anything under `reference-android/`.
