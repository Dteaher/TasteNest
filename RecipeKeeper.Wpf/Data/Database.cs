using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace RecipeKeeper.Wpf.Data;

public static class Database
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
            """;
        command.ExecuteNonQuery();

        EnsureUserColumns(connection);
        EnsureRecipeColumns(connection);
        EnsureProductColumns(connection);
        Seed();
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
            Scalar("SELECT COUNT(*) FROM dbo.Favorites"));
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
            SELECT r.Id, r.Title, r.Description, r.Instructions, r.CookingTime, r.Servings, c.Name, ISNULL(r.ImageUrl, N''), ISNULL(r.Difficulty, N'Лёгкий')
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
            SELECT DISTINCT r.Id, r.Title, r.Description, r.Instructions, r.CookingTime, r.Servings, c.Name, ISNULL(r.ImageUrl, N''), ISNULL(r.Difficulty, N'Лёгкий')
            FROM dbo.Recipes r
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            LEFT JOIN dbo.Ingredients i ON i.RecipeId = r.Id
            WHERE 1 = 1
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
        recipeCommand.CommandText = """
            INSERT INTO dbo.Recipes (Title, Description, Instructions, CookingTime, Servings, CategoryId, UserId, ImageUrl, Difficulty)
            VALUES (@title, @description, @instructions, @cookingTime, @servings, @categoryId, @userId, @imageUrl, @difficulty);
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
            SELECT TOP 20 r.Title, v.ViewedAt
            FROM dbo.RecipeViews v
            JOIN dbo.Recipes r ON r.Id = v.RecipeId
            WHERE v.UserId = @userId
            ORDER BY v.ViewedAt DESC
            """;
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        var views = new List<RecipeActivity>();
        while (reader.Read())
        {
            views.Add(new RecipeActivity(reader.GetString(0), reader.GetDateTime(1).ToLocalTime()));
        }

        return views;
    }

    public static List<Recipe> GetFavoriteRecipes(int userId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.Id, r.Title, r.Description, r.Instructions, r.CookingTime, r.Servings, c.Name, ISNULL(r.ImageUrl, N''), ISNULL(r.Difficulty, N'Лёгкий')
            FROM dbo.Favorites f
            JOIN dbo.Recipes r ON r.Id = f.RecipeId
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            WHERE f.UserId = @userId
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
            SELECT r.Id, r.Title, r.Description, r.Instructions, r.CookingTime, r.Servings, c.Name, ISNULL(r.ImageUrl, N''), ISNULL(r.Difficulty, N'Лёгкий')
            FROM dbo.Recipes r
            JOIN dbo.Categories c ON c.Id = r.CategoryId
            LEFT JOIN dbo.RecipeStats s ON s.RecipeId = r.Id
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

    public static void IncrementCookCount(int recipeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM dbo.RecipeStats WHERE RecipeId = @recipeId)
                UPDATE dbo.RecipeStats SET CookCount = CookCount + 1 WHERE RecipeId = @recipeId;
            ELSE
                INSERT INTO dbo.RecipeStats (RecipeId, CookCount, Rating) VALUES (@recipeId, 1, 5);
            """;
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
        using var clearCommand = connection.CreateCommand();
        clearCommand.CommandText = "DELETE FROM dbo.ShoppingItems WHERE UserId = @userId";
        clearCommand.Parameters.AddWithValue("@userId", userId);
        clearCommand.ExecuteNonQuery();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.ShoppingItems (UserId, Name, Quantity, IsBought)
            SELECT @userId, i.Name, STRING_AGG(ISNULL(i.Amount, N''), N', '), 0
            FROM dbo.Favorites f
            JOIN dbo.Ingredients i ON i.RecipeId = f.RecipeId
            WHERE f.UserId = @userId
            GROUP BY i.Name
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.ExecuteNonQuery();
    }

    public static List<StatItem> GetCategoryStats()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP 8 c.Name, COUNT(r.Id)
            FROM dbo.Categories c
            JOIN dbo.Recipes r ON r.CategoryId = c.Id
            GROUP BY c.Name
            ORDER BY COUNT(r.Id) DESC
            """;
        using var reader = command.ExecuteReader();
        var stats = new List<StatItem>();
        while (reader.Read())
        {
            stats.Add(new StatItem(reader.GetString(0), reader.GetInt32(1)));
        }

        return stats;
    }

    public static List<StatItem> GetIngredientStats()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP 8 Name, COUNT(*)
            FROM dbo.Ingredients
            GROUP BY Name
            ORDER BY COUNT(*) DESC
            """;
        using var reader = command.ExecuteReader();
        var stats = new List<StatItem>();
        while (reader.Read())
        {
            stats.Add(new StatItem(reader.GetString(0), reader.GetInt32(1)));
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
    }

    private static void EnsureProductColumns(SqlConnection connection)
    {
        EnsureColumn(connection, "Products", "Unit", "NVARCHAR(40) NOT NULL DEFAULT N''");
        EnsureColumn(connection, "Products", "Category", "NVARCHAR(100) NOT NULL DEFAULT N''");
        EnsureColumn(connection, "Products", "Note", "NVARCHAR(500) NOT NULL DEFAULT N''");
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
            reader.GetString(8));
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

        AddSeedRecipe("Омлет с сыром и томатами", "Нежный омлет на сковороде.", "Взбейте яйца с молоком, добавьте томаты и сыр. Готовьте 6-8 минут.", 15, 2, "Завтраки", "яйца - 3 шт.\nмолоко - 50 мл\nсыр - 60 г\nтоматы - 1 шт.");
        AddSeedRecipe("Куриный суп с лапшой", "Домашний суп с курицей и овощами.", "Сварите курицу, добавьте овощи и лапшу. Варите до готовности.", 55, 4, "Супы", "курица - 300 г\nкартофель - 3 шт.\nморковь - 1 шт.\nлапша - 80 г");
        AddSeedRecipe("Паста с томатным соусом", "Паста с соусом из томатов и чеснока.", "Отварите пасту. Обжарьте чеснок, добавьте томаты, смешайте с пастой.", 25, 3, "Паста и крупы", "паста - 250 г\nтоматы - 300 г\nчеснок - 2 зубчика\nсыр - 40 г");
        AddSeedRecipe("Овощной салат с фетой", "Свежий салат из овощей и сыра.", "Нарежьте овощи, добавьте фету, масло и зелень.", 12, 2, "Салаты", "огурцы - 2 шт.\nтоматы - 2 шт.\nперец - 1 шт.\nфета - 80 г");
        AddSeedRecipe("Сырники с ягодами", "Классические творожные сырники к завтраку.", "Смешайте творог, яйцо, муку и сахар. Обжарьте сырники и подайте с ягодами.", 30, 3, "Десерты", "творог - 400 г\nяйцо - 1 шт.\nмука - 4 ст. л.\nсахар - 2 ст. л.\nягоды - 100 г");
        AddSeedRecipe("Гречка с грибами", "Простое горячее блюдо из крупы и шампиньонов.", "Отварите гречку. Обжарьте лук и грибы, смешайте с крупой.", 35, 3, "Паста и крупы", "гречка - 200 г\nгрибы - 250 г\nлук - 1 шт.\nмасло - 2 ст. л.");
        AddSeedRecipe("Рис с овощами", "Лёгкий гарнир или самостоятельное блюдо.", "Обжарьте овощи, добавьте рис и немного воды. Тушите до готовности.", 30, 3, "Вегетарианское", "рис - 200 г\nморковь - 1 шт.\nперец - 1 шт.\nгорошек - 100 г");
        AddSeedRecipe("Брускетта с томатами", "Хрустящая закуска с томатами и зеленью.", "Подсушите хлеб. Смешайте томаты, чеснок, масло и зелень, выложите сверху.", 15, 2, "Закуски", "хлеб - 4 ломтика\nтоматы - 2 шт.\nчеснок - 1 зубчик\nзелень - 20 г");
        AddSeedRecipe("Тыквенный суп-пюре", "Нежный кремовый суп из тыквы.", "Отварите тыкву с картофелем, пробейте блендером, добавьте сливки.", 40, 4, "Супы", "тыква - 500 г\nкартофель - 2 шт.\nсливки - 100 мл\nлук - 1 шт.");
        AddSeedRecipe("Курица с картофелем", "Сытное блюдо для ужина.", "Запеките курицу с картофелем, луком и специями до золотистой корочки.", 60, 4, "Основные блюда", "курица - 600 г\nкартофель - 700 г\nлук - 1 шт.\nспеции - 1 ч. л.");
        AddSeedRecipe("Банановые панкейки", "Мягкие панкейки без сложной подготовки.", "Разомните банан, смешайте с яйцом, молоком и мукой. Жарьте небольшими порциями.", 20, 2, "Завтраки", "банан - 1 шт.\nяйцо - 1 шт.\nмолоко - 120 мл\nмука - 120 г");
        AddSeedRecipe("Яблочный пирог", "Домашняя выпечка к чаю.", "Смешайте тесто, добавьте яблоки и запекайте до румяной корочки.", 50, 6, "Выпечка", "яблоки - 4 шт.\nмука - 200 г\nяйца - 2 шт.\nсахар - 120 г");
        AddSeedRecipe("Домашний лимонад", "Освежающий напиток с лимоном и мятой.", "Смешайте лимонный сок, воду, сахар и мяту. Охладите перед подачей.", 10, 4, "Напитки", "лимон - 2 шт.\nвода - 1 л\nсахар - 3 ст. л.\nмята - 20 г");

        EnsureInitialStats();
        EnsureInitialFavorites();
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

    private static void AddSeedRecipe(string title, string description, string instructions, int cookingTime, int servings, string categoryName, string ingredientsText)
    {
        using var connection = OpenConnection();
        using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT COUNT(*) FROM dbo.Recipes WHERE Title = @title";
        existsCommand.Parameters.AddWithValue("@title", title);
        if (Convert.ToInt32(existsCommand.ExecuteScalar()) > 0)
        {
            return;
        }

        AddRecipe(0, title, description, instructions, cookingTime, servings, EnsureCategory(categoryName), ingredientsText);
    }

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
