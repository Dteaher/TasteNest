using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class SearchView : UserControl
{
    public event Action<int>? OpenRecipeRequested;

    public SearchView()
    {
        InitializeComponent();
        LoadCategories();
        LoadRecipes();
    }

    private void LoadCategories()
    {
        CategoryComboBox.Items.Add(new Category(0, "Все категории"));
        foreach (var category in Database.GetCategories())
        {
            CategoryComboBox.Items.Add(category);
        }
        CategoryComboBox.SelectedIndex = 0;
    }

    private void ShowButton_Click(object sender, RoutedEventArgs e) => LoadRecipes();

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Clear();
        TagsTextBox.Clear();
        CategoryComboBox.SelectedIndex = 0;
        LoadRecipes();
    }

    private void LoadRecipes()
    {
        RecipesPanel.Children.Clear();
        var category = CategoryComboBox.SelectedItem as Category;
        var query = string.Join(' ', new[] { SearchTextBox.Text, TagsTextBox.Text }.Where(text => !string.IsNullOrWhiteSpace(text)));
        var recipes = Database.SearchRecipes(query, category?.Id > 0 ? category.Id : null, Array.Empty<string>());

        if (recipes.Count == 0)
        {
            RecipesPanel.Children.Add(new TextBlock { Text = "Ничего не найдено. Попробуйте изменить фильтры.", Style = (Style)FindResource("MutedText") });
            return;
        }

        foreach (var recipe in recipes)
        {
            var item = new RecipeListItem(recipe);
            item.OpenRequested += id => OpenRecipeRequested?.Invoke(id);
            item.SecondaryRequested += id => Database.ToggleFavorite(User.Id, id);
            RecipesPanel.Children.Add(item);
        }
    }
}
