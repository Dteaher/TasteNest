using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Pages;

public partial class AuthorizationPage : Page
{
    private bool _passwordVisible;

    public AuthorizationPage()
    {
        InitializeComponent();
        UpdateLoginButtonState();
    }

    private string CurrentPassword => _passwordVisible
        ? PasswordTextBox.Text
        : PasswordBox.Password;

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = string.Empty;

        if (!Database.UserExists(EmailTextBox.Text))
        {
            StatusTextBlock.Text = "Такого пользователя нет. Сначала зарегистрируйтесь.";
            UpdateLoginButtonState();
            return;
        }

        var user = Database.Login(EmailTextBox.Text, CurrentPassword);
        if (user is null)
        {
            StatusTextBlock.Text = "Пароль неверный или аккаунт отключен.";
            return;
        }

        OpenRecipesPage(user.Value.Id, user.Value.Email, user.Value.Role);
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || CurrentPassword.Length < 4)
        {
            StatusTextBlock.Text = "Введите логин или email и пароль минимум 4 символа.";
            return;
        }

        if (!Database.Register(EmailTextBox.Text, CurrentPassword))
        {
            StatusTextBlock.Text = "Такой пользователь уже зарегистрирован. Используйте вход.";
            UpdateLoginButtonState();
            return;
        }

        var user = Database.Login(EmailTextBox.Text, CurrentPassword);
        if (user is not null)
        {
            OpenRecipesPage(user.Value.Id, user.Value.Email, user.Value.Role);
        }
    }

    private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;

        if (_passwordVisible)
        {
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordTextBox.Focus();
            PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
        }
        else
        {
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Focus();
        }

        UpdateLoginButtonState();
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;

        if (LoginButton.IsEnabled)
        {
            LoginButton_Click(sender, e);
        }
        else
        {
            RegisterButton_Click(sender, e);
        }
    }

    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        if (_passwordVisible && sender == PasswordTextBox)
        {
            PasswordBox.Password = PasswordTextBox.Text;
        }
        else if (!_passwordVisible && sender == PasswordBox)
        {
            PasswordTextBox.Text = PasswordBox.Password;
        }

        UpdateLoginButtonState();
    }

    private void OpenRecipesPage(int id, string email, string role)
    {
        try
        {
            User.SignIn(id, email, role);

            if (RememberMeCheckBox.IsChecked == true)
            {
                AuthSessionStore.Save(id, email, role);
            }

            FrameObject.MainFrame?.Navigate(new RecipesPage());
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = $"Вход выполнен, но открыть приложение не удалось: {exception.Message}";
        }
    }

    private void UpdateLoginButtonState()
    {
        var loginFilled = !string.IsNullOrWhiteSpace(EmailTextBox.Text);
        var passwordFilled = !string.IsNullOrWhiteSpace(CurrentPassword);
        var userExists = loginFilled && Database.UserExists(EmailTextBox.Text);

        LoginButton.IsEnabled = userExists && passwordFilled;
        LoginHintTextBlock.Text = userExists
            ? "Пользователь найден, можно войти."
            : "Новый логин? Нажмите «Зарегистрироваться».";
    }
}
