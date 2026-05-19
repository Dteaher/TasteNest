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

        CategoryItems.ItemsSource = ToBars(Database.GetCategoryStats());
        IngredientItems.ItemsSource = Database.GetIngredientStats()
            .Take(12)
            .Select(item => new StatRow(item.Name, item.Count));
        PopularityItems.ItemsSource = Database.GetRecipePopularityStats()
            .Take(10)
            .Select(item => new PopularRecipeRow(item.Title, $"{item.CookCount} приготовл.", $"★ {item.Rating}"));
    }

    private static List<StatBarRow> ToBars(IEnumerable<StatItem> items)
    {
        var rows = items.Take(10).ToList();
        var max = Math.Max(1, rows.Max(row => row.Count));
        return rows
            .Select(row => new StatBarRow(row.Name, row.Count, Math.Max(4, row.Count * 100 / max)))
            .ToList();
    }

    private sealed record StatBarRow(string Name, int Count, int Percent);
    private sealed record StatRow(string Name, int Count);
    private sealed record PopularRecipeRow(string Title, string CookText, string RatingText);
}
