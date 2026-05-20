using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class OperatorPanelView : UserControl
{
    private OperatorQueueItem? _selectedItem;

    public OperatorPanelView()
    {
        InitializeComponent();
        RefreshQueue();
    }

    private void RefreshQueue()
    {
        var users = Database.GetManagedUsers();
        var queue = Database.GetOperatorQueue();

        QueueGrid.ItemsSource = queue;
        TotalUsersText.Text = users.Count(user => user.Role == "User").ToString();
        ActiveUsersText.Text = users.Count(user => user.Role == "User" && user.IsActive).ToString();
        ActionUsersText.Text = queue.Count(item => item.Priority is "Высокий" or "Средний").ToString();
        UpdateSelectedState(null);
    }

    private void QueueGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedState(QueueGrid.SelectedItem as OperatorQueueItem);
    }

    private void ToggleStatus_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is null)
        {
            StatusTextBlock.Text = "Выберите задачу в очереди.";
            return;
        }

        var user = Database.GetManagedUsers().FirstOrDefault(item => item.Id == _selectedItem.UserId);
        if (user is null || user.Role != "User")
        {
            StatusTextBlock.Text = "Оператор может менять статус только обычных пользователей.";
            return;
        }

        try
        {
            Database.SetUserStatus(user.Id, !user.IsActive);
            StatusTextBlock.Text = user.IsActive
                ? $"Пользователь {user.Email} заблокирован."
                : $"Пользователь {user.Email} разблокирован.";
            RefreshQueue();
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = exception.Message;
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshQueue();
        StatusTextBlock.Text = "Очередь обновлена.";
    }

    private void UpdateSelectedState(OperatorQueueItem? item)
    {
        _selectedItem = item;

        if (item is null)
        {
            SelectedUserNameText.Text = "Задача не выбрана";
            SelectedUserRoleText.Text = "Выберите строку очереди";
            ToggleStatusButton.Content = "Выберите пользователя";
            ToggleStatusButton.IsEnabled = false;
            return;
        }

        var user = Database.GetManagedUsers().FirstOrDefault(managedUser => managedUser.Id == item.UserId);
        var status = user?.IsActive == true ? "активен" : "заблокирован";

        SelectedUserNameText.Text = item.Email;
        SelectedUserRoleText.Text = $"{item.Issue} · Приоритет: {item.Priority} · Статус: {status}";
        ToggleStatusButton.Content = user?.IsActive == true
            ? "Блокировать пользователя"
            : "Разблокировать пользователя";
        ToggleStatusButton.IsEnabled = item.CanBlock && user is not null && user.Role == "User";
    }
}
