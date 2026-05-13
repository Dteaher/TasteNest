using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Pages;

public partial class RecipeFormPage : Page
{
    public RecipeFormPage()
    {
        InitializeComponent();
        if (!User.IsAuthorized)
        {
            FrameObject.MainFrame?.Navigate(new AuthorizationPage());
            return;
        }

        CategoryComboBox.ItemsSource = Database.GetCategories();
        CategoryComboBox.SelectedIndex = 0;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || string.IsNullOrWhiteSpace(IngredientsTextBox.Text))
        {
            MessageBox.Show("Заполните название и ингредиенты.", "Рецепт", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CategoryComboBox.SelectedItem is not Category category ||
            !int.TryParse(TimeTextBox.Text, out var time) ||
            !int.TryParse(ServingsTextBox.Text, out var servings))
        {
            MessageBox.Show("Проверьте категорию, время и порции.", "Рецепт", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Database.AddRecipe(User.Id, TitleTextBox.Text, DescriptionTextBox.Text, InstructionsTextBox.Text, time, servings, category.Id, IngredientsTextBox.Text);
        MessageBox.Show("Рецепт сохранен.", "Рецепт", MessageBoxButton.OK, MessageBoxImage.Information);
        FrameObject.MainFrame?.Navigate(new RecipesPage());
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => FrameObject.MainFrame?.Navigate(new RecipesPage());
}
