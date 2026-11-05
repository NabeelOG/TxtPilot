using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Threading;

namespace TxtPilot;

public partial class MainWindow : Window
{
    private string? _selectedFolderPath;
    public MainWindow()
    {
        InitializeComponent();

        _toastTimer.Tick += (sender, _) =>
        {
            RefreshToast.Visibility = Visibility.Collapsed;
            _toastTimer.Stop();
        };
    }

    // in the below like received e is event and sender is what ui element initiated the function call
    private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog()
        {
            Title = "Select a Folder",
            Multiselect = false
        };
        
        var result = dialog.ShowDialog();

        if (result != true) return;

        var newFolderPath = dialog.FolderName;

        if (_selectedFolderPath != newFolderPath)
        {
            FileListView.SelectedItem = null;
            
            EditorTextBox.Clear();
            FileNameTextBox.Clear();
            FileMetadataTextBlock.Text = "";
        }
        
        _selectedFolderPath = dialog.FolderName;

        FolderPathTextBox.Text = _selectedFolderPath;

        LoadTextFiles();
    }

    private void LoadTextFiles()
    {
        if (string.IsNullOrEmpty(_selectedFolderPath))
        {
            return;
        }
        
        var files = Directory.GetFiles(
            _selectedFolderPath, 
            "*.txt", 
            SearchOption.TopDirectoryOnly);

        var fileInfos = files
            .Select(filePath => new FileInfo(filePath))
            .ToList();
        
        FileListView.ItemsSource = fileInfos;
    }

    // in the below like received e is event and sender is what ui element initiated the function call
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadTextFiles();
        
        ShowToast("Explorer refreshed");
    }

    private readonly DispatcherTimer _toastTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };

    private void ShowToast(string message)
    {
        ToastMessage.Text = message;
        
        RefreshToast.Visibility = Visibility.Visible;
        
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileListView.SelectedItem is not FileInfo selectedFile)
        {
            return;
        }
        
        EditorTextBox.Text = File.ReadAllText(selectedFile.FullName);
        FileNameTextBox.Text =  selectedFile.Name;

        FileMetadataTextBlock.Text =
            $"Size: {selectedFile.Length} bytes\n" + $"Created: {selectedFile.CreationTime}\n" + $"Modified: {selectedFile.LastWriteTime}";
    }
}