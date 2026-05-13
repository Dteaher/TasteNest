using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class ShoppingListView : UserControl
{
    public ShoppingListView()
    {
        InitializeComponent();
        Refresh();
    }

    private void Refresh() => ShoppingGrid.ItemsSource = Database.GetShoppingItems(User.Id);
    private void GenerateButton_Click(object sender, RoutedEventArgs e) { Database.GenerateShoppingFromFavorites(User.Id); Refresh(); }
    private void ToggleButton_Click(object sender, RoutedEventArgs e) { if (ShoppingGrid.SelectedItem is ShoppingItem item) { Database.ToggleShoppingItem(User.Id, item.Id); Refresh(); } }
    private void DeleteButton_Click(object sender, RoutedEventArgs e) { if (ShoppingGrid.SelectedItem is ShoppingItem item) { Database.DeleteShoppingItem(User.Id, item.Id); Refresh(); } }
    private void ClearButton_Click(object sender, RoutedEventArgs e) { Database.ClearShoppingItems(User.Id); Refresh(); }
}
