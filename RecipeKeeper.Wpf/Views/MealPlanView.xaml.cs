using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class MealPlanView : UserControl
{
    public event Action<int>? OpenRecipeRequested;
    private readonly List<Recipe> _recipes;
    private bool _isFilteringRecipes;

    public MealPlanView()
    {
        InitializeComponent();
        DayComboBox.ItemsSource = new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье" };
        MealComboBox.ItemsSource = new[] { "Завтрак", "Обед", "Ужин", "Перекус" };
        DayComboBox.SelectedIndex = 0;
        MealComboBox.SelectedIndex = 0;
        _recipes = Database.SearchRecipes("", null, Array.Empty<string>());
        RecipeComboBox.ItemsSource = _recipes;
        RecipeComboBox.SelectedIndex = -1;
        RecipeComboBox.Text = string.Empty;
        RecipeComboBox.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(RecipeSearchTextChanged));
        Refresh();
    }

    private void Refresh() => PlanListBox.ItemsSource = Database.GetMealPlan(User.Id).Select(x => $"{x.DayName}: {x.RecipeTitle}");

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var recipe = ResolveSelectedRecipe();
        if (DayComboBox.SelectedItem is string day && recipe is not null)
        {
            Database.SetMealPlanRecipe(User.Id, $"{day} / {MealComboBox.Text}", recipe.Id);
            Refresh();
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (DayComboBox.SelectedItem is string day)
        {
            Database.DeleteMealPlanRecipe(User.Id, $"{day} / {MealComboBox.Text}");
            Refresh();
        }
    }

    private void ShoppingButton_Click(object sender, RoutedEventArgs e) => Database.GenerateShoppingFromMealPlan(User.Id);

    private void PlanListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var recipe = ResolveSelectedRecipe();
        if (recipe is not null)
        {
            OpenRecipeRequested?.Invoke(recipe.Id);
        }
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
