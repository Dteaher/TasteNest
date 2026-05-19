using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class FavoritesView : UserControl
{
    public event Action<int>? OpenRecipeRequested;

    public FavoritesView()
    {
        InitializeComponent();
        Refresh();
    }

    private void Refresh()
    {
        RecipesPanel.Children.Clear();
        var recipes = Database.GetFavoriteRecipes(User.Id);
        if (recipes.Count == 0)
        {
            RecipesPanel.Children.Add(new TextBlock
            {
                Text = "В избранном пока пусто. Добавьте рецепты из поиска или популярных.",
                Style = (Style)FindResource("MutedText")
            });
            return;
        }

        foreach (var recipe in recipes)
        {
            var item = new RecipeListItem(recipe, "Убрать");
            item.OpenRequested += id => OpenRecipeRequested?.Invoke(id);
            item.SecondaryRequested += id =>
            {
                Database.RemoveFavorite(User.Id, id);
                Refresh();
            };
            RecipesPanel.Children.Add(item);
        }
    }
}
