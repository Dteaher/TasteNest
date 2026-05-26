using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RecipeKeeper.Wpf.Data;
using RecipeKeeper.Wpf.Ui;

namespace RecipeKeeper.Wpf.Views;

public partial class SearchView : UserControl
{
    private const string SortNewest = "Сначала новые";
    private const string SortOldest = "Сначала старые";
    private const string SortTitleAsc = "По названию А–Я";
    private const string SortTitleDesc = "По названию Я–А";
    private const string SortFastest = "Сначала быстрые";
    private const string SortSlowest = "Сначала долгие";

    public event Action<int>? OpenRecipeRequested;
    public event Action? AddRecipeRequested;

    private readonly Dictionary<int, string> _ingredientsByRecipeId = new();
    private List<Recipe> _allRecipes = new();
    private bool _compactLayout;
    private bool _isReady;
    private string _globalQuery = string.Empty;
    private RecipeViewMode _viewMode = RecipeViewMode.List;

    public SearchView()
    {
        InitializeComponent();
        SortComboBox.ItemsSource = new[]
        {
            SortNewest,
            SortOldest,
            SortTitleAsc,
            SortTitleDesc,
            SortFastest,
            SortSlowest
        };
        SortComboBox.SelectedIndex = 0;

        LoadCategories();
        LoadRecipeSource();
        _isReady = true;
        RefreshRecipes();
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

    private void LoadRecipeSource()
    {
        _allRecipes = Database.SearchRecipes(string.Empty, null, Array.Empty<string>());
        _ingredientsByRecipeId.Clear();
    }

    private void ShowButton_Click(object sender, RoutedEventArgs e) => RefreshRecipes();

    private void AddRecipeButton_Click(object sender, RoutedEventArgs e) => AddRecipeRequested?.Invoke();

    public void ApplyQuickSearch(string query) => ApplyGlobalSearch(query);

    public void ApplyGlobalSearch(string query)
    {
        _globalQuery = query.Trim();
        RefreshRecipes();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Clear();
        TagsTextBox.Clear();
        CategoryComboBox.SelectedIndex = 0;
        RefreshRecipes();
    }

    private void FilterInput_Changed(object sender, RoutedEventArgs e)
    {
        if (_isReady)
        {
            RefreshRecipes();
        }
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isReady)
        {
            RefreshRecipes();
        }
    }

    private void GridViewButton_Click(object sender, RoutedEventArgs e)
    {
        _viewMode = RecipeViewMode.Grid;
        RefreshRecipes();
    }

    private void ListViewButton_Click(object sender, RoutedEventArgs e)
    {
        _viewMode = RecipeViewMode.List;
        RefreshRecipes();
    }

    private void RefreshRecipes()
    {
        if (!_isReady)
        {
            return;
        }

        RecipesPanel.Children.Clear();
        RecipesGridPanel.Children.Clear();
        UpdateViewModeButtons();

        var category = CategoryComboBox.SelectedItem as Category;
        var nameFilter = Normalize(SearchTextBox.Text);
        var ingredientFilter = Normalize(TagsTextBox.Text);
        var globalQuery = Normalize(_globalQuery);

        var recipes = _allRecipes
            .Where(recipe => MatchesGlobalQuery(recipe, globalQuery))
            .Where(recipe => MatchesNameFilter(recipe, nameFilter))
            .Where(recipe => MatchesCategory(recipe, category))
            .Where(recipe => MatchesIngredientFilter(recipe, ingredientFilter))
            .ToList();

        recipes = ApplySort(recipes).ToList();

        ResultCountTextBlock.Text = recipes.Count == 0
            ? "0 рецептов"
            : $"{recipes.Count} рецептов";

        if (recipes.Count == 0)
        {
            AddEmptyState();
            return;
        }

        foreach (var recipe in recipes)
        {
            AddRecipeItem(recipe);
        }

        UpdateGridItemWidth();
    }

    private bool MatchesGlobalQuery(Recipe recipe, string query)
    {
        if (query.Length == 0)
        {
            return true;
        }

        var searchableText = string.Join(' ', new[]
        {
            recipe.Title,
            recipe.Description,
            recipe.Instructions,
            recipe.Category,
            recipe.Difficulty,
            recipe.CookingTime.ToString(),
            $"{recipe.CookingTime} мин",
            GetIngredientsText(recipe.Id)
        });

        return ContainsAllTokens(searchableText, query);
    }

    private static bool MatchesNameFilter(Recipe recipe, string nameFilter) =>
        nameFilter.Length == 0 || ContainsAllTokens(recipe.Title, nameFilter);

    private static bool MatchesCategory(Recipe recipe, Category? category) =>
        category is null || category.Id == 0 || string.Equals(recipe.Category, category.Name, StringComparison.OrdinalIgnoreCase);

    private bool MatchesIngredientFilter(Recipe recipe, string ingredientFilter) =>
        ingredientFilter.Length == 0 || ContainsAllTokens(GetIngredientsText(recipe.Id), ingredientFilter);

    private static bool ContainsAllTokens(string source, string query)
    {
        var normalizedSource = Normalize(source);
        return query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(normalizedSource.Contains);
    }

    private string GetIngredientsText(int recipeId)
    {
        if (_ingredientsByRecipeId.TryGetValue(recipeId, out var text))
        {
            return text;
        }

        text = string.Join(' ', Database.GetIngredients(recipeId).Select(ingredient => $"{ingredient.Name} {ingredient.Amount}"));
        _ingredientsByRecipeId[recipeId] = text;
        return text;
    }

    private IEnumerable<Recipe> ApplySort(IEnumerable<Recipe> recipes) =>
        SortComboBox.SelectedItem?.ToString() switch
        {
            SortOldest => recipes.OrderBy(recipe => recipe.Id),
            SortTitleAsc => recipes.OrderBy(recipe => recipe.Title),
            SortTitleDesc => recipes.OrderByDescending(recipe => recipe.Title),
            SortFastest => recipes.OrderBy(recipe => recipe.CookingTime).ThenBy(recipe => recipe.Title),
            SortSlowest => recipes.OrderByDescending(recipe => recipe.CookingTime).ThenBy(recipe => recipe.Title),
            _ => recipes.OrderByDescending(recipe => recipe.Id)
        };

    private void AddRecipeItem(Recipe recipe)
    {
        if (_viewMode == RecipeViewMode.Grid)
        {
            RecipesGridPanel.Children.Add(CreateGridRecipeCard(recipe));
            return;
        }

        var item = new RecipeListItem(recipe)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        item.OpenRequested += id => OpenRecipeRequested?.Invoke(id);
        item.SecondaryRequested += id => Database.ToggleFavorite(User.Id, id);
        RecipesPanel.Children.Add(item);
    }

    private Border CreateGridRecipeCard(Recipe recipe)
    {
        var card = new Border
        {
            Style = (Style)FindResource("RecipeCard"),
            Width = GetGridItemWidth(),
            MinHeight = 0,
            Margin = new Thickness(0, 0, 14, 14)
        };

        var content = new StackPanel();

        var imageBorder = new Border
        {
            Height = 150,
            CornerRadius = new CornerRadius(12),
            Background = (Brush)FindResource("SecondaryButtonBrush"),
            ClipToBounds = true,
            Margin = new Thickness(0, 0, 0, 12)
        };
        var image = new Image
        {
            Stretch = Stretch.UniformToFill,
            Source = RecipeImageLoader.Load(recipe.ImageUrl)
        };
        imageBorder.Child = image;
        content.Children.Add(imageBorder);

        content.Children.Add(new TextBlock
        {
            Text = recipe.Title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextBrush"),
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 48
        });

        content.Children.Add(new TextBlock
        {
            Text = recipe.Description,
            Style = (Style)FindResource("MutedText"),
            FontSize = 14,
            Margin = new Thickness(0, 6, 0, 10),
            MaxHeight = 42
        });

        var badges = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        badges.Children.Add(CreateBadge($"{recipe.CookingTime} мин", "SecondaryButtonBrush", "TextBrush"));
        badges.Children.Add(CreateBadge(recipe.Category, "AccentLightBrush", "AccentBrush"));
        content.Children.Add(badges);

        var openButton = new Button
        {
            Content = "Открыть",
            Style = (Style)FindResource("PrimaryButton"),
            MinHeight = 40,
            Margin = new Thickness(0, 0, 0, 8)
        };
        openButton.Click += (_, _) => OpenRecipeRequested?.Invoke(recipe.Id);
        content.Children.Add(openButton);

        var favoriteButton = new Button
        {
            Content = "В избранное",
            Style = (Style)FindResource("SecondaryButton"),
            MinHeight = 40,
            Margin = new Thickness(0)
        };
        favoriteButton.Click += (_, _) => Database.ToggleFavorite(User.Id, recipe.Id);
        content.Children.Add(favoriteButton);

        card.Child = content;
        return card;
    }

    private Border CreateBadge(string text, string backgroundResource, string foregroundResource)
    {
        return new Border
        {
            Style = (Style)FindResource("StatusBadge"),
            Background = (Brush)FindResource(backgroundResource),
            Margin = new Thickness(0, 0, 8, 0),
            Child = new TextBlock
            {
                Text = text,
                Foreground = (Brush)FindResource(foregroundResource),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            }
        };
    }

    private void AddEmptyState()
    {
        var emptyState = new Border
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
                        Text = "Измените название, категорию или ингредиенты.",
                        Style = (Style)FindResource("MutedText"),
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 6, 0, 0)
                    }
                }
            }
        };

        if (_viewMode == RecipeViewMode.Grid)
        {
            RecipesGridPanel.Children.Add(emptyState);
            return;
        }

        RecipesPanel.Children.Add(emptyState);
    }

    private void UpdateViewModeButtons()
    {
        RecipesPanel.Visibility = _viewMode == RecipeViewMode.List
            ? Visibility.Visible
            : Visibility.Collapsed;
        RecipesGridPanel.Visibility = _viewMode == RecipeViewMode.Grid
            ? Visibility.Visible
            : Visibility.Collapsed;

        ListViewButton.Tag = _viewMode == RecipeViewMode.List ? "Active" : null;
        GridViewButton.Tag = _viewMode == RecipeViewMode.Grid ? "Active" : null;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private double GetGridItemWidth()
    {
        var available = ResultsCard.ActualWidth - 64;
        if (available <= 0)
        {
            return 306;
        }

        return Math.Max(280, Math.Min(306, available));
    }

    private void UpdateGridItemWidth()
    {
        if (_viewMode != RecipeViewMode.Grid)
        {
            return;
        }

        var width = GetGridItemWidth();
        RecipesGridPanel.ItemWidth = width + 14;
        foreach (var item in RecipesGridPanel.Children.OfType<Border>())
        {
            item.Width = width;
        }
    }

    private void SearchView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 860;
        if (compact == _compactLayout)
        {
            UpdateGridItemWidth();
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
            Grid.SetColumn(ResultsActionsPanel, 0);
            Grid.SetRow(ResultsActionsPanel, 1);
            ResultsActionsPanel.HorizontalAlignment = HorizontalAlignment.Left;
            UpdateGridItemWidth();
            return;
        }

        FilterColumn.Width = new GridLength(320);
        GapColumn.Width = new GridLength(22);
        ResultsColumn.Width = new GridLength(1, GridUnitType.Star);
        FilterRow.Height = new GridLength(1, GridUnitType.Star);
        GapRow.Height = new GridLength(0);
        ResultsRow.Height = new GridLength(1, GridUnitType.Star);

        Grid.SetColumn(FilterCard, 0);
        Grid.SetRow(FilterCard, 0);
        Grid.SetColumn(ResultsCard, 2);
        Grid.SetRow(ResultsCard, 0);
        Grid.SetColumn(ResultsActionsPanel, 1);
        Grid.SetRow(ResultsActionsPanel, 0);
        ResultsActionsPanel.HorizontalAlignment = HorizontalAlignment.Right;
        UpdateGridItemWidth();
    }

    private enum RecipeViewMode
    {
        List,
        Grid
    }
}
