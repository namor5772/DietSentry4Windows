using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace DietSentry
{
    public static class DatabaseInitializer
    {
        private const string DatabaseName = "foods.db";

        public static async Task EnsureDatabaseAsync()
        {
            var targetPath = Path.Combine(FileSystem.AppDataDirectory, DatabaseName);
            if (File.Exists(targetPath))
            {
                return;
            }

            Directory.CreateDirectory(FileSystem.AppDataDirectory);

            await using var sourceStream = await FileSystem.OpenAppPackageFileAsync(DatabaseName);
            await using var targetStream = File.Create(targetPath);
            await sourceStream.CopyToAsync(targetStream);
        }

        public static string GetDatabasePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, DatabaseName);
        }
    }
}
