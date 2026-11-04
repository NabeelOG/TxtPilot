using System.Windows;
using Microsoft.Win32;

namespace TxtPilot;

public partial class MainWindow : Window
{
    private string? _selectedFolderPath;
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BrowseFolderButton_Click(object sender, RoutedEventArgs eventArgs)
    {
        var dialog = new OpenFolderDialog()
        {
            Title = "Select a Folder",
            Multiselect = false
        };
        
        var result = dialog.ShowDialog();

        if (result != true) return;
        _selectedFolderPath = dialog.FolderName;

        FolderPathTextBox.Text = _selectedFolderPath;
    }
}