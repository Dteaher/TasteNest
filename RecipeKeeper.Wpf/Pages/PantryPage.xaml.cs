using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Pages;

public partial class PantryPage : Page
{
    private List<Product> _products = new();

    public PantryPage()
    {
        InitializeComponent();
        if (!User.IsAuthorized)
        {
            FrameObject.MainFrame?.Navigate(new AuthorizationPage());
            return;
        }

        RefreshProducts();
    }

    private void RefreshProducts()
    {
        _products = Database.GetProducts(User.Id);
        ProductsListBox.ItemsSource = _products.Select(product =>
            $"{product.Name}   {product.Quantity}   {(product.ExpiresAt.HasValue ? product.ExpiresAt.Value.ToString("dd.MM.yyyy") : "")}");
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Введите название продукта.", "Мои продукты", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Database.AddProduct(User.Id, NameTextBox.Text, QuantityTextBox.Text, ExpiresDatePicker.SelectedDate);
        NameTextBox.Clear();
        QuantityTextBox.Clear();
        RefreshProducts();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsListBox.SelectedIndex < 0 || ProductsListBox.SelectedIndex >= _products.Count)
        {
            return;
        }

        Database.DeleteProduct(User.Id, _products[ProductsListBox.SelectedIndex].Id);
        RefreshProducts();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => FrameObject.MainFrame?.Navigate(new RecipesPage());
}
