using System.Windows;
using System.Windows.Controls;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Views;

public partial class OperatorPanelView : UserControl
{
    private const string FilterAll = "Все пользователи";
    private const string FilterActive = "Только активные";
    private const string FilterBlocked = "Только заблокированные";
    private const string FilterNoActivity = "Пользователи без активности";
    private const string FilterProblem = "Проблемные пользователи";

    private readonly string[] _blockReasons =
    {
        "спам",
        "некорректные рецепты",
        "подозрительная активность",
        "нарушение правил",
        "другое"
    };

    private List<OperatorUserRow> _users = new();
    private OperatorUserRow? _selectedUser;

    public OperatorPanelView()
    {
        InitializeComponent();
        FilterComboBox.ItemsSource = new[] { FilterAll, FilterActive, FilterBlocked, FilterNoActivity, FilterProblem };
        FilterComboBox.SelectedIndex = 0;
        ReasonComboBox.ItemsSource = _blockReasons;
        ReasonComboBox.SelectedIndex = 0;
        RefreshData();
    }

    private void RefreshData()
    {
        _users = Database.GetOperatorUsers();
        RenderUsers();
        RenderSummary();
        RenderActionLog();
        UpdateSelectedState(null);
    }

    private void RenderUsers()
    {
        var filter = FilterComboBox.SelectedItem?.ToString() ?? FilterAll;
        IEnumerable<OperatorUserRow> users = _users;

        users = filter switch
        {
            FilterActive => users.Where(user => user.IsActive),
            FilterBlocked => users.Where(user => !user.IsActive),
            FilterNoActivity => users.Where(IsWithoutActivity),
            FilterProblem => users.Where(IsProblemUser),
            _ => users
        };

        UsersGrid.ItemsSource = users.ToList();
    }

    private void RenderSummary()
    {
        TotalUsersText.Text = _users.Count.ToString();
        ActiveUsersText.Text = _users.Count(user => user.IsActive).ToString();
        BlockedUsersText.Text = _users.Count(user => !user.IsActive).ToString();
        ProblemUsersText.Text = _users.Count(IsProblemUser).ToString();
    }

    private void RenderActionLog()
    {
        ActionLogGrid.ItemsSource = Database.GetOperatorActionLog();
    }

    private static bool IsWithoutActivity(OperatorUserRow user)
    {
        return user.RecipesCount == 0
            && user.ViewsCount == 0
            && user.ProductsCount == 0
            && user.LastLoginAt is null;
    }

    private static bool IsProblemUser(OperatorUserRow user)
    {
        return !user.IsActive || IsWithoutActivity(user) || !string.IsNullOrWhiteSpace(user.Note);
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersGrid is not null)
        {
            RenderUsers();
        }
    }

    private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedState(UsersGrid.SelectedItem as OperatorUserRow);
    }

    private void BlockButton_Click(object sender, RoutedEventArgs e)
    {
        ChangeSelectedUserStatus(false);
    }

    private void UnblockButton_Click(object sender, RoutedEventArgs e)
    {
        ChangeSelectedUserStatus(true);
    }

    private void ChangeSelectedUserStatus(bool isActive)
    {
        if (_selectedUser is null)
        {
            StatusTextBlock.Text = "Выберите пользователя.";
            return;
        }

        var reason = ReasonComboBox.SelectedItem?.ToString() ?? "другое";
        try
        {
            Database.SetUserStatusByOperator(User.Id, _selectedUser.Id, isActive, reason, ActionCommentTextBox.Text);
            StatusTextBlock.Text = isActive
                ? $"Пользователь {_selectedUser.Email} разблокирован."
                : $"Пользователь {_selectedUser.Email} заблокирован.";
            ActionCommentTextBox.Clear();
            RefreshData();
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = exception.Message;
        }
    }

    private void SaveNoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedUser is null)
        {
            StatusTextBlock.Text = "Выберите пользователя.";
            return;
        }

        try
        {
            Database.SaveOperatorNote(User.Id, _selectedUser.Id, NoteTextBox.Text);
            StatusTextBlock.Text = "Служебная заметка сохранена.";
            RefreshData();
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = exception.Message;
        }
    }

    private void LoadActivityButton_Click(object sender, RoutedEventArgs e)
    {
        LoadSelectedUserActivity();
    }

    private void ViewProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedUser is null)
        {
            StatusTextBlock.Text = "Выберите пользователя.";
            return;
        }

        StatusTextBlock.Text =
            $"{_selectedUser.Email}: статус {_selectedUser.StatusText}, зарегистрирован {_selectedUser.CreatedAt:dd.MM.yyyy}, рецепты {_selectedUser.RecipesCount}, просмотры {_selectedUser.ViewsCount}, продукты {_selectedUser.ProductsCount}.";
    }

    private void LoadSelectedUserActivity()
    {
        ActivityListBox.ItemsSource = null;

        if (_selectedUser is null)
        {
            return;
        }

        var activity = Database.GetUserActivityForOperator(_selectedUser.Id)
            .Select(item => $"{item.ViewedAt:dd.MM.yyyy HH:mm} · {item.Title}")
            .ToList();

        ActivityListBox.ItemsSource = activity.Count == 0
            ? new[] { "Истории просмотров пока нет." }
            : activity;
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshData();
        StatusTextBlock.Text = "Панель пользователей обновлена.";
    }

    private void UpdateSelectedState(OperatorUserRow? user)
    {
        _selectedUser = user;
        var hasUser = user is not null;

        SelectedUserNameText.Text = user?.Email ?? "Пользователь не выбран";
        SelectedUserDetailsText.Text = user is null
            ? "Выберите строку в таблице"
            : $"Статус: {(user.IsActive ? "активен" : "заблокирован")} · рецепты: {user.RecipesCount} · просмотры: {user.ViewsCount} · продукты: {user.ProductsCount}";

        NoteTextBox.Text = user?.Note ?? string.Empty;
        BlockButton.IsEnabled = hasUser && user!.IsActive;
        UnblockButton.IsEnabled = hasUser && !user!.IsActive;
        SaveNoteButton.IsEnabled = hasUser;
        ViewProfileButton.IsEnabled = hasUser;
        LoadActivityButton.IsEnabled = hasUser;
        ActivityListBox.ItemsSource = null;

        if (hasUser)
        {
            LoadSelectedUserActivity();
        }
    }
}
