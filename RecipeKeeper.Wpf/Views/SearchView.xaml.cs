using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class SearchView : UserControl
{
    public event Action<int>? OpenRecipeRequested;
    private bool _compactLayout;

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

        ResultCountTextBlock.Text = recipes.Count == 0
            ? "Подходящих рецептов нет."
            : $"Найдено рецептов: {recipes.Count}";

        if (recipes.Count == 0)
        {
            RecipesPanel.Children.Add(new Border
            {
                Style = (Style)FindResource("EmptyState"),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Рецепты не найдены",
                            FontSize = 20,
                            FontWeight = FontWeights.Bold,
                            TextAlignment = TextAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = "Измените название, категорию или теги.",
                            Style = (Style)FindResource("MutedText"),
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 6, 0, 0)
                        }
                    }
                }
            });
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

    private void SearchView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 860;
        if (compact == _compactLayout)
        {
            return;
        }

        _compactLayout = compact;

        if (compact)
        {
            FilterColumn.Width = new GridLength(1, GridUnitType.Star);
            GapColumn.Width = new GridLength(0);
            ResultsColumn.Width = new GridLength(0);
            FilterRow.Height = GridLength.Auto;
            GapRow.Height = new GridLength(14);
            ResultsRow.Height = new GridLength(1, GridUnitType.Star);

            Grid.SetColumn(FilterCard, 0);
            Grid.SetRow(FilterCard, 0);
            Grid.SetColumn(ResultsCard, 0);
            Grid.SetRow(ResultsCard, 2);
            return;
        }

        FilterColumn.Width = new GridLength(292);
        GapColumn.Width = new GridLength(16);
        ResultsColumn.Width = new GridLength(1, GridUnitType.Star);
        FilterRow.Height = new GridLength(1, GridUnitType.Star);
        GapRow.Height = new GridLength(0);
        ResultsRow.Height = new GridLength(1, GridUnitType.Star);

        Grid.SetColumn(FilterCard, 0);
        Grid.SetRow(FilterCard, 0);
        Grid.SetColumn(ResultsCard, 2);
        Grid.SetRow(ResultsCard, 0);
    }
}
