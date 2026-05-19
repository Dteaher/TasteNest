using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RecipeKeeper.Wpf.Data;
using RecipeKeeper.Wpf.Views;

namespace RecipeKeeper.Wpf.Pages;

public partial class RecipesPage : Page
{
    private readonly List<Button> _navigationButtons = new();

    public RecipesPage()
    {
        InitializeComponent();
        AddRoleNavigation();
        _navigationButtons.AddRange(NavigationPanel.Children.OfType<Button>());
        Navigate("Search");
    }

    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string section)
        {
            Navigate(section);
        }
    }

    private void Navigate(string section)
    {
        Highlight(section);
        WorkspaceContent.Content = section switch
        {
            "Search" => Wire(new SearchView()),
            "Products" => Wire(new ProductsView()),
            "AddRecipe" => new AddRecipeView(),
            "Profile" => Wire(new ProfileView()),
            "Favorites" => Wire(new FavoritesView()),
            "MealPlan" => Wire(new MealPlanView()),
            "Shopping" => new ShoppingListView(),
            "Popular" => Wire(new PopularView()),
            "Stats" => new StatisticsView(),
            "AdminPanel" => new AdminPanelView(),
            "OperatorPanel" => new OperatorPanelView(),
            _ => Wire(new SearchView())
        };
    }

    private void AddRoleNavigation()
    {
        if (User.Role == "Admin")
        {
            NavigationPanel.Children.Add(new Button
            {
                Style = (Style)FindResource("NavigationButton"),
                Content = "Админ-панель",
                Tag = "AdminPanel"
            });
        }
        else if (User.Role == "Operator")
        {
            NavigationPanel.Children.Add(new Button
            {
                Style = (Style)FindResource("NavigationButton"),
                Content = "Оператор",
                Tag = "OperatorPanel"
            });
        }

        foreach (var button in NavigationPanel.Children.OfType<Button>())
        {
            button.Click -= NavigationButton_Click;
            button.Click += NavigationButton_Click;
        }
    }

    private T Wire<T>(T view) where T : UserControl
    {
        switch (view)
        {
            case SearchView search:
                search.OpenRecipeRequested += OpenRecipe;
                break;
            case FavoritesView favorites:
                favorites.OpenRecipeRequested += OpenRecipe;
                break;
            case PopularView popular:
                popular.OpenRecipeRequested += OpenRecipe;
                break;
            case MealPlanView mealPlan:
                mealPlan.OpenRecipeRequested += OpenRecipe;
                break;
            case ProductsView products:
                products.NavigateRequested += Navigate;
                products.OpenRecipeRequested += OpenRecipe;
                break;
            case ProfileView profile:
                profile.NavigateRequested += Navigate;
                profile.OpenRecipeRequested += OpenRecipe;
                break;
        }

        return view;
    }

    private void OpenRecipe(int recipeId)
    {
        var window = new RecipeDetailsWindow(recipeId)
        {
            Owner = Window.GetWindow(this)
        };
        window.ShowDialog();
    }

    private void Highlight(string section)
    {
        var accent = (Brush)FindResource("AccentBrush");
        var accentDark = (Brush)FindResource("AccentDarkBrush");
        var text = (Brush)FindResource("TextBrush");
        var normal = new SolidColorBrush(Color.FromRgb(244, 240, 232));
        var border = (Brush)FindResource("BorderBrushSoft");

        foreach (var button in _navigationButtons)
        {
            var isActive = button.Tag?.ToString() == section;
            button.Background = isActive ? Brushes.White : normal;
            button.BorderBrush = isActive ? accent : border;
            button.Foreground = isActive ? accentDark : text;
        }
    }
}
