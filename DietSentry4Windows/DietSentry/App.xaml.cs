using Microsoft.Extensions.DependencyInjection;

namespace DietSentry
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            _ = DatabaseInitializer.EnsureDatabaseAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
