namespace RecipeKeeper.Wpf.Data;

public static class User
{
    public static int Id { get; private set; }
    public static string Email { get; private set; } = string.Empty;
    public static string Role { get; private set; } = string.Empty;
    public static bool IsAuthorized => Id > 0;
    public static Action? CurrentChanged { get; set; }

    public static void SignIn(int id, string email, string role)
    {
        Id = id;
        Email = email;
        Role = role;
        CurrentChanged?.Invoke();
    }

    public static void SignOut()
    {
        Id = 0;
        Email = string.Empty;
        Role = string.Empty;
        CurrentChanged?.Invoke();
    }
}
