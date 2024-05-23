using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SharpBackup.App.Common;
using SharpBackup.App.Windows;

namespace SharpBackup.App;

public partial class MainWindow : Window
{
    private readonly HashSet<DirectoryInfo> _folderList = [];
    private readonly HashSet<FileInfo> _fileList = [];

    private DirectoryInfo? _selectedFolderPath;
    private FileInfo? _selectedFilePath;

    private DirectoryInfo? _selectedBackupPath;
    private readonly TextBox? _backupPathBox;

    private readonly ListBox? _folderListBox, _fileListBox;
    private readonly Label? _folderLocationLabel, _fileLocationLabel;

    private readonly Configuration _config = new();

    public MainWindow()
    {
        InitializeComponent();
        _folderListBox = this.FindControl<ListBox>("FolderListBox");
        _fileListBox = this.FindControl<ListBox>("FileListBox");
        _backupPathBox = this.FindControl<TextBox>("BackupLocationOption");
        _folderLocationLabel = this.FindControl<Label>("FolderLocationLabel");
        _fileLocationLabel = this.FindControl<Label>("FileLocationLabel");

        UpdateLabelVisibility(_folderListBox, _folderLocationLabel);
        UpdateLabelVisibility(_fileListBox, _fileLocationLabel);

        ApplyConfigurations();
    }

    private void ApplyConfigurations()
    {
        _backupPathBox!.Text = _config.GetConfigValueString("backup", "backupFolder") ?? @"C:\Backup";

        string[] folderPaths = _config.GetConfigValueRange("paths", "folderList") ?? [];
        foreach (string path in folderPaths)
        {
            _folderList.Add(new DirectoryInfo(path));
        }
        WindowHelpers.UpdateListBox(_folderListBox, _folderList);

        string[] filePaths = _config.GetConfigValueRange("paths", "fileList") ?? [];
        foreach (string path in filePaths)
        {
            _fileList.Add(new FileInfo(path));
        }
        WindowHelpers.UpdateListBox(_fileListBox, _fileList);
    }

    private async void OnAddFolderClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> folderResult = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Folder to Backup"
            });

        if (folderResult.Count > 0)
        {
            _selectedFolderPath = new DirectoryInfo(folderResult[0].Path.LocalPath);
            _folderList.Add(_selectedFolderPath);
            WindowHelpers.UpdateListBox(_folderListBox, _folderList);
            UpdateLabelVisibility(_folderListBox, _folderLocationLabel);
            _config.SaveToConfigFile("paths", "folderList", _folderList.ToArray());
        }
        else
        {
            _selectedFolderPath = null;
        }
    }

    private async void OnAddFileClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> fileResult = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                Title = "Select File to Backup"
            });

        if (fileResult.Count > 0)
        {
            _selectedFilePath = new FileInfo(fileResult[0].Path.LocalPath);
            _fileList.Add(_selectedFilePath);
            WindowHelpers.UpdateListBox(_fileListBox, _fileList);
            UpdateLabelVisibility(_fileListBox, _fileLocationLabel);
            _config.SaveToConfigFile("paths", "fileList", _fileList.ToArray());
        }
        else
        {
            _selectedFilePath = null;
        }
    }

    private async void SelectBackupLocation(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> backupFolder = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Backup Folder"
            });

        if (backupFolder.Count > 0)
        {
            _selectedBackupPath = new DirectoryInfo(backupFolder[0].Path.LocalPath);
            WindowHelpers.UpdateTextBox(_backupPathBox, _selectedBackupPath.FullName);
            _config.SaveToConfigFile("backup", "backupFolder", _selectedBackupPath.FullName);
        }
        else
        {
            _selectedFolderPath = null;
        }
    }

    private void OnBackupClick(object sender, RoutedEventArgs e)
    {
        DirectoryInfo backupDirectory = new(_config.GetConfigValueString("backup", "backupFolder") ?? @"C:\Backup");

        if (!backupDirectory.Exists)
        {
            backupDirectory.Create();
        }

        try
        {
            foreach (var folder in _folderList)
            {
                string destinationFolderPath = Path.Combine(backupDirectory.FullName, folder.Name);
                if (!Directory.Exists(destinationFolderPath))
                {
                    Directory.CreateDirectory(destinationFolderPath);
                }
                FileUtils.CopyDirectoryContents(folder, destinationFolderPath);
            }

            foreach (FileInfo file in _fileList)
            {
                string destinationFilePath = Path.Combine(backupDirectory.FullName, file.Name);
                file.CopyTo(destinationFilePath, true);
            }

            InfoWindow infoWindow = new()
            {
                InfoText = "Backup completed with no errors."
            };
            infoWindow.Show();
        }
        catch (Exception exc)
        {
            Console.WriteLine("Error during backup: " + exc.Message);
        }
    }

    private static void UpdateLabelVisibility(ListBox? listBox, Label? label)
    {
        if (listBox == null || label == null) return;

        if (listBox.Items.Count == 0)
        {
            label.IsVisible = false;
        }
        else
        {
            label.IsVisible = true;
            ScrollViewer.SetHorizontalScrollBarVisibility(listBox, ScrollBarVisibility.Auto);
        }
    }
}
