using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DietSentry
{
    public sealed class FoodDatabaseService
    {
        private readonly string _databasePath;

        public FoodDatabaseService()
        {
            _databasePath = DatabaseInitializer.GetDatabasePath();
        }

        public Task<int> GetFoodCountAsync()
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Foods";
                var result = command.ExecuteScalar();
                return Convert.ToInt32(result);
            });
        }
    }
}
