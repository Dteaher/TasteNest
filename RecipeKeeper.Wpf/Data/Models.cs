    namespace RecipeKeeper.Wpf.Data;

public sealed record Category(int Id, string Name);
public sealed record Recipe(int Id, string Title, string Description, string Instructions, int CookingTime, int Servings, string Category, string ImageUrl = "", string Difficulty = "Лёгкий", string Status = "Published");
public sealed record Product(int Id, string Name, string Quantity, DateTime? ExpiresAt, string Unit = "", string Category = "", string Note = "");
public sealed record RecipeActivity(int RecipeId, string Title, DateTime ViewedAt);
public sealed record ShoppingItem(int Id, string Name, string Quantity, bool IsBought);
public sealed record MealPlanItem(string DayName, string RecipeTitle);
public sealed record StatItem(string Name, int Count);
public sealed record ProfileSummary(int RecipesCount, int FavoritesCount, int ViewsCount, int ProductsCount);
public sealed record ManagedUser(int Id, string Email, string Role, bool IsActive, int RecipesCount, int FavoritesCount, int ViewsCount, int ProductsCount);
public sealed record OperatorQueueItem(int UserId, string Email, string Issue, string Priority, string Detail, bool CanBlock);
public sealed record AdminSystemSummary(int TotalUsers, int Admins, int Operators, int Users, int BlockedUsers, int Recipes, int Products, int Favorites, int Views, int CookCount);
public sealed record StatisticsSummary(int RecipesCount, int FavoritesCount, int ProductsCount, int CookCount);
public sealed record RecipePopularityStat(string Title, int CookCount, int Rating);
public sealed record UserRecipeSummary(int Id, string Title, string Category, string Status, DateTime CreatedAt, string ModerationComment);
public sealed record RecipeModerationItem(int Id, string Title, string Description, string Category, string AuthorEmail, DateTime CreatedAt);
public sealed record AdminRecipeItem(int Id, string Title, string Category, string AuthorEmail, string Status, DateTime CreatedAt);
