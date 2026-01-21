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

        public Task<bool> UpdateFoodAsync(Food food)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE Foods SET " +
                    "FoodDescription = @FoodDescription, " +
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
                    "Alcohol = @Alcohol, " +
                    "notes = @Notes " +
                    "WHERE FoodId = @FoodId";
                command.Parameters.AddWithValue("@FoodId", food.FoodId);
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

        public Task<int?> InsertFoodReturningIdAsync(Food food)
        {
            return Task.Run<int?>(() =>
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
                if (command.ExecuteNonQuery() <= 0)
                {
                    return null;
                }

                using var idCommand = connection.CreateCommand();
                idCommand.CommandText = "SELECT last_insert_rowid();";
                var result = idCommand.ExecuteScalar();
                return result == null ? null : Convert.ToInt32(result, CultureInfo.InvariantCulture);
            });
        }

        public Task<Food?> GetFoodByIdAsync(int foodId)
        {
            return Task.Run<Food?>(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Foods WHERE FoodId = @FoodId";
                command.Parameters.AddWithValue("@FoodId", foodId);
                var foods = ReadFoods(command);
                return foods.Count > 0 ? foods[0] : null;
            });
        }

        public Task<bool> InsertRecipeItemFromFoodAsync(
            Food food,
            double amount,
            int foodId = 0,
            int copyFlag = 0)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    "INSERT INTO Recipe (" +
                    "FoodId, CopyFg, Amount, FoodDescription, Energy, Protein, FatTotal, " +
                    "SaturatedFat, TransFat, PolyunsaturatedFat, MonounsaturatedFat, Carbohydrate, " +
                    "Sugars, DietaryFibre, SodiumNa, CalciumCa, PotassiumK, ThiaminB1, RiboflavinB2, " +
                    "NiacinB3, Folate, IronFe, MagnesiumMg, VitaminC, Caffeine, Cholesterol, Alcohol" +
                    ") VALUES (" +
                    "@FoodId, @CopyFg, @Amount, @FoodDescription, @Energy, @Protein, @FatTotal, " +
                    "@SaturatedFat, @TransFat, @PolyunsaturatedFat, @MonounsaturatedFat, @Carbohydrate, " +
                    "@Sugars, @DietaryFibre, @SodiumNa, @CalciumCa, @PotassiumK, @ThiaminB1, @RiboflavinB2, " +
                    "@NiacinB3, @Folate, @IronFe, @MagnesiumMg, @VitaminC, @Caffeine, @Cholesterol, @Alcohol" +
                    ")";
                var scale = amount / 100.0;
                command.Parameters.AddWithValue("@FoodId", foodId);
                command.Parameters.AddWithValue("@CopyFg", copyFlag);
                command.Parameters.AddWithValue("@Amount", amount);
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

        public Task<IReadOnlyList<RecipeItem>> GetRecipeItemsAsync(int foodId = 0)
        {
            return Task.Run<IReadOnlyList<RecipeItem>>(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Recipe WHERE FoodId = @FoodId ORDER BY RecipeId DESC";
                command.Parameters.AddWithValue("@FoodId", foodId);
                return ReadRecipeItems(command);
            });
        }

        public Task<IReadOnlyList<RecipeItem>> GetCopiedRecipeItemsAsync(int foodId)
        {
            return Task.Run<IReadOnlyList<RecipeItem>>(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT * FROM Recipe WHERE CopyFg = 1 AND FoodId = @FoodId ORDER BY RecipeId DESC";
                command.Parameters.AddWithValue("@FoodId", foodId);
                return ReadRecipeItems(command);
            });
        }

        public Task<bool> UpdateRecipeItemAsync(RecipeItem recipeItem)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE Recipe SET " +
                    "FoodId = @FoodId, " +
                    "CopyFg = @CopyFg, " +
                    "Amount = @Amount, " +
                    "FoodDescription = @FoodDescription, " +
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
                    "WHERE RecipeId = @RecipeId";
                command.Parameters.AddWithValue("@RecipeId", recipeItem.RecipeId);
                command.Parameters.AddWithValue("@FoodId", recipeItem.FoodId);
                command.Parameters.AddWithValue("@CopyFg", recipeItem.CopyFg);
                command.Parameters.AddWithValue("@Amount", recipeItem.Amount);
                command.Parameters.AddWithValue("@FoodDescription", recipeItem.FoodDescription);
                command.Parameters.AddWithValue("@Energy", RoundToTwoDecimals(recipeItem.Energy));
                command.Parameters.AddWithValue("@Protein", RoundToTwoDecimals(recipeItem.Protein));
                command.Parameters.AddWithValue("@FatTotal", RoundToTwoDecimals(recipeItem.FatTotal));
                command.Parameters.AddWithValue("@SaturatedFat", RoundToTwoDecimals(recipeItem.SaturatedFat));
                command.Parameters.AddWithValue("@TransFat", RoundToTwoDecimals(recipeItem.TransFat));
                command.Parameters.AddWithValue("@PolyunsaturatedFat", RoundToTwoDecimals(recipeItem.PolyunsaturatedFat));
                command.Parameters.AddWithValue("@MonounsaturatedFat", RoundToTwoDecimals(recipeItem.MonounsaturatedFat));
                command.Parameters.AddWithValue("@Carbohydrate", RoundToTwoDecimals(recipeItem.Carbohydrate));
                command.Parameters.AddWithValue("@Sugars", RoundToTwoDecimals(recipeItem.Sugars));
                command.Parameters.AddWithValue("@DietaryFibre", RoundToTwoDecimals(recipeItem.DietaryFibre));
                command.Parameters.AddWithValue("@SodiumNa", RoundToTwoDecimals(recipeItem.Sodium));
                command.Parameters.AddWithValue("@CalciumCa", RoundToTwoDecimals(recipeItem.CalciumCa));
                command.Parameters.AddWithValue("@PotassiumK", RoundToTwoDecimals(recipeItem.PotassiumK));
                command.Parameters.AddWithValue("@ThiaminB1", RoundToTwoDecimals(recipeItem.ThiaminB1));
                command.Parameters.AddWithValue("@RiboflavinB2", RoundToTwoDecimals(recipeItem.RiboflavinB2));
                command.Parameters.AddWithValue("@NiacinB3", RoundToTwoDecimals(recipeItem.NiacinB3));
                command.Parameters.AddWithValue("@Folate", RoundToTwoDecimals(recipeItem.Folate));
                command.Parameters.AddWithValue("@IronFe", RoundToTwoDecimals(recipeItem.IronFe));
                command.Parameters.AddWithValue("@MagnesiumMg", RoundToTwoDecimals(recipeItem.MagnesiumMg));
                command.Parameters.AddWithValue("@VitaminC", RoundToTwoDecimals(recipeItem.VitaminC));
                command.Parameters.AddWithValue("@Caffeine", RoundToTwoDecimals(recipeItem.Caffeine));
                command.Parameters.AddWithValue("@Cholesterol", RoundToTwoDecimals(recipeItem.Cholesterol));
                command.Parameters.AddWithValue("@Alcohol", RoundToTwoDecimals(recipeItem.Alcohol));
                return command.ExecuteNonQuery() > 0;
            });
        }

        public Task<bool> DeleteRecipeItemAsync(int recipeId)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Recipe WHERE RecipeId = @RecipeId";
                command.Parameters.AddWithValue("@RecipeId", recipeId);
                return command.ExecuteNonQuery() > 0;
            });
        }

        public Task<bool> DeleteRecipesWithFoodIdZeroAsync()
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Recipe WHERE FoodId = 0";
                return command.ExecuteNonQuery() >= 0;
            });
        }

        public Task<bool> DeleteCopiedRecipesAsync(int foodId)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Recipe WHERE FoodId = @FoodId AND CopyFg = 1";
                command.Parameters.AddWithValue("@FoodId", foodId);
                return command.ExecuteNonQuery() >= 0;
            });
        }

        public Task<bool> UpdateRecipeFoodIdForTemporaryRecordsAsync(int newFoodId)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE Recipe SET FoodId = @FoodId WHERE FoodId = 0";
                command.Parameters.AddWithValue("@FoodId", newFoodId);
                return command.ExecuteNonQuery() > 0;
            });
        }

        public Task<bool> CopyRecipesForFoodAsync(int foodId)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var transaction = connection.BeginTransaction();
                try
                {
                    using var deleteCommand = connection.CreateCommand();
                    deleteCommand.Transaction = transaction;
                    deleteCommand.CommandText = "DELETE FROM Recipe WHERE FoodId = @FoodId AND CopyFg = 1";
                    deleteCommand.Parameters.AddWithValue("@FoodId", foodId);
                    deleteCommand.ExecuteNonQuery();

                    using var selectCommand = connection.CreateCommand();
                    selectCommand.Transaction = transaction;
                    selectCommand.CommandText =
                        "SELECT * FROM Recipe WHERE FoodId = @FoodId AND CopyFg != 1";
                    selectCommand.Parameters.AddWithValue("@FoodId", foodId);
                    var recipes = ReadRecipeItems(selectCommand);

                    foreach (var recipe in recipes)
                    {
                        using var insertCommand = connection.CreateCommand();
                        insertCommand.Transaction = transaction;
                        insertCommand.CommandText =
                            "INSERT INTO Recipe (" +
                            "FoodId, CopyFg, Amount, FoodDescription, Energy, Protein, FatTotal, " +
                            "SaturatedFat, TransFat, PolyunsaturatedFat, MonounsaturatedFat, Carbohydrate, " +
                            "Sugars, DietaryFibre, SodiumNa, CalciumCa, PotassiumK, ThiaminB1, RiboflavinB2, " +
                            "NiacinB3, Folate, IronFe, MagnesiumMg, VitaminC, Caffeine, Cholesterol, Alcohol" +
                            ") VALUES (" +
                            "@FoodId, 1, @Amount, @FoodDescription, @Energy, @Protein, @FatTotal, " +
                            "@SaturatedFat, @TransFat, @PolyunsaturatedFat, @MonounsaturatedFat, @Carbohydrate, " +
                            "@Sugars, @DietaryFibre, @SodiumNa, @CalciumCa, @PotassiumK, @ThiaminB1, @RiboflavinB2, " +
                            "@NiacinB3, @Folate, @IronFe, @MagnesiumMg, @VitaminC, @Caffeine, @Cholesterol, @Alcohol" +
                            ")";
                        insertCommand.Parameters.AddWithValue("@FoodId", recipe.FoodId);
                        insertCommand.Parameters.AddWithValue("@Amount", recipe.Amount);
                        insertCommand.Parameters.AddWithValue("@FoodDescription", recipe.FoodDescription);
                        insertCommand.Parameters.AddWithValue("@Energy", RoundToTwoDecimals(recipe.Energy));
                        insertCommand.Parameters.AddWithValue("@Protein", RoundToTwoDecimals(recipe.Protein));
                        insertCommand.Parameters.AddWithValue("@FatTotal", RoundToTwoDecimals(recipe.FatTotal));
                        insertCommand.Parameters.AddWithValue("@SaturatedFat", RoundToTwoDecimals(recipe.SaturatedFat));
                        insertCommand.Parameters.AddWithValue("@TransFat", RoundToTwoDecimals(recipe.TransFat));
                        insertCommand.Parameters.AddWithValue("@PolyunsaturatedFat", RoundToTwoDecimals(recipe.PolyunsaturatedFat));
                        insertCommand.Parameters.AddWithValue("@MonounsaturatedFat", RoundToTwoDecimals(recipe.MonounsaturatedFat));
                        insertCommand.Parameters.AddWithValue("@Carbohydrate", RoundToTwoDecimals(recipe.Carbohydrate));
                        insertCommand.Parameters.AddWithValue("@Sugars", RoundToTwoDecimals(recipe.Sugars));
                        insertCommand.Parameters.AddWithValue("@DietaryFibre", RoundToTwoDecimals(recipe.DietaryFibre));
                        insertCommand.Parameters.AddWithValue("@SodiumNa", RoundToTwoDecimals(recipe.Sodium));
                        insertCommand.Parameters.AddWithValue("@CalciumCa", RoundToTwoDecimals(recipe.CalciumCa));
                        insertCommand.Parameters.AddWithValue("@PotassiumK", RoundToTwoDecimals(recipe.PotassiumK));
                        insertCommand.Parameters.AddWithValue("@ThiaminB1", RoundToTwoDecimals(recipe.ThiaminB1));
                        insertCommand.Parameters.AddWithValue("@RiboflavinB2", RoundToTwoDecimals(recipe.RiboflavinB2));
                        insertCommand.Parameters.AddWithValue("@NiacinB3", RoundToTwoDecimals(recipe.NiacinB3));
                        insertCommand.Parameters.AddWithValue("@Folate", RoundToTwoDecimals(recipe.Folate));
                        insertCommand.Parameters.AddWithValue("@IronFe", RoundToTwoDecimals(recipe.IronFe));
                        insertCommand.Parameters.AddWithValue("@MagnesiumMg", RoundToTwoDecimals(recipe.MagnesiumMg));
                        insertCommand.Parameters.AddWithValue("@VitaminC", RoundToTwoDecimals(recipe.VitaminC));
                        insertCommand.Parameters.AddWithValue("@Caffeine", RoundToTwoDecimals(recipe.Caffeine));
                        insertCommand.Parameters.AddWithValue("@Cholesterol", RoundToTwoDecimals(recipe.Cholesterol));
                        insertCommand.Parameters.AddWithValue("@Alcohol", RoundToTwoDecimals(recipe.Alcohol));
                        insertCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            });
        }

        public Task<bool> ReplaceOriginalRecipesWithCopiesAsync(int foodId)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var transaction = connection.BeginTransaction();
                try
                {
                    using var deleteCommand = connection.CreateCommand();
                    deleteCommand.Transaction = transaction;
                    deleteCommand.CommandText = "DELETE FROM Recipe WHERE FoodId = @FoodId AND CopyFg = 0";
                    deleteCommand.Parameters.AddWithValue("@FoodId", foodId);
                    deleteCommand.ExecuteNonQuery();

                    using var updateCommand = connection.CreateCommand();
                    updateCommand.Transaction = transaction;
                    updateCommand.CommandText =
                        "UPDATE Recipe SET CopyFg = 0 WHERE FoodId = @FoodId AND CopyFg = 1";
                    updateCommand.Parameters.AddWithValue("@FoodId", foodId);
                    var updated = updateCommand.ExecuteNonQuery() >= 0;

                    transaction.Commit();
                    return updated;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            });
        }

        public Task<bool> DuplicateRecipesToFoodIdZeroAsync(int foodId)
        {
            return Task.Run(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                using var transaction = connection.BeginTransaction();
                try
                {
                    using var deleteCommand = connection.CreateCommand();
                    deleteCommand.Transaction = transaction;
                    deleteCommand.CommandText = "DELETE FROM Recipe WHERE FoodId = 0 AND CopyFg = 0";
                    deleteCommand.ExecuteNonQuery();

                    using var selectCommand = connection.CreateCommand();
                    selectCommand.Transaction = transaction;
                    selectCommand.CommandText = "SELECT * FROM Recipe WHERE FoodId = @FoodId";
                    selectCommand.Parameters.AddWithValue("@FoodId", foodId);
                    var recipes = ReadRecipeItems(selectCommand);

                    foreach (var recipe in recipes)
                    {
                        using var insertCommand = connection.CreateCommand();
                        insertCommand.Transaction = transaction;
                        insertCommand.CommandText =
                            "INSERT INTO Recipe (" +
                            "FoodId, CopyFg, Amount, FoodDescription, Energy, Protein, FatTotal, " +
                            "SaturatedFat, TransFat, PolyunsaturatedFat, MonounsaturatedFat, Carbohydrate, " +
                            "Sugars, DietaryFibre, SodiumNa, CalciumCa, PotassiumK, ThiaminB1, RiboflavinB2, " +
                            "NiacinB3, Folate, IronFe, MagnesiumMg, VitaminC, Caffeine, Cholesterol, Alcohol" +
                            ") VALUES (" +
                            "0, 0, @Amount, @FoodDescription, @Energy, @Protein, @FatTotal, " +
                            "@SaturatedFat, @TransFat, @PolyunsaturatedFat, @MonounsaturatedFat, @Carbohydrate, " +
                            "@Sugars, @DietaryFibre, @SodiumNa, @CalciumCa, @PotassiumK, @ThiaminB1, @RiboflavinB2, " +
                            "@NiacinB3, @Folate, @IronFe, @MagnesiumMg, @VitaminC, @Caffeine, @Cholesterol, @Alcohol" +
                            ")";
                        insertCommand.Parameters.AddWithValue("@Amount", recipe.Amount);
                        insertCommand.Parameters.AddWithValue("@FoodDescription", recipe.FoodDescription);
                        insertCommand.Parameters.AddWithValue("@Energy", RoundToTwoDecimals(recipe.Energy));
                        insertCommand.Parameters.AddWithValue("@Protein", RoundToTwoDecimals(recipe.Protein));
                        insertCommand.Parameters.AddWithValue("@FatTotal", RoundToTwoDecimals(recipe.FatTotal));
                        insertCommand.Parameters.AddWithValue("@SaturatedFat", RoundToTwoDecimals(recipe.SaturatedFat));
                        insertCommand.Parameters.AddWithValue("@TransFat", RoundToTwoDecimals(recipe.TransFat));
                        insertCommand.Parameters.AddWithValue("@PolyunsaturatedFat", RoundToTwoDecimals(recipe.PolyunsaturatedFat));
                        insertCommand.Parameters.AddWithValue("@MonounsaturatedFat", RoundToTwoDecimals(recipe.MonounsaturatedFat));
                        insertCommand.Parameters.AddWithValue("@Carbohydrate", RoundToTwoDecimals(recipe.Carbohydrate));
                        insertCommand.Parameters.AddWithValue("@Sugars", RoundToTwoDecimals(recipe.Sugars));
                        insertCommand.Parameters.AddWithValue("@DietaryFibre", RoundToTwoDecimals(recipe.DietaryFibre));
                        insertCommand.Parameters.AddWithValue("@SodiumNa", RoundToTwoDecimals(recipe.Sodium));
                        insertCommand.Parameters.AddWithValue("@CalciumCa", RoundToTwoDecimals(recipe.CalciumCa));
                        insertCommand.Parameters.AddWithValue("@PotassiumK", RoundToTwoDecimals(recipe.PotassiumK));
                        insertCommand.Parameters.AddWithValue("@ThiaminB1", RoundToTwoDecimals(recipe.ThiaminB1));
                        insertCommand.Parameters.AddWithValue("@RiboflavinB2", RoundToTwoDecimals(recipe.RiboflavinB2));
                        insertCommand.Parameters.AddWithValue("@NiacinB3", RoundToTwoDecimals(recipe.NiacinB3));
                        insertCommand.Parameters.AddWithValue("@Folate", RoundToTwoDecimals(recipe.Folate));
                        insertCommand.Parameters.AddWithValue("@IronFe", RoundToTwoDecimals(recipe.IronFe));
                        insertCommand.Parameters.AddWithValue("@MagnesiumMg", RoundToTwoDecimals(recipe.MagnesiumMg));
                        insertCommand.Parameters.AddWithValue("@VitaminC", RoundToTwoDecimals(recipe.VitaminC));
                        insertCommand.Parameters.AddWithValue("@Caffeine", RoundToTwoDecimals(recipe.Caffeine));
                        insertCommand.Parameters.AddWithValue("@Cholesterol", RoundToTwoDecimals(recipe.Cholesterol));
                        insertCommand.Parameters.AddWithValue("@Alcohol", RoundToTwoDecimals(recipe.Alcohol));
                        insertCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
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

        private static IReadOnlyList<RecipeItem> ReadRecipeItems(SqliteCommand command)
        {
            var recipes = new List<RecipeItem>();
            using var reader = command.ExecuteReader();
            var recipeIdOrdinal = reader.GetOrdinal("RecipeId");
            var foodIdOrdinal = reader.GetOrdinal("FoodId");
            var copyFgOrdinal = reader.GetOrdinal("CopyFg");
            var amountOrdinal = reader.GetOrdinal("Amount");
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
                recipes.Add(new RecipeItem
                {
                    RecipeId = reader.GetInt32(recipeIdOrdinal),
                    FoodId = reader.GetInt32(foodIdOrdinal),
                    CopyFg = reader.GetInt32(copyFgOrdinal),
                    Amount = reader.GetDouble(amountOrdinal),
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
                    Alcohol = reader.GetDouble(alcoholOrdinal)
                });
            }

            return recipes;
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
