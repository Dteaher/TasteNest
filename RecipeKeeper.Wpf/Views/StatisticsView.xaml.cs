using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class StatisticsView : UserControl
{
    public StatisticsView()
    {
        InitializeComponent();
        LoadDashboard();
    }

    private void LoadDashboard()
    {
        var summary = Database.GetStatisticsSummary(User.Id);
        RecipesCountText.Text = summary.RecipesCount.ToString();
        FavoritesCountText.Text = summary.FavoritesCount.ToString();
        ProductsCountText.Text = summary.ProductsCount.ToString();
        CookCountText.Text = summary.CookCount.ToString();

        CategoryItems.ItemsSource = ToBars(Database.GetCategoryStats(User.Id));

        var ingredients = Database.GetIngredientStats(User.Id)
            .Take(12)
            .Select(item => new StatRow(item.Name, item.Count))
            .ToList();
        IngredientItems.ItemsSource = ingredients.Count == 0
            ? new[] { new StatRow("Пока нет данных по ингредиентам", 0) }
            : ingredients;

        var popularity = Database.GetRecipePopularityStats(User.Id)
            .Take(10)
            .Select(item => new PopularRecipeRow(item.Title, $"{item.CookCount} действий", $"{item.Rating} приготовл."))
            .ToList();
        PopularityItems.ItemsSource = popularity.Count == 0
            ? new[] { new PopularRecipeRow("Пока нет личной активности", "0 действий", "0 приготовл.") }
            : popularity;
    }

    private static List<StatBarRow> ToBars(IEnumerable<StatItem> items)
    {
        var rows = items.Take(10).ToList();
        if (rows.Count == 0)
        {
            return new List<StatBarRow>
            {
                new("Пока нет данных по категориям", 0, 0)
            };
        }

        var max = Math.Max(1, rows.Max(row => row.Count));
        return rows
            .Select(row => new StatBarRow(row.Name, row.Count, Math.Max(4, row.Count * 100 / max)))
            .ToList();
    }

    private sealed record StatBarRow(string Name, int Count, int Percent);
    private sealed record StatRow(string Name, int Count);
    private sealed record PopularRecipeRow(string Title, string CookText, string RatingText);
}
