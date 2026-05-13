using System.Windows;
using System.Windows.Controls;
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
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e) => OpenRequested?.Invoke(Recipe.Id);

    private void SecondaryButton_Click(object sender, RoutedEventArgs e) => SecondaryRequested?.Invoke(Recipe.Id);
}
