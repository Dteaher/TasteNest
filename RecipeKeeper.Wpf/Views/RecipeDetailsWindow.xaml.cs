using System.Windows;
using RecipeKeeper.Wpf.Data;
using RecipeKeeper.Wpf.Ui;

namespace RecipeKeeper.Wpf.Views;

public partial class RecipeDetailsWindow : Window
{
    private readonly Recipe _recipe;

    public RecipeDetailsWindow(int recipeId)
    {
        InitializeComponent();
        _recipe = Database.GetRecipeById(recipeId) ?? throw new InvalidOperationException("Рецепт не найден.");
        Database.AddRecipeView(User.Id, recipeId);
        Render();
    }

    private void Render()
    {
        Title = _recipe.Title;
        TitleTextBlock.Text = _recipe.Title;
        DescriptionTextBlock.Text = _recipe.Description;
        CategoryTextBlock.Text = _recipe.Category;
        TimeTextBlock.Text = $"{_recipe.CookingTime} мин";
        ServingsTextBlock.Text = $"{_recipe.Servings} порции";
        DifficultyTextBlock.Text = _recipe.Difficulty;
        InstructionsTextBlock.Text = _recipe.Instructions;
        IngredientsListBox.ItemsSource = Database.GetIngredients(_recipe.Id).Select(x => $"{x.Name} - {x.Amount}");
        FavoriteButton.Content = Database.IsFavorite(User.Id, _recipe.Id) ? "Убрать из избранного" : "Добавить в избранное";
        RecipeImage.Source = RecipeImageLoader.Load(_recipe.ImageUrl);
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        Database.ToggleFavorite(User.Id, _recipe.Id);
        Render();
    }

    private void CookButton_Click(object sender, RoutedEventArgs e) => Database.IncrementCookCount(User.Id, _recipe.Id);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
