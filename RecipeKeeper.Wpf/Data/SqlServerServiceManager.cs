using System.Diagnostics;

namespace RecipeKeeper.Wpf.Data;

public static class SqlServerServiceManager
{
    private const string ServiceName = "MSSQL$SQLEXPRESS";

    public static string GetStatus()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"query \"{ServiceName}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return "Неизвестно";
        }

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(3000);

        if (output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
        {
            return "Запущен";
        }

        if (output.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
        {
            return "Остановлен";
        }

        return "Неизвестно";
    }

    public static void StartServer()
    {
        RunElevatedServiceCommand("Start-Service");
    }

    public static void StopServer()
    {
        RunElevatedServiceCommand("Stop-Service");
    }

    private static void RunElevatedServiceCommand(string commandName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{commandName} -Name 'MSSQL$SQLEXPRESS' -ErrorAction Stop\"",
            Verb = "runas",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(startInfo);
    }
}
