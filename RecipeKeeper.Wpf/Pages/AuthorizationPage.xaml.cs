using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Pages;

public partial class AuthorizationPage : Page
{
    public AuthorizationPage()
    {
        InitializeComponent();
        UpdateLoginButtonState();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = string.Empty;

        if (!Database.UserExists(EmailTextBox.Text))
        {
            StatusTextBlock.Text = "Такого пользователя нет. Сначала нажмите «Регистрация».";
            UpdateLoginButtonState();
            return;
        }

        var user = Database.Login(EmailTextBox.Text, PasswordBox.Password);
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

        if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || PasswordBox.Password.Length < 4)
        {
            StatusTextBlock.Text = "Введите email и пароль минимум 4 символа.";
            return;
        }

        if (!Database.Register(EmailTextBox.Text, PasswordBox.Password))
        {
            StatusTextBlock.Text = "Такой email уже зарегистрирован. Используйте кнопку «Войти».";
            UpdateLoginButtonState();
            return;
        }

        var user = Database.Login(EmailTextBox.Text, PasswordBox.Password);
        if (user is not null)
        {
            OpenRecipesPage(user.Value.Id, user.Value.Email, user.Value.Role);
        }
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
            StatusTextBlock.Text = $"Вход выполнен, но страницу рецептов открыть не удалось: {exception.Message}";
        }
    }

    private void UpdateLoginButtonState()
    {
        var emailFilled = !string.IsNullOrWhiteSpace(EmailTextBox.Text);
        var passwordFilled = !string.IsNullOrWhiteSpace(PasswordBox.Password);
        var userExists = emailFilled && Database.UserExists(EmailTextBox.Text);

        LoginButton.IsEnabled = userExists && passwordFilled;
        LoginHintTextBlock.Text = userExists
            ? "Пользователь найден, можно войти."
            : "Если email новый, используйте регистрацию.";
    }
}
