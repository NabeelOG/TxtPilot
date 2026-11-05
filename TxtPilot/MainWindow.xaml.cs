using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Serilog;

namespace TxtPilot;

public partial class MainWindow : Window
{
    private string? _selectedFolderPath;

    private string? _selectedFilePath;

    private bool _isEditMode;
    private bool _hasUnsavedChanges;

    private string? _originalContent;

    private bool _isRestoringSelection;

    private DateTime _originalLastWriteTime;

    public MainWindow()
    {
        InitializeComponent();

        UpdateSelectedFileStatus();
        UpdateEditModeStatus();

        _toastTimer.Tick += (_, _) =>
        {
            RefreshToast.Visibility = Visibility.Collapsed;
            _toastTimer.Stop();
        };
    }

    public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!ConfirmUnsavedChanges())
        {
            e.Cancel = true;
        }
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

        if (_selectedFolderPath == newFolderPath) return;

        if (!ConfirmUnsavedChanges())
        {
            return;
        }

        FileListView.SelectedItem = null;

        //EditorTextBox.Clear();
        //FileNameTextBox.Clear();
        //FileMetadataTextBlock.Text = "";

        ClearEditor();
        
        _selectedFolderPath = newFolderPath;

        Log.ForContext("Operation", "folder")
            .Information("Folder selected: {FolderPath}", _selectedFolderPath);

        FolderPathTextBox.Text = _selectedFolderPath;

        LoadTextFiles();
    }

    private void LoadTextFiles()
    {
        if (string.IsNullOrEmpty(_selectedFolderPath))
        {
            return;
        }

        try
        {
            var files = Directory.GetFiles(
                _selectedFolderPath,
                "*.txt",
                SearchOption.TopDirectoryOnly);

            var fileInfos = files
                .Select(filePath => new FileInfo(filePath))
                .ToList();

            FileListView.ItemsSource = fileInfos;
        }
        catch (DirectoryNotFoundException ex)
        {
            Log.ForContext("Operation", "Folder")
                .Error(ex, "Folder not found while loading files: {FolderPath}", _selectedFolderPath);

            FileListView.ItemsSource = null;
            ClearEditor();

            MessageBox.Show(
                "The selected folder no longer exists.",
                "Folder Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.ForContext("Operation", "Folder")
                .Error(ex, "Access denied while loading files: {FolderPath}", _selectedFolderPath);

            FileListView.ItemsSource = null;

            MessageBox.Show(
                "You don't have permission to read this folder.",
                "Access Denied",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (IOException ex)
        {
            Log.ForContext("Operation", "Folder")
                .Error(ex, "Unable to load files from folder: {FolderPath}", _selectedFolderPath);

            FileListView.ItemsSource = null;

            MessageBox.Show(
                $"Unable to read the folder: {ex.Message}",
                "Folder Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            Log.ForContext("Operation", "Folder")
                .Error(ex, "Unexpected error while loading files: {FolderPath}", _selectedFolderPath);

            FileListView.ItemsSource = null;

            MessageBox.Show(
                $"Unexpected error while reading the folder: {ex.Message}",
                "Unexpected Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
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

    private void ShowToast(string message, bool showIcon = true)
    {
        ToastMessage.Text = message;
        ToastIconTextBlock.Visibility = showIcon
            ? Visibility.Visible
            : Visibility.Collapsed;
        
        RefreshToast.Visibility = Visibility.Visible;
        
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if(_isRestoringSelection)
        {
            return;
        }

        if (FileListView.SelectedItem is not FileInfo selectedFile)
        {
            return;
        }

        if (_selectedFilePath != null && _selectedFilePath != selectedFile.FullName)
        {
            if(!ConfirmUnsavedChanges())
            {
                var previousFile = FileListView.Items
                    .Cast<FileInfo>()
                    .FirstOrDefault(file =>
                        file.FullName == _selectedFilePath);

                _isRestoringSelection = true;
                FileListView.SelectedItem = previousFile;
                _isRestoringSelection = false;

                return;
            }
        }

        try
        {
            // Check whether the file still exists
            if (!File.Exists(selectedFile.FullName))
            {
                
                Log.ForContext("Operation", "File")
                    .Warning("File not found before opening: {FilePath}", selectedFile.FullName);
                
                MessageBox.Show(
                    $"The file '{selectedFile.Name}' no longer exists.\n\nThe Explorer will be refreshed.",
                    "File Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                LoadTextFiles();

                ClearEditor();

                return;
            }

            _selectedFilePath = selectedFile.FullName;

            EditorTextBox.Text = File.ReadAllText(selectedFile.FullName);
            FileNameTextBox.Text = selectedFile.Name;

            _originalContent = EditorTextBox.Text;
            _originalLastWriteTime = new FileInfo(selectedFile.FullName).LastWriteTime;

            _isEditMode = false;
            _hasUnsavedChanges = false;
            UpdateSelectedFileStatus();
            UpdateEditModeStatus();

            EditorTextBox.IsReadOnly = true;

            EditButton.IsEnabled = true;
            SaveButton.IsEnabled = false;
            CancelEditButton.IsEnabled = false;

            UpdateFileMetaData(new FileInfo(selectedFile.FullName));

            Log.ForContext("Operation", "File")
                .Information("File opened: {FileName}", selectedFile.Name);
        }
        catch (FileNotFoundException ex)
        {
            Log.ForContext("Operation", "File")
                .Error(ex, "File not found while opening: {FilePath}", selectedFile.FullName);
            
            MessageBox.Show(
                "This file was deleted or moved outside TxtPilot.",
                "File Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            LoadTextFiles();
            ClearEditor();
        }
        catch (UnauthorizedAccessException ex)
        {
            
            Log.ForContext("Operation", "File")
                .Error(ex, "Access denied while opening file: {FilePath}", selectedFile.FullName);
            
            MessageBox.Show("You don't have permission to access this file.");
        }
        catch (IOException ex)
        {
            
            Log.ForContext("Operation", "File")
                .Error(ex, "Unable to open file: {FilePath}", selectedFile.FullName);

            MessageBox.Show($"Unable to open the file: {ex.Message}");
        }
        catch (Exception ex)
        {
            
            Log.ForContext("Operation", "File")
                .Error(ex, "Unexpected error while opening file: {FilePath}", selectedFile.FullName);

            MessageBox.Show($"Unexpected error: {ex.Message}");
        }
    }

    private void ClearEditor()
    {
        _selectedFilePath = null;
        _originalContent = null;
        _hasUnsavedChanges = false;
        _isEditMode = false;
        UpdateEditModeStatus();

        EditorTextBox.Clear();

        FileNameTextBox.Text = "No file selected";
        UpdateSelectedFileStatus();
        FileMetadataTextBlock.Text = "File information will appear here";

        EditorTextBox.IsReadOnly = true;

        EditButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        CancelEditButton.IsEnabled = false;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isEditMode) return;
        if (string.IsNullOrEmpty(_selectedFilePath))
        {
            return;
        }

        _originalContent = EditorTextBox.Text;

        _isEditMode = true;
        _hasUnsavedChanges = false;
        UpdateSelectedFileStatus();
        UpdateEditModeStatus();

        EditorTextBox.IsReadOnly = false;

        EditButton.IsEnabled = false;
        SaveButton.IsEnabled = true;
        CancelEditButton.IsEnabled = true;

        EditorTextBox.Focus();
        EditorTextBox.CaretIndex = EditorTextBox.Text.Length;
    }

    private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isEditMode)
        {
            return;
        }

        _hasUnsavedChanges = EditorTextBox.Text != _originalContent;
        UpdateSelectedFileStatus();
    }

    private void EditorTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_isEditMode || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        ShowEditingDisabledToast();
        e.Handled = true;
    }

    private void EditorTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_isEditMode)
        {
            return;
        }

        if (e.Key is Key.Back or Key.Delete or Key.Enter)
        {
            ShowEditingDisabledToast();
            e.Handled = true;
        }
    }

    private void ShowEditingDisabledToast()
    {
        ShowToast(string.IsNullOrEmpty(_selectedFilePath)
                ? "Select a file from Explorer"
                : "Enable edit mode to type (Ctrl+I)",
            showIcon: false);
    }

    private void CancelEditButton_Click(object sender, RoutedEventArgs e)
    {
        EditorTextBox.Text = _originalContent ?? "";

        ExitEditMode();
    }

    private void ExitEditMode()
    {
        _isEditMode = false;
        _hasUnsavedChanges = false;
        UpdateSelectedFileStatus();
        UpdateEditModeStatus();

        EditorTextBox.IsReadOnly = true;

        this.Focus();

        EditButton.IsEnabled = true;
        SaveButton.IsEnabled = false;
        CancelEditButton.IsEnabled = false;

        _originalContent = EditorTextBox.Text;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFilePath))
        {
            return;
        }

        if (!_hasUnsavedChanges)
        {
            MessageBox.Show("There are no changes to save.");
            ExitEditMode();
            return;
        }
        
        var folderPath = Path.GetDirectoryName(_selectedFilePath);

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            Log.ForContext("Operation", "File")
                .Warning("Folder not found while saving file: {FilePath}", _selectedFilePath);

            MessageBox.Show(
                "The folder containing this file no longer exists.\n\n" +
                "Your changes are still in the editor. Copy them somewhere else before closing.",
                "Folder Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {

            var currentFile = new FileInfo(_selectedFilePath);

            if(currentFile.LastWriteTime != _originalLastWriteTime)
            {

                var dialog = new FileConflictWindow
                {
                    Owner = this
                };

                dialog.ShowDialog();

                if (dialog.Reload)
                {
                    EditorTextBox.Text = File.ReadAllText(_selectedFilePath);

                    var reloadFile = new FileInfo(_selectedFilePath);

                    _originalContent = EditorTextBox.Text;
                    _originalLastWriteTime = reloadFile.LastWriteTime;

                    UpdateFileMetaData(reloadFile);
                    ExitEditMode();

                    ShowToast("File reloaded");

                    return;
                }

                if(!dialog.Overwrite)
                {
                    return;
                }
            }

            // replacin the editor text to file
            File.WriteAllText(_selectedFilePath, EditorTextBox.Text);

            Log.ForContext("Operation", "File")
                .Information("File saved: {FileName}", Path.GetFileName(_selectedFilePath));


            // update the _originalcontent variable with updated data
            _originalContent = EditorTextBox.Text;

            // refresh the file metadata
            var updatedFile = new FileInfo(_selectedFilePath);
            updatedFile.Refresh();

            _originalLastWriteTime = updatedFile.LastWriteTime;

            UpdateFileMetaData(updatedFile);

            ExitEditMode();

            ShowToast("File saved successfully");

        }
        catch (UnauthorizedAccessException ex)
        {
            Log.ForContext("Operation", "File")
                .Error(ex, "Access denied while saving file: {FilePath}", _selectedFilePath);

            MessageBox.Show("You don't have permission to modify this file.");
        }
        catch (DirectoryNotFoundException ex)
        {
            Log.ForContext("Operation", "File")
                .Error(ex, "Folder not found while saving file: {FilePath}", _selectedFilePath);

            MessageBox.Show(
                "The folder containing this file no longer exists.\n\n" +
                "Your changes are still in the editor. Copy them somewhere else before closing.",
                "Folder Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (FileNotFoundException ex)
        {
            Log.ForContext("Operation", "File")
                .Error(ex, "File not found while saving: {FilePath}", _selectedFilePath);

            MessageBox.Show(
                "This file no longer exists.\n\nYour changes are still in the editor.",
                "File Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (IOException ex)
        {
            Log.ForContext("Operation", "File")
                .Error(ex, "Unable to save file: {FilePath}", _selectedFilePath);

            MessageBox.Show($"Unable to save the file: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.ForContext("Operation", "File")
                .Error(ex, "Unexpected error while saving file: {FilePath}", _selectedFilePath);

            MessageBox.Show($"An unexpected error occurred: {ex.Message}");
        }
    }

    private void UpdateFileMetaData(FileInfo file)
    {
        FileMetadataTextBlock.Text =
            $"Size: {file.Length} bytes\n" +
            $"Created: {file.CreationTime}\n" +
            $"Modified: {file.LastWriteTime}";
    }

    private void UpdateEditModeStatus()
    {
        EditModeStatusTextBlock.Text = _isEditMode
            ? "Editing Mode enabled"
            : "View Mode";

        EditModeStatusTextBlock.Foreground = _isEditMode
            ? Brushes.LimeGreen
            : Brushes.Gray;
    }

    private void UpdateSelectedFileStatus()
    {
        if (string.IsNullOrEmpty(_selectedFilePath))
        {
            SelectedFileStatusTextBlock.Text = "No file selected";
            return;
        }

        var fileName = Path.GetFileName(_selectedFilePath);

        SelectedFileStatusTextBlock.Text = _hasUnsavedChanges
            ? $"*{fileName}"
            : fileName;
    }


    private void NewFileButton_Click(object sender, EventArgs e) {
        if (string.IsNullOrEmpty(_selectedFolderPath))
        {
            MessageBox.Show("Please select a folder first");
            return;
        }

        var dialog = new NewFileWindow
        {
            Owner = this
        };

        if(dialog.ShowDialog() != true)
        {
            return;
        }

        string newFilePath = string.Empty;

        try
        {
            string fileName = dialog.FileName;

            if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".txt";
            }

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                fileName != Path.GetFileName(fileName) ||
                fileName.EndsWith('.') ||
                fileName.EndsWith(' '))
            {
                MessageBox.Show("Invalid file name.");
                return;
            }

            newFilePath = Path.Combine(_selectedFolderPath, fileName);
            
            var fileMode = FileMode.CreateNew;

            if (File.Exists(newFilePath))
            {
                var overwriteResult = MessageBox.Show(
                    $"A file names '{fileName}' already exists. Overwrite?",
                    "File Already Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (overwriteResult != MessageBoxResult.Yes)
                {
                    return;
                }

                fileMode = FileMode.Create;
                
                Log.ForContext("Operation", "File")
                    .Warning("Overwrite confirmed for existing file: {FilePath}", newFilePath);
            }

            using (new FileStream(
                newFilePath,
                fileMode,
                FileAccess.Write,
                FileShare.None))
            {
            }

            Log.ForContext("Operation", "File")
                .Information("File created: {FileName}", fileName);

            LoadTextFiles();

            var createdFile = new FileInfo(newFilePath);

            var fileToSelect = FileListView.Items
                .Cast<FileInfo>()
                .FirstOrDefault(file => file.FullName == createdFile.FullName);

            FileListView.SelectedItem = fileToSelect;

            if(fileToSelect != null)
            {
                EditButton_Click(this, new RoutedEventArgs());
            }

            ShowToast("File created successfully");

        }

        catch (UnauthorizedAccessException ex)
        {
            Log.ForContext("Operation", "File")
                .Error(ex, "Access denied while creating file: {FilePath}", newFilePath);

            MessageBox.Show("You don't have permission to create the file.");
        }
        catch (IOException ex)
        {
            Log.ForContext("Operation", "File")
                .Error(ex, "Unable to create file: {FilePath}", newFilePath);

            MessageBox.Show($"Unable to create the file: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.ForContext("Operation", "File")
                .Error(ex, "Unexpected error while creating file: {FilePath}", newFilePath);

            MessageBox.Show($"An unexpected error occurred: {ex.Message}");
        }
    }

    private bool ConfirmUnsavedChanges()
    {
        if(!_hasUnsavedChanges)
        {
            return true;
        }

        var resukt = MessageBox.Show(
            "You have unsaved changes, Do you want to save them before closing?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if ( resukt == MessageBoxResult.Yes )
        {
            SaveButton_Click(this, new RoutedEventArgs());

            return !_hasUnsavedChanges;
        }

        if (resukt == MessageBoxResult.No)
        {
            EditorTextBox.Text = _originalContent ?? "";

            ExitEditMode();

            return true;
        }
        return false;
    }


    private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        SaveButton_Click(sender, e);
    }

    private void EditCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        EditButton_Click(sender, e);
    }

    private void CreateCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        NewFileButton_Click(sender, e);
    }
}