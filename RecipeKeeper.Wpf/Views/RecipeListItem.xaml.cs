using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class RecipeListItem : UserControl
{
    public event Action<int>? OpenRequested;
    public event Action<int>? SecondaryRequested;

    public Recipe Recipe { get; }

    public RecipeListItem(Recipe recipe, string secondaryText = "В избранное")
    {
        InitializeComponent();
        Recipe = recipe;
        TitleTextBlock.Text = recipe.Title;
        DescriptionTextBlock.Text = recipe.Description;
        TimeTextBlock.Text = $"{recipe.CookingTime} мин";
        CategoryTextBlock.Text = recipe.Category;
        SecondaryButton.Content = secondaryText;
        LoadImage(recipe.ImageUrl);
    }

    private void LoadImage(string imageUrl)
    {
        var path = ResolveImagePath(imageUrl);
        if (!File.Exists(path))
        {
            path = ResolveImagePath("Assets/Recipes/recipe-placeholder.jpg");
        }

        if (!File.Exists(path))
        {
            return;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        RecipeImage.Source = image;
    }

    private static string ResolveImagePath(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(imageUrl))
        {
            return imageUrl;
        }

        return Path.Combine(AppContext.BaseDirectory, imageUrl.Replace('/', Path.DirectorySeparatorChar));
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e) => OpenRequested?.Invoke(Recipe.Id);

    private void SecondaryButton_Click(object sender, RoutedEventArgs e) => SecondaryRequested?.Invoke(Recipe.Id);

    private void RecipeListItem_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 660;

        ImageColumn.Width = compact ? new GridLength(166) : new GridLength(252);
        ActionColumn.Width = compact ? new GridLength(0) : new GridLength(210);
        ImageBorder.Width = compact ? 150 : 232;
        ImageBorder.Height = compact ? 104 : 128;

        Grid.SetColumn(ActionPanel, compact ? 0 : 2);
        Grid.SetRow(ActionPanel, compact ? 1 : 0);
        Grid.SetColumnSpan(ActionPanel, compact ? 2 : 1);
        ActionPanel.Orientation = compact ? Orientation.Horizontal : Orientation.Vertical;
        ActionPanel.Width = compact ? double.NaN : 200;
        ActionPanel.HorizontalAlignment = compact ? HorizontalAlignment.Stretch : HorizontalAlignment.Right;

        foreach (var button in ActionPanel.Children.OfType<Button>())
        {
            button.MinWidth = compact ? 0 : 104;
            button.Margin = compact
                ? new Thickness(0, 12, 8, 0)
                : button == SecondaryButton
                    ? new Thickness(0)
                    : new Thickness(0, 0, 0, 8);
        }
    }
}
