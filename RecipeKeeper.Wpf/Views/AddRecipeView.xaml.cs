using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class AddRecipeView : UserControl
{
    public AddRecipeView()
    {
        InitializeComponent();
        CategoryComboBox.ItemsSource = Database.GetCategories();
        CategoryComboBox.SelectedIndex = 0;
        DifficultyComboBox.ItemsSource = new[] { "Лёгкий", "Средний", "Сложный" };
        DifficultyComboBox.SelectedIndex = 0;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || CategoryComboBox.SelectedItem is not Category category)
        {
            StatusTextBlock.Text = "Заполните название и категорию.";
            return;
        }

        if (!int.TryParse(TimeTextBox.Text, out var time) || !int.TryParse(ServingsTextBox.Text, out var servings))
        {
            StatusTextBlock.Text = "Время и порции должны быть числами.";
            return;
        }

        Database.AddRecipeExtended(
            User.Id,
            TitleTextBox.Text,
            DescriptionTextBox.Text,
            InstructionsTextBox.Text,
            time,
            servings,
            category.Id,
            DifficultyComboBox.Text,
            ImageTextBox.Text,
            IngredientsTextBox.Text);

        StatusTextBlock.Text = "Рецепт сохранён.";
        Clear();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e) => Clear();

    private void Clear()
    {
        TitleTextBox.Clear();
        DescriptionTextBox.Clear();
        InstructionsTextBox.Clear();
        ImageTextBox.Clear();
        IngredientsTextBox.Text = string.Empty;
        TimeTextBox.Text = "30";
        ServingsTextBox.Text = "2";
    }
}
