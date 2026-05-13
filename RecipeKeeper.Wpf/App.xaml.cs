using System.Windows;
using System.Windows.Threading;

namespace RecipeKeeper.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        base.OnStartup(e);
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var message = e.Exception.Message;
        var inner = e.Exception.InnerException;
        while (inner is not null)
        {
            message += $"\n\nПричина: {inner.Message}";
            inner = inner.InnerException;
        }

        MessageBox.Show(
            $"Произошла ошибка:\n{message}",
            "TasteNest",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
