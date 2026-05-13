using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class StatisticsView : UserControl
{
    public StatisticsView()
    {
        InitializeComponent();
        CategoryItems.ItemsSource = Database.GetCategoryStats().Select(x => $"{x.Name}: {x.Count}");
        IngredientItems.ItemsSource = Database.GetIngredientStats().Select(x => $"{x.Name}: {x.Count}");
    }
}
