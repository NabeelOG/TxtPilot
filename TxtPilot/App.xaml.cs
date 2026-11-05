using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Serilog;

namespace TxtPilot;

public partial class App : Application
{
    public App()
    {
        string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");

        //creating rhe folder
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} " +
                    "[{Level:u3}] " +
                    "[{Operation}] " +
                    "{Message:lj}{NewLine}{Exception}")
            .CreateLogger();

    }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.ForContext("Operation", "Application")
            .Information("Application started");

        MainWindow = new MainWindow();
        MainWindow.Show();

        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None, true);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.ForContext("Operation", "Application")
            .Information("Application Exited");

        Log.CloseAndFlush();

        base.OnExit(e);
    }
}