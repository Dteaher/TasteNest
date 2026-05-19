using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class PopularView : UserControl
{
    public event Action<int>? OpenRecipeRequested;

    public PopularView()
    {
        InitializeComponent();
        foreach (var recipe in Database.GetPopularRecipes())
        {
            var item = new RecipeListItem(recipe);
            item.OpenRequested += id => OpenRecipeRequested?.Invoke(id);
            item.SecondaryRequested += id => Database.ToggleFavorite(User.Id, id);
            RecipesPanel.Children.Add(item);
        }
    }
}
