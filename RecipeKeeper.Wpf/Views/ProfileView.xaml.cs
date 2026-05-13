using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class ProfileView : UserControl
{
    private Recipe? _recipeOfDay;

    public event Action<string>? NavigateRequested;
    public event Action<int>? OpenRecipeRequested;

    public ProfileView()
    {
        InitializeComponent();
        RenderProfile();
        RenderInsights();
    }

    private void RenderProfile()
    {
        UserTextBlock.Text = User.Email;
        RoleTextBlock.Text = $"Роль: {User.Role}";

        var summary = Database.GetProfileSummary(User.Id);
        RecipesCountText.Text = summary.RecipesCount.ToString();
        FavoritesCountText.Text = summary.FavoritesCount.ToString();
        ViewsCountText.Text = summary.ViewsCount.ToString();
        ProductsCountText.Text = summary.ProductsCount.ToString();

        var history = Database.GetRecipeViews(User.Id);
        HistoryListBox.ItemsSource = history.Count == 0
            ? new[] { new RecipeActivity("Вы пока не открывали рецепты", DateTime.Now) }
            : history;
    }

    private void RenderInsights()
    {
        RenderExpiryInsight();
        RenderCookNowInsight();
        RenderShoppingInsight();
        RenderRecipeOfDay();
    }

    private void RenderExpiryInsight()
    {
        var products = Database.GetProducts(User.Id);
        var expired = products.Where(product => product.ExpiresAt.HasValue && product.ExpiresAt.Value.Date < DateTime.Today).ToList();
        var soon = products.Where(product => product.ExpiresAt.HasValue && product.ExpiresAt.Value.Date >= DateTime.Today && product.ExpiresAt.Value.Date <= DateTime.Today.AddDays(3)).ToList();

        if (products.Count == 0)
        {
            ExpirySummaryText.Text = "У вас пока нет добавленных продуктов.";
            ExpiryItemsControl.ItemsSource = new[] { "Добавьте продукты, чтобы видеть сроки." };
            return;
        }

        ExpirySummaryText.Text = expired.Count == 0 && soon.Count == 0
            ? "Все продукты выглядят свежими."
            : $"Скоро истекают: {soon.Count}. Просрочены: {expired.Count}.";

        ExpiryItemsControl.ItemsSource = soon
            .Concat(expired)
            .OrderBy(product => product.ExpiresAt)
            .Take(3)
            .Select(product => $"{product.Name} — до {product.ExpiresAt:dd.MM.yyyy}")
            .DefaultIfEmpty("Критичных сроков нет.")
            .ToList();
    }

    private void RenderCookNowInsight()
    {
        var products = Database.GetProducts(User.Id);
        var productNames = products.Select(product => product.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();

        if (productNames.Count < 2)
        {
            CookNowSummaryText.Text = "Добавьте больше продуктов, и мы подберём рецепты.";
            CookNowItemsControl.ItemsSource = new[] { "Минимум 2 продукта для хорошей подсказки." };
            return;
        }

        var recipes = Database.SearchRecipes(string.Empty, null, productNames);
        CookNowSummaryText.Text = $"Найдено рецептов по вашим продуктам: {recipes.Count}.";
        CookNowItemsControl.ItemsSource = recipes.Take(3).Select(recipe => recipe.Title).DefaultIfEmpty("Пока нет совпадений.").ToList();
    }

    private void RenderShoppingInsight()
    {
        var items = Database.GetShoppingItems(User.Id);
        var notBought = items.Where(item => !item.IsBought).ToList();

        if (items.Count == 0)
        {
            ShoppingSummaryText.Text = "Список покупок пуст.";
            ShoppingItemsControl.ItemsSource = new[] { "Сформируйте список из избранных рецептов." };
            return;
        }

        ShoppingSummaryText.Text = $"В списке покупок {items.Count} позиций, не куплено: {notBought.Count}.";
        ShoppingItemsControl.ItemsSource = notBought.Take(3).Select(item => $"{item.Name} — {item.Quantity}").DefaultIfEmpty("Все товары отмечены купленными.").ToList();
    }

    private void RenderRecipeOfDay()
    {
        _recipeOfDay = Database.GetFavoriteRecipes(User.Id).FirstOrDefault()
            ?? Database.GetPopularRecipes().FirstOrDefault()
            ?? Database.SearchRecipes(string.Empty, null, Array.Empty<string>()).FirstOrDefault();

        if (_recipeOfDay is null)
        {
            RecipeOfDayTitleText.Text = "Рецептов пока нет";
            RecipeOfDaySummaryText.Text = "Добавьте первый рецепт, чтобы получать рекомендации.";
            return;
        }

        RecipeOfDayTitleText.Text = _recipeOfDay.Title;
        RecipeOfDaySummaryText.Text = $"{_recipeOfDay.Category} · {_recipeOfDay.CookingTime} мин · {_recipeOfDay.Description}";
    }

    private void InsightNavigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string section)
        {
            NavigateRequested?.Invoke(section);
        }
    }

    private void RecipeOfDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_recipeOfDay is not null)
        {
            OpenRecipeRequested?.Invoke(_recipeOfDay.Id);
        }
    }
}
