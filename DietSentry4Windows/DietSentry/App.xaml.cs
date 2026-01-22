using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
#if WINDOWS
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
using Windows.Graphics;
#endif

namespace DietSentry
{
    public partial class App : Application
    {
        private const string WindowWidthKey = "AppWindowWidth";
        private const string WindowHeightKey = "AppWindowHeight";
        private const string WindowXKey = "AppWindowX";
        private const string WindowYKey = "AppWindowY";
        private const string WindowStateKey = "AppWindowState";
        private const string WindowWidthDipKey = "AppWindowWidthDips";
        private const string WindowHeightDipKey = "AppWindowHeightDips";
        private const string WindowXDipKey = "AppWindowXDips";
        private const string WindowYDipKey = "AppWindowYDips";

        public App()
        {
            InitializeComponent();
            _ = DatabaseInitializer.EnsureDatabaseAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.Destroying += OnWindowDestroying;

#if WINDOWS
            window.HandlerChanged += OnWindowHandlerChanged;
#endif

            return window;
        }

        private async void OnWindowDestroying(object? sender, EventArgs e)
        {
            await CleanupRecipeDraftsAsync();
        }

        private static async Task CleanupRecipeDraftsAsync()
        {
            try
            {
                await DatabaseInitializer.EnsureDatabaseAsync();
                var databaseService = new FoodDatabaseService();
                await databaseService.DeleteAllCopiedRecipesAsync();
                await databaseService.DeleteRecipesWithFoodIdZeroAsync();
            }
            catch
            {
                // Best effort cleanup during shutdown.
            }
        }

#if WINDOWS
        private const int WindowSaveDebounceMs = 400;
        private static readonly object WindowSaveLock = new();
        private static System.Timers.Timer? _windowSaveTimer;
        private static AppWindow? _pendingAppWindow;
        private static IntPtr _windowHandle;

        private void OnWindowHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is not Window window)
            {
                return;
            }

            if (window.Handler?.PlatformView is not MauiWinUIWindow nativeWindow)
            {
                return;
            }

            window.HandlerChanged -= OnWindowHandlerChanged;
            _windowHandle = nativeWindow.WindowHandle;
            InitializeWindowPersistence(nativeWindow.AppWindow);
        }

        private static void InitializeWindowPersistence(AppWindow appWindow)
        {
            RestoreWindowPlacement(appWindow);

            appWindow.Changed += (_, __) => ScheduleWindowSave(appWindow);
        }

        private static void ScheduleWindowSave(AppWindow appWindow)
        {
            lock (WindowSaveLock)
            {
                _pendingAppWindow = appWindow;

                if (_windowSaveTimer == null)
                {
                    _windowSaveTimer = new System.Timers.Timer(WindowSaveDebounceMs)
                    {
                        AutoReset = false
                    };
                    _windowSaveTimer.Elapsed += (_, __) => FlushWindowSave();
                }

                _windowSaveTimer.Stop();
                _windowSaveTimer.Start();
            }
        }

        private static void FlushWindowSave()
        {
            AppWindow? appWindow;

            lock (WindowSaveLock)
            {
                appWindow = _pendingAppWindow;
                _pendingAppWindow = null;
            }

            if (appWindow == null)
            {
                return;
            }

            if (appWindow.DispatcherQueue is { } queue)
            {
                queue.TryEnqueue(() => SaveWindowPlacement(appWindow));
            }
            else
            {
                SaveWindowPlacement(appWindow);
            }
        }

        private static void RestoreWindowPlacement(AppWindow appWindow)
        {
            var scale = GetDpiScale();
            var widthDips = Preferences.Default.Get(WindowWidthDipKey, double.NaN);
            var heightDips = Preferences.Default.Get(WindowHeightDipKey, double.NaN);
            var xDips = Preferences.Default.Get(WindowXDipKey, double.NaN);
            var yDips = Preferences.Default.Get(WindowYDipKey, double.NaN);

            var width = double.NaN;
            var height = double.NaN;
            if (IsValidWindowSize(widthDips, heightDips))
            {
                width = widthDips * scale;
                height = heightDips * scale;
            }
            else
            {
                width = Preferences.Default.Get(WindowWidthKey, -1d);
                height = Preferences.Default.Get(WindowHeightKey, -1d);
            }

            var x = double.NaN;
            var y = double.NaN;
            if (IsValidWindowPosition(xDips, yDips))
            {
                x = xDips * scale;
                y = yDips * scale;
            }
            else
            {
                x = Preferences.Default.Get(WindowXKey, double.NaN);
                y = Preferences.Default.Get(WindowYKey, double.NaN);
            }

            var hasSize = IsValidWindowSize(width, height);
            var hasPosition = IsValidWindowPosition(x, y);
            var desiredSize = hasSize
                ? new SizeInt32((int)Math.Round(width), (int)Math.Round(height))
                : (SizeInt32?)null;
            var desiredPosition = hasPosition
                ? new PointInt32((int)Math.Round(x), (int)Math.Round(y))
                : (PointInt32?)null;

            ApplyWindowBounds(appWindow, desiredSize, desiredPosition);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                var savedState = (OverlappedPresenterState)Preferences.Default.Get(
                    WindowStateKey,
                    (int)OverlappedPresenterState.Restored);

                if (savedState == OverlappedPresenterState.Maximized)
                {
                    presenter.Maximize();
                }
                else
                {
                    presenter.Restore();
                }
            }
        }

        private static void SaveWindowPlacement(AppWindow appWindow)
        {
            if (appWindow.Presenter is not OverlappedPresenter presenter)
            {
                return;
            }

            var state = presenter.State;

            if (state == OverlappedPresenterState.Minimized)
            {
                return;
            }

            if (state == OverlappedPresenterState.Restored)
            {
                var size = appWindow.Size;
                var position = appWindow.Position;
                var width = (double)size.Width;
                var height = (double)size.Height;
                var scale = GetDpiScale();

                if (IsValidWindowSize(width, height))
                {
                    Preferences.Default.Set(WindowWidthKey, width);
                    Preferences.Default.Set(WindowHeightKey, height);

                    var widthDips = width / scale;
                    var heightDips = height / scale;
                    if (IsValidWindowSize(widthDips, heightDips))
                    {
                        Preferences.Default.Set(WindowWidthDipKey, widthDips);
                        Preferences.Default.Set(WindowHeightDipKey, heightDips);
                    }
                }

                Preferences.Default.Set(WindowXKey, (double)position.X);
                Preferences.Default.Set(WindowYKey, (double)position.Y);

                var xDips = position.X / scale;
                var yDips = position.Y / scale;
                if (IsValidWindowPosition(xDips, yDips))
                {
                    Preferences.Default.Set(WindowXDipKey, xDips);
                    Preferences.Default.Set(WindowYDipKey, yDips);
                }
            }

            Preferences.Default.Set(WindowStateKey, (int)state);
        }

        private static void ApplyWindowBounds(
            AppWindow appWindow,
            SizeInt32? desiredSize,
            PointInt32? desiredPosition)
        {
            if (desiredSize == null && desiredPosition == null)
            {
                return;
            }

            var anchorPoint = desiredPosition ?? appWindow.Position;
            var workArea = GetWorkArea(anchorPoint);
            var size = desiredSize ?? appWindow.Size;
            var maxWidth = Math.Max(1, workArea.Width);
            var maxHeight = Math.Max(1, workArea.Height);
            var clampedWidth = Math.Min(size.Width, maxWidth);
            var clampedHeight = Math.Min(size.Height, maxHeight);
            var finalSize = new SizeInt32(clampedWidth, clampedHeight);

            if (desiredSize.HasValue)
            {
                appWindow.Resize(finalSize);
            }

            if (desiredPosition.HasValue)
            {
                var minX = workArea.X;
                var minY = workArea.Y;
                var maxX = workArea.X + workArea.Width - finalSize.Width;
                var maxY = workArea.Y + workArea.Height - finalSize.Height;
                if (maxX < minX)
                {
                    maxX = minX;
                }

                if (maxY < minY)
                {
                    maxY = minY;
                }

                var clampedX = Math.Clamp(desiredPosition.Value.X, minX, maxX);
                var clampedY = Math.Clamp(desiredPosition.Value.Y, minY, maxY);
                appWindow.Move(new PointInt32(clampedX, clampedY));
            }
        }

        private static RectInt32 GetWorkArea(PointInt32 anchorPoint)
        {
            return DisplayArea.GetFromPoint(anchorPoint, DisplayAreaFallback.Primary).WorkArea;
        }

        private static double GetDpiScale()
        {
            var hwnd = _windowHandle;
            if (hwnd == IntPtr.Zero)
            {
                return 1d;
            }

            var dpi = GetDpiForWindow(hwnd);
            if (dpi == 0)
            {
                return 1d;
            }

            return dpi / 96d;
        }

        private static bool IsValidWindowPosition(double x, double y)
        {
            return !double.IsNaN(x)
                && !double.IsNaN(y);
        }

        private static bool IsValidWindowSize(double width, double height)
        {
            return !double.IsNaN(width)
                && !double.IsNaN(height)
                && width > 0
                && height > 0;
        }

        [DllImport("User32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);
#endif
    }
}
