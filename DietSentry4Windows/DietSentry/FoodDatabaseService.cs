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

        public Task<bool> InsertFoodAsync(Food food)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    "INSERT INTO Foods (" +
                    "FoodDescription, Energy, Protein, FatTotal, SaturatedFat, TransFat, " +
                    "PolyunsaturatedFat, MonounsaturatedFat, Carbohydrate, Sugars, DietaryFibre, " +
                    "SodiumNa, CalciumCa, PotassiumK, ThiaminB1, RiboflavinB2, NiacinB3, Folate, " +
                    "IronFe, MagnesiumMg, VitaminC, Caffeine, Cholesterol, Alcohol, notes" +
                    ") VALUES (" +
                    "@FoodDescription, @Energy, @Protein, @FatTotal, @SaturatedFat, @TransFat, " +
                    "@PolyunsaturatedFat, @MonounsaturatedFat, @Carbohydrate, @Sugars, @DietaryFibre, " +
                    "@SodiumNa, @CalciumCa, @PotassiumK, @ThiaminB1, @RiboflavinB2, @NiacinB3, @Folate, " +
                    "@IronFe, @MagnesiumMg, @VitaminC, @Caffeine, @Cholesterol, @Alcohol, @Notes" +
                    ")";
                command.Parameters.AddWithValue("@FoodDescription", food.FoodDescription);
                command.Parameters.AddWithValue("@Energy", RoundToTwoDecimals(food.Energy));
                command.Parameters.AddWithValue("@Protein", RoundToTwoDecimals(food.Protein));
                command.Parameters.AddWithValue("@FatTotal", RoundToTwoDecimals(food.FatTotal));
                command.Parameters.AddWithValue("@SaturatedFat", RoundToTwoDecimals(food.SaturatedFat));
                command.Parameters.AddWithValue("@TransFat", RoundToTwoDecimals(food.TransFat));
                command.Parameters.AddWithValue("@PolyunsaturatedFat", RoundToTwoDecimals(food.PolyunsaturatedFat));
                command.Parameters.AddWithValue("@MonounsaturatedFat", RoundToTwoDecimals(food.MonounsaturatedFat));
                command.Parameters.AddWithValue("@Carbohydrate", RoundToTwoDecimals(food.Carbohydrate));
                command.Parameters.AddWithValue("@Sugars", RoundToTwoDecimals(food.Sugars));
                command.Parameters.AddWithValue("@DietaryFibre", RoundToTwoDecimals(food.DietaryFibre));
                command.Parameters.AddWithValue("@SodiumNa", RoundToTwoDecimals(food.Sodium));
                command.Parameters.AddWithValue("@CalciumCa", RoundToTwoDecimals(food.CalciumCa));
                command.Parameters.AddWithValue("@PotassiumK", RoundToTwoDecimals(food.PotassiumK));
                command.Parameters.AddWithValue("@ThiaminB1", RoundToTwoDecimals(food.ThiaminB1));
                command.Parameters.AddWithValue("@RiboflavinB2", RoundToTwoDecimals(food.RiboflavinB2));
                command.Parameters.AddWithValue("@NiacinB3", RoundToTwoDecimals(food.NiacinB3));
                command.Parameters.AddWithValue("@Folate", RoundToTwoDecimals(food.Folate));
                command.Parameters.AddWithValue("@IronFe", RoundToTwoDecimals(food.IronFe));
                command.Parameters.AddWithValue("@MagnesiumMg", RoundToTwoDecimals(food.MagnesiumMg));
                command.Parameters.AddWithValue("@VitaminC", RoundToTwoDecimals(food.VitaminC));
                command.Parameters.AddWithValue("@Caffeine", RoundToTwoDecimals(food.Caffeine));
                command.Parameters.AddWithValue("@Cholesterol", RoundToTwoDecimals(food.Cholesterol));
                command.Parameters.AddWithValue("@Alcohol", RoundToTwoDecimals(food.Alcohol));
                command.Parameters.AddWithValue("@Notes", food.Notes);
                return command.ExecuteNonQuery() > 0;
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

        public Task<IReadOnlyList<EatenFood>> GetEatenFoodsAsync()
        {
            return Task.Run<IReadOnlyList<EatenFood>>(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Eaten ORDER BY EatenTs DESC";
                return ReadEatenFoods(command);
            });
        }

        public Task<bool> UpdateEatenFoodAsync(EatenFood eatenFood, double newAmount, DateTime newDateTime)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE Eaten SET " +
                    "DateEaten = @DateEaten, " +
                    "TimeEaten = @TimeEaten, " +
                    "EatenTs = @EatenTs, " +
                    "AmountEaten = @AmountEaten, " +
                    "Energy = @Energy, " +
                    "Protein = @Protein, " +
                    "FatTotal = @FatTotal, " +
                    "SaturatedFat = @SaturatedFat, " +
                    "TransFat = @TransFat, " +
                    "PolyunsaturatedFat = @PolyunsaturatedFat, " +
                    "MonounsaturatedFat = @MonounsaturatedFat, " +
                    "Carbohydrate = @Carbohydrate, " +
                    "Sugars = @Sugars, " +
                    "DietaryFibre = @DietaryFibre, " +
                    "SodiumNa = @SodiumNa, " +
                    "CalciumCa = @CalciumCa, " +
                    "PotassiumK = @PotassiumK, " +
                    "ThiaminB1 = @ThiaminB1, " +
                    "RiboflavinB2 = @RiboflavinB2, " +
                    "NiacinB3 = @NiacinB3, " +
                    "Folate = @Folate, " +
                    "IronFe = @IronFe, " +
                    "MagnesiumMg = @MagnesiumMg, " +
                    "VitaminC = @VitaminC, " +
                    "Caffeine = @Caffeine, " +
                    "Cholesterol = @Cholesterol, " +
                    "Alcohol = @Alcohol " +
                    "WHERE EatenId = @EatenId";

                var scale = eatenFood.AmountEaten <= 0
                    ? 0
                    : newAmount / eatenFood.AmountEaten;

                command.Parameters.AddWithValue("@DateEaten", newDateTime.ToString("d-MMM-yy", CultureInfo.CurrentCulture));
                command.Parameters.AddWithValue("@TimeEaten", newDateTime.ToString("HH:mm", CultureInfo.CurrentCulture));
                command.Parameters.AddWithValue("@EatenTs", CalculateEatenTimestampMinutes(newDateTime));
                command.Parameters.AddWithValue("@AmountEaten", newAmount);
                command.Parameters.AddWithValue("@Energy", RoundToTwoDecimals(eatenFood.Energy * scale));
                command.Parameters.AddWithValue("@Protein", RoundToTwoDecimals(eatenFood.Protein * scale));
                command.Parameters.AddWithValue("@FatTotal", RoundToTwoDecimals(eatenFood.FatTotal * scale));
                command.Parameters.AddWithValue("@SaturatedFat", RoundToTwoDecimals(eatenFood.SaturatedFat * scale));
                command.Parameters.AddWithValue("@TransFat", RoundToTwoDecimals(eatenFood.TransFat * scale));
                command.Parameters.AddWithValue("@PolyunsaturatedFat", RoundToTwoDecimals(eatenFood.PolyunsaturatedFat * scale));
                command.Parameters.AddWithValue("@MonounsaturatedFat", RoundToTwoDecimals(eatenFood.MonounsaturatedFat * scale));
                command.Parameters.AddWithValue("@Carbohydrate", RoundToTwoDecimals(eatenFood.Carbohydrate * scale));
                command.Parameters.AddWithValue("@Sugars", RoundToTwoDecimals(eatenFood.Sugars * scale));
                command.Parameters.AddWithValue("@DietaryFibre", RoundToTwoDecimals(eatenFood.DietaryFibre * scale));
                command.Parameters.AddWithValue("@SodiumNa", RoundToTwoDecimals(eatenFood.SodiumNa * scale));
                command.Parameters.AddWithValue("@CalciumCa", RoundToTwoDecimals(eatenFood.CalciumCa * scale));
                command.Parameters.AddWithValue("@PotassiumK", RoundToTwoDecimals(eatenFood.PotassiumK * scale));
                command.Parameters.AddWithValue("@ThiaminB1", RoundToTwoDecimals(eatenFood.ThiaminB1 * scale));
                command.Parameters.AddWithValue("@RiboflavinB2", RoundToTwoDecimals(eatenFood.RiboflavinB2 * scale));
                command.Parameters.AddWithValue("@NiacinB3", RoundToTwoDecimals(eatenFood.NiacinB3 * scale));
                command.Parameters.AddWithValue("@Folate", RoundToTwoDecimals(eatenFood.Folate * scale));
                command.Parameters.AddWithValue("@IronFe", RoundToTwoDecimals(eatenFood.IronFe * scale));
                command.Parameters.AddWithValue("@MagnesiumMg", RoundToTwoDecimals(eatenFood.MagnesiumMg * scale));
                command.Parameters.AddWithValue("@VitaminC", RoundToTwoDecimals(eatenFood.VitaminC * scale));
                command.Parameters.AddWithValue("@Caffeine", RoundToTwoDecimals(eatenFood.Caffeine * scale));
                command.Parameters.AddWithValue("@Cholesterol", RoundToTwoDecimals(eatenFood.Cholesterol * scale));
                command.Parameters.AddWithValue("@Alcohol", RoundToTwoDecimals(eatenFood.Alcohol * scale));
                command.Parameters.AddWithValue("@EatenId", eatenFood.EatenId);

                return command.ExecuteNonQuery() > 0;
            });
        }

        public Task<bool> DeleteEatenFoodAsync(int eatenId)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Eaten WHERE EatenId = @EatenId";
                command.Parameters.AddWithValue("@EatenId", eatenId);
                return command.ExecuteNonQuery() > 0;
            });
        }

        public Task<IReadOnlyList<WeightEntry>> GetWeightEntriesAsync()
        {
            return Task.Run<IReadOnlyList<WeightEntry>>(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT WeightId, DateWeight, Weight, Comments FROM Weight ORDER BY WeightId DESC";
                return ReadWeightEntries(command);
            });
        }

        public Task<bool> InsertWeightAsync(string dateWeight, double weight, string comments)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Weight (DateWeight, Weight, Comments)
                    VALUES (@DateWeight, @Weight, @Comments)";
                command.Parameters.AddWithValue("@DateWeight", dateWeight);
                command.Parameters.AddWithValue("@Weight", RoundToTwoDecimals(weight));
                command.Parameters.AddWithValue("@Comments", comments);
                return command.ExecuteNonQuery() > 0;
            });
        }

        public Task<bool> UpdateWeightAsync(int weightId, string dateWeight, double weight, string comments)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Weight
                    SET DateWeight = @DateWeight,
                        Weight = @Weight,
                        Comments = @Comments
                    WHERE WeightId = @WeightId";
                command.Parameters.AddWithValue("@DateWeight", dateWeight);
                command.Parameters.AddWithValue("@Weight", RoundToTwoDecimals(weight));
                command.Parameters.AddWithValue("@Comments", comments);
                command.Parameters.AddWithValue("@WeightId", weightId);
                return command.ExecuteNonQuery() > 0;
            });
        }

        public Task<bool> DeleteWeightAsync(int weightId)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Weight WHERE WeightId = @WeightId";
                command.Parameters.AddWithValue("@WeightId", weightId);
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

        private static IReadOnlyList<EatenFood> ReadEatenFoods(SqliteCommand command)
        {
            var foods = new List<EatenFood>();
            using var reader = command.ExecuteReader();
            var eatenIdOrdinal = reader.GetOrdinal("EatenId");
            var dateEatenOrdinal = reader.GetOrdinal("DateEaten");
            var timeEatenOrdinal = reader.GetOrdinal("TimeEaten");
            var eatenTsOrdinal = reader.GetOrdinal("EatenTs");
            var amountEatenOrdinal = reader.GetOrdinal("AmountEaten");
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

            while (reader.Read())
            {
                foods.Add(new EatenFood
                {
                    EatenId = reader.GetInt32(eatenIdOrdinal),
                    DateEaten = reader.GetString(dateEatenOrdinal),
                    TimeEaten = reader.GetString(timeEatenOrdinal),
                    EatenTs = reader.GetInt32(eatenTsOrdinal),
                    AmountEaten = reader.GetDouble(amountEatenOrdinal),
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
                    SodiumNa = reader.GetDouble(sodiumOrdinal),
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
                    Alcohol = reader.GetDouble(alcoholOrdinal)
                });
            }

            return foods;
        }

        private static IReadOnlyList<WeightEntry> ReadWeightEntries(SqliteCommand command)
        {
            var weights = new List<WeightEntry>();
            using var reader = command.ExecuteReader();
            var weightIdOrdinal = reader.GetOrdinal("WeightId");
            var dateWeightOrdinal = reader.GetOrdinal("DateWeight");
            var weightOrdinal = reader.GetOrdinal("Weight");
            var commentsOrdinal = reader.GetOrdinal("Comments");

            while (reader.Read())
            {
                weights.Add(new WeightEntry
                {
                    WeightId = reader.GetInt32(weightIdOrdinal),
                    DateWeight = reader.IsDBNull(dateWeightOrdinal) ? string.Empty : reader.GetString(dateWeightOrdinal),
                    Weight = reader.IsDBNull(weightOrdinal) ? 0 : reader.GetDouble(weightOrdinal),
                    Comments = reader.IsDBNull(commentsOrdinal) ? string.Empty : reader.GetString(commentsOrdinal)
                });
            }

            return weights;
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
