using System.Configuration;
using System.Data;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace TxtPilot;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow = new MainWindow();
        MainWindow.Show();

        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None, true);
    }
}