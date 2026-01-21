# Repository Guidelines

## Project Structure & Module Organization
- `DietSentry4Windows/` holds the solution file `DietSentry4Windows.slnx`.
- `DietSentry4Windows/DietSentry/` is the .NET MAUI app project (`App.xaml`, `AppShell.xaml`, `MainPage.xaml`, `MauiProgram.cs`).
- `DietSentry4Windows/DietSentry/Platforms/` contains platform-specific entry points and manifests (Android, iOS, MacCatalyst, Windows).
- `DietSentry4Windows/DietSentry/Resources/` stores icons, splash screens, images, fonts, and XAML styles.
- `DietSentry4Windows/DietSentry/bin/` and `DietSentry4Windows/DietSentry/obj/` are build outputs; do not edit.

## Build, Test, and Development Commands
- `dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj` builds all target frameworks configured in the project.
- `dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-windows10.0.19041.0` builds the Windows target.
- `dotnet run --project DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-windows10.0.19041.0` runs the Windows app.
- `dotnet build DietSentry4Windows/DietSentry/DietSentry.csproj -f net10.0-android` builds the Android target.
- `dotnet test` currently has no effect because no test project is present.

## Coding Style & Naming Conventions
- Follow existing C# and XAML formatting (4-space indentation, braces on new lines).
- Use `PascalCase` for public types/methods and `camelCase` for locals/fields.
- Keep XAML attributes on separate lines as shown in `DietSentry4Windows/DietSentry/MainPage.xaml`.
- Nullable reference types and implicit usings are enabled; avoid introducing `#nullable` toggles unless required.

## Testing Guidelines
- No testing framework is configured yet.
- If tests are added, place them in a dedicated project (for example, `DietSentry.Tests`) and name files `*Tests.cs`.
- Run tests with `dotnet test` at the repo root once a test project exists.

## Commit & Pull Request Guidelines
- This repository has no commit history yet; use short, imperative commit subjects (optionally with a scope like `ui:` or `platform:`).
- PRs should include a clear description, linked issues if applicable, and screenshots for UI changes.
- Note which target frameworks were built or run (for example, Windows or Android).

## Security & Configuration Tips
- App identifiers and versions live in `DietSentry4Windows/DietSentry/DietSentry.csproj`; update them deliberately.
- Avoid committing secrets or API keys; prefer environment-specific configuration outside source control.

## Additional Resources
- the `reference-android/` directory holds the source *.kt files for my original Android app, which is intended to help in creating this app that is essentially a rewrite of that app using .NET MAUI.

## Do Not Edit
- Never modify `MAUIcodexall.txt` or anything under `reference-android/`.
