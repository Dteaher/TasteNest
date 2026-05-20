using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RecipeKeeper.Wpf.Data;
using RecipeKeeper.Wpf.Pages;

namespace RecipeKeeper.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Database.Initialize();
        FrameObject.MainFrame = MainFrame;
        User.CurrentChanged = RefreshUserText;
        TryRestoreSession();
        RefreshUserText();
    }

    private void RefreshUserText()
    {
        var authorized = User.IsAuthorized;

        HeaderSearchContainer.Visibility = authorized
            ? Visibility.Visible
            : Visibility.Collapsed;
        ProfileMenuButton.Visibility = authorized
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!authorized)
        {
            ProfilePopup.IsOpen = false;
            HeaderSearchTextBox.Clear();
            return;
        }

        var displayRole = GetDisplayRole(User.Role);
        var avatarText = GetAvatarText(User.Email);

        UserTextBlock.Text = User.Email;
        UserRoleTextBlock.Text = displayRole;
        HeaderAvatarText.Text = avatarText;
        PopupAvatarText.Text = avatarText;
        PopupUserNameText.Text = User.Email;
        PopupUserRoleText.Text = displayRole;
        AdminPanelButton.Visibility = User.Role == "Admin"
            ? Visibility.Visible
            : Visibility.Collapsed;
        OperatorPanelButton.Visibility = User.Role == "Operator"
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static string GetDisplayRole(string role) => role switch
    {
        "Admin" => "Администратор",
        "Operator" => "Оператор",
        _ => "Пользователь"
    };

    private static string GetAvatarText(string email)
    {
        var trimmed = email.Trim();
        return trimmed.Length == 0
            ? "?"
            : char.ToUpperInvariant(trimmed[0]).ToString();
    }

    private void HeaderSearchButton_Click(object sender, RoutedEventArgs e) => ExecuteHeaderSearch();

    private void HeaderSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!User.IsAuthorized || HeaderSearchContainer.Visibility != Visibility.Visible)
        {
            return;
        }

        if (MainFrame.Content is RecipesPage recipesPage)
        {
            recipesPage.OpenSearch(HeaderSearchTextBox.Text);
        }
    }

    private void HeaderSearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ExecuteHeaderSearch();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            HeaderSearchTextBox.Clear();
            e.Handled = true;
        }
    }

    private void ExecuteHeaderSearch()
    {
        if (!User.IsAuthorized)
        {
            return;
        }

        var query = HeaderSearchTextBox.Text.Trim();
        if (MainFrame.Content is RecipesPage currentRecipesPage)
        {
            currentRecipesPage.OpenSearch(query);
            return;
        }

        var recipesPage = new RecipesPage();
        MainFrame.Navigate(recipesPage);
        recipesPage.OpenSearch(query);
    }

    private void ProfileMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (!User.IsAuthorized)
        {
            return;
        }

        ProfilePopup.IsOpen = !ProfilePopup.IsOpen;
    }

    private void OpenProfileButton_Click(object sender, RoutedEventArgs e)
    {
        ProfilePopup.IsOpen = false;
        OpenRecipesPageSection(page => page.OpenProfile());
    }

    private void StatisticsButton_Click(object sender, RoutedEventArgs e)
    {
        ProfilePopup.IsOpen = false;
        OpenRecipesPageSection(page => page.OpenStatistics());
    }

    private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
    {
        ProfilePopup.IsOpen = false;
        if (User.Role != "Admin")
        {
            return;
        }

        OpenRecipesPageSection(page => page.OpenAdminPanel());
    }

    private void OperatorPanelButton_Click(object sender, RoutedEventArgs e)
    {
        ProfilePopup.IsOpen = false;
        if (User.Role != "Operator")
        {
            return;
        }

        OpenRecipesPageSection(page => page.OpenOperatorPanel());
    }

    private void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        ProfilePopup.IsOpen = false;
        AuthSessionStore.Clear();
        User.SignOut();
        MainFrame.Navigate(new AuthorizationPage());
    }

    private void OpenRecipesPageSection(Action<RecipesPage> openSection)
    {
        if (!User.IsAuthorized)
        {
            return;
        }

        if (MainFrame.Content is RecipesPage currentRecipesPage)
        {
            openSection(currentRecipesPage);
            return;
        }

        var recipesPage = new RecipesPage();
        MainFrame.Navigate(recipesPage);
        openSection(recipesPage);
    }

    private void TryRestoreSession()
    {
        var savedSession = AuthSessionStore.Load();
        if (savedSession is null)
        {
            return;
        }

        var user = Database.GetUserById(savedSession.UserId);
        if (user is null)
        {
            AuthSessionStore.Clear();
            return;
        }

        User.SignIn(user.Value.Id, user.Value.Email, user.Value.Role);
        MainFrame.Navigate(new RecipesPage());
    }
}
