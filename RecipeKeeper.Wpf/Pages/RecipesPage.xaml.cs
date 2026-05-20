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

    public void OpenSearch(string query)
    {
        Highlight("Search");

        if (WorkspaceContent.Content is SearchView currentSearch)
        {
            currentSearch.ApplyGlobalSearch(query);
            return;
        }

        var search = Wire(new SearchView());
        search.ApplyGlobalSearch(query);
        WorkspaceContent.Content = search;
    }

    public void OpenProfile()
    {
        Highlight("Profile");
        WorkspaceContent.Content = Wire(new ProfileView());
    }

    public void OpenAdminPanel()
    {
        if (User.Role != "Admin")
        {
            return;
        }

        Highlight("AdminPanel");
        WorkspaceContent.Content = new AdminPanelView();
    }

    public void OpenOperatorPanel()
    {
        if (User.Role != "Operator")
        {
            return;
        }

        Highlight("OperatorPanel");
        WorkspaceContent.Content = new OperatorPanelView();
    }

    public void OpenStatistics()
    {
        Highlight("Stats");
        WorkspaceContent.Content = new StatisticsView();
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

    private T Wire<T>(T view) where T : UserControl
    {
        switch (view)
        {
            case SearchView search:
                search.OpenRecipeRequested += OpenRecipe;
                search.AddRecipeRequested += () => Navigate("AddRecipe");
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
        var text = (Brush)FindResource("TextBrush");
        var normal = new SolidColorBrush(Color.FromRgb(253, 251, 247));
        var border = (Brush)FindResource("BorderBrushSoft");

        foreach (var button in _navigationButtons)
        {
            var isActive = button.Tag?.ToString() == section;
            button.Background = isActive ? accent : normal;
            button.BorderBrush = isActive ? accent : border;
            button.Foreground = isActive ? Brushes.White : text;
        }
    }

    private void NavigationPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var buttonCount = NavigationPanel.Children.OfType<Button>().Count();
        if (buttonCount == 0)
        {
            return;
        }

        NavigationPanel.Rows = e.NewSize.Width < 760 ? 2 : 1;
        NavigationPanel.Columns = NavigationPanel.Rows == 1
            ? buttonCount
            : (int)Math.Ceiling(buttonCount / 2.0);
    }
}
