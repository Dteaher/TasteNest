using System.IO;
using System.Text.Json;

namespace RecipeKeeper.Wpf.Data;

public sealed class SqlServerSettings
{
    public string ServerName { get; set; } = @"localhost\SQLEXPRESS";
    public string DatabaseName { get; set; } = "RecipeKeeperDb";
    public bool UseSqlLogin { get; set; }
    public string SqlUser { get; set; } = "tastenest_app";
    public string SqlPassword { get; set; } = string.Empty;
    public bool Encrypt { get; set; } = true;
    public bool TrustServerCertificate { get; set; } = true;
    public int ConnectTimeoutSeconds { get; set; } = 5;
    public bool CreateSqlLoginForClients { get; set; } = true;

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "TasteNest.settings.json");

    public static SqlServerSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            var settings = new SqlServerSettings();
            settings.Save();
            return settings;
        }

        try
        {
            return JsonSerializer.Deserialize<SqlServerSettings>(File.ReadAllText(FilePath), SerializerOptions())
                ?? new SqlServerSettings();
        }
        catch
        {
            var settings = new SqlServerSettings();
            settings.Save();
            return settings;
        }
    }

    public void Save()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, SerializerOptions()));
    }

    private static JsonSerializerOptions SerializerOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }
}
