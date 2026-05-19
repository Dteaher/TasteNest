using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RecipeKeeper.Wpf.Data;

namespace RecipeKeeper.Wpf.Pages;

public partial class AuthorizationPage : Page
{
    private bool _passwordVisible;
    private bool _isRegisterMode;

    public AuthorizationPage()
    {
        InitializeComponent();
        UpdateMode();
        UpdatePrimaryButtonState();
    }

    private string CurrentPassword => _passwordVisible
        ? PasswordTextBox.Text
        : PasswordBox.Password;

    private void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRegisterMode)
        {
            RegisterAndLogin();
            return;
        }

        Login();
    }

    private void Login()
    {
        StatusTextBlock.Text = string.Empty;

        if (!Database.UserExists(EmailTextBox.Text))
        {
            StatusTextBlock.Text = "Такого пользователя нет. Сначала зарегистрируйтесь.";
            UpdatePrimaryButtonState();
            return;
        }

        var user = Database.Login(EmailTextBox.Text, CurrentPassword);
        if (user is null)
        {
            StatusTextBlock.Text = "Пароль неверный или аккаунт отключён.";
            return;
        }

        OpenRecipesPage(user.Value.Id, user.Value.Email, user.Value.Role);
    }

    private void RegisterAndLogin()
    {
        StatusTextBlock.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
        {
            StatusTextBlock.Text = "Введите логин или email.";
            return;
        }

        if (CurrentPassword.Length < 4)
        {
            StatusTextBlock.Text = "Пароль должен быть минимум 4 символа.";
            return;
        }

        if (CurrentPassword != ConfirmPasswordBox.Password)
        {
            StatusTextBlock.Text = "Пароли не совпадают.";
            return;
        }

        if (!Database.Register(EmailTextBox.Text, CurrentPassword))
        {
            StatusTextBlock.Text = "Такой пользователь уже зарегистрирован. Используйте вход.";
            UpdatePrimaryButtonState();
            return;
        }

        var user = Database.Login(EmailTextBox.Text, CurrentPassword);
        if (user is not null)
        {
            OpenRecipesPage(user.Value.Id, user.Value.Email, user.Value.Role);
        }
    }

    private void ModeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isRegisterMode = !_isRegisterMode;
        StatusTextBlock.Text = string.Empty;
        UpdateMode();
        UpdatePrimaryButtonState();
    }

    private void UpdateMode()
    {
        TitleTextBlock.Text = _isRegisterMode ? "Регистрация в TasteNest" : "Вход в TasteNest";
        PrimaryActionButton.Content = _isRegisterMode ? "Зарегистрироваться" : "Войти";
        ConfirmPasswordPanel.Visibility = _isRegisterMode ? Visibility.Visible : Visibility.Collapsed;
        ModePromptTextBlock.Text = _isRegisterMode ? "Уже есть аккаунт?" : "Нет аккаунта?";
        ModeToggleButton.Content = _isRegisterMode ? "Войти" : "Зарегистрироваться";
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

        UpdatePrimaryButtonState();
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;

        if (PrimaryActionButton.IsEnabled)
        {
            PrimaryActionButton_Click(sender, e);
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

        UpdatePrimaryButtonState();
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

    private void UpdatePrimaryButtonState()
    {
        var login = EmailTextBox.Text.Trim();
        var loginFilled = login.Length > 0;
        var passwordFilled = !string.IsNullOrWhiteSpace(CurrentPassword);
        var userExists = loginFilled && Database.UserExists(login);

        if (_isRegisterMode)
        {
            var passwordIsValid = CurrentPassword.Length >= 4;
            var passwordsMatch = CurrentPassword == ConfirmPasswordBox.Password;
            PrimaryActionButton.IsEnabled = loginFilled && passwordIsValid && passwordsMatch && !userExists;
            LoginHintTextBlock.Text = userExists
                ? "Такой пользователь уже есть. Переключитесь на вход."
                : "Заполните логин, пароль и повтор пароля.";
            return;
        }

        PrimaryActionButton.IsEnabled = userExists && passwordFilled;
        LoginHintTextBlock.Text = userExists
            ? "Пользователь найден, можно войти."
            : "Новый логин? Переключитесь на регистрацию.";
    }
}
