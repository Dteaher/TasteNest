using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class ShoppingListView : UserControl
{
    private readonly List<Recipe> _recipes;
    private bool _isFilteringRecipes;

    public ShoppingListView()
    {
        InitializeComponent();

        _recipes = Database.SearchRecipes("", null, Array.Empty<string>());
        RecipeComboBox.ItemsSource = _recipes;
        RecipeComboBox.SelectedIndex = _recipes.Count > 0 ? 0 : -1;
        RecipeComboBox.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(RecipeSearchTextChanged));

        Refresh();
    }

    private void Refresh()
    {
        var items = Database.GetShoppingItems(User.Id);
        ShoppingGrid.ItemsSource = items;

        var activeCount = items.Count(item => !item.IsBought);
        SummaryTextBlock.Text = items.Count == 0
            ? "Список пуст. Сформируйте его из блюда, плана питания, избранного или добавьте позицию вручную."
            : $"Всего позиций: {items.Count}. Осталось купить: {activeCount}.";
    }

    private void GenerateRecipeButton_Click(object sender, RoutedEventArgs e)
    {
        var recipe = ResolveSelectedRecipe();
        if (recipe is null)
        {
            SetStatus("Выберите блюдо, по которому нужно сформировать список.");
            return;
        }

        Database.GenerateShoppingFromRecipe(User.Id, recipe.Id);
        Refresh();
        SetStatus($"Список сформирован по блюду: {recipe.Title}.");
    }

    private void GenerateMealPlanButton_Click(object sender, RoutedEventArgs e)
    {
        Database.GenerateShoppingFromMealPlan(User.Id);
        Refresh();
        SetStatus("Список сформирован по плану питания.");
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        Database.GenerateShoppingFromFavorites(User.Id);
        Refresh();
        SetStatus("Список сформирован по избранным рецептам.");
    }

    private void AddManualButton_Click(object sender, RoutedEventArgs e)
    {
        var name = ManualNameTextBox.Text.Trim();
        var quantity = ManualQuantityTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            SetStatus("Введите название продукта.");
            return;
        }

        Database.AddShoppingItem(User.Id, name, quantity);
        ManualNameTextBox.Clear();
        ManualQuantityTextBox.Clear();
        Refresh();
        SetStatus("Позиция добавлена в список.");
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ShoppingGrid.SelectedItem is not ShoppingItem item)
        {
            SetStatus("Выберите позицию в списке.");
            return;
        }

        Database.ToggleShoppingItem(User.Id, item.Id);
        Refresh();
        SetStatus(item.IsBought ? "Позиция возвращена в покупки." : "Позиция отмечена купленной.");
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ShoppingGrid.SelectedItem is not ShoppingItem item)
        {
            SetStatus("Выберите позицию для удаления.");
            return;
        }

        Database.DeleteShoppingItem(User.Id, item.Id);
        Refresh();
        SetStatus("Позиция удалена.");
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Database.ClearShoppingItems(User.Id);
        Refresh();
        SetStatus("Список покупок очищен.");
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private void RecipeSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFilteringRecipes)
        {
            return;
        }

        var query = RecipeComboBox.Text.Trim();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _recipes
            : _recipes
                .Where(recipe => recipe.Title.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

        if (filtered.Count == 0 && !string.IsNullOrWhiteSpace(query))
        {
            filtered = _recipes
                .Where(recipe => recipe.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        _isFilteringRecipes = true;
        RecipeComboBox.ItemsSource = filtered;
        RecipeComboBox.Text = query;
        RecipeComboBox.IsDropDownOpen = filtered.Count > 0;

        if (RecipeComboBox.Template.FindName("PART_EditableTextBox", RecipeComboBox) is TextBox textBox)
        {
            textBox.CaretIndex = query.Length;
        }

        _isFilteringRecipes = false;
    }

    private Recipe? ResolveSelectedRecipe()
    {
        if (RecipeComboBox.SelectedItem is Recipe selected)
        {
            return selected;
        }

        var query = RecipeComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        return _recipes.FirstOrDefault(recipe => recipe.Title.Equals(query, StringComparison.CurrentCultureIgnoreCase))
            ?? _recipes.FirstOrDefault(recipe => recipe.Title.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
            ?? _recipes.FirstOrDefault(recipe => recipe.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase));
    }
}
