using System.IO;
using System.Windows;
using System.Windows.Controls;
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
}
