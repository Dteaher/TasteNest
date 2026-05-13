using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class AdminPanelView : UserControl
{
    private ManagedUser? _selectedUser;

    public AdminPanelView()
    {
        InitializeComponent();
        RoleComboBox.ItemsSource = new[] { "User", "Operator" };
        RefreshUsers();
        RefreshServerStatus();
    }

    private void RefreshUsers()
    {
        var users = Database.GetManagedUsers();
        var summary = Database.GetAdminSystemSummary();
        UsersGrid.ItemsSource = users;

        TotalUsersText.Text = summary.TotalUsers.ToString();
        RolesSummaryText.Text = $"Admin {summary.Admins} · Operator {summary.Operators} · User {summary.Users}";
        ContentSummaryText.Text = $"Рецепты {summary.Recipes} · Продукты {summary.Products} · Избранное {summary.Favorites}";
        BlockedUsersText.Text = summary.BlockedUsers.ToString();
        UpdateSelectedState(null);
    }

    private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedState(UsersGrid.SelectedItem as ManagedUser);
    }

    private void ApplyRole_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedUser is null || RoleComboBox.SelectedItem is not string role)
        {
            StatusTextBlock.Text = "Выберите пользователя и роль.";
            return;
        }

        try
        {
            Database.UpdateUserRole(_selectedUser.Id, role);
            StatusTextBlock.Text = $"Роль пользователя {_selectedUser.Email} изменена на {role}.";
            RefreshUsers();
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = exception.Message;
        }
    }

    private void ToggleStatus_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedUser is null)
        {
            StatusTextBlock.Text = "Выберите пользователя.";
            return;
        }

        try
        {
            Database.SetUserStatus(_selectedUser.Id, !_selectedUser.IsActive);
            StatusTextBlock.Text = _selectedUser.IsActive
                ? $"Пользователь {_selectedUser.Email} заблокирован."
                : $"Пользователь {_selectedUser.Email} разблокирован.";
            RefreshUsers();
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = exception.Message;
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshUsers();
        StatusTextBlock.Text = "Список обновлён.";
    }

    private void RefreshServerStatus_Click(object sender, RoutedEventArgs e)
    {
        RefreshServerStatus();
    }

    private void StartServer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SqlServerServiceManager.StartServer();
            StatusTextBlock.Text = "Запрос на запуск SQL Server отправлен. Подтвердите окно администратора Windows.";
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = $"Не удалось запустить сервер: {exception.Message}";
        }
    }

    private void StopServer_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Если выключить SQL Server, TasteNest потеряет подключение к базе до повторного запуска сервера. Продолжить?",
            "Остановка сервера",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            SqlServerServiceManager.StopServer();
            StatusTextBlock.Text = "Запрос на остановку SQL Server отправлен. Подтвердите окно администратора Windows.";
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = $"Не удалось остановить сервер: {exception.Message}";
        }
    }

    private void RefreshServerStatus()
    {
        ServerStatusText.Text = SqlServerServiceManager.GetStatus();
    }

    private void UpdateSelectedState(ManagedUser? user)
    {
        _selectedUser = user;
        var canEdit = user is not null && user.Role != "Admin";

        SelectedUserNameText.Text = user is null
            ? "Пользователь не выбран"
            : user.Email;

        SelectedUserRoleText.Text = user is null
            ? "Выберите строку в таблице"
            : $"Роль: {user.Role} · Статус: {(user.IsActive ? "активен" : "заблокирован")}";

        RoleComboBox.IsEnabled = canEdit;
        ToggleStatusButton.IsEnabled = canEdit;
        RoleComboBox.SelectedItem = canEdit ? user!.Role : null;
    }
}
