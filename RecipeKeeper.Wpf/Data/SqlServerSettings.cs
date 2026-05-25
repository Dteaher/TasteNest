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
    public static string ClientFilePath => Path.Combine(AppContext.BaseDirectory, "TasteNest.client.settings.json");

    public static SqlServerSettings Load()
    {
        var clientSettings = TryLoad(ClientFilePath);
        var appSettings = TryLoad(FilePath);

        if (clientSettings is not null && clientSettings.UseSqlLogin && IsDefaultLocalSettings(appSettings))
        {
            return clientSettings;
        }

        if (appSettings is not null)
        {
            return appSettings;
        }

        if (clientSettings is not null)
        {
            return clientSettings;
        }

        if (!File.Exists(FilePath))
        {
            var settings = new SqlServerSettings();
            settings.Save();
            return settings;
        }

        var fallback = new SqlServerSettings();
        fallback.Save();
        return fallback;
    }

    private static SqlServerSettings? TryLoad(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SqlServerSettings>(File.ReadAllText(path), SerializerOptions());
        }
        catch
        {
            return null;
        }
    }

    private static bool IsDefaultLocalSettings(SqlServerSettings? settings)
    {
        return settings is null
            || (!settings.UseSqlLogin
                && string.Equals(settings.ServerName, @"localhost\SQLEXPRESS", StringComparison.OrdinalIgnoreCase)
                && settings.CreateSqlLoginForClients);
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
