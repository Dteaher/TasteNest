using System.IO;
using System.Text.Json;

namespace RecipeKeeper.Wpf.Data;

public sealed record SavedAuthSession(int UserId, string Email, string Role);

public static class AuthSessionStore
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TasteNest");

    private static readonly string FilePath = Path.Combine(DirectoryPath, "auth-session.json");

    public static void Save(int userId, string email, string role)
    {
        Directory.CreateDirectory(DirectoryPath);
        var session = new SavedAuthSession(userId, email, role);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(session));
    }

    public static SavedAuthSession? Load()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SavedAuthSession>(File.ReadAllText(FilePath));
        }
        catch
        {
            Clear();
            return null;
        }
    }

    public static void Clear()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}
