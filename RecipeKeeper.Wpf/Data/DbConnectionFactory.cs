using Microsoft.Data.SqlClient;

namespace RecipeKeeper.Wpf.Data;

public static class DbConnectionFactory
{
    private static bool _databaseChecked;
    private static string? _resolvedServerName;
    private static SqlServerSettings? _settings;

    public static SqlServerSettings Settings => _settings ??= SqlServerSettings.Load();

    public static string ServerName => _resolvedServerName
        ?? Environment.GetEnvironmentVariable("RECIPEKEEPER_SQL_SERVER")
        ?? Settings.ServerName;

    public static string DatabaseName => Settings.DatabaseName;

    public static string ConnectionString => BuildConnectionString(ServerName, DatabaseName, Settings);

    public static SqlConnection OpenConnection()
    {
        EnsureDatabaseExists();

        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    private static void EnsureDatabaseExists()
    {
        if (_databaseChecked)
        {
            return;
        }

        if (Settings.UseSqlLogin)
        {
            _resolvedServerName = Settings.ServerName;
            _databaseChecked = true;
            return;
        }

        var serverNames = new[]
        {
            Environment.GetEnvironmentVariable("RECIPEKEEPER_SQL_SERVER"),
            Settings.ServerName,
            @"localhost\SQLEXPRESS",
            @".\SQLEXPRESS",
            @"(localdb)\MSSQLLocalDB",
            "localhost"
        }.Where(server => !string.IsNullOrWhiteSpace(server)).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var serverName in serverNames)
        {
            try
            {
                using var masterConnection = new SqlConnection(BuildIntegratedConnectionString(serverName!, "master", Settings.ConnectTimeoutSeconds));
                masterConnection.Open();
                EnsureDatabase(masterConnection, DatabaseName);

                using var databaseConnection = new SqlConnection(BuildIntegratedConnectionString(serverName!, DatabaseName, Settings.ConnectTimeoutSeconds));
                databaseConnection.Open();
                EnsureClientLogin(masterConnection, databaseConnection, Settings);

                _resolvedServerName = serverName;
                _databaseChecked = true;
                return;
            }
            catch (SqlException)
            {
            }
        }

        throw new InvalidOperationException(
            $"Не удалось подключиться к SQL Server. Проверьте TasteNest.settings.json и имя сервера: {Settings.ServerName}.");
    }

    private static void EnsureDatabase(SqlConnection masterConnection, string databaseName)
    {
        using var command = masterConnection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NULL
            BEGIN
                CREATE DATABASE [{databaseName.Replace("]", "]]")}];
            END
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureClientLogin(SqlConnection masterConnection, SqlConnection databaseConnection, SqlServerSettings settings)
    {
        if (!settings.CreateSqlLoginForClients || string.IsNullOrWhiteSpace(settings.SqlUser) || string.IsNullOrWhiteSpace(settings.SqlPassword))
        {
            return;
        }

        using (var loginCommand = masterConnection.CreateCommand())
        {
            loginCommand.CommandText = $"""
                IF SUSER_ID(N'{settings.SqlUser.Replace("'", "''")}') IS NULL
                BEGIN
                    CREATE LOGIN [{settings.SqlUser.Replace("]", "]]")}] WITH PASSWORD = N'{settings.SqlPassword.Replace("'", "''")}', CHECK_POLICY = OFF;
                END
                """;
            loginCommand.ExecuteNonQuery();
        }

        using var userCommand = databaseConnection.CreateCommand();
        userCommand.CommandText = $"""
            IF USER_ID(N'{settings.SqlUser.Replace("'", "''")}') IS NULL
            BEGIN
                CREATE USER [{settings.SqlUser.Replace("]", "]]")}] FOR LOGIN [{settings.SqlUser.Replace("]", "]]")}];
            END

            IF IS_ROLEMEMBER(N'db_datareader', N'{settings.SqlUser.Replace("'", "''")}') <> 1
                ALTER ROLE db_datareader ADD MEMBER [{settings.SqlUser.Replace("]", "]]")}];

            IF IS_ROLEMEMBER(N'db_datawriter', N'{settings.SqlUser.Replace("'", "''")}') <> 1
                ALTER ROLE db_datawriter ADD MEMBER [{settings.SqlUser.Replace("]", "]]")}];
            """;
        userCommand.ExecuteNonQuery();
    }

    private static string BuildConnectionString(string serverName, string databaseName, SqlServerSettings settings)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = serverName,
            InitialCatalog = databaseName,
            Encrypt = settings.Encrypt,
            TrustServerCertificate = settings.TrustServerCertificate,
            ConnectTimeout = settings.ConnectTimeoutSeconds
        };

        if (settings.UseSqlLogin)
        {
            builder.IntegratedSecurity = false;
            builder.UserID = settings.SqlUser;
            builder.Password = settings.SqlPassword;
        }
        else
        {
            builder.IntegratedSecurity = true;
        }

        return builder.ConnectionString;
    }

    private static string BuildIntegratedConnectionString(string serverName, string databaseName, int timeoutSeconds)
    {
        return new SqlConnectionStringBuilder
        {
            DataSource = serverName,
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = timeoutSeconds
        }.ConnectionString;
    }
}
