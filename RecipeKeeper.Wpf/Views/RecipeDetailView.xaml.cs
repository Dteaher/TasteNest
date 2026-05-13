using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class RecipeDetailView : UserControl
{
    private const string FallbackImage = "https://images.unsplash.com/photo-1528207776546-365bb710ee93?auto=format&fit=crop&w=900&q=80";
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
        IngredientsListBox.ItemsSource = Database.GetIngredients(_recipe.Id).Select(x => $"{x.Name} — {x.Amount}");
        FavoriteButton.Content = Database.IsFavorite(User.Id, _recipe.Id) ? "Убрать из избранного" : "Добавить в избранное";
        try
        {
            RecipeImage.Source = new BitmapImage(new Uri(string.IsNullOrWhiteSpace(_recipe.ImageUrl) ? FallbackImage : _recipe.ImageUrl));
        }
        catch
        {
            RecipeImage.Source = null;
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        Database.ToggleFavorite(User.Id, _recipe.Id);
        Render();
    }

    private void CookButton_Click(object sender, RoutedEventArgs e) => Database.IncrementCookCount(_recipe.Id);
    private void BackButton_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();
}
