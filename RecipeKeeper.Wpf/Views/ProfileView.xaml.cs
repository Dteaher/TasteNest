using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class ProfileView : UserControl
{
    public event Action<string>? NavigateRequested;
    public event Action<int>? OpenRecipeRequested;

    public ProfileView()
    {
        InitializeComponent();
        RenderProfile();
        RenderUserRecipes();
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
        ActivityCountText.Text = history.Count == 0
            ? "0 записей"
            : $"{history.Count} записей";
        HistoryListBox.ItemsSource = history.Count == 0
            ? new[] { new RecipeActivity(0, "Вы пока не открывали рецепты", DateTime.Now) }
            : history;
    }

    private void RenderUserRecipes()
    {
        var recipes = Database.GetUserRecipes(User.Id);
        MyRecipesPanel.Children.Clear();

        if (recipes.Count == 0)
        {
            MyRecipesPanel.Children.Add(new TextBlock
            {
                Text = "Вы пока не добавляли свои рецепты.",
                Style = (Style)FindResource("MutedText")
            });
            return;
        }

        foreach (var recipe in recipes)
        {
            MyRecipesPanel.Children.Add(CreateRecipeRow(recipe));
        }
    }

    private Border CreateRecipeRow(UserRecipeSummary recipe)
    {
        var root = new Border
        {
            BorderBrush = (Brush)FindResource("BorderBrushSoft"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10),
            Background = Brushes.White
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();
        info.Children.Add(new TextBlock
        {
            Text = recipe.Title,
            FontWeight = FontWeights.Bold,
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap
        });
        info.Children.Add(new TextBlock
        {
            Text = $"{recipe.Category} · создано {recipe.CreatedAt:dd.MM.yyyy}",
            Style = (Style)FindResource("MutedText"),
            Margin = new Thickness(0, 4, 0, 0)
        });

        if (!string.IsNullOrWhiteSpace(recipe.ModerationComment))
        {
            info.Children.Add(new TextBlock
            {
                Text = recipe.ModerationComment,
                Style = (Style)FindResource("MutedText"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
        }

        var status = new Border
        {
            Style = (Style)FindResource("StatusBadge"),
            Background = GetStatusBrush(recipe.Status),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(14, 0, 0, 0)
        };
        status.Child = new TextBlock
        {
            Text = GetStatusText(recipe.Status),
            FontWeight = FontWeights.SemiBold,
            Foreground = GetStatusForeground(recipe.Status)
        };

        Grid.SetColumn(info, 0);
        Grid.SetColumn(status, 1);
        grid.Children.Add(info);
        grid.Children.Add(status);
        root.Child = grid;

        if (recipe.Status == "Published")
        {
            root.Cursor = System.Windows.Input.Cursors.Hand;
            root.MouseLeftButtonUp += (_, _) => OpenRecipeRequested?.Invoke(recipe.Id);
        }

        return root;
    }

    private static string GetStatusText(string status) => status switch
    {
        "Pending" => "на проверке",
        "Published" => "опубликован",
        "Rejected" => "отклонён",
        _ => status
    };

    private static Brush GetStatusBrush(string status) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(status switch
    {
        "Pending" => "#FFF3D5",
        "Published" => "#E7EFEA",
        "Rejected" => "#F7E7E4",
        _ => "#EEEAE0"
    }));

    private static Brush GetStatusForeground(string status) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(status switch
    {
        "Pending" => "#8A5A00",
        "Published" => "#1F614D",
        "Rejected" => "#8C2B21",
        _ => "#5D5A50"
    }));

    private void InsightNavigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string section)
        {
            NavigateRequested?.Invoke(section);
        }
    }

    private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryListBox.SelectedItem is not RecipeActivity activity)
        {
            return;
        }

        HistoryListBox.SelectedItem = null;

        if (activity.RecipeId > 0)
        {
            OpenRecipeRequested?.Invoke(activity.RecipeId);
        }
    }
}
