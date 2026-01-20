using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DietSentry
{
    public sealed class FoodDatabaseService
    {
        private const long ReferenceTimestampSeconds = 1672491600L;
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

        public Task<IReadOnlyList<Food>> GetFoodsAsync()
        {
            return Task.Run<IReadOnlyList<Food>>(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Foods";
                return ReadFoods(command);
            });
        }

        public Task<IReadOnlyList<Food>> SearchFoodsAsync(string query)
        {
            return Task.Run<IReadOnlyList<Food>>(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                var terms = query
                    .Split('|')
                    .Select(term => term.Trim())
                    .Where(term => !string.IsNullOrWhiteSpace(term))
                    .ToList();

                if (terms.Count == 0)
                {
                    command.CommandText = "SELECT * FROM Foods";
                    return ReadFoods(command);
                }

                var whereClauses = terms.Select((_, index) => $"FoodDescription LIKE @p{index}");
                command.CommandText = $"SELECT * FROM Foods WHERE {string.Join(" AND ", whereClauses)}";
                for (var index = 0; index < terms.Count; index++)
                {
                    command.Parameters.AddWithValue($"@p{index}", $"%{terms[index]}%");
                }

                return ReadFoods(command);
            });
        }

        public Task<bool> LogEatenFoodAsync(Food food, double amount, DateTime dateTime)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    "INSERT INTO Eaten (" +
                    "DateEaten, TimeEaten, EatenTs, AmountEaten, FoodDescription, " +
                    "Energy, Protein, FatTotal, SaturatedFat, TransFat, PolyunsaturatedFat, MonounsaturatedFat, " +
                    "Carbohydrate, Sugars, DietaryFibre, SodiumNa, CalciumCa, PotassiumK, ThiaminB1, RiboflavinB2, " +
                    "NiacinB3, Folate, IronFe, MagnesiumMg, VitaminC, Caffeine, Cholesterol, Alcohol" +
                    ") VALUES (" +
                    "@DateEaten, @TimeEaten, @EatenTs, @AmountEaten, @FoodDescription, " +
                    "@Energy, @Protein, @FatTotal, @SaturatedFat, @TransFat, @PolyunsaturatedFat, @MonounsaturatedFat, " +
                    "@Carbohydrate, @Sugars, @DietaryFibre, @SodiumNa, @CalciumCa, @PotassiumK, @ThiaminB1, @RiboflavinB2, " +
                    "@NiacinB3, @Folate, @IronFe, @MagnesiumMg, @VitaminC, @Caffeine, @Cholesterol, @Alcohol" +
                    ")";

                var scale = amount / 100.0;
                command.Parameters.AddWithValue("@DateEaten", dateTime.ToString("d-MMM-yy", CultureInfo.CurrentCulture));
                command.Parameters.AddWithValue("@TimeEaten", dateTime.ToString("HH:mm", CultureInfo.CurrentCulture));
                command.Parameters.AddWithValue("@EatenTs", CalculateEatenTimestampMinutes(dateTime));
                command.Parameters.AddWithValue("@AmountEaten", amount);
                command.Parameters.AddWithValue("@FoodDescription", food.FoodDescription);
                command.Parameters.AddWithValue("@Energy", RoundToTwoDecimals(food.Energy * scale));
                command.Parameters.AddWithValue("@Protein", RoundToTwoDecimals(food.Protein * scale));
                command.Parameters.AddWithValue("@FatTotal", RoundToTwoDecimals(food.FatTotal * scale));
                command.Parameters.AddWithValue("@SaturatedFat", RoundToTwoDecimals(food.SaturatedFat * scale));
                command.Parameters.AddWithValue("@TransFat", RoundToTwoDecimals(food.TransFat * scale));
                command.Parameters.AddWithValue("@PolyunsaturatedFat", RoundToTwoDecimals(food.PolyunsaturatedFat * scale));
                command.Parameters.AddWithValue("@MonounsaturatedFat", RoundToTwoDecimals(food.MonounsaturatedFat * scale));
                command.Parameters.AddWithValue("@Carbohydrate", RoundToTwoDecimals(food.Carbohydrate * scale));
                command.Parameters.AddWithValue("@Sugars", RoundToTwoDecimals(food.Sugars * scale));
                command.Parameters.AddWithValue("@DietaryFibre", RoundToTwoDecimals(food.DietaryFibre * scale));
                command.Parameters.AddWithValue("@SodiumNa", RoundToTwoDecimals(food.Sodium * scale));
                command.Parameters.AddWithValue("@CalciumCa", RoundToTwoDecimals(food.CalciumCa * scale));
                command.Parameters.AddWithValue("@PotassiumK", RoundToTwoDecimals(food.PotassiumK * scale));
                command.Parameters.AddWithValue("@ThiaminB1", RoundToTwoDecimals(food.ThiaminB1 * scale));
                command.Parameters.AddWithValue("@RiboflavinB2", RoundToTwoDecimals(food.RiboflavinB2 * scale));
                command.Parameters.AddWithValue("@NiacinB3", RoundToTwoDecimals(food.NiacinB3 * scale));
                command.Parameters.AddWithValue("@Folate", RoundToTwoDecimals(food.Folate * scale));
                command.Parameters.AddWithValue("@IronFe", RoundToTwoDecimals(food.IronFe * scale));
                command.Parameters.AddWithValue("@MagnesiumMg", RoundToTwoDecimals(food.MagnesiumMg * scale));
                command.Parameters.AddWithValue("@VitaminC", RoundToTwoDecimals(food.VitaminC * scale));
                command.Parameters.AddWithValue("@Caffeine", RoundToTwoDecimals(food.Caffeine * scale));
                command.Parameters.AddWithValue("@Cholesterol", RoundToTwoDecimals(food.Cholesterol * scale));
                command.Parameters.AddWithValue("@Alcohol", RoundToTwoDecimals(food.Alcohol * scale));

                return command.ExecuteNonQuery() > 0;
            });
        }

        private static IReadOnlyList<Food> ReadFoods(SqliteCommand command)
        {
            var foods = new List<Food>();
            using var reader = command.ExecuteReader();
            var foodIdOrdinal = reader.GetOrdinal("FoodId");
            var foodDescriptionOrdinal = reader.GetOrdinal("FoodDescription");
            var energyOrdinal = reader.GetOrdinal("Energy");
            var proteinOrdinal = reader.GetOrdinal("Protein");
            var fatTotalOrdinal = reader.GetOrdinal("FatTotal");
            var saturatedFatOrdinal = reader.GetOrdinal("SaturatedFat");
            var transFatOrdinal = reader.GetOrdinal("TransFat");
            var polyunsaturatedFatOrdinal = reader.GetOrdinal("PolyunsaturatedFat");
            var monounsaturatedFatOrdinal = reader.GetOrdinal("MonounsaturatedFat");
            var carbohydrateOrdinal = reader.GetOrdinal("Carbohydrate");
            var sugarsOrdinal = reader.GetOrdinal("Sugars");
            var dietaryFibreOrdinal = reader.GetOrdinal("DietaryFibre");
            var sodiumOrdinal = reader.GetOrdinal("SodiumNa");
            var calciumOrdinal = reader.GetOrdinal("CalciumCa");
            var potassiumOrdinal = reader.GetOrdinal("PotassiumK");
            var thiaminOrdinal = reader.GetOrdinal("ThiaminB1");
            var riboflavinOrdinal = reader.GetOrdinal("RiboflavinB2");
            var niacinOrdinal = reader.GetOrdinal("NiacinB3");
            var folateOrdinal = reader.GetOrdinal("Folate");
            var ironOrdinal = reader.GetOrdinal("IronFe");
            var magnesiumOrdinal = reader.GetOrdinal("MagnesiumMg");
            var vitaminCOrdinal = reader.GetOrdinal("VitaminC");
            var caffeineOrdinal = reader.GetOrdinal("Caffeine");
            var cholesterolOrdinal = reader.GetOrdinal("Cholesterol");
            var alcoholOrdinal = reader.GetOrdinal("Alcohol");
            var notesOrdinal = reader.GetOrdinal("notes");

            while (reader.Read())
            {
                foods.Add(new Food
                {
                    FoodId = reader.GetInt32(foodIdOrdinal),
                    FoodDescription = reader.GetString(foodDescriptionOrdinal),
                    Energy = reader.GetDouble(energyOrdinal),
                    Protein = reader.GetDouble(proteinOrdinal),
                    FatTotal = reader.GetDouble(fatTotalOrdinal),
                    SaturatedFat = reader.GetDouble(saturatedFatOrdinal),
                    TransFat = reader.GetDouble(transFatOrdinal),
                    PolyunsaturatedFat = reader.GetDouble(polyunsaturatedFatOrdinal),
                    MonounsaturatedFat = reader.GetDouble(monounsaturatedFatOrdinal),
                    Carbohydrate = reader.GetDouble(carbohydrateOrdinal),
                    Sugars = reader.GetDouble(sugarsOrdinal),
                    DietaryFibre = reader.GetDouble(dietaryFibreOrdinal),
                    Sodium = reader.GetDouble(sodiumOrdinal),
                    CalciumCa = reader.GetDouble(calciumOrdinal),
                    PotassiumK = reader.GetDouble(potassiumOrdinal),
                    ThiaminB1 = reader.GetDouble(thiaminOrdinal),
                    RiboflavinB2 = reader.GetDouble(riboflavinOrdinal),
                    NiacinB3 = reader.GetDouble(niacinOrdinal),
                    Folate = reader.GetDouble(folateOrdinal),
                    IronFe = reader.GetDouble(ironOrdinal),
                    MagnesiumMg = reader.GetDouble(magnesiumOrdinal),
                    VitaminC = reader.GetDouble(vitaminCOrdinal),
                    Caffeine = reader.GetDouble(caffeineOrdinal),
                    Cholesterol = reader.GetDouble(cholesterolOrdinal),
                    Alcohol = reader.GetDouble(alcoholOrdinal),
                    Notes = reader.IsDBNull(notesOrdinal) ? string.Empty : reader.GetString(notesOrdinal)
                });
            }

            return foods;
        }

        private static double RoundToTwoDecimals(double value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static int CalculateEatenTimestampMinutes(DateTime dateTime)
        {
            var seconds = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
            return (int)((seconds - ReferenceTimestampSeconds) / 60);
        }
    }
}
