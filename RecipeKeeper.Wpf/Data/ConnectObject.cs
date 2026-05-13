using Microsoft.Data.SqlClient;

namespace RecipeKeeper.Wpf.Data;

/// <summary>
/// Объект подключения по образцу из методички: C# обращается к SQL Server через SqlConnection.
/// </summary>
public sealed class ConnectObject : IDisposable
{
    public SqlConnection? Connection { get; private set; }

    public void Connect()
    {
        Connection?.Dispose();
        Connection = DbConnectionFactory.OpenConnection();
    }

    public void Dispose()
    {
        Connection?.Dispose();
    }
}
