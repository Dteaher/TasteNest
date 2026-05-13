using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class ProductsView : UserControl
{
    private readonly List<Product> _products = new();
    private Recipe? _recipeOfDay;
    private int? _selectedProductId;

    public event Action<string>? NavigateRequested;
    public event Action<int>? OpenRecipeRequested;

    public ProductsView()
    {
        InitializeComponent();

        UnitComboBox.ItemsSource = new[] { "шт.", "г", "кг", "мл", "л", "уп." };
        CategoryComboBox.ItemsSource = new[] { "Овощи", "Фрукты", "Молочное", "Мясо", "Рыба", "Крупы", "Зелень", "Заморозка", "Бакалея", "Прочее" };
        ExpiryFilterComboBox.ItemsSource = new[] { "Все", "Свежие", "Скоро истекают", "Просрочены", "Без срока" };
        SortComboBox.ItemsSource = new[] { "По названию", "По сроку", "По категории", "Сначала проблемные" };
        ExpiryFilterComboBox.SelectedIndex = 0;
        SortComboBox.SelectedIndex = 0;

        Refresh();
    }

    private void Refresh()
    {
        _products.Clear();
        _products.AddRange(Database.GetProducts(User.Id));
        RefreshFilters();
        ApplyFilters();
        UpdateSummary();
        RenderInsights();
    }

    private void RefreshFilters()
    {
        var selected = FilterCategoryComboBox.SelectedItem?.ToString();
        var categories = new[] { "Все категории" }
            .Concat(_products
                .Select(product => string.IsNullOrWhiteSpace(product.Category) ? "Без категории" : product.Category)
                .Distinct()
                .Order())
            .ToList();

        FilterCategoryComboBox.ItemsSource = categories;
        FilterCategoryComboBox.SelectedItem = selected is not null && categories.Contains(selected) ? selected : "Все категории";
    }

    private void ApplyFilters()
    {
        IEnumerable<Product> query = _products;
        var text = FilterTextBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(product =>
                product.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                product.Note.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                product.Category.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        var category = FilterCategoryComboBox.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(category) && category != "Все категории")
        {
            query = category == "Без категории"
                ? query.Where(product => string.IsNullOrWhiteSpace(product.Category))
                : query.Where(product => product.Category == category);
        }

        var expiry = ExpiryFilterComboBox.SelectedItem?.ToString();
        query = expiry switch
        {
            "Свежие" => query.Where(product => GetStatus(product).Kind == ExpiryKind.Fresh),
            "Скоро истекают" => query.Where(product => GetStatus(product).Kind == ExpiryKind.Soon),
            "Просрочены" => query.Where(product => GetStatus(product).Kind == ExpiryKind.Expired),
            "Без срока" => query.Where(product => product.ExpiresAt is null),
            _ => query
        };

        query = SortComboBox.SelectedItem?.ToString() switch
        {
            "По сроку" => query.OrderBy(product => product.ExpiresAt ?? DateTime.MaxValue).ThenBy(product => product.Name),
            "По категории" => query.OrderBy(product => product.Category).ThenBy(product => product.Name),
            "Сначала проблемные" => query.OrderBy(product => GetStatus(product).SortOrder).ThenBy(product => product.ExpiresAt ?? DateTime.MaxValue),
            _ => query.OrderBy(product => product.Name)
        };

        var rows = query.Select(ToRow).ToList();
        ProductsGrid.ItemsSource = rows;
        EmptyStateBorder.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        FilteredCountText.Text = rows.Count == _products.Count
            ? $"Показано продуктов: {rows.Count}"
            : $"Показано: {rows.Count} из {_products.Count}";
    }

    private void UpdateSummary()
    {
        TotalCountText.Text = _products.Count.ToString();
        SoonCountText.Text = _products.Count(product => GetStatus(product).Kind == ExpiryKind.Soon).ToString();
        ExpiredCountText.Text = _products.Count(product => GetStatus(product).Kind == ExpiryKind.Expired).ToString();
        CategoryCountText.Text = _products.Select(product => product.Category).Where(category => !string.IsNullOrWhiteSpace(category)).Distinct().Count().ToString();
    }

    private void RenderInsights()
    {
        RenderExpiryInsight();
        RenderCookInsight();
        RenderShoppingInsight();
        RenderRecipeOfDay();
    }

    private void RenderExpiryInsight()
    {
        var expired = _products.Where(product => GetStatus(product).Kind == ExpiryKind.Expired).ToList();
        var soon = _products.Where(product => GetStatus(product).Kind == ExpiryKind.Soon).ToList();

        if (_products.Count == 0)
        {
            ExpiryInsightText.Text = "У вас пока нет добавленных продуктов.";
            ExpiryInsightItems.ItemsSource = new[] { "Добавьте продукты, чтобы видеть сроки и напоминания." };
            return;
        }

        ExpiryInsightText.Text = expired.Count == 0 && soon.Count == 0
            ? "Все продукты выглядят свежими."
            : $"Скоро истекают: {soon.Count}. Просрочены: {expired.Count}.";

        ExpiryInsightItems.ItemsSource = soon
            .Concat(expired)
            .OrderBy(product => product.ExpiresAt)
            .Take(3)
            .Select(product => $"{product.Name} - {product.ExpiresAt:dd.MM.yyyy}")
            .DefaultIfEmpty("Критичных сроков нет.")
            .ToList();
    }

    private void RenderCookInsight()
    {
        var productNames = _products
            .Select(product => product.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (productNames.Count < 2)
        {
            CookInsightText.Text = "Добавьте больше продуктов, и мы подберём рецепты.";
            CookInsightItems.ItemsSource = new[] { "Минимум 2 продукта для полезной подсказки." };
            return;
        }

        var recipes = Database.SearchRecipes(string.Empty, null, productNames);
        CookInsightText.Text = $"Найдено рецептов по вашим продуктам: {recipes.Count}.";
        CookInsightItems.ItemsSource = recipes
            .Take(3)
            .Select(recipe => recipe.Title)
            .DefaultIfEmpty("Пока нет точных совпадений.")
            .ToList();
    }

    private void RenderShoppingInsight()
    {
        var shoppingItems = Database.GetShoppingItems(User.Id);
        var active = shoppingItems.Where(item => !item.IsBought).ToList();

        if (shoppingItems.Count == 0)
        {
            ShoppingInsightText.Text = "Список покупок пока пуст.";
            ShoppingInsightItems.ItemsSource = new[] { "Сформируйте покупки из плана питания или рецептов." };
            return;
        }

        ShoppingInsightText.Text = $"В списке {shoppingItems.Count} позиций, не куплено: {active.Count}.";
        ShoppingInsightItems.ItemsSource = active
            .Take(3)
            .Select(item => $"{item.Name} - {item.Quantity}")
            .DefaultIfEmpty("Все товары отмечены купленными.")
            .ToList();
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

    private ProductRow ToRow(Product product)
    {
        var status = GetStatus(product);
        return new ProductRow(
            product.Id,
            product.Name,
            string.IsNullOrWhiteSpace(product.Quantity) && string.IsNullOrWhiteSpace(product.Unit)
                ? "не указано"
                : $"{product.Quantity} {product.Unit}".Trim(),
            string.IsNullOrWhiteSpace(product.Category) ? "Без категории" : product.Category,
            product.ExpiresAt?.ToString("dd.MM.yyyy") ?? "не указан",
            status.Text,
            status.Brush,
            status.Foreground,
            string.IsNullOrWhiteSpace(product.Note) ? "Без заметки" : product.Note,
            product);
    }

    private static ExpiryStatus GetStatus(Product product)
    {
        if (product.ExpiresAt is null)
        {
            return new ExpiryStatus(ExpiryKind.NoDate, "без срока", 3, BrushFrom("#EEEAE0"), BrushFrom("#5D5A50"));
        }

        var days = (product.ExpiresAt.Value.Date - DateTime.Today).TotalDays;
        if (days < 0)
        {
            return new ExpiryStatus(ExpiryKind.Expired, "просрочен", 0, BrushFrom("#F7E7E4"), BrushFrom("#8C2B21"));
        }

        if (days <= 3)
        {
            return new ExpiryStatus(ExpiryKind.Soon, "скоро", 1, BrushFrom("#FFF3D5"), BrushFrom("#8A5A00"));
        }

        return new ExpiryStatus(ExpiryKind.Fresh, "свежий", 2, BrushFrom("#E7EFEA"), BrushFrom("#1F614D"));
    }

    private static Brush BrushFrom(string hex)
    {
        return (Brush)new BrushConverter().ConvertFromString(hex)!;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm())
        {
            return;
        }

        Database.AddProduct(User.Id, NameTextBox.Text, QuantityTextBox.Text, ExpiresDatePicker.SelectedDate, UnitComboBox.Text, CategoryComboBox.Text, NoteTextBox.Text);
        StatusTextBlock.Text = "Продукт добавлен.";
        ClearForm();
        Refresh();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProductId is null)
        {
            StatusTextBlock.Text = "Выберите продукт для редактирования.";
            return;
        }

        if (!ValidateForm())
        {
            return;
        }

        Database.UpdateProduct(User.Id, _selectedProductId.Value, NameTextBox.Text, QuantityTextBox.Text, ExpiresDatePicker.SelectedDate, UnitComboBox.Text, CategoryComboBox.Text, NoteTextBox.Text);
        StatusTextBlock.Text = "Изменения сохранены.";
        Refresh();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProductId is null)
        {
            StatusTextBlock.Text = "Выберите продукт для удаления.";
            return;
        }

        Database.DeleteProduct(User.Id, _selectedProductId.Value);
        StatusTextBlock.Text = "Продукт удалён.";
        ClearForm();
        Refresh();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        StatusTextBlock.Text = "Форма очищена.";
    }

    private bool ValidateForm()
    {
        if (!string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            return true;
        }

        StatusTextBlock.Text = "Введите название продукта.";
        NameTextBox.Focus();
        return false;
    }

    private void ClearForm()
    {
        _selectedProductId = null;
        NameTextBox.Clear();
        QuantityTextBox.Clear();
        UnitComboBox.Text = string.Empty;
        CategoryComboBox.Text = string.Empty;
        NoteTextBox.Clear();
        SetExpiryDate(null);
        ProductsGrid.SelectedItem = null;
    }

    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductRow row)
        {
            return;
        }

        var product = row.Source;
        _selectedProductId = product.Id;
        NameTextBox.Text = product.Name;
        QuantityTextBox.Text = product.Quantity;
        UnitComboBox.Text = product.Unit;
        CategoryComboBox.Text = product.Category;
        NoteTextBox.Text = product.Note;
        SetExpiryDate(product.ExpiresAt);
        StatusTextBlock.Text = "Продукт выбран для редактирования.";
    }

    private void Filter_Changed(object sender, EventArgs e)
    {
        if (ProductsGrid is not null)
        {
            ApplyFilters();
        }
    }

    private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        FilterTextBox.Clear();
        FilterCategoryComboBox.SelectedIndex = 0;
        ExpiryFilterComboBox.SelectedIndex = 0;
        SortComboBox.SelectedIndex = 0;
        ApplyFilters();
    }

    private void FocusAddProductButton_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        NameTextBox.Focus();
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

    private void ExpiryTodayButton_Click(object sender, RoutedEventArgs e) => SetExpiryDate(DateTime.Today);
    private void ExpiryWeekButton_Click(object sender, RoutedEventArgs e) => SetExpiryDate(DateTime.Today.AddDays(7));
    private void ExpiryMonthButton_Click(object sender, RoutedEventArgs e) => SetExpiryDate(DateTime.Today.AddDays(30));
    private void ExpiryClearButton_Click(object sender, RoutedEventArgs e) => SetExpiryDate(null);
    private void ExpiresDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => UpdateExpiryPreview();

    private void SetExpiryDate(DateTime? date)
    {
        ExpiresDatePicker.SelectedDate = date;
        UpdateExpiryPreview();
    }

    private void UpdateExpiryPreview()
    {
        ExpiryPreviewTextBlock.Text = ExpiresDatePicker.SelectedDate.HasValue
            ? $"до {ExpiresDatePicker.SelectedDate.Value:dd.MM.yyyy}"
            : "Срок не указан";
    }

    private enum ExpiryKind { Fresh, Soon, Expired, NoDate }
    private sealed record ExpiryStatus(ExpiryKind Kind, string Text, int SortOrder, Brush Brush, Brush Foreground);
    public sealed record ProductRow(int Id, string Name, string DisplayQuantity, string Category, string ExpiryText, string StatusText, Brush StatusBrush, Brush StatusForeground, string NotePreview, Product Source);
}
