using System.Windows;
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
        UserTextBlock.Text = User.IsAuthorized
            ? $"{User.Email} ({User.Role})"
            : "Гость";

        SignOutButton.Visibility = User.IsAuthorized
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        AuthSessionStore.Clear();
        User.SignOut();
        MainFrame.Navigate(new AuthorizationPage());
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
