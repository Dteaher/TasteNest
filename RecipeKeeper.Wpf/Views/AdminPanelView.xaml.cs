using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class AdminPanelView : UserControl
{
    private ManagedUser? _selectedUser;
    private AdminRecipeItem? _selectedRecipe;

    public AdminPanelView()
    {
        InitializeComponent();
        RoleComboBox.ItemsSource = new[] { "User", "Operator" };
        RefreshUsers();
        RefreshModeration();
        RefreshAdminRecipes();
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
        UpdateSelectedState(null);
    }

    private void RefreshModeration()
    {
        var recipes = Database.GetPendingRecipes();
        PendingRecipesCountText.Text = recipes.Count.ToString();
        PendingRecipesPanel.Children.Clear();

        if (recipes.Count == 0)
        {
            PendingRecipesPanel.Children.Add(new TextBlock
            {
                Text = "Нет рецептов на проверке.",
                Style = (Style)FindResource("MutedText"),
                Margin = new Thickness(0, 8, 0, 0)
            });
            return;
        }

        foreach (var recipe in recipes)
        {
            PendingRecipesPanel.Children.Add(CreateModerationCard(recipe));
        }
    }

    private void RefreshAdminRecipes()
    {
        var recipes = Database.GetAdminRecipes();
        AdminRecipesGrid.ItemsSource = recipes;
        _selectedRecipe = null;
        DeleteSelectedRecipeButton.IsEnabled = false;
    }

    private Border CreateModerationCard(RecipeModerationItem recipe)
    {
        var root = new Border
        {
            BorderBrush = (Brush)FindResource("BorderBrushSoft"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12),
            Background = Brushes.White
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();
        info.Children.Add(new TextBlock
        {
            Text = recipe.Title,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        info.Children.Add(new TextBlock
        {
            Text = $"{recipe.Category} · автор: {recipe.AuthorEmail} · {recipe.CreatedAt:dd.MM.yyyy HH:mm}",
            Style = (Style)FindResource("MutedText"),
            Margin = new Thickness(0, 4, 0, 6),
            TextWrapping = TextWrapping.Wrap
        });
        info.Children.Add(new TextBlock
        {
            Text = recipe.Description,
            TextWrapping = TextWrapping.Wrap
        });

        var actions = new StackPanel
        {
            Width = 230,
            Margin = new Thickness(18, 0, 0, 0)
        };
        var commentBox = new TextBox
        {
            MinHeight = 70,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            ToolTip = "Комментарий при отклонении"
        };
        var publishButton = new Button
        {
            Content = "Опубликовать",
            Style = (Style)FindResource("PrimaryButton"),
            Tag = recipe
        };
        publishButton.Click += PublishRecipe_Click;

        var rejectButton = new Button
        {
            Content = "Отклонить",
            Style = (Style)FindResource("SecondaryButton"),
            Tag = (recipe, commentBox)
        };
        rejectButton.Click += RejectRecipe_Click;

        actions.Children.Add(commentBox);
        actions.Children.Add(publishButton);
        actions.Children.Add(rejectButton);

        Grid.SetColumn(info, 0);
        Grid.SetColumn(actions, 1);
        grid.Children.Add(info);
        grid.Children.Add(actions);
        root.Child = grid;
        return root;
    }

    private void PublishRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RecipeModerationItem recipe })
        {
            return;
        }

        Database.PublishRecipe(recipe.Id);
        StatusTextBlock.Text = $"Рецепт «{recipe.Title}» опубликован.";
        RefreshUsers();
        RefreshModeration();
        RefreshAdminRecipes();
    }

    private void RejectRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ValueTuple<RecipeModerationItem, TextBox> data })
        {
            return;
        }

        Database.RejectRecipe(data.Item1.Id, data.Item2.Text);
        StatusTextBlock.Text = $"Рецепт «{data.Item1.Title}» отклонён.";
        RefreshUsers();
        RefreshModeration();
        RefreshAdminRecipes();
    }

    private void AdminRecipesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedRecipe = AdminRecipesGrid.SelectedItem as AdminRecipeItem;
        DeleteSelectedRecipeButton.IsEnabled = _selectedRecipe is not null;
    }

    private void DeleteSelectedRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRecipe is null)
        {
            StatusTextBlock.Text = "Выберите рецепт для удаления.";
            return;
        }

        var result = MessageBox.Show(
            $"Удалить рецепт «{_selectedRecipe.Title}»? Вместе с ним будут удалены ингредиенты, просмотры, избранное и записи плана питания.",
            "Удаление рецепта",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        Database.DeleteRecipe(_selectedRecipe.Id);
        StatusTextBlock.Text = $"Рецепт «{_selectedRecipe.Title}» удалён.";
        RefreshUsers();
        RefreshModeration();
        RefreshAdminRecipes();
    }

    private void RefreshRecipesButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshAdminRecipes();
        StatusTextBlock.Text = "Список рецептов обновлён.";
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
        RefreshModeration();
        RefreshAdminRecipes();
        StatusTextBlock.Text = "Данные обновлены.";
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

        SelectedUserNameText.Text = user is null ? "Пользователь не выбран" : user.Email;
        SelectedUserRoleText.Text = user is null
            ? "Выберите строку в таблице"
            : $"Роль: {user.Role} · Статус: {(user.IsActive ? "активен" : "заблокирован")}";

        RoleComboBox.IsEnabled = canEdit;
        ToggleStatusButton.IsEnabled = canEdit;
        RoleComboBox.SelectedItem = canEdit ? user!.Role : null;
    }
}
