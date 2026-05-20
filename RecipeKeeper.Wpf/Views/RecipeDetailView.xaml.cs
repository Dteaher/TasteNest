using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class RecipeDetailView : UserControl
{
    private const string FallbackImage = "Assets/Recipes/recipe-placeholder.jpg";
    private readonly Recipe _recipe;
    public event Action? BackRequested;

    public RecipeDetailView(int recipeId)
    {
        InitializeComponent();
        _recipe = Database.GetRecipeById(recipeId) ?? throw new InvalidOperationException("Рецепт не найден.");
        Database.AddRecipeView(User.Id, recipeId);
        Render();
    }

    private void Render()
    {
        TitleTextBlock.Text = _recipe.Title;
        DescriptionTextBlock.Text = _recipe.Description;
        CategoryTextBlock.Text = $"Категория: {_recipe.Category}";
        TimeTextBlock.Text = $"{_recipe.CookingTime} мин";
        ServingsTextBlock.Text = $"{_recipe.Servings} порции";
        DifficultyTextBlock.Text = _recipe.Difficulty;
        InstructionsTextBlock.Text = _recipe.Instructions;
        IngredientsListBox.ItemsSource = Database.GetIngredients(_recipe.Id).Select(x => $"{x.Name} - {x.Amount}");
        FavoriteButton.Content = Database.IsFavorite(User.Id, _recipe.Id) ? "Убрать из избранного" : "Добавить в избранное";
        RecipeImage.Source = LoadImage(_recipe.ImageUrl);
    }

    private static BitmapImage? LoadImage(string imagePath)
    {
        var resolvedPath = ResolveImagePath(string.IsNullOrWhiteSpace(imagePath) ? FallbackImage : imagePath);
        if (resolvedPath is null)
        {
            return null;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = resolvedPath;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return imagePath == FallbackImage ? null : LoadImage(FallbackImage);
        }
    }

    private static Uri? ResolveImagePath(string imagePath)
    {
        if (Uri.TryCreate(imagePath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        var localPath = Path.Combine(AppContext.BaseDirectory, imagePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(localPath)
            ? new Uri(localPath, UriKind.Absolute)
            : null;
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        Database.ToggleFavorite(User.Id, _recipe.Id);
        Render();
    }

    private void CookButton_Click(object sender, RoutedEventArgs e) => Database.IncrementCookCount(User.Id, _recipe.Id);

    private void BackButton_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();
}
