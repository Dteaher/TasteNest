using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class MealPlanView : UserControl
{
    public event Action<int>? OpenRecipeRequested;
    private readonly List<Recipe> _recipes;

    public MealPlanView()
    {
        InitializeComponent();
        DayComboBox.ItemsSource = new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье" };
        MealComboBox.ItemsSource = new[] { "Завтрак", "Обед", "Ужин", "Перекус" };
        DayComboBox.SelectedIndex = 0;
        MealComboBox.SelectedIndex = 0;
        _recipes = Database.SearchRecipes("", null, Array.Empty<string>());
        RecipeComboBox.ItemsSource = _recipes;
        RecipeComboBox.SelectedIndex = _recipes.Count > 0 ? 0 : -1;
        Refresh();
    }

    private void Refresh() => PlanListBox.ItemsSource = Database.GetMealPlan(User.Id).Select(x => $"{x.DayName}: {x.RecipeTitle}");

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (DayComboBox.SelectedItem is string day && RecipeComboBox.SelectedItem is Recipe recipe)
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
        if (RecipeComboBox.SelectedItem is Recipe recipe)
        {
            OpenRecipeRequested?.Invoke(recipe.Id);
        }
    }
}
