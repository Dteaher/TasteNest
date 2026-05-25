using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace RecipeKeeper.Wpf.Data;

public static partial class Database
{
    public static void Initialize()
    {
        if (DbConnectionFactory.Settings.UseSqlLogin)
        {
            EnsureClientDatabaseIsReady();
            return;
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Users (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Email NVARCHAR(255) NOT NULL UNIQUE,
                    PasswordHash NVARCHAR(128) NOT NULL,
                    Role NVARCHAR(40) NOT NULL DEFAULT N'User',
                    Status BIT NOT NULL DEFAULT 1
                );
            END;

            IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Categories (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(120) NOT NULL UNIQUE
                );
            END;

            IF OBJECT_ID(N'dbo.Recipes', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Recipes (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Title NVARCHAR(255) NOT NULL,
                    Description NVARCHAR(MAX) NOT NULL,
                    Instructions NVARCHAR(MAX) NOT NULL,
                    CookingTime INT NOT NULL,
                    Servings INT NOT NULL,
                    CategoryId INT NOT NULL,
                    UserId INT NULL,
                    ImageUrl NVARCHAR(600) NOT NULL DEFAULT N'',
                    Difficulty NVARCHAR(60) NOT NULL DEFAULT N'Лёгкий',
                    Status NVARCHAR(40) NOT NULL DEFAULT N'Published',
                    ModerationComment NVARCHAR(500) NOT NULL DEFAULT N'',
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT FK_Recipes_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id),
                    CONSTRAINT FK_Recipes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
                );
            END;

            IF OBJECT_ID(N'dbo.Ingredients', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Ingredients (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    RecipeId INT NOT NULL,
                    Name NVARCHAR(160) NOT NULL,
                    Amount NVARCHAR(80) NULL,
                    CONSTRAINT FK_Ingredients_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Products (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL,
                    Name NVARCHAR(160) NOT NULL,
                    Quantity NVARCHAR(80) NULL,
                    ExpiresAt DATE NULL,
                    Unit NVARCHAR(40) NOT NULL DEFAULT N'',
                    Category NVARCHAR(100) NOT NULL DEFAULT N'',
                    Note NVARCHAR(500) NOT NULL DEFAULT N'',
                    CONSTRAINT FK_Products_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.Favorites', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Favorites (
                    UserId INT NOT NULL,
                    RecipeId INT NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_Favorites PRIMARY KEY (UserId, RecipeId),
                    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_Favorites_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.RecipeViews', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.RecipeViews (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL,
                    RecipeId INT NOT NULL,
                    ViewedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT FK_RecipeViews_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_RecipeViews_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.MealPlan', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.MealPlan (
                    UserId INT NOT NULL,
                    DayName NVARCHAR(80) NOT NULL,
                    RecipeId INT NOT NULL,
                    CONSTRAINT PK_MealPlan PRIMARY KEY (UserId, DayName),
                    CONSTRAINT FK_MealPlan_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_MealPlan_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.ShoppingItems', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ShoppingItems (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL,
                    Name NVARCHAR(160) NOT NULL,
                    Quantity NVARCHAR(500) NULL,
                    IsBought BIT NOT NULL DEFAULT 0,
                    CONSTRAINT FK_ShoppingItems_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.RecipeStats', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.RecipeStats (
                    RecipeId INT PRIMARY KEY,
                    CookCount INT NOT NULL DEFAULT 0,
                    Rating INT NOT NULL DEFAULT 5,
                    CONSTRAINT FK_RecipeStats_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.UserRecipeStats', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.UserRecipeStats (
                    UserId INT NOT NULL,
                    RecipeId INT NOT NULL,
                    CookCount INT NOT NULL DEFAULT 0,
                    LastCookedAt DATETIME2 NULL,
                    CONSTRAINT PK_UserRecipeStats PRIMARY KEY (UserId, RecipeId),
                    CONSTRAINT FK_UserRecipeStats_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_UserRecipeStats_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
                );
            END;
            """;
        command.ExecuteNonQuery();

        EnsureUserColumns(connection);
        EnsureRecipeColumns(connection);
        EnsureProductColumns(connection);
        Seed();
        RepairSeedRecipeInstructions(connection);
        RepairSeedRecipeImages(connection);
    }

    private static void EnsureClientDatabaseIsReady()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME IN ('Users', 'Recipes', 'Categories', 'Products')
            """;

        if (Convert.ToInt32(command.ExecuteScalar()) < 4)
        {
            throw new InvalidOperationException("База TasteNest на сервере ещё не подготовлена. Сначала запустите приложение на компьютере-сервере с локальными настройками.");
        }

        EnsureUserRecipeStatsTable(connection);
        EnsureUserColumns(connection);
        EnsureRecipeColumns(connection);
        EnsureProductColumns(connection);
        RepairSeedRecipeInstructions(connection);
        RepairSeedRecipeImages(connection);
    }

    public static (int Id, string Email, string Role)? Login(string email, string password)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Email, Role
            FROM dbo.Users
            WHERE Email = @email AND PasswordHash = @password AND Status = 1
            """;
        command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@password", HashPassword(password));

        using var reader = command.ExecuteReader();
        return reader.Read()
            ? (reader.GetInt32(0), reader.GetString(1), reader.GetString(2))
            : null;
    }

    public static (int Id, string Email, string Role)? GetUserById(int userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Email, Role
            FROM dbo.Users
            WHERE Id = @userId AND Status = 1
            """;
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        return reader.Read()
            ? (reader.GetInt32(0), reader.GetString(1), reader.GetString(2))
            : null;
    }

    public static bool Register(string email, string password)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.Users (Email, PasswordHash, Role, Status) VALUES (@email, @password, N'User', 1)";
        command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@password", HashPassword(password));

        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    public static bool UserExists(string email)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM dbo.Users WHERE Email = @email";
        command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public static List<ManagedUser> GetManagedUsers()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                u.Id,
                u.Email,
                u.Role,
                CAST(u.Status AS BIT),
                (SELECT COUNT(*) FROM dbo.Recipes r WHERE r.UserId = u.Id) AS RecipesCount,
                (SELECT COUNT(*) FROM dbo.Favorites f WHERE f.UserId = u.Id) AS FavoritesCount,
                (SELECT COUNT(*) FROM dbo.RecipeViews v WHERE v.UserId = u.Id) AS ViewsCount,
                (SELECT COUNT(*) FROM dbo.Products p WHERE p.UserId = u.Id) AS ProductsCount
            FROM dbo.Users u
            ORDER BY
                CASE u.Role WHEN N'Admin' THEN 0 WHEN N'Operator' THEN 1 ELSE 2 END,
                u.Email
            """;

        using var reader = command.ExecuteReader();
        var users = new List<ManagedUser>();
        while (reader.Read())
        {
            users.Add(new ManagedUser(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetBoolean(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetInt32(6),
                reader.GetInt32(7)));
        }

        return users;
    }

    public static void UpdateUserRole(int targetUserId, string role)
    {
        if (role is not ("User" or "Operator"))
        {
            throw new InvalidOperationException("Можно назначить только роль User или Operator. Admin в системе один.");
        }

        var target = GetManagedUserById(targetUserId)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        if (target.Role == "Admin" || target.Email.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Роль главного администратора менять нельзя.");
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.Users SET Role = @role WHERE Id = @targetUserId";
        command.Parameters.AddWithValue("@role", role);
        command.Parameters.AddWithValue("@targetUserId", targetUserId);
        command.ExecuteNonQuery();
    }

    public static void SetUserStatus(int targetUserId, bool isActive)
    {
        var target = GetManagedUserById(targetUserId)
            ?? throw new InvalidOperationException("Пользователь не найден.");

        if (target.Role == "Admin" || target.Email.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Главного администратора нельзя отключить.");
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.Users SET Status = @status WHERE Id = @targetUserId";
        command.Parameters.AddWithValue("@status", isActive);
        command.Parameters.AddWithValue("@targetUserId", targetUserId);
        command.ExecuteNonQuery();
    }

    public static AdminSystemSummary GetAdminSystemSummary()
    {
        var users = GetManagedUsers();

        using var connection = OpenConnection();
        int Scalar(string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt32(command.ExecuteScalar());
        }

        return new AdminSystemSummary(
            users.Count,
            users.Count(user => user.Role == "Admin"),
            users.Count(user => user.Role == "Operator"),
            users.Count(user => user.Role == "User"),
            users.Count(user => !user.IsActive),
            Scalar("SELECT COUNT(*) FROM dbo.Recipes"),
            Scalar("SELECT COUNT(*) FROM dbo.Products"),
            Scalar("SELECT COUNT(*) FROM dbo.Favorites"),
            Scalar("SELECT COUNT(*) FROM dbo.RecipeViews"),
            Scalar("SELECT ISNULL(SUM(CookCount), 0) FROM dbo.RecipeStats"));
    }

    public static List<OperatorQueueItem> GetOperatorQueue()
    {
        return GetManagedUsers()
            .Where(user => user.Role == "User")
            .Select(user =>
            {
                if (!user.IsActive)
                {
                    return new OperatorQueueItem(user.Id, user.Email, "Аккаунт заблокирован", "Высокий", "Пользователь не может войти. Проверьте причину блокировки.", true);
                }

                if (user.ProductsCount == 0 && user.FavoritesCount == 0 && user.ViewsCount == 0)
                {
                    return new OperatorQueueItem(user.Id, user.Email, "Нет активности", "Средний", "Пользователь зарегистрирован, но пока не начал работу с приложением.", true);
                }

                if (user.ViewsCount > 10 && user.FavoritesCount == 0)
                {
                    return new OperatorQueueItem(user.Id, user.Email, "Много просмотров без избранного", "Низкий", "Можно рекомендовать подборку популярных рецептов.", true);
                }

                if (user.ProductsCount > 0 && user.FavoritesCount == 0)
                {
                    return new OperatorQueueItem(user.Id, user.Email, "Есть продукты, нет рецептов", "Низкий", "Пользователь ведёт продукты, но не сохраняет рецепты.", true);
                }

                return new OperatorQueueItem(user.Id, user.Email, "Активность в норме", "Наблюдение", "Критичных действий не требуется.", true);
            })
            .OrderBy(item => item.Priority switch
            {
                "Высокий" => 0,
                "Средний" => 1,
                "Низкий" => 2,
                _ => 3
            })
            .ThenBy(item => item.Email)
            .ToList();
    }

    private static ManagedUser? GetManagedUserById(int userId)
    {
        return GetManagedUsers().FirstOrDefault(user => user.Id == userId);
    }

    private static bool IsAdminUser(int userId)
    {
        if (userId == 0)
        {
            return false;
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM dbo.Users WHERE Id = @userId AND Role = N'Admin'";
        command.Parameters.AddWithValue("@userId", userId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public static List<Category> GetCategories()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name FROM dbo.Categories ORDER BY Name";
        using var reader = command.ExecuteReader();
        var categories = new List<Category>();
        while (reader.Read())
        {
            categories.Add(new Category(reader.GetInt32(0), reader.GetString(1)));
        }

        return categories;
    }

    public static Recipe? GetRecipeById(int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.Id, r.Title, r.Description, r.Instructions, r.CookingTime, r.Servings, c.Name, ISNULL(r.ImageUrl, N''), ISNULL(r.Difficulty, N'Лёгкий'), ISNULL(r.Status, N'Published')
            FROM dbo.Recipes r
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            WHERE r.Id = @recipeId
            """;
        command.Parameters.AddWithValue("@recipeId", recipeId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadRecipe(reader) : null;
    }

    public static List<Recipe> SearchRecipes(string query, int? categoryId, IReadOnlyCollection<string> products)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        var sql = new StringBuilder("""
            SELECT DISTINCT r.Id, r.Title, r.Description, r.Instructions, r.CookingTime, r.Servings, c.Name, ISNULL(r.ImageUrl, N''), ISNULL(r.Difficulty, N'Лёгкий'), ISNULL(r.Status, N'Published')
            FROM dbo.Recipes r
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            LEFT JOIN dbo.Ingredients i ON i.RecipeId = r.Id
            WHERE ISNULL(r.Status, N'Published') = N'Published'
            """);

        if (!string.IsNullOrWhiteSpace(query))
        {
            sql.AppendLine(" AND (LOWER(r.Title) LIKE @query OR LOWER(r.Description) LIKE @query OR LOWER(i.Name) LIKE @query)");
            command.Parameters.AddWithValue("@query", $"%{query.Trim().ToLowerInvariant()}%");
        }

        if (categoryId.HasValue)
        {
            sql.AppendLine(" AND r.CategoryId = @categoryId");
            command.Parameters.AddWithValue("@categoryId", categoryId.Value);
        }

        var normalizedProducts = products.Select(item => item.Trim().ToLowerInvariant()).Where(item => item.Length > 0).ToList();
        if (normalizedProducts.Count > 0)
        {
            var names = normalizedProducts.Select((_, index) => $"@product{index}").ToList();
            sql.AppendLine($" AND EXISTS (SELECT 1 FROM dbo.Ingredients pi WHERE pi.RecipeId = r.Id AND LOWER(pi.Name) IN ({string.Join(", ", names)}))");
            for (var index = 0; index < normalizedProducts.Count; index++)
            {
                command.Parameters.AddWithValue(names[index], normalizedProducts[index]);
            }
        }

        sql.AppendLine(" ORDER BY r.Title");
        command.CommandText = sql.ToString();

        using var reader = command.ExecuteReader();
        var recipes = new List<Recipe>();
        while (reader.Read())
        {
            recipes.Add(ReadRecipe(reader));
        }

        return recipes;
    }

    public static List<(string Name, string Amount)> GetIngredients(int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, ISNULL(Amount, N'') FROM dbo.Ingredients WHERE RecipeId = @recipeId ORDER BY Name";
        command.Parameters.AddWithValue("@recipeId", recipeId);
        using var reader = command.ExecuteReader();
        var ingredients = new List<(string Name, string Amount)>();
        while (reader.Read())
        {
            ingredients.Add((reader.GetString(0), reader.GetString(1)));
        }

        return ingredients;
    }

    public static void AddRecipe(int userId, string title, string description, string instructions, int cookingTime, int servings, int categoryId, string ingredientsText)
    {
        AddRecipeExtended(userId, title, description, instructions, cookingTime, servings, categoryId, "Лёгкий", string.Empty, ingredientsText);
    }

    public static void AddRecipeExtended(int userId, string title, string description, string instructions, int cookingTime, int servings, int categoryId, string difficulty, string imageUrl, string ingredientsText)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var recipeCommand = connection.CreateCommand();
        recipeCommand.Transaction = transaction;
        var status = userId == 0 || IsAdminUser(userId) ? "Published" : "Pending";
        recipeCommand.CommandText = """
            INSERT INTO dbo.Recipes (Title, Description, Instructions, CookingTime, Servings, CategoryId, UserId, ImageUrl, Difficulty, Status, ModerationComment, CreatedAt)
            VALUES (@title, @description, @instructions, @cookingTime, @servings, @categoryId, @userId, @imageUrl, @difficulty, @status, N'', SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        recipeCommand.Parameters.AddWithValue("@title", title.Trim());
        recipeCommand.Parameters.AddWithValue("@description", description.Trim());
        recipeCommand.Parameters.AddWithValue("@instructions", instructions.Trim());
        recipeCommand.Parameters.AddWithValue("@cookingTime", cookingTime);
        recipeCommand.Parameters.AddWithValue("@servings", servings);
        recipeCommand.Parameters.AddWithValue("@categoryId", categoryId);
        recipeCommand.Parameters.AddWithValue("@userId", userId == 0 ? DBNull.Value : userId);
        recipeCommand.Parameters.AddWithValue("@imageUrl", imageUrl.Trim());
        recipeCommand.Parameters.AddWithValue("@difficulty", difficulty.Trim());
        recipeCommand.Parameters.AddWithValue("@status", status);
        var recipeId = Convert.ToInt32(recipeCommand.ExecuteScalar());

        foreach (var ingredient in ParseIngredients(ingredientsText))
        {
            using var ingredientCommand = connection.CreateCommand();
            ingredientCommand.Transaction = transaction;
            ingredientCommand.CommandText = "INSERT INTO dbo.Ingredients (RecipeId, Name, Amount) VALUES (@recipeId, @name, @amount)";
            ingredientCommand.Parameters.AddWithValue("@recipeId", recipeId);
            ingredientCommand.Parameters.AddWithValue("@name", ingredient.Name);
            ingredientCommand.Parameters.AddWithValue("@amount", ingredient.Amount);
            ingredientCommand.ExecuteNonQuery();
        }

        if (userId > 0 && status == "Published")
        {
            using var favoriteCommand = connection.CreateCommand();
            favoriteCommand.Transaction = transaction;
            favoriteCommand.CommandText = """
                IF NOT EXISTS (
                    SELECT 1 FROM dbo.Favorites
                    WHERE UserId = @userId AND RecipeId = @recipeId
                )
                BEGIN
                    INSERT INTO dbo.Favorites (UserId, RecipeId, CreatedAt)
                    VALUES (@userId, @recipeId, SYSUTCDATETIME());
                END
                """;
            favoriteCommand.Parameters.AddWithValue("@userId", userId);
            favoriteCommand.Parameters.AddWithValue("@recipeId", recipeId);
            favoriteCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static List<Product> GetProducts(int userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, ISNULL(Quantity, N''), ExpiresAt, ISNULL(Unit, N''), ISNULL(Category, N''), ISNULL(Note, N'')
            FROM dbo.Products
            WHERE UserId = @userId
            ORDER BY Name
            """;
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var products = new List<Product>();
        while (reader.Read())
        {
            var expiresAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
            products.Add(new Product(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), expiresAt, reader.GetString(4), reader.GetString(5), reader.GetString(6)));
        }

        return products;
    }

    public static void AddProduct(int userId, string name, string quantity, DateTime? expiresAt)
    {
        AddProduct(userId, name, quantity, expiresAt, string.Empty, string.Empty, string.Empty);
    }

    public static void AddProduct(int userId, string name, string quantity, DateTime? expiresAt, string unit, string category, string note)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Products (UserId, Name, Quantity, ExpiresAt, Unit, Category, Note)
            VALUES (@userId, @name, @quantity, @expiresAt, @unit, @category, @note)
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@name", name.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@quantity", quantity.Trim());
        command.Parameters.AddWithValue("@expiresAt", expiresAt.HasValue ? expiresAt.Value.Date : DBNull.Value);
        command.Parameters.AddWithValue("@unit", unit.Trim());
        command.Parameters.AddWithValue("@category", category.Trim());
        command.Parameters.AddWithValue("@note", note.Trim());
        command.ExecuteNonQuery();
    }

    public static void UpdateProduct(int userId, int productId, string name, string quantity, DateTime? expiresAt, string unit, string category, string note)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Products
            SET Name = @name, Quantity = @quantity, ExpiresAt = @expiresAt, Unit = @unit, Category = @category, Note = @note
            WHERE UserId = @userId AND Id = @productId
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@name", name.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@quantity", quantity.Trim());
        command.Parameters.AddWithValue("@expiresAt", expiresAt.HasValue ? expiresAt.Value.Date : DBNull.Value);
        command.Parameters.AddWithValue("@unit", unit.Trim());
        command.Parameters.AddWithValue("@category", category.Trim());
        command.Parameters.AddWithValue("@note", note.Trim());
        command.ExecuteNonQuery();
    }

    public static void DeleteProduct(int userId, int productId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.Products WHERE Id = @productId AND UserId = @userId";
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@userId", userId);
        command.ExecuteNonQuery();
    }

    public static bool IsFavorite(int userId, int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM dbo.Favorites WHERE UserId = @userId AND RecipeId = @recipeId";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@recipeId", recipeId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public static void ToggleFavorite(int userId, int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM dbo.Favorites WHERE UserId = @userId AND RecipeId = @recipeId)
                DELETE FROM dbo.Favorites WHERE UserId = @userId AND RecipeId = @recipeId;
            ELSE
                INSERT INTO dbo.Favorites (UserId, RecipeId, CreatedAt) VALUES (@userId, @recipeId, SYSUTCDATETIME());
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@recipeId", recipeId);
        command.ExecuteNonQuery();
    }

    public static void RemoveFavorite(int userId, int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.Favorites WHERE UserId = @userId AND RecipeId = @recipeId";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@recipeId", recipeId);
        command.ExecuteNonQuery();
    }

    public static void AddRecipeView(int userId, int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.RecipeViews (UserId, RecipeId, ViewedAt) VALUES (@userId, @recipeId, SYSUTCDATETIME())";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@recipeId", recipeId);
        command.ExecuteNonQuery();
    }

    public static List<RecipeActivity> GetRecipeViews(int userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP 20 r.Id, r.Title, v.ViewedAt
            FROM dbo.RecipeViews v
            JOIN dbo.Recipes r ON r.Id = v.RecipeId
            WHERE v.UserId = @userId
              AND ISNULL(r.Status, N'Published') = N'Published'
            ORDER BY v.ViewedAt DESC
            """;
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var views = new List<RecipeActivity>();
        while (reader.Read())
        {
            views.Add(new RecipeActivity(reader.GetInt32(0), reader.GetString(1), reader.GetDateTime(2).ToLocalTime()));
        }

        return views;
    }

    public static List<Recipe> GetFavoriteRecipes(int userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.Id, r.Title, r.Description, r.Instructions, r.CookingTime, r.Servings, c.Name, ISNULL(r.ImageUrl, N''), ISNULL(r.Difficulty, N'Лёгкий'), ISNULL(r.Status, N'Published')
            FROM dbo.Favorites f
            JOIN dbo.Recipes r ON r.Id = f.RecipeId
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            WHERE f.UserId = @userId
              AND ISNULL(r.Status, N'Published') = N'Published'
            ORDER BY f.CreatedAt DESC
            """;
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var recipes = new List<Recipe>();
        while (reader.Read())
        {
            recipes.Add(ReadRecipe(reader));
        }

        return recipes;
    }

    public static List<Recipe> GetPopularRecipes()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.Id, r.Title, r.Description, r.Instructions, r.CookingTime, r.Servings, c.Name, ISNULL(r.ImageUrl, N''), ISNULL(r.Difficulty, N'Лёгкий'), ISNULL(r.Status, N'Published')
            FROM dbo.Recipes r
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            LEFT JOIN dbo.RecipeStats s ON s.RecipeId = r.Id
            WHERE ISNULL(r.Status, N'Published') = N'Published'
            ORDER BY ISNULL(s.Rating, 5) DESC, ISNULL(s.CookCount, 0) DESC, r.Title
            """;

        using var reader = command.ExecuteReader();
        var recipes = new List<Recipe>();
        while (reader.Read())
        {
            recipes.Add(ReadRecipe(reader));
        }

        return recipes;
    }

    public static List<MealPlanItem> GetMealPlan(int userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.DayName, r.Title
            FROM dbo.MealPlan m
            JOIN dbo.Recipes r ON r.Id = m.RecipeId
            WHERE m.UserId = @userId
            ORDER BY m.DayName
            """;
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var items = new List<MealPlanItem>();
        while (reader.Read())
        {
            items.Add(new MealPlanItem(reader.GetString(0), reader.GetString(1)));
        }

        return items;
    }

    public static void SetMealPlanRecipe(int userId, string dayName, int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM dbo.MealPlan WHERE UserId = @userId AND DayName = @dayName)
                UPDATE dbo.MealPlan SET RecipeId = @recipeId WHERE UserId = @userId AND DayName = @dayName;
            ELSE
                INSERT INTO dbo.MealPlan (UserId, DayName, RecipeId) VALUES (@userId, @dayName, @recipeId);
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@dayName", dayName);
        command.Parameters.AddWithValue("@recipeId", recipeId);
        command.ExecuteNonQuery();
    }

    public static void RemoveMealPlanRecipe(int userId, string dayName)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.MealPlan WHERE UserId = @userId AND DayName = @dayName";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@dayName", dayName);
        command.ExecuteNonQuery();
    }

    public static void DeleteMealPlanRecipe(int userId, string dayName)
    {
        RemoveMealPlanRecipe(userId, dayName);
    }

    public static void IncrementCookCount(int recipeId) => IncrementCookCount(0, recipeId);

    public static void IncrementCookCount(int userId, int recipeId)
    {
        using var connection = OpenConnection();
        EnsureUserRecipeStatsTable(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM dbo.RecipeStats WHERE RecipeId = @recipeId)
                UPDATE dbo.RecipeStats SET CookCount = CookCount + 1 WHERE RecipeId = @recipeId;
            ELSE
                INSERT INTO dbo.RecipeStats (RecipeId, CookCount, Rating) VALUES (@recipeId, 1, 5);

            IF @userId > 0
            BEGIN
                IF EXISTS (SELECT 1 FROM dbo.UserRecipeStats WHERE UserId = @userId AND RecipeId = @recipeId)
                    UPDATE dbo.UserRecipeStats
                    SET CookCount = CookCount + 1,
                        LastCookedAt = SYSUTCDATETIME()
                    WHERE UserId = @userId AND RecipeId = @recipeId;
                ELSE
                    INSERT INTO dbo.UserRecipeStats (UserId, RecipeId, CookCount, LastCookedAt)
                    VALUES (@userId, @recipeId, 1, SYSUTCDATETIME());
            END;
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@recipeId", recipeId);
        command.ExecuteNonQuery();
    }

    public static List<ShoppingItem> GetShoppingItems(int userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, ISNULL(Quantity, N''), IsBought FROM dbo.ShoppingItems WHERE UserId = @userId ORDER BY IsBought, Name";
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var items = new List<ShoppingItem>();
        while (reader.Read())
        {
            items.Add(new ShoppingItem(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3)));
        }

        return items;
    }

    public static void AddShoppingItem(int userId, string name, string quantity)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.ShoppingItems (UserId, Name, Quantity, IsBought) VALUES (@userId, @name, @quantity, 0)";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@name", name.Trim());
        command.Parameters.AddWithValue("@quantity", quantity.Trim());
        command.ExecuteNonQuery();
    }

    public static void ToggleShoppingItem(int userId, int itemId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.ShoppingItems SET IsBought = CASE WHEN IsBought = 1 THEN 0 ELSE 1 END WHERE UserId = @userId AND Id = @itemId";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@itemId", itemId);
        command.ExecuteNonQuery();
    }

    public static void DeleteShoppingItem(int userId, int itemId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.ShoppingItems WHERE UserId = @userId AND Id = @itemId";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@itemId", itemId);
        command.ExecuteNonQuery();
    }

    public static void ClearShoppingItems(int userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.ShoppingItems WHERE UserId = @userId";
        command.Parameters.AddWithValue("@userId", userId);
        command.ExecuteNonQuery();
    }

    public static void GenerateShoppingFromFavorites(int userId)
    {
        using var connection = OpenConnection();
        ClearShoppingItems(connection, userId);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.Name, ISNULL(i.Amount, N'')
            FROM dbo.Favorites f
            JOIN dbo.Ingredients i ON i.RecipeId = f.RecipeId
            WHERE f.UserId = @userId
            """;
        command.Parameters.AddWithValue("@userId", userId);
        InsertMissingShoppingItems(connection, userId, ReadIngredientAmounts(command));
    }

    public static void GenerateShoppingFromRecipe(int userId, int recipeId)
    {
        using var connection = OpenConnection();
        ClearShoppingItems(connection, userId);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.Name, ISNULL(i.Amount, N'')
            FROM dbo.Ingredients i
            WHERE i.RecipeId = @recipeId
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@recipeId", recipeId);
        InsertMissingShoppingItems(connection, userId, ReadIngredientAmounts(command));
    }

    public static void GenerateShoppingFromMealPlan(int userId)
    {
        using var connection = OpenConnection();
        ClearShoppingItems(connection, userId);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.Name, ISNULL(i.Amount, N'')
            FROM dbo.MealPlan m
            JOIN dbo.Ingredients i ON i.RecipeId = m.RecipeId
            WHERE m.UserId = @userId
            """;
        command.Parameters.AddWithValue("@userId", userId);
        InsertMissingShoppingItems(connection, userId, ReadIngredientAmounts(command));
    }

    private static List<IngredientAmount> ReadIngredientAmounts(SqlCommand command)
    {
        using var reader = command.ExecuteReader();
        var ingredients = new List<IngredientAmount>();
        while (reader.Read())
        {
            ingredients.Add(new IngredientAmount(reader.GetString(0), reader.GetString(1)));
        }

        return ingredients;
    }

    private static void InsertMissingShoppingItems(SqlConnection connection, int userId, List<IngredientAmount> ingredients)
    {
        var products = GetProducts(connection, userId);
        var needs = BuildShoppingNeeds(ingredients);

        foreach (var need in needs)
        {
            var available = products
                .Where(product => NormalizeProductName(product.Name) == need.NameKey)
                .Select(product => ParseAmount($"{product.Quantity} {product.Unit}".Trim()))
                .Where(amount => amount.CanCompareWith(need.Amount))
                .Sum(amount => amount.BaseValue);

            var missing = need.Amount.BaseValue - available;
            if (missing <= 0)
            {
                continue;
            }

            AddShoppingItem(connection, userId, need.DisplayName, FormatAmount(missing, need.Amount));
        }
    }

    private static List<Product> GetProducts(SqlConnection connection, int userId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, ISNULL(Quantity, N''), ExpiresAt, ISNULL(Unit, N''), ISNULL(Category, N''), ISNULL(Note, N'')
            FROM dbo.Products
            WHERE UserId = @userId
            """;
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var products = new List<Product>();
        while (reader.Read())
        {
            var expiresAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
            products.Add(new Product(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), expiresAt, reader.GetString(4), reader.GetString(5), reader.GetString(6)));
        }

        return products;
    }

    private static List<ShoppingNeed> BuildShoppingNeeds(IEnumerable<IngredientAmount> ingredients)
    {
        var needs = new Dictionary<string, ShoppingNeed>();
        var fallbackNeeds = new Dictionary<string, List<string>>();
        var names = new Dictionary<string, string>();

        foreach (var ingredient in ingredients)
        {
            var nameKey = NormalizeProductName(ingredient.Name);
            if (string.IsNullOrWhiteSpace(nameKey))
            {
                continue;
            }

            names.TryAdd(nameKey, ingredient.Name.Trim());
            var amount = ParseAmount(ingredient.Amount);
            if (!amount.IsKnown)
            {
                if (!fallbackNeeds.TryGetValue(nameKey, out var rawAmounts))
                {
                    rawAmounts = new List<string>();
                    fallbackNeeds[nameKey] = rawAmounts;
                }

                rawAmounts.Add(string.IsNullOrWhiteSpace(ingredient.Amount) ? "по вкусу" : ingredient.Amount.Trim());
                continue;
            }

            var key = $"{nameKey}|{amount.UnitGroup}";
            if (needs.TryGetValue(key, out var need))
            {
                need.Amount = need.Amount with { BaseValue = need.Amount.BaseValue + amount.BaseValue };
            }
            else
            {
                needs[key] = new ShoppingNeed(nameKey, ingredient.Name.Trim(), amount);
            }
        }

        foreach (var fallback in fallbackNeeds)
        {
            if (needs.Keys.Any(key => key.StartsWith($"{fallback.Key}|", StringComparison.Ordinal)))
            {
                continue;
            }

            needs[$"{fallback.Key}|raw"] = new ShoppingNeed(
                fallback.Key,
                names.GetValueOrDefault(fallback.Key, fallback.Key),
                new ParsedAmount(false, fallback.Value.Count, "raw", "раз", 1, string.Join(", ", fallback.Value)));
        }

        return needs.Values.ToList();
    }

    private static ParsedAmount ParseAmount(string amountText)
    {
        var text = amountText.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text))
        {
            return ParsedAmount.Unknown(text);
        }

        var match = Regex.Match(text, @"(?<value>\d+(?:[,.]\d+)?)\s*(?<unit>[а-яё. ]*)", RegexOptions.IgnoreCase);
        if (!match.Success || !decimal.TryParse(match.Groups["value"].Value.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            return ParsedAmount.Unknown(text);
        }

        var unit = NormalizeUnit(match.Groups["unit"].Value);
        if (unit is null)
        {
            return ParsedAmount.Unknown(text);
        }

        return new ParsedAmount(true, value * unit.Value.Factor, unit.Value.Group, unit.Value.DisplayUnit, unit.Value.Factor, text);
    }

    private static (string Group, string DisplayUnit, decimal Factor)? NormalizeUnit(string unitText)
    {
        var unit = Regex.Replace(unitText.Trim().ToLowerInvariant(), @"\s+", " ").Trim('.', ' ');

        if (unit is "" or "шт" or "штука" or "штуки" or "штук")
        {
            return ("piece", "шт.", 1m);
        }

        if (unit is "г" or "гр" or "грамм" or "грамма" or "граммов")
        {
            return ("mass", "г", 1m);
        }

        if (unit is "кг" or "килограмм" or "килограмма" or "килограммов")
        {
            return ("mass", "кг", 1000m);
        }

        if (unit is "мл" or "миллилитр" or "миллилитра" or "миллилитров")
        {
            return ("volume", "мл", 1m);
        }

        if (unit is "л" or "литр" or "литра" or "литров")
        {
            return ("volume", "л", 1000m);
        }

        if (unit is "ч. л" or "ч л" or "чайная ложка" or "чайные ложки")
        {
            return ("teaspoon", "ч. л.", 1m);
        }

        if (unit is "ст. л" or "ст л" or "столовая ложка" or "столовые ложки")
        {
            return ("tablespoon", "ст. л.", 1m);
        }

        if (unit is "уп" or "упаковка" or "упаковки")
        {
            return ("pack", "уп.", 1m);
        }

        return null;
    }

    private static string NormalizeProductName(string name)
    {
        var normalized = Regex.Replace(name.Trim().ToLowerInvariant(), @"\s+", " ");
        normalized = normalized.Trim('.', ',', ';', ':', '-', ' ');

        return ProductNameAliases.TryGetValue(normalized, out var alias)
            ? alias
            : normalized;
    }

    private static readonly Dictionary<string, string> ProductNameAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["картошка"] = "картофель",
        ["картошки"] = "картофель",
        ["картофеля"] = "картофель",
        ["помидор"] = "томаты",
        ["помидоры"] = "томаты",
        ["помидоров"] = "томаты",
        ["томат"] = "томаты",
        ["томата"] = "томаты",
        ["яйца"] = "яйцо",
        ["яиц"] = "яйцо",
        ["луковица"] = "лук",
        ["луковицы"] = "лук",
        ["морковка"] = "морковь",
        ["морковки"] = "морковь",
        ["шампиньон"] = "грибы",
        ["шампиньоны"] = "грибы",
        ["гриб"] = "грибы",
        ["чеснока"] = "чеснок",
        ["зубчик чеснока"] = "чеснок",
        ["зубчики чеснока"] = "чеснок",
        ["зелень"] = "зелень",
        ["зелени"] = "зелень"
    };

    private static string FormatAmount(decimal missingBaseValue, ParsedAmount original)
    {
        if (!original.IsKnown)
        {
            return original.RawText;
        }

        if (original.UnitGroup == "mass" && original.DisplayUnit == "г" && missingBaseValue >= 1000)
        {
            return $"{FormatDecimal(missingBaseValue / 1000m)} кг";
        }

        if (original.UnitGroup == "volume" && original.DisplayUnit == "мл" && missingBaseValue >= 1000)
        {
            return $"{FormatDecimal(missingBaseValue / 1000m)} л";
        }

        return $"{FormatDecimal(missingBaseValue / original.DisplayFactor)} {original.DisplayUnit}";
    }

    private static string FormatDecimal(decimal value)
    {
        return value % 1 == 0
            ? value.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
    }

    private static void AddShoppingItem(SqlConnection connection, int userId, string name, string quantity)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.ShoppingItems (UserId, Name, Quantity, IsBought) VALUES (@userId, @name, @quantity, 0)";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@name", name.Trim());
        command.Parameters.AddWithValue("@quantity", quantity.Trim());
        command.ExecuteNonQuery();
    }

    private static void ClearShoppingItems(SqlConnection connection, int userId)
    {
        using var clearCommand = connection.CreateCommand();
        clearCommand.CommandText = "DELETE FROM dbo.ShoppingItems WHERE UserId = @userId";
        clearCommand.Parameters.AddWithValue("@userId", userId);
        clearCommand.ExecuteNonQuery();
    }

    private sealed record IngredientAmount(string Name, string Amount);

    private sealed record ShoppingNeed(string NameKey, string DisplayName, ParsedAmount Amount)
    {
        public ParsedAmount Amount { get; set; } = Amount;
    }

    private sealed record ParsedAmount(bool IsKnown, decimal BaseValue, string UnitGroup, string DisplayUnit, decimal DisplayFactor, string RawText)
    {
        public static ParsedAmount Unknown(string rawText) => new(false, 0, "raw", string.Empty, 1, rawText);

        public bool CanCompareWith(ParsedAmount other)
        {
            return IsKnown && other.IsKnown && UnitGroup == other.UnitGroup;
        }
    }

    public static List<StatItem> GetCategoryStats() => GetCategoryStats(null);

    public static List<StatItem> GetCategoryStats(int? userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        if (userId.HasValue)
        {
            EnsureUserRecipeStatsTable(connection);
            command.CommandText = """
                SELECT TOP 8 c.Name, COUNT(*)
                FROM (
                    SELECT RecipeId FROM dbo.RecipeViews WHERE UserId = @userId
                    UNION ALL
                    SELECT RecipeId FROM dbo.Favorites WHERE UserId = @userId
                    UNION ALL
                    SELECT RecipeId FROM dbo.UserRecipeStats WHERE UserId = @userId AND CookCount > 0
                ) activity
                JOIN dbo.Recipes r ON r.Id = activity.RecipeId
                JOIN dbo.Categories c ON c.Id = r.CategoryId
                GROUP BY c.Name
                ORDER BY COUNT(*) DESC, c.Name
                """;
            command.Parameters.AddWithValue("@userId", userId.Value);
        }
        else
        {
            command.CommandText = """
                SELECT TOP 8 c.Name, COUNT(r.Id)
                FROM dbo.Categories c
                JOIN dbo.Recipes r ON r.CategoryId = c.Id
                GROUP BY c.Name
                ORDER BY COUNT(r.Id) DESC
                """;
        }
        using var reader = command.ExecuteReader();
        var stats = new List<StatItem>();
        while (reader.Read())
        {
            stats.Add(new StatItem(reader.GetString(0), reader.GetInt32(1)));
        }

        return stats;
    }

    public static List<StatItem> GetIngredientStats() => GetIngredientStats(null);

    public static List<StatItem> GetIngredientStats(int? userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        if (userId.HasValue)
        {
            EnsureUserRecipeStatsTable(connection);
            command.CommandText = """
                SELECT TOP 8 i.Name, COUNT(*)
                FROM (
                    SELECT RecipeId FROM dbo.RecipeViews WHERE UserId = @userId
                    UNION ALL
                    SELECT RecipeId FROM dbo.Favorites WHERE UserId = @userId
                    UNION ALL
                    SELECT RecipeId FROM dbo.UserRecipeStats WHERE UserId = @userId AND CookCount > 0
                ) activity
                JOIN dbo.Ingredients i ON i.RecipeId = activity.RecipeId
                GROUP BY i.Name
                ORDER BY COUNT(*) DESC, i.Name
                """;
            command.Parameters.AddWithValue("@userId", userId.Value);
        }
        else
        {
            command.CommandText = """
                SELECT TOP 8 Name, COUNT(*)
                FROM dbo.Ingredients
                GROUP BY Name
                ORDER BY COUNT(*) DESC
                """;
        }
        using var reader = command.ExecuteReader();
        var stats = new List<StatItem>();
        while (reader.Read())
        {
            stats.Add(new StatItem(reader.GetString(0), reader.GetInt32(1)));
        }

        return stats;
    }

    public static StatisticsSummary GetStatisticsSummary(int userId)
    {
        using var connection = OpenConnection();
        EnsureUserRecipeStatsTable(connection);

        int Scalar(string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@userId", userId);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        return new StatisticsSummary(
            Scalar("SELECT COUNT(*) FROM dbo.Recipes WHERE UserId = @userId"),
            Scalar("SELECT COUNT(*) FROM dbo.Favorites WHERE UserId = @userId"),
            Scalar("SELECT COUNT(*) FROM dbo.Products WHERE UserId = @userId"),
            Scalar("SELECT ISNULL(SUM(CookCount), 0) FROM dbo.UserRecipeStats WHERE UserId = @userId"));
    }

    public static List<RecipePopularityStat> GetRecipePopularityStats() => GetRecipePopularityStats(null);

    public static List<RecipePopularityStat> GetRecipePopularityStats(int? userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        if (userId.HasValue)
        {
            EnsureUserRecipeStatsTable(connection);
            command.CommandText = """
                SELECT TOP 6
                    r.Title,
                    COUNT(v.Id) + CASE WHEN f.RecipeId IS NULL THEN 0 ELSE 1 END + ISNULL(us.CookCount, 0) AS ActivityCount,
                    ISNULL(us.CookCount, 0) AS CookCount
                FROM dbo.Recipes r
                LEFT JOIN dbo.RecipeViews v ON v.RecipeId = r.Id AND v.UserId = @userId
                LEFT JOIN dbo.Favorites f ON f.RecipeId = r.Id AND f.UserId = @userId
                LEFT JOIN dbo.UserRecipeStats us ON us.RecipeId = r.Id AND us.UserId = @userId
                GROUP BY r.Id, r.Title, f.RecipeId, us.CookCount
                HAVING COUNT(v.Id) > 0 OR f.RecipeId IS NOT NULL OR ISNULL(us.CookCount, 0) > 0
                ORDER BY ActivityCount DESC, CookCount DESC, r.Title
                """;
            command.Parameters.AddWithValue("@userId", userId.Value);
        }
        else
        {
            command.CommandText = """
                SELECT TOP 6 r.Title, ISNULL(s.CookCount, 0), ISNULL(s.Rating, 5)
                FROM dbo.Recipes r
                LEFT JOIN dbo.RecipeStats s ON s.RecipeId = r.Id
                ORDER BY ISNULL(s.CookCount, 0) DESC, ISNULL(s.Rating, 5) DESC, r.Title
                """;
        }

        using var reader = command.ExecuteReader();
        var stats = new List<RecipePopularityStat>();
        while (reader.Read())
        {
            stats.Add(new RecipePopularityStat(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)));
        }

        return stats;
    }

    public static ProfileSummary GetProfileSummary(int userId)
    {
        using var connection = OpenConnection();
        int Scalar(string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@userId", userId);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        return new ProfileSummary(
            Scalar("SELECT COUNT(*) FROM dbo.Recipes WHERE UserId = @userId"),
            Scalar("SELECT COUNT(*) FROM dbo.Favorites WHERE UserId = @userId"),
            Scalar("SELECT COUNT(*) FROM dbo.RecipeViews WHERE UserId = @userId"),
            Scalar("SELECT COUNT(*) FROM dbo.Products WHERE UserId = @userId"));
    }

    public static List<UserRecipeSummary> GetUserRecipes(int userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.Id,
                   r.Title,
                   c.Name,
                   ISNULL(r.Status, N'Published'),
                   ISNULL(r.CreatedAt, SYSUTCDATETIME()),
                   ISNULL(r.ModerationComment, N'')
            FROM dbo.Recipes r
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            WHERE r.UserId = @userId
            ORDER BY r.CreatedAt DESC, r.Title
            """;
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var recipes = new List<UserRecipeSummary>();
        while (reader.Read())
        {
            recipes.Add(new UserRecipeSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDateTime(4).ToLocalTime(),
                reader.GetString(5)));
        }

        return recipes;
    }

    public static List<RecipeModerationItem> GetPendingRecipes()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.Id,
                   r.Title,
                   r.Description,
                   c.Name,
                   ISNULL(u.Email, N'неизвестно'),
                   ISNULL(r.CreatedAt, SYSUTCDATETIME())
            FROM dbo.Recipes r
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            LEFT JOIN dbo.Users u ON u.Id = r.UserId
            WHERE ISNULL(r.Status, N'Published') = N'Pending'
            ORDER BY r.CreatedAt ASC, r.Title
            """;

        using var reader = command.ExecuteReader();
        var recipes = new List<RecipeModerationItem>();
        while (reader.Read())
        {
            recipes.Add(new RecipeModerationItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetDateTime(5).ToLocalTime()));
        }

        return recipes;
    }

    public static List<AdminRecipeItem> GetAdminRecipes()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.Id,
                   r.Title,
                   c.Name,
                   ISNULL(u.Email, N'системный рецепт'),
                   ISNULL(r.Status, N'Published'),
                   ISNULL(r.CreatedAt, SYSUTCDATETIME())
            FROM dbo.Recipes r
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            LEFT JOIN dbo.Users u ON u.Id = r.UserId
            ORDER BY
                CASE ISNULL(r.Status, N'Published')
                    WHEN N'Pending' THEN 0
                    WHEN N'Published' THEN 1
                    WHEN N'Rejected' THEN 2
                    ELSE 3
                END,
                ISNULL(r.CreatedAt, SYSUTCDATETIME()) DESC,
                r.Title
            """;

        using var reader = command.ExecuteReader();
        var recipes = new List<AdminRecipeItem>();
        while (reader.Read())
        {
            recipes.Add(new AdminRecipeItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetDateTime(5).ToLocalTime()));
        }

        return recipes;
    }

    public static void DeleteRecipe(int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.Recipes WHERE Id = @recipeId";
        command.Parameters.AddWithValue("@recipeId", recipeId);
        command.ExecuteNonQuery();
    }

    public static void PublishRecipe(int recipeId)
    {
        UpdateRecipeModerationStatus(recipeId, "Published", string.Empty);
        AddRecipeToAuthorFavorites(recipeId);
        EnsureInitialStats();
    }

    public static void RejectRecipe(int recipeId, string comment)
    {
        UpdateRecipeModerationStatus(recipeId, "Rejected", string.IsNullOrWhiteSpace(comment)
            ? "Рецепт отклонён администратором."
            : comment.Trim());
    }

    private static void UpdateRecipeModerationStatus(int recipeId, string status, string comment)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Recipes
            SET Status = @status,
                ModerationComment = @comment
            WHERE Id = @recipeId
              AND UserId IS NOT NULL
            """;
        command.Parameters.AddWithValue("@recipeId", recipeId);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@comment", comment);
        command.ExecuteNonQuery();
    }

    private static void AddRecipeToAuthorFavorites(int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @authorId INT = (
                SELECT UserId
                FROM dbo.Recipes
                WHERE Id = @recipeId
                  AND UserId IS NOT NULL
            );

            IF @authorId IS NOT NULL
               AND NOT EXISTS (
                   SELECT 1
                   FROM dbo.Favorites
                   WHERE UserId = @authorId AND RecipeId = @recipeId
               )
            BEGIN
                INSERT INTO dbo.Favorites (UserId, RecipeId, CreatedAt)
                VALUES (@authorId, @recipeId, SYSUTCDATETIME());
            END
            """;
        command.Parameters.AddWithValue("@recipeId", recipeId);
        command.ExecuteNonQuery();
    }

    private static SqlConnection OpenConnection()
    {
        return DbConnectionFactory.OpenConnection();
    }

    private static void EnsureUserColumns(SqlConnection connection)
    {
        EnsureColumn(connection, "Users", "Role", "NVARCHAR(40) NOT NULL DEFAULT N'User'");
        EnsureColumn(connection, "Users", "Status", "BIT NOT NULL DEFAULT 1");
    }

    private static void EnsureRecipeColumns(SqlConnection connection)
    {
        EnsureColumn(connection, "Recipes", "ImageUrl", "NVARCHAR(600) NOT NULL DEFAULT N''");
        EnsureColumn(connection, "Recipes", "Difficulty", "NVARCHAR(60) NOT NULL DEFAULT N'Лёгкий'");
        EnsureColumn(connection, "Recipes", "Status", "NVARCHAR(40) NOT NULL DEFAULT N'Published'");
        EnsureColumn(connection, "Recipes", "ModerationComment", "NVARCHAR(500) NOT NULL DEFAULT N''");
        EnsureColumn(connection, "Recipes", "CreatedAt", "DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()");

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Recipes
            SET Status = N'Published'
            WHERE Status IS NULL OR Status = N'';
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureProductColumns(SqlConnection connection)
    {
        EnsureColumn(connection, "Products", "Unit", "NVARCHAR(40) NOT NULL DEFAULT N''");
        EnsureColumn(connection, "Products", "Category", "NVARCHAR(100) NOT NULL DEFAULT N''");
        EnsureColumn(connection, "Products", "Note", "NVARCHAR(500) NOT NULL DEFAULT N''");
    }

    private static void EnsureUserRecipeStatsTable(SqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID(N'dbo.UserRecipeStats', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.UserRecipeStats (
                    UserId INT NOT NULL,
                    RecipeId INT NOT NULL,
                    CookCount INT NOT NULL DEFAULT 0,
                    LastCookedAt DATETIME2 NULL,
                    CONSTRAINT PK_UserRecipeStats PRIMARY KEY (UserId, RecipeId),
                    CONSTRAINT FK_UserRecipeStats_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_UserRecipeStats_Recipes FOREIGN KEY (RecipeId) REFERENCES dbo.Recipes(Id) ON DELETE CASCADE
                );
            END;
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureColumn(SqlConnection connection, string tableName, string columnName, string definition)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NULL
            BEGIN
                ALTER TABLE dbo.{tableName} ADD {columnName} {definition};
            END
            """;
        command.ExecuteNonQuery();
    }

    private static Recipe ReadRecipe(SqlDataReader reader)
    {
        return new Recipe(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetString(9));
    }

    private static void Seed()
    {
        EnsureSystemUsers();

        var categories = new[]
        {
            "Завтраки", "Супы", "Основные блюда", "Салаты", "Десерты",
            "Паста и крупы", "Выпечка", "Напитки", "Закуски", "Вегетарианское"
        };

        foreach (var category in categories)
        {
            EnsureCategory(category);
        }

        AddSeedRecipe("Омлет с сыром и томатами", "Нежный омлет на сковороде.", "Взбейте яйца с молоком, добавьте томаты и сыр. Готовьте 6-8 минут.", 15, 2, "Завтраки", "яйца - 3 шт.\nмолоко - 50 мл\nсыр - 60 г\nтоматы - 1 шт.", "Лёгкий", "Assets/Recipes/recipe-051.jpg");
        AddSeedRecipe("Куриный суп с лапшой", "Домашний суп с курицей и овощами.", "Сварите курицу, добавьте овощи и лапшу. Варите до готовности.", 55, 4, "Супы", "курица - 300 г\nкартофель - 3 шт.\nморковь - 1 шт.\nлапша - 80 г", "Средний", "Assets/Recipes/recipe-052.jpg");
        AddSeedRecipe("Паста с томатным соусом", "Паста с соусом из томатов и чеснока.", "Отварите пасту. Обжарьте чеснок, добавьте томаты, смешайте с пастой.", 25, 3, "Паста и крупы", "паста - 250 г\nтоматы - 300 г\nчеснок - 2 зубчика\nсыр - 40 г", "Лёгкий", "Assets/Recipes/recipe-053.jpg");
        AddSeedRecipe("Овощной салат с фетой", "Свежий салат из овощей и сыра.", "Нарежьте овощи, добавьте фету, масло и зелень.", 12, 2, "Салаты", "огурцы - 2 шт.\nтоматы - 2 шт.\nперец - 1 шт.\nфета - 80 г", "Лёгкий", "Assets/Recipes/recipe-054.jpg");
        AddSeedRecipe("Сырники с ягодами", "Классические творожные сырники к завтраку.", "Смешайте творог, яйцо, муку и сахар. Обжарьте сырники и подайте с ягодами.", 30, 3, "Десерты", "творог - 400 г\nяйцо - 1 шт.\nмука - 4 ст. л.\nсахар - 2 ст. л.\nягоды - 100 г", "Лёгкий", "Assets/Recipes/recipe-055.jpg");
        AddSeedRecipe("Гречка с грибами", "Простое горячее блюдо из крупы и шампиньонов.", "Отварите гречку. Обжарьте лук и грибы, смешайте с крупой.", 35, 3, "Паста и крупы", "гречка - 200 г\nгрибы - 250 г\nлук - 1 шт.\nмасло - 2 ст. л.", "Лёгкий", "Assets/Recipes/recipe-056.jpg");
        AddSeedRecipe("Рис с овощами", "Лёгкий гарнир или самостоятельное блюдо.", "Обжарьте овощи, добавьте рис и немного воды. Тушите до готовности.", 30, 3, "Вегетарианское", "рис - 200 г\nморковь - 1 шт.\nперец - 1 шт.\nгорошек - 100 г", "Лёгкий", "Assets/Recipes/recipe-057.jpg");
        AddSeedRecipe("Брускетта с томатами", "Хрустящая закуска с томатами и зеленью.", "Подсушите хлеб. Смешайте томаты, чеснок, масло и зелень, выложите сверху.", 15, 2, "Закуски", "хлеб - 4 ломтика\nтоматы - 2 шт.\nчеснок - 1 зубчик\nзелень - 20 г", "Лёгкий", "Assets/Recipes/recipe-058.jpg");
        AddSeedRecipe("Тыквенный суп-пюре", "Нежный кремовый суп из тыквы.", "Отварите тыкву с картофелем, пробейте блендером, добавьте сливки.", 40, 4, "Супы", "тыква - 500 г\nкартофель - 2 шт.\nсливки - 100 мл\nлук - 1 шт.", "Средний", "Assets/Recipes/recipe-059.jpg");
        AddSeedRecipe("Курица с картофелем", "Сытное блюдо для ужина.", "Запеките курицу с картофелем, луком и специями до золотистой корочки.", 60, 4, "Основные блюда", "курица - 600 г\nкартофель - 700 г\nлук - 1 шт.\nспеции - 1 ч. л.", "Средний", "Assets/Recipes/recipe-060.jpg");
        AddSeedRecipe("Банановые панкейки", "Мягкие панкейки без сложной подготовки.", "Разомните банан, смешайте с яйцом, молоком и мукой. Жарьте небольшими порциями.", 20, 2, "Завтраки", "банан - 1 шт.\nяйцо - 1 шт.\nмолоко - 120 мл\nмука - 120 г", "Лёгкий", "Assets/Recipes/recipe-061.jpg");
        AddSeedRecipe("Яблочный пирог", "Домашняя выпечка к чаю.", "Смешайте тесто, добавьте яблоки и запекайте до румяной корочки.", 50, 6, "Выпечка", "яблоки - 4 шт.\nмука - 200 г\nяйца - 2 шт.\nсахар - 120 г", "Средний", "Assets/Recipes/recipe-062.jpg");
        AddSeedRecipe("Домашний лимонад", "Освежающий напиток с лимоном и мятой.", "Смешайте лимонный сок, воду, сахар и мяту. Охладите перед подачей.", 10, 4, "Напитки", "лимон - 2 шт.\nвода - 1 л\nсахар - 3 ст. л.\nмята - 20 г", "Лёгкий", "Assets/Recipes/recipe-063.jpg");

        AddExpandedRecipeCatalog();

        EnsureInitialStats();
        EnsureInitialFavorites();
    }

    private static void AddExpandedRecipeCatalog()
    {
        var recipes = new[]
        {
            new SeedRecipe("Овсяная каша с ягодами", "Сытный завтрак с овсянкой, молоком и свежими ягодами.", "Сварите овсянку на молоке, добавьте мёд, ягоды и орехи. Подавайте горячей.", 15, 2, "Завтраки", "Лёгкий", "Assets/Recipes/recipe-001.jpg", "овсяные хлопья - 120 г\nмолоко - 350 мл\nягоды - 120 г\nмёд - 1 ст. л.\nорехи - 30 г"),
            new SeedRecipe("Шакшука с перцем", "Яйца в густом томатном соусе с болгарским перцем.", "Обжарьте лук и перец, добавьте томаты и специи. Сделайте углубления, разбейте яйца и готовьте под крышкой.", 25, 2, "Завтраки", "Средний", "Assets/Recipes/recipe-002.jpg", "яйца - 4 шт.\nтоматы - 350 г\nперец - 1 шт.\nлук - 1 шт.\nпаприка - 1 ч. л."),
            new SeedRecipe("Тосты с авокадо и яйцом", "Хрустящие тосты с авокадо, яйцом и зеленью.", "Подсушите хлеб, разомните авокадо с лимонным соком. Сверху выложите яйцо и зелень.", 12, 2, "Завтраки", "Лёгкий", "Assets/Recipes/recipe-003.jpg", "хлеб - 4 ломтика\nавокадо - 1 шт.\nяйца - 2 шт.\nлимонный сок - 1 ч. л.\nзелень - 15 г"),
            new SeedRecipe("Гранола с йогуртом", "Быстрый завтрак с хрустящей гранолой, йогуртом и фруктами.", "Выложите йогурт в чашу, добавьте гранолу, банан, ягоды и немного мёда.", 8, 2, "Завтраки", "Лёгкий", "Assets/Recipes/recipe-004.jpg", "йогурт - 300 г\nгранола - 120 г\nбанан - 1 шт.\nягоды - 100 г\nмёд - 1 ст. л."),
            new SeedRecipe("Блины на кефире", "Тонкие домашние блины на кефире для завтрака или десерта.", "Смешайте кефир, яйца, муку и сахар. Добавьте масло и жарьте тонкие блины на разогретой сковороде.", 35, 4, "Завтраки", "Средний", "Assets/Recipes/recipe-005.jpg", "кефир - 500 мл\nмука - 220 г\nяйца - 2 шт.\nсахар - 2 ст. л.\nмасло - 2 ст. л."),
            new SeedRecipe("Рисовая каша с яблоком", "Нежная молочная рисовая каша с яблоком и корицей.", "Отварите рис в молоке, добавьте тёртое яблоко, сахар и корицу. Дайте настояться под крышкой.", 30, 3, "Завтраки", "Лёгкий", "Assets/Recipes/recipe-006.jpg", "рис - 160 г\nмолоко - 500 мл\nяблоко - 1 шт.\nсахар - 1 ст. л.\nкорица - 0.5 ч. л."),
            new SeedRecipe("Французские тосты", "Сладкие тосты в яично-молочной смеси с ягодами.", "Взбейте яйца с молоком и сахаром. Обмакните хлеб и обжарьте с двух сторон до золотистой корочки.", 18, 2, "Завтраки", "Лёгкий", "Assets/Recipes/recipe-007.jpg", "хлеб - 4 ломтика\nяйца - 2 шт.\nмолоко - 120 мл\nсахар - 1 ст. л.\nягоды - 80 г"),

            new SeedRecipe("Борщ с говядиной", "Классический насыщенный борщ с говядиной, свёклой и капустой.", "Сварите бульон, добавьте картофель, капусту и овощную зажарку со свёклой. Подавайте со сметаной.", 90, 6, "Супы", "Сложный", "Assets/Recipes/recipe-008.jpg", "говядина - 500 г\nсвёкла - 2 шт.\nкапуста - 300 г\nкартофель - 3 шт.\nтоматная паста - 2 ст. л."),
            new SeedRecipe("Сырный суп с брокколи", "Кремовый суп с брокколи, плавленым сыром и сухариками.", "Отварите овощи, добавьте сыр и пробейте блендером. Прогрейте до однородности.", 35, 4, "Супы", "Лёгкий", "Assets/Recipes/recipe-009.jpg", "брокколи - 400 г\nкартофель - 2 шт.\nсыр плавленый - 180 г\nсливки - 100 мл\nлук - 1 шт."),
            new SeedRecipe("Чечевичный суп", "Питательный суп из красной чечевицы с морковью и специями.", "Обжарьте овощи, добавьте чечевицу и воду. Варите до мягкости, приправьте зирой и паприкой.", 40, 4, "Супы", "Лёгкий", "Assets/Recipes/recipe-010.jpg", "чечевица - 220 г\nморковь - 1 шт.\nлук - 1 шт.\nтомат - 1 шт.\nзира - 0.5 ч. л."),
            new SeedRecipe("Солянка мясная", "Кисло-пряный суп с копчёностями, огурцами и маслинами.", "Сварите бульон, добавьте мясные продукты, огурцы, томатную пасту и маслины. Дайте настояться.", 70, 6, "Супы", "Сложный", "Assets/Recipes/recipe-011.jpg", "говядина - 300 г\nкопчёности - 250 г\nсолёные огурцы - 3 шт.\nмаслины - 80 г\nтоматная паста - 2 ст. л."),
            new SeedRecipe("Грибной крем-суп", "Бархатный крем-суп из шампиньонов со сливками.", "Обжарьте грибы с луком, добавьте картофель и бульон. Пробейте блендером и влейте сливки.", 35, 4, "Супы", "Средний", "Assets/Recipes/recipe-012.jpg", "шампиньоны - 450 г\nкартофель - 2 шт.\nлук - 1 шт.\nсливки - 150 мл\nбульон - 700 мл"),
            new SeedRecipe("Рыбный суп с картофелем", "Лёгкий рыбный суп с картофелем, морковью и зеленью.", "Отварите картофель и морковь, добавьте рыбу и готовьте до мягкости. Подавайте с укропом.", 35, 4, "Супы", "Лёгкий", "Assets/Recipes/recipe-013.jpg", "рыба - 400 г\nкартофель - 3 шт.\nморковь - 1 шт.\nлук - 1 шт.\nукроп - 20 г"),

            new SeedRecipe("Лосось с овощами", "Запечённый лосось с брокколи, морковью и лимоном.", "Выложите лосось и овощи в форму, сбрызните маслом и лимоном. Запекайте до готовности.", 35, 2, "Основные блюда", "Средний", "Assets/Recipes/recipe-014.jpg", "лосось - 400 г\nброкколи - 250 г\nморковь - 1 шт.\nлимон - 0.5 шт.\nоливковое масло - 2 ст. л."),
            new SeedRecipe("Котлеты с картофельным пюре", "Домашние котлеты с нежным картофельным пюре.", "Сформируйте котлеты из фарша и обжарьте. Отварите картофель и разомните с молоком и маслом.", 55, 4, "Основные блюда", "Средний", "Assets/Recipes/recipe-015.jpg", "фарш - 500 г\nкартофель - 800 г\nмолоко - 150 мл\nлук - 1 шт.\nмасло - 40 г"),
            new SeedRecipe("Говядина в соусе", "Тушёная говядина в густом соусе с луком и морковью.", "Обжарьте мясо, добавьте овощи, бульон и тушите до мягкости. Подавайте с гарниром.", 85, 4, "Основные блюда", "Сложный", "Assets/Recipes/recipe-016.jpg", "говядина - 600 г\nлук - 1 шт.\nморковь - 1 шт.\nбульон - 400 мл\nтоматная паста - 1 ст. л."),
            new SeedRecipe("Индейка с гречкой", "Полезное горячее блюдо из индейки, гречки и овощей.", "Обжарьте индейку, добавьте овощи и гречку. Влейте воду и тушите до готовности.", 45, 3, "Основные блюда", "Лёгкий", "Assets/Recipes/recipe-017.jpg", "индейка - 400 г\nгречка - 200 г\nморковь - 1 шт.\nлук - 1 шт.\nвода - 450 мл"),
            new SeedRecipe("Запечённые перцы с фаршем", "Болгарские перцы с мясной начинкой и рисом.", "Смешайте фарш с рисом и овощами, наполните перцы. Запекайте под соусом до мягкости.", 70, 4, "Основные блюда", "Средний", "Assets/Recipes/recipe-018.jpg", "перец - 6 шт.\nфарш - 500 г\nрис - 120 г\nтоматный соус - 300 мл\nлук - 1 шт."),
            new SeedRecipe("Куриное филе в сливочном соусе", "Нежное куриное филе в сливках с чесноком и зеленью.", "Обжарьте курицу, добавьте чеснок и сливки. Тушите до густого соуса.", 30, 3, "Основные блюда", "Лёгкий", "Assets/Recipes/recipe-019.jpg", "куриное филе - 500 г\nсливки - 200 мл\nчеснок - 2 зубчика\nзелень - 20 г\nмасло - 1 ст. л."),
            new SeedRecipe("Тефтели в томатном соусе", "Мясные тефтели с рисом в ароматном томатном соусе.", "Сформируйте тефтели, обжарьте и тушите в томатном соусе до готовности.", 50, 4, "Основные блюда", "Средний", "Assets/Recipes/recipe-020.jpg", "фарш - 500 г\nрис - 100 г\nтоматный соус - 350 мл\nлук - 1 шт.\nяйцо - 1 шт."),
            new SeedRecipe("Рагу из овощей и курицы", "Домашнее рагу с курицей, картофелем и сезонными овощами.", "Нарежьте курицу и овощи, обжарьте и тушите под крышкой до мягкости.", 55, 4, "Основные блюда", "Лёгкий", "Assets/Recipes/recipe-021.jpg", "курица - 500 г\nкартофель - 4 шт.\nкабачок - 1 шт.\nморковь - 1 шт.\nлук - 1 шт."),
            new SeedRecipe("Свинина с рисом и овощами", "Сытное блюдо из свинины, риса и овощной смеси.", "Обжарьте свинину, добавьте рис, овощи и воду. Готовьте под крышкой до мягкости.", 50, 4, "Основные блюда", "Средний", "Assets/Recipes/recipe-022.jpg", "свинина - 500 г\nрис - 220 г\nперец - 1 шт.\nморковь - 1 шт.\nсоевый соус - 2 ст. л."),
            new SeedRecipe("Лазанья домашняя", "Слоёная лазанья с мясным соусом, бешамелем и сыром.", "Приготовьте мясной соус и бешамель. Соберите слои с листами лазаньи и запекайте.", 90, 6, "Основные блюда", "Сложный", "Assets/Recipes/recipe-023.jpg", "листы лазаньи - 250 г\nфарш - 600 г\nтоматный соус - 400 мл\nмолоко - 500 мл\nсыр - 180 г"),

            new SeedRecipe("Цезарь с курицей", "Популярный салат с курицей, сухариками и сыром.", "Обжарьте курицу, нарежьте салат, добавьте сухарики, сыр и соус.", 25, 2, "Салаты", "Лёгкий", "Assets/Recipes/recipe-024.jpg", "куриное филе - 250 г\nсалат ромэн - 150 г\nсухарики - 50 г\nпармезан - 40 г\nсоус цезарь - 60 г"),
            new SeedRecipe("Греческий салат", "Свежий салат с фетой, овощами и маслинами.", "Нарежьте овощи крупно, добавьте фету и маслины. Заправьте маслом и орегано.", 15, 3, "Салаты", "Лёгкий", "Assets/Recipes/recipe-025.jpg", "томаты - 3 шт.\nогурцы - 2 шт.\nфета - 120 г\nмаслины - 80 г\nоливковое масло - 2 ст. л."),
            new SeedRecipe("Салат с тунцом и фасолью", "Белковый салат с тунцом, фасолью и красным луком.", "Смешайте фасоль, тунец, лук и зелень. Заправьте лимонным соком и маслом.", 12, 2, "Салаты", "Лёгкий", "Assets/Recipes/recipe-026.jpg", "тунец - 1 банка\nфасоль - 200 г\nкрасный лук - 0.5 шт.\nлимонный сок - 1 ст. л.\nзелень - 20 г"),
            new SeedRecipe("Винегрет", "Классический овощной салат со свёклой, картофелем и огурцами.", "Отварите овощи, нарежьте кубиками, добавьте огурцы, горошек и масло.", 60, 5, "Салаты", "Средний", "Assets/Recipes/recipe-027.jpg", "свёкла - 2 шт.\nкартофель - 3 шт.\nморковь - 1 шт.\nогурцы солёные - 3 шт.\nгорошек - 120 г"),
            new SeedRecipe("Салат с курицей и ананасом", "Нежный салат с курицей, ананасом, сыром и кукурузой.", "Отварите курицу, нарежьте ингредиенты, смешайте с лёгкой заправкой.", 25, 4, "Салаты", "Лёгкий", "Assets/Recipes/recipe-028.jpg", "курица - 300 г\nананас - 200 г\nсыр - 100 г\nкукуруза - 150 г\nйогурт - 80 г"),
            new SeedRecipe("Тёплый салат с баклажанами", "Салат из обжаренных баклажанов, томатов и зелени.", "Обжарьте баклажаны, добавьте томаты, чеснок и зелень. Подавайте тёплым.", 25, 2, "Салаты", "Средний", "Assets/Recipes/recipe-029.jpg", "баклажан - 2 шт.\nтоматы - 2 шт.\nчеснок - 1 зубчик\nкинза - 20 г\nмасло - 2 ст. л."),

            new SeedRecipe("Тирамису без выпечки", "Нежный десерт с кремом, кофе и печеньем савоярди.", "Приготовьте крем, окуните печенье в кофе и соберите слои. Охладите несколько часов.", 30, 6, "Десерты", "Средний", "Assets/Recipes/recipe-030.jpg", "савоярди - 200 г\nмаскарпоне - 400 г\nкофе - 200 мл\nсливки - 200 мл\nкакао - 2 ст. л."),
            new SeedRecipe("Шоколадный брауни", "Плотный шоколадный пирог с насыщенным вкусом.", "Растопите шоколад с маслом, смешайте с яйцами, сахаром и мукой. Выпекайте до влажной середины.", 40, 8, "Десерты", "Средний", "Assets/Recipes/recipe-031.jpg", "шоколад - 200 г\nмасло - 120 г\nяйца - 3 шт.\nсахар - 150 г\nмука - 90 г"),
            new SeedRecipe("Панна-котта с ягодами", "Сливочный итальянский десерт с ягодным соусом.", "Прогрейте сливки с сахаром и желатином. Разлейте по формам и охладите, подайте с ягодами.", 25, 4, "Десерты", "Средний", "Assets/Recipes/recipe-032.jpg", "сливки - 400 мл\nсахар - 80 г\nжелатин - 10 г\nягоды - 150 г\nваниль - 1 ч. л."),
            new SeedRecipe("Морковный кекс", "Ароматный кекс с морковью, орехами и корицей.", "Смешайте сухие и влажные ингредиенты, добавьте морковь и орехи. Выпекайте до готовности.", 55, 8, "Десерты", "Лёгкий", "Assets/Recipes/recipe-033.jpg", "морковь - 250 г\nмука - 220 г\nяйца - 2 шт.\nорехи - 80 г\nкорица - 1 ч. л."),
            new SeedRecipe("Медовик ленивый", "Упрощённая версия медового торта со сметанным кремом.", "Испеките медовый корж, нарежьте и прослоите кремом. Дайте пропитаться.", 60, 8, "Десерты", "Средний", "Assets/Recipes/recipe-034.jpg", "мёд - 3 ст. л.\nмука - 260 г\nяйца - 2 шт.\nсметана - 500 г\nсахар - 160 г"),
            new SeedRecipe("Творожная запеканка", "Мягкая запеканка из творога с изюмом и ванилью.", "Смешайте творог, яйца, манку и сахар. Добавьте изюм и запекайте до румяной корочки.", 50, 6, "Десерты", "Лёгкий", "Assets/Recipes/recipe-035.jpg", "творог - 600 г\nяйца - 2 шт.\nманка - 4 ст. л.\nсахар - 80 г\nизюм - 70 г"),

            new SeedRecipe("Спагетти карбонара", "Паста с беконом, яйцом и сыром в сливочной текстуре.", "Отварите пасту, обжарьте бекон. Смешайте горячую пасту с яйцом, сыром и беконом.", 25, 3, "Паста и крупы", "Средний", "Assets/Recipes/recipe-036.jpg", "спагетти - 300 г\nбекон - 150 г\nяйца - 2 шт.\nпармезан - 70 г\nперец - 0.5 ч. л."),
            new SeedRecipe("Плов с курицей", "Рассыпчатый плов с курицей, морковью и специями.", "Обжарьте курицу с овощами, добавьте рис, специи и воду. Готовьте под крышкой.", 60, 5, "Паста и крупы", "Средний", "Assets/Recipes/recipe-037.jpg", "курица - 500 г\nрис - 350 г\nморковь - 2 шт.\nлук - 1 шт.\nзира - 1 ч. л."),
            new SeedRecipe("Ризотто с грибами", "Кремовое ризотто с шампиньонами и пармезаном.", "Обжарьте грибы и рис, постепенно вливайте бульон, постоянно помешивая. Добавьте сыр.", 45, 3, "Паста и крупы", "Сложный", "Assets/Recipes/recipe-038.jpg", "рис арборио - 240 г\nгрибы - 250 г\nбульон - 800 мл\nпармезан - 60 г\nлук - 1 шт."),
            new SeedRecipe("Лапша удон с овощами", "Быстрая лапша с овощами и соевым соусом.", "Обжарьте овощи, добавьте удон и соус. Быстро прогрейте на сильном огне.", 20, 2, "Паста и крупы", "Лёгкий", "Assets/Recipes/recipe-039.jpg", "удон - 300 г\nперец - 1 шт.\nморковь - 1 шт.\nсоевый соус - 3 ст. л.\nкунжут - 1 ч. л."),
            new SeedRecipe("Кускус с овощами", "Лёгкое блюдо из кускуса с овощами и зеленью.", "Залейте кускус кипятком, обжарьте овощи и смешайте. Добавьте зелень.", 18, 3, "Паста и крупы", "Лёгкий", "Assets/Recipes/recipe-040.jpg", "кускус - 220 г\nкабачок - 1 шт.\nперец - 1 шт.\nтоматы - 2 шт.\nзелень - 20 г"),

            new SeedRecipe("Хачапури по-аджарски", "Лодочка из теста с сыром, яйцом и сливочным маслом.", "Сформируйте лодочки, наполните сыром и выпекайте. В конце добавьте яйцо и масло.", 75, 4, "Выпечка", "Сложный", "Assets/Recipes/recipe-041.jpg", "мука - 450 г\nсыр сулугуни - 400 г\nяйца - 4 шт.\nдрожжи - 7 г\nмасло - 40 г"),
            new SeedRecipe("Киш с курицей и грибами", "Открытый пирог с курицей, грибами и сливочной заливкой.", "Подготовьте песочное тесто, начинку и заливку. Соберите киш и выпекайте.", 70, 6, "Выпечка", "Средний", "Assets/Recipes/recipe-042.jpg", "тесто - 300 г\nкурица - 250 г\nгрибы - 200 г\nсливки - 200 мл\nяйца - 3 шт."),
            new SeedRecipe("Домашний хлеб", "Простой пшеничный хлеб с хрустящей корочкой.", "Замесите тесто, дайте подняться, сформируйте буханку и выпекайте до готовности.", 140, 8, "Выпечка", "Средний", "Assets/Recipes/recipe-043.jpg", "мука - 500 г\nвода - 320 мл\nдрожжи - 7 г\nсоль - 1 ч. л.\nсахар - 1 ч. л."),
            new SeedRecipe("Пирожки с капустой", "Мягкие пирожки с тушёной капустой.", "Приготовьте тесто, потушите капусту с луком. Сформируйте пирожки и выпекайте.", 100, 8, "Выпечка", "Средний", "Assets/Recipes/recipe-044.jpg", "мука - 500 г\nкапуста - 500 г\nлук - 1 шт.\nдрожжи - 7 г\nяйцо - 1 шт."),

            new SeedRecipe("Смузи с бананом и шпинатом", "Зелёный смузи с бананом, шпинатом и йогуртом.", "Сложите ингредиенты в блендер и взбейте до гладкости.", 7, 2, "Напитки", "Лёгкий", "Assets/Recipes/recipe-045.jpg", "банан - 1 шт.\nшпинат - 60 г\nйогурт - 250 мл\nмёд - 1 ч. л.\nвода - 100 мл"),
            new SeedRecipe("Морс из клюквы", "Кисло-сладкий домашний морс из клюквы.", "Разомните ягоды, отожмите сок. Жмых проварите, смешайте с соком и сахаром.", 25, 6, "Напитки", "Лёгкий", "Assets/Recipes/recipe-046.jpg", "клюква - 300 г\nвода - 1.5 л\nсахар - 120 г\nлимон - 0.5 шт."),
            new SeedRecipe("Имбирный чай с лимоном", "Согревающий напиток с имбирём, лимоном и мёдом.", "Залейте имбирь кипятком, добавьте лимон и мёд. Дайте настояться.", 10, 2, "Напитки", "Лёгкий", "Assets/Recipes/recipe-047.jpg", "имбирь - 20 г\nлимон - 0.5 шт.\nмёд - 1 ст. л.\nвода - 500 мл"),

            new SeedRecipe("Хумус с питой", "Нежная закуска из нута с тахини и лимоном.", "Пробейте нут с тахини, чесноком, лимонным соком и маслом. Подавайте с питой.", 15, 4, "Закуски", "Лёгкий", "Assets/Recipes/recipe-048.jpg", "нут - 300 г\nтахини - 2 ст. л.\nлимонный сок - 2 ст. л.\nчеснок - 1 зубчик\nпита - 4 шт."),
            new SeedRecipe("Рулетики из лаваша", "Быстрая закуска из лаваша с сыром, зеленью и ветчиной.", "Смажьте лаваш, выложите начинку, сверните рулетом и нарежьте.", 15, 4, "Закуски", "Лёгкий", "Assets/Recipes/recipe-049.jpg", "лаваш - 2 листа\nсыр творожный - 160 г\nветчина - 150 г\nзелень - 20 г\nогурец - 1 шт."),
            new SeedRecipe("Сырная тарелка с орехами", "Простая закуска из разных сыров, орехов и мёда.", "Нарежьте сыры, выложите орехи, мёд и фрукты. Подавайте охлаждённой.", 10, 4, "Закуски", "Лёгкий", "Assets/Recipes/recipe-050.jpg", "сыр твёрдый - 120 г\nсыр мягкий - 120 г\nорехи - 60 г\nмёд - 2 ст. л.\nвиноград - 120 г")
        };

        foreach (var recipe in recipes)
        {
            AddSeedRecipe(recipe.Title, recipe.Description, recipe.Instructions, recipe.CookingTime, recipe.Servings, recipe.CategoryName, recipe.IngredientsText, recipe.Difficulty, recipe.ImageUrl);
        }
    }

    private static void EnsureSystemUsers()
    {
        EnsureSeedUser("admin", "admin", "Admin");
        EnsureSeedUser("operator", "operator", "Operator");
        EnsureSeedUser("user", "user", "User");

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Users
            SET Role = N'User'
            WHERE Role = N'Admin' AND Email <> N'admin';

            UPDATE dbo.Users
            SET Role = N'Admin', Status = 1
            WHERE Email = N'admin';

            UPDATE dbo.Users
            SET Role = N'Operator', Status = 1
            WHERE Email = N'operator';

            UPDATE dbo.Users
            SET Role = N'User', Status = 1
            WHERE Email = N'user';
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureSeedUser(string login, string password, string role)
    {
        using var connection = OpenConnection();
        using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT COUNT(*) FROM dbo.Users WHERE Email = @login";
        existsCommand.Parameters.AddWithValue("@login", login);
        if (Convert.ToInt32(existsCommand.ExecuteScalar()) > 0)
        {
            return;
        }

        using var userCommand = connection.CreateCommand();
        userCommand.CommandText = "INSERT INTO dbo.Users (Email, PasswordHash, Role, Status) VALUES (@login, @password, @role, 1)";
        userCommand.Parameters.AddWithValue("@login", login);
        userCommand.Parameters.AddWithValue("@password", HashPassword(password));
        userCommand.Parameters.AddWithValue("@role", role);
        userCommand.ExecuteNonQuery();
    }

    private static int EnsureCategory(string name)
    {
        using var connection = OpenConnection();
        using var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT Id FROM dbo.Categories WHERE Name = @name";
        selectCommand.Parameters.AddWithValue("@name", name);
        var existing = selectCommand.ExecuteScalar();
        if (existing is not null)
        {
            return Convert.ToInt32(existing);
        }

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.Categories (Name) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        command.Parameters.AddWithValue("@name", name);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void RepairSeedRecipeImages(SqlConnection connection)
    {
        var images = new Dictionary<string, string>
        {
            ["Омлет с сыром и томатами"] = "Assets/Recipes/recipe-051.jpg",
            ["Куриный суп с лапшой"] = "Assets/Recipes/recipe-052.jpg",
            ["Паста с томатным соусом"] = "Assets/Recipes/recipe-053.jpg",
            ["Овощной салат с фетой"] = "Assets/Recipes/recipe-054.jpg",
            ["Сырники с ягодами"] = "Assets/Recipes/recipe-055.jpg",
            ["Гречка с грибами"] = "Assets/Recipes/recipe-056.jpg",
            ["Рис с овощами"] = "Assets/Recipes/recipe-057.jpg",
            ["Брускетта с томатами"] = "Assets/Recipes/recipe-058.jpg",
            ["Тыквенный суп-пюре"] = "Assets/Recipes/recipe-059.jpg",
            ["Курица с картофелем"] = "Assets/Recipes/recipe-060.jpg",
            ["Банановые панкейки"] = "Assets/Recipes/recipe-061.jpg",
            ["Яблочный пирог"] = "Assets/Recipes/recipe-062.jpg",
            ["Домашний лимонад"] = "Assets/Recipes/recipe-063.jpg"
        };

        foreach (var item in images)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE dbo.Recipes
                SET ImageUrl = @imageUrl
                WHERE Title = @title
                  AND (ImageUrl IS NULL OR ImageUrl = N'' OR ImageUrl LIKE N'http%')
                """;
            command.Parameters.AddWithValue("@title", item.Key);
            command.Parameters.AddWithValue("@imageUrl", item.Value);
            command.ExecuteNonQuery();
        }
    }

    private static void AddSeedRecipe(string title, string description, string instructions, int cookingTime, int servings, string categoryName, string ingredientsText, string difficulty = "Лёгкий", string imageUrl = "")
    {
        var fullInstructions = GetSeedRecipeInstructions(title, instructions);
        using var connection = OpenConnection();
        using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT COUNT(*) FROM dbo.Recipes WHERE Title = @title AND UserId IS NULL";
        existsCommand.Parameters.AddWithValue("@title", title);
        if (Convert.ToInt32(existsCommand.ExecuteScalar()) > 0)
        {
            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = """
                UPDATE dbo.Recipes
                SET Instructions = @instructions,
                    ImageUrl = CASE WHEN @imageUrl = N'' THEN ImageUrl ELSE @imageUrl END,
                    Difficulty = @difficulty,
                    Status = N'Published'
                WHERE Title = @title
                  AND UserId IS NULL
                """;
            updateCommand.Parameters.AddWithValue("@title", title);
            updateCommand.Parameters.AddWithValue("@instructions", fullInstructions);
            updateCommand.Parameters.AddWithValue("@imageUrl", imageUrl.Trim());
            updateCommand.Parameters.AddWithValue("@difficulty", difficulty);
            updateCommand.ExecuteNonQuery();

            return;
        }

        AddRecipeExtended(0, title, description, fullInstructions, cookingTime, servings, EnsureCategory(categoryName), difficulty, imageUrl, ingredientsText);
    }

    private sealed record SeedRecipe(string Title, string Description, string Instructions, int CookingTime, int Servings, string CategoryName, string Difficulty, string ImageUrl, string IngredientsText);

    private static void EnsureInitialStats()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.RecipeStats (RecipeId, CookCount, Rating)
            SELECT r.Id, 5 + (ABS(CHECKSUM(r.Title)) % 40), 4 + (ABS(CHECKSUM(r.Description)) % 2)
            FROM dbo.Recipes r
            WHERE NOT EXISTS (SELECT 1 FROM dbo.RecipeStats s WHERE s.RecipeId = r.Id)
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureInitialFavorites()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @userId INT = (SELECT TOP 1 Id FROM dbo.Users WHERE Email = N'admin');

            INSERT INTO dbo.Favorites (UserId, RecipeId, CreatedAt)
            SELECT TOP 5 @userId, r.Id, SYSUTCDATETIME()
            FROM dbo.Recipes r
            WHERE @userId IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.Favorites f
                  WHERE f.UserId = @userId AND f.RecipeId = r.Id
              )
            ORDER BY r.Title;
            """;
        command.ExecuteNonQuery();
    }

    private static IEnumerable<(string Name, string Amount)> ParseIngredients(string ingredientsText)
    {
        return ingredientsText
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('-', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts[0].Length > 0)
            .Select(parts => (parts[0].ToLowerInvariant(), parts.Length > 1 ? parts[1] : string.Empty));
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
